use std::collections::HashMap;
use std::result::Result;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::time::{Duration, Instant};
use std::vec;

use cpal::Stream;
use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use tonic::Response;

use crate::audio::audio_proto::NewAudioClipRequest;
use crate::audio::audio_proto::audio_proxy_proto_server::AudioProxyProto;
use crate::audio::audio_proto::{Ack, Empty, PlayRequest, Track};
use crate::audio::audio_proto::{DecodeResponse, PlayheadPos};
use crate::decode;

pub mod audio_proto {
    tonic::include_proto!("audio");
}

use once_cell::sync::Lazy;
use std::sync::{Arc, Mutex, RwLock};

static AUDIO_ENGINE: Lazy<Arc<AudioPlayer>> = Lazy::new(|| Arc::new(AudioPlayer::new()));
static CLIP_CACHES: Lazy<Arc<RwLock<HashMap<String, Vec<f32>>>>> =
    Lazy::new(|| Arc::new(RwLock::new(HashMap::new())));

pub fn get_audio_engine() -> Arc<AudioPlayer> {
    AUDIO_ENGINE.clone()
}

pub struct AudioPlayer {
    pub stream: Mutex<Option<Stream>>,
    pub tracks: Mutex<Vec<Track>>,
    pub samples: Mutex<Arc<Vec<f32>>>, // 解码后所有样本
    pub position: Arc<AtomicUsize>,    // 当前样本索引
    pub sample_rate: Mutex<u32>,       // 采样率（用于时间计算）
    pub start_time: Mutex<Option<Instant>>,
}

impl AudioPlayer {
    pub fn new() -> Self {
        Self {
            stream: Mutex::new(None),
            tracks: Mutex::new(vec![]),
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

    #[deprecated]
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

    pub fn play(&self) {
        let mut samples: Vec<f32> = vec![         /*啥也没有*/           ];
        let lock = self.tracks.lock().unwrap();
        println!("tracks len: {}", lock.len());
        for track in lock.iter() {
            let clips = &track.clips;
            for clip in clips {
                println!("clip id: {}", &clip.id);
                let binding = CLIP_CACHES.read().unwrap();
                let e = &Vec::<f32>::new();
                let sss = binding.get(&clip.id).unwrap_or(e);
                samples.extend(sss.iter().cloned());
            }
        }

        *self.samples.lock().unwrap() = Arc::new(samples);

        println!("[play] 缓存带入终了。");

        self.position.store(0, Ordering::Relaxed);
        *self.start_time.lock().unwrap() = Some(Instant::now());

        self.start_output();
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
        println!("[start_output] samples len: {:?}", samples.len());

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

    pub fn _get_time_seconds(&self) -> f32 {
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
        _request: tonic::Request<Empty>,
    ) -> Result<tonic::Response<Ack>, tonic::Status> {
        Ok(Response::new(Ack {}))
    }

    async fn stop(
        &self,
        _request: tonic::Request<Empty>,
    ) -> Result<tonic::Response<Ack>, tonic::Status> {
        let engine = get_audio_engine();
        engine.clear();
        Ok(Response::new(Ack {}))
    }

    async fn get_playhead_pos(
        &self,
        _request: tonic::Request<Empty>,
    ) -> Result<tonic::Response<PlayheadPos>, tonic::Status> {
        let engine = get_audio_engine();
        let guard = engine.start_time.lock().unwrap();
        let time = guard.map(|s| s.elapsed().as_secs_f32()).unwrap_or(0.0);
        Ok(Response::new(PlayheadPos { time }))
    }

    // async fn update_track(
    //     &self,
    //     request: tonic::Request<Track>,
    // ) -> std::result::Result<tonic::Response<Ack>, tonic::Status> {
    //     let engine: Arc<AudioPlayer> = get_audio_engine();
    //     let track = request.get_ref();
    //     // let id = &request.get_ref().id;
    //     let clips = &request.get_ref().clips;
    //     let mut result: Vec<f32> = vec![];

    //     for clip in clips {
    //         let path = &clip.path;
    //         let (mut s, _c, sr) = decode::auto_decode(path).unwrap();
    //         let id = clip.id;

    //         // TODO: 它不应在此
    //         engine.sample_rate.lock().unwrap().clone_from(&sr);

    //         result.append(&mut s);
    //     }

    //     CLIP_CACHES.write().unwrap().insert(id.to_string(), result);

    //     let mut tracks = engine.tracks.lock().unwrap();

    //     for t in tracks.iter_mut() {
    //         if t.id == *id {
    //             t.clips = track.clips.clone();
    //             t.color = track.color.clone();
    //             return Ok(Response::new(Ack {}));
    //         }
    //     }

    //     tracks.push(track.clone());

    //     Ok(Response::new(Ack {}))
    // }

    async fn handle_new_audio_clip(
        &self,
        request: tonic::Request<NewAudioClipRequest>,
    ) -> Result<tonic::Response<Ack>, tonic::Status> {
        let track = &request.get_ref().track;

        if let Some(clip) = &request.get_ref().clip {
            let id = &clip.id;
            let path = &clip.path;
            let (mut s, _c, sr) = decode::auto_decode(path).unwrap();
            CLIP_CACHES.write().unwrap().insert(id.to_string(), s);

            let engine = get_audio_engine();

            if let Some(track) = track {
                let mut tracks = engine.tracks.lock().unwrap();

                for t in tracks.iter_mut() {
                    if t.id == *track.id {
                        t.clips.push(clip.clone());
                        return Ok(Response::new(Ack {}));
                    }
                }

                tracks.push(track.clone());
            }
        }

        Ok(Response::new(Ack {}))
    }
}

fn render(_req: PlayRequest) -> DecodeResponse {
    // let track = req.tracks.get(0).unwrap();
    // let clip = track.clips.get(0).unwrap();
    // let path = &clip.path;
    // println!("[render](A): {}", path);

    let engine = get_audio_engine().clone();
    engine.clear();
    // let (_samples, channels, sample_rate) = engine.load_and_play(path);
    engine.play();

    DecodeResponse {
        samples: vec![],
        sample_rate: 0,
        channels: 0,
    }

    // 由于波形渲染解码改为在前端处理，这里传回空的samples
    // DecodeResponse {
    //     samples: vec![],
    //     sample_rate: sample_rate,
    //     channels: channels as u32,
    // }
}
