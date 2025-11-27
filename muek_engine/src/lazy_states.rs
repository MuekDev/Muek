use std::{
    collections::HashMap,
    sync::{Arc, Mutex, RwLock, mpsc::Sender},
};

use once_cell::sync::Lazy;

use crate::{audio::{AudioConfig, AudioEngine}, protos::tracks_proto::ClipProto};

pub static EVENT_LOOP_SENDER: Lazy<Arc<Mutex<Option<Sender<String>>>>> =
    Lazy::new(|| Arc::new(Mutex::new(None)));

pub static CLIP_CACHES: Lazy<Arc<RwLock<HashMap<String, Vec<f32>>>>> =
    Lazy::new(|| Arc::new(RwLock::new(HashMap::new())));

pub static AUDIO_ENGINE: Lazy<Arc<Mutex<AudioEngine>>> = Lazy::new(|| {
    let config = AudioConfig::builder()
        .sample_rate(44100)
        .channels(2)
        .buffer_size(512)
        .bpm(120.0)
        .build();
    let engine = AudioEngine::new(&config);
    Arc::new(Mutex::new(engine))
});
