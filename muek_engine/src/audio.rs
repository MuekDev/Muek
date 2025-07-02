use std::result::Result;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::time::{Duration, Instant};
use std::vec;

use cpal::Stream;
use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use tonic::Response;

use crate::audio::audio_proto::audio_proxy_proto_server::AudioProxyProto;
use crate::audio::audio_proto::{Ack, Empty, PlayRequest};
use crate::audio::audio_proto::{DecodeResponse, PlayheadPos};
use crate::decode;

pub mod audio_proto {
    tonic::include_proto!("audio");
}

use once_cell::sync::Lazy;
use std::sync::{Arc, Mutex};

static AUDIO_ENGINE: Lazy<Arc<AudioPlayer>> = Lazy::new(|| Arc::new(AudioPlayer::new()));

pub fn get_audio_engine() -> Arc<AudioPlayer> {
    AUDIO_ENGINE.clone()
}

pub struct AudioPlayer {
    pub stream: Mutex<Option<Stream>>,
    pub samples: Mutex<Arc<Vec<f32>>>, // 解码后所有样本
    pub position: Arc<AtomicUsize>,    // 当前样本索引
    pub sample_rate: Mutex<u32>,       // 采样率（用于时间计算）
    pub start_time: Mutex<Option<Instant>>,
}

impl AudioPlayer {
    pub fn new() -> Self {
        Self {
            stream: Mutex::new(None),
            samples: Mutex::new(Arc::new(vec![])),
            position: Arc::new(AtomicUsize::new(0)),
            sample_rate: Mutex::new(44100),
            start_time: Mutex::new(None),
        }
    }

    pub fn clear(&self) {
        *self.samples.lock().unwrap() = Arc::new(vec![]);
        self.position.store(0, Ordering::Relaxed);
        *self.start_time.lock().unwrap() = None;

        let mut stream_guard = self.stream.lock().unwrap();

        // 停止旧流（让其 drop）
        if let Some(old) = stream_guard.take() {
            drop(old);
        }
    }

    pub fn load_and_play(&self, path: &str) -> (Vec<f32>, usize, u32) {
        let (samples, channels, sample_rate) = decode::auto_decode(path).unwrap();

        self.sample_rate.lock().unwrap().clone_from(&sample_rate);
        *self.samples.lock().unwrap() = Arc::new(samples.clone());
        self.position.store(0, Ordering::Relaxed);

        *self.start_time.lock().unwrap() = Some(Instant::now());

        println!("[load_and_play] 解码完成");

        // 启动 cpal 播放
        self.start_output();

        (samples, channels, sample_rate)
    }

    fn start_output(&self) {
        let host = cpal::default_host();
        let device = host.default_output_device().unwrap();
        let config = device.default_output_config().unwrap();
        println!(
            "[start_output] Output Device Conifg SR-{:?}",
            config.sample_rate()
        );

        let samples = self.samples.lock().unwrap().clone();
        let position = self.position.clone();

        let err_fn = |err| eprintln!("stream error: {}", err);

        let stream = device
            .build_output_stream(
                &config.into(),
                move |output: &mut [f32], _| {
                    let mut pos = position.load(Ordering::Relaxed);
                    for out in output {
                        *out = *samples.get(pos).unwrap_or(&0.0);
                        pos += 1;
                    }
                    position.store(pos, Ordering::Relaxed);
                },
                err_fn,
                None,
            )
            .unwrap();

        let mut stream_guard = self.stream.lock().unwrap();

        // 停止旧流（让其 drop）
        // TODO: 或许可以去除二次drop
        if let Some(old) = stream_guard.take() {
            drop(old);
        }

        stream.play().unwrap();
        *stream_guard = Some(stream);

        tokio::task::spawn_blocking(move || {
            loop {
                std::thread::sleep(Duration::from_secs(1));
            }
        });
    }

    pub fn get_time_seconds(&self) -> f32 {
        // self.position.load(Ordering::Relaxed) as f32 / sample_rate as f32
        let sample_rate = *self.sample_rate.lock().unwrap(); // 先解引用得到 u32
        let pos = self.position.load(Ordering::Relaxed);
        pos as f32 / sample_rate as f32
    }
}

#[derive(Debug, Default)]
pub struct AudioProxy {}

#[tonic::async_trait]
impl AudioProxyProto for AudioProxy {
    async fn play(
        &self,
        request: tonic::Request<PlayRequest>,
    ) -> Result<tonic::Response<DecodeResponse>, tonic::Status> {
        println!("[play](proxy impl) omg it is playing");

        let r = render(request.get_ref().clone());
        Ok(Response::new(r))
    }

    async fn pause(
        &self,
        request: tonic::Request<Empty>,
    ) -> Result<tonic::Response<Ack>, tonic::Status> {
        Ok(Response::new(Ack {}))
    }

    async fn stop(
        &self,
        request: tonic::Request<Empty>,
    ) -> Result<tonic::Response<Ack>, tonic::Status> {
        let engine = get_audio_engine();
        engine.clear();
        Ok(Response::new(Ack {}))
    }

    async fn get_playhead_pos(
        &self,
        request: tonic::Request<Empty>,
    ) -> Result<tonic::Response<PlayheadPos>, tonic::Status> {
        let engine = get_audio_engine();
        let guard = engine.start_time.lock().unwrap();
        let time = guard.map(|s| s.elapsed().as_secs_f32()).unwrap_or(0.0);
        Ok(Response::new(PlayheadPos { time }))
    }
}

fn render(req: PlayRequest) -> DecodeResponse {
    let track = req.tracks.get(0).unwrap();
    let clip = track.clips.get(0).unwrap();
    let path = &clip.path;
    println!("[render](A): {}", path);

    let engine = get_audio_engine().clone();
    engine.clear();
    let (samples, channels, sample_rate) = engine.load_and_play(path);

    DecodeResponse {
        samples: vec![],
        sample_rate: sample_rate,
        channels: channels as u32,
    }
}
