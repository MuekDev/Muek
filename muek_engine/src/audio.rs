use std::cmp::max;
use std::collections::HashMap;
use std::result::Result;
use std::sync::atomic::{AtomicUsize, Ordering};
use std::time::{Duration, Instant};
use std::vec;

use cpal::Stream;
use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use tonic::{Response, Status};

use crate::audio::audio_proto::MoveClipPosRequest;
use crate::audio::audio_proto::NewAudioClipRequest;
use crate::audio::audio_proto::ReDurationRequest;
use crate::audio::audio_proto::audio_proxy_proto_server::AudioProxyProto;
use crate::audio::audio_proto::{Ack, Empty, PlayRequest, Track};
use crate::audio::audio_proto::{DecodeResponse, PlayheadPos};
use crate::decode;

pub mod audio_proto {
    tonic::include_proto!("audio");
}

use once_cell::sync::Lazy;
use rayon::prelude::*;
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
    pub bpm: Mutex<f64>,
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
            bpm: Mutex::new(150.0),
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
    pub fn _load_and_play(&self, path: &str) -> (Vec<f32>, usize, u32) {
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
        // Try to get mutex lock for tracks
        let l = self.tracks.lock();
        if l.is_err() {
            todo!("failed to get mutex lock")
        }
        let lock = l.unwrap();

        // Get bpm
        let b = self.bpm.lock();
        if b.is_err() {
            todo!("failed to get mutex")
        }
        let bpm = *b.unwrap();

        // Get sample rate
        let sr = self.sample_rate.lock();
        if sr.is_err() {
            todo!("failed to get mutex")
        }
        let sample_rate = *sr.unwrap();

        // Join into samples
        println!("[play] tracks len: {}", lock.len());

        // TODO: 实际上这部分的逻辑应当独立，并添加进独立的预混合计算和缓存功能。在采样被导入时预计算混合。
        let mut samples: Vec<Vec<f32>> = vec![]; // empty buffer
        let mut max_length: usize = 0;

        for track in lock.iter() {
            let mut current_track: Vec<f32> = vec![];

            for clip in &track.clips {
                println!("[play] clip id: {}", &clip.id);

                let binding = CLIP_CACHES.read().unwrap();
                let empty = &Vec::<f32>::new();
                let sss = binding.get(&clip.id).unwrap_or(empty);

                let duration_sample = (((clip.duration * 60.0) / bpm) * sample_rate as f64).round() as usize
                    * 4 // 4 拍每小节
                    * 2; // 双通道

                let clip_samples = if sss.len() > duration_sample {
                    &sss[0..duration_sample]
                } else {
                    sss.as_slice()
                };

                let start_sample = (((clip.start_beat * 60.0) / bpm) * sample_rate as f64).round()
                    as usize
                    * 4     // TODO: 改为beats_per_bar变量，与前端同步，目前是4/4拍
                    * 2; // TODO: 双通道需要*2，因为左右会交错填充

                // 判断，防止panic
                if current_track.len() < start_sample {
                    let pad_len = start_sample - current_track.len();
                    current_track.extend(std::iter::repeat(0.0).take(pad_len));
                }

                current_track.extend(clip_samples.into_iter());
            }

            max_length = max(max_length, current_track.len());
            samples.push(current_track);
            println!("[play] processed track: {:?}", track.id);
        }

        println!("[play] tracks loaded ok");

        // mix tracks
        let mixed: Vec<f32> = (0..max_length)
            .into_par_iter()
            .map(|i| {
                let mut sum = 0.0;
                for track in &samples {
                    sum += *track.get(i).unwrap_or(&0.0);
                }
                // sum.clamp(-1.0, 1.0)     // TODO: clamp硬削波了，我们需要找到一个类似其他DAW的钳制方法
                sum
            })
            .collect();

        *self.samples.lock().unwrap() = Arc::new(mixed);

        println!("[play] 缓存带入终了。");

        // Reset play position
        self.position.store(0, Ordering::Relaxed);
        *self.start_time.lock().unwrap() = Some(Instant::now());

        // Start play audio
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

        let from_rate = 44100; // TODO: 由用户设置
        let to_rate = config.sample_rate().0 as usize;
        let samples = self.samples.lock().unwrap().clone();

        let samples = if from_rate != to_rate {
            println!(
                "[start_output] Resampling from {} to {}",
                from_rate, to_rate
            );
            resample_stereo(&samples, from_rate, to_rate)
        } else {
            samples.to_vec()
        };

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
            println!("{:#?}", clip);
            let id = &clip.id;
            let path = &clip.path;
            let (s, _c, _sr) = decode::auto_decode(path).unwrap();
            CLIP_CACHES.write().unwrap().insert(id.to_string(), s);

            let engine = get_audio_engine();

            if let Some(track) = track {
                let mut tracks = engine.tracks.lock().unwrap();

                for t in tracks.iter_mut() {
                    if t.id == *track.id {
                        t.clips.push(clip.clone());
                        // 如果track存在就直接修改并return
                        return Ok(Response::new(Ack {}));
                    }
                }

                // 否则添加新轨道
                tracks.push(track.clone());
            }
        }

        Ok(Response::new(Ack {}))
    }

    async fn re_duration_clip(
        &self,
        request: tonic::Request<ReDurationRequest>,
    ) -> Result<tonic::Response<Ack>, tonic::Status> {
        let r = request.get_ref();

        let req_track = r
            .track
            .as_ref()
            .ok_or_else(|| Status::internal("Cannot find the track"))?;
        let req_clip = r
            .clip
            .as_ref()
            .ok_or_else(|| Status::internal("Cannot find the clip"))?;

        let new_duration = r.new_duration;
        println!("[re_duration_clip] {}", &new_duration);

        let engine = get_audio_engine();
        let mut tracks = engine
            .tracks
            .lock()
            .map_err(|_| Status::internal("Failed to lock audio engine tracks"))?;

        // 查找对应轨道
        if let Some(track) = tracks.iter_mut().find(|t| t.id == req_track.id) {
            // 查找对应片段
            if let Some(clip) = track.clips.iter_mut().find(|c| c.id == req_clip.id) {
                clip.duration = new_duration;
                return Ok(Response::new(Ack {}));
            } else {
                return Err(Status::not_found("Clip not found in track"));
            }
        } else {
            return Err(Status::not_found("Track not found"));
        }
    }

    async fn move_clip(
        &self,
        request: tonic::Request<MoveClipPosRequest>,
    ) -> std::result::Result<tonic::Response<Ack>, tonic::Status> {
        let r = request.get_ref();
        let req_track = r
            .track
            .as_ref()
            .ok_or_else(|| Status::internal("Cannot find the track"))?;
        let req_clip = r
            .clip
            .as_ref()
            .ok_or_else(|| Status::internal("Cannot find the clip"))?;

        println!("[move_clip] {}", req_clip.name);

        let engine = get_audio_engine();
        let mut tracks = engine
            .tracks
            .lock()
            .map_err(|_| Status::internal("Failed to lock audio engine tracks"))?;

        // 查找对应轨道
        if let Some(track) = tracks.iter_mut().find(|t| t.id == req_track.id) {
            // 查找对应片段
            if let Some(clip) = track.clips.iter_mut().find(|c| c.id == req_clip.id) {
                clip.start_beat = req_clip.start_beat;
                return Ok(Response::new(Ack {}));
            } else {
                return Err(Status::not_found("Clip not found in track"));
            }
        } else {
            return Err(Status::not_found("Track not found"));
        }
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

// TODO: 使用rubato
fn resample_stereo(samples: &[f32], from_rate: usize, to_rate: usize) -> Vec<f32> {
    assert!(samples.len() % 2 == 0, "立体声数据长度应为偶数");

    let (left, right): (Vec<f32>, Vec<f32>) = samples
        .chunks(2)
        .map(|chunk| (chunk[0], chunk[1]))
        .unzip();

    let left_resampled = resample_mono(&left, from_rate, to_rate);
    let right_resampled = resample_mono(&right, from_rate, to_rate);

    // 合并为交错立体声
    let mut interleaved = Vec::with_capacity(left_resampled.len() * 2);
    for (l, r) in left_resampled.into_iter().zip(right_resampled) {
        interleaved.push(l);
        interleaved.push(r);
    }

    interleaved
}

fn resample_mono(samples: &[f32], from_rate: usize, to_rate: usize) -> Vec<f32> {
    let ratio = to_rate as f32 / from_rate as f32;
    let new_len = (samples.len() as f32 * ratio).round() as usize;
    let mut resampled = Vec::with_capacity(new_len);

    for i in 0..new_len {
        let pos = i as f32 / ratio;
        let idx = pos.floor() as usize;
        let frac = pos.fract();

        let s0 = samples.get(idx).copied().unwrap_or(0.0);
        let s1 = samples.get(idx + 1).copied().unwrap_or(0.0);
        resampled.push(s0 * (1.0 - frac) + s1 * frac);
    }

    resampled
}
