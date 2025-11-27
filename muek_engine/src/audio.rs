use std::{
    ops::Index,
    sync::{
        atomic::{AtomicBool, AtomicU64, Ordering},
        Arc, Mutex,
    },
    thread,
    time::Instant,
};

use bon::Builder;
use cpal::{
    Stream, traits::{DeviceTrait, HostTrait, StreamTrait}
};

use crate::lazy_states::CLIP_CACHES;

pub struct AudioEngine {
    pub config: AudioConfig,
    pub state: Arc<EngineState>,
    pub stream: Mutex<Option<Stream>>,
    pub rendered_clips: Vec<RenderedClip>,
}

pub struct EngineState {
    pub pos_idx: AtomicU64,
    pub buffer: Mutex<Vec<f32>>,
    pub start_time: Mutex<Option<Instant>>,
    pub is_playing: AtomicBool,
}

#[derive(Clone, Builder)]
pub struct AudioConfig {
    pub sample_rate: u32,
    pub channels: u16,
    pub buffer_size: usize,
    pub bpm: f32,
}

pub struct RenderedClip {
    pub start_sample_idx: u64,
    pub end_sample_idx: u64,
    pub samples: Vec<f32>,
}

impl AudioEngine {
    pub fn new(config: &AudioConfig) -> AudioEngine {
        Self {
            config: config.clone(),
            stream: Mutex::new(None),
            rendered_clips: Vec::new(),
            // buffer: vec![0.0; config.buffer_size],
            state: Arc::new(EngineState {
                pos_idx: AtomicU64::new(0),
                buffer: Mutex::new(Vec::new()),
                start_time: Mutex::new(None),
                is_playing: AtomicBool::new(false),
            }),
        }
    }

    pub fn spawn(&self) {
        let host = cpal::default_host();
        let device = host
            .default_output_device()
            .expect("no output device available");
        let supported_config = device.default_output_config().unwrap();

        println!(
            "[start_output] Output Device Conifg SR-{:?}",
            supported_config.sample_rate()
        );

        let err_fn = |err| eprintln!("stream error: {}", err);

        let config = supported_config.into();

        let state_clone = self.state.clone();

        let stream = device
            .build_output_stream(
                &config,
                move |output: &mut [f32], _| {
                    if state_clone.is_playing.load(Ordering::SeqCst) {
                        let buffer = state_clone.buffer.lock().unwrap();
                        for out in output.iter_mut() {
                            let idx = state_clone.pos_idx.fetch_add(1, Ordering::SeqCst);
                            let s = buffer.get(idx as usize).unwrap_or(&0.0);
                            *out = *s;
                        }
                    } else {
                        for out in output.iter_mut() {
                            *out = 0.0;
                        }
                    }
                },
                err_fn,
                None,
            )
            .unwrap();

        stream.play().unwrap();

        let mut stream_lock = self.stream.lock().unwrap();
        *stream_lock = Some(stream);

        thread::spawn(move || {
            thread::sleep(std::time::Duration::from_secs(1));
        });
    }

    pub fn play(&mut self) {
        self.render();
        self.state.pos_idx.store(0, Ordering::SeqCst);
        *self.state.start_time.lock().unwrap() = Some(Instant::now());
        self.state.is_playing.store(true, Ordering::SeqCst);
    }

    pub fn stop(&self) {
        self.state.is_playing.store(false, Ordering::SeqCst);
        self.state.pos_idx.store(0, Ordering::SeqCst);
        *self.state.start_time.lock().unwrap() = None;
    }

    fn render(&mut self) {
        let total_samples = self
            .rendered_clips
            .iter()
            .map(|c| c.end_sample_idx)
            .max()
            .unwrap_or(0);

        let mut buffer = self.state.buffer.lock().unwrap();
        buffer.clear();
        buffer.resize(total_samples as usize, 0.0);

        for clip in &self.rendered_clips {
            let start = clip.start_sample_idx as usize;
            for (i, sample) in clip.samples.iter().enumerate() {
                let frame_index = start + i;
                if frame_index < buffer.len() {
                    buffer[frame_index] += *sample;
                }
            }
        }
    }

    pub fn get_position_beat(&self) -> f32 {
        let pos_idx = self.state.pos_idx.load(Ordering::SeqCst);
        sample_idx_to_beat(
            pos_idx,
            self.config.bpm,
            self.config.sample_rate,
            4,
            self.config.channels.try_into().unwrap(),
        )
    }
}

pub fn cache_clip_data(id: &str, data: Vec<f32>) {
    let mut cache = CLIP_CACHES.write().unwrap();
    cache.insert(id.to_string(), data);
}

pub fn beat_to_sample_idx(
    beat: f32,
    bpm: f32,
    sample_rate: u32,
    scale_factor: u8,
    channels: u8,
) -> u64 {
    let secs = beat / bpm * 60.0;
    let index = secs * sample_rate as f32 * scale_factor as f32 * channels as f32;

    index as u64 - (index as u64 % 2)
}

pub fn sample_idx_to_beat(
    sample_idx: u64,
    bpm: f32,
    sample_rate: u32,
    scale_factor: u8,
    channels: u8,
) -> f32 {
    let played_secs = sample_idx as f32 / channels as f32 / sample_rate as f32;
    let total_secs = played_secs / scale_factor as f32;
    total_secs * (bpm / 60.0)
}
