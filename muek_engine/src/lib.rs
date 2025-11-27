use std::{
    env,
    ffi::{CString, c_char},
    thread,
};

use winit::{event_loop::EventLoop, platform::windows::EventLoopBuilderExtWindows};

use crate::{
    audio::RenderedClip,
    lazy_states::{AUDIO_ENGINE, CLIP_CACHES, EVENT_LOOP_SENDER},
    muek_event::MuekEvent,
    protos::{
        byte_buffer::ByteBuffer,
        tracks_proto::{ClipProto, TrackProto},
    },
    winit_app::App,
};

mod audio;
mod decode;
mod lazy_states;
mod muek_event;
mod protos;
mod vst_box;
mod winit_app;

#[repr(C)]
pub struct MyClassRepr {
    pub id: i32,
    pub value: *const f32,
    pub len: i32,
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn receive_tracks_proto(ptr: *const TrackProto, len: i32) {
    let tracks_proto = unsafe { std::slice::from_raw_parts(ptr, len as usize) };
    for track in tracks_proto {
        println!("Track: {}", track.track_id);
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn rust_receive_array(ptr: *const MyClassRepr, len: i32) {
    let slice = unsafe { std::slice::from_raw_parts(ptr, len as usize) };
    for item in slice {
        let s = unsafe { std::slice::from_raw_parts(item.value, item.len as usize) };
        println!("id={} value={}", item.id, s.len());
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn my_add(x: i32, y: i32) -> i32 {
    x + y
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn alloc_u8_string() -> *mut ByteBuffer {
    let str = format!("foo bar baz");
    let buf = ByteBuffer::from_vec(str.into_bytes());
    Box::into_raw(Box::new(buf))
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_string_length(ptr: *const ByteBuffer) -> i32 {
    let buf = unsafe { &*ptr };
    buf.len() as i32
}

#[unsafe(no_mangle)]
#[allow(unused_must_use)]
pub unsafe extern "C" fn free_c_string(str: *mut c_char) {
    unsafe { CString::from_raw(str) };
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn init_vst_box() {
    let (tx, rx) = std::sync::mpsc::channel::<String>();

    thread::spawn(move || {
        let mut app = App::default();

        let event_loop = EventLoop::<MuekEvent>::with_user_event()
            .with_any_thread(true)
            .build()
            .unwrap();

        let event_loop_proxy = event_loop.create_proxy();

        thread::spawn(move || {
            while let Ok(msg) = rx.recv() {
                println!("Main receiver got: {}", msg);
                event_loop_proxy
                    .send_event(MuekEvent::CreateNewPlugin(msg))
                    .ok();
            }
        });

        // vst_box::run_event_loop(event_loop);
        event_loop.run_app(&mut app).unwrap();
    });

    *EVENT_LOOP_SENDER.lock().unwrap() = Some(tx);
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn run_vst_instance_by_path(utf16_str: *const u16, utf16_len: i32) {
    let slice = unsafe { std::slice::from_raw_parts(utf16_str, utf16_len as usize) };
    let path = String::from_utf16(slice).unwrap();
    println!("Loading VST: {}", path);

    let m = EVENT_LOOP_SENDER.lock().unwrap();
    let tx = m.as_ref().unwrap();
    tx.send(path).unwrap();
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn run_vst_instance_by_path_with_handle(
    utf16_str: *const u16,
    utf16_len: i32,
    hwnd: usize,
) {
    let slice = unsafe { std::slice::from_raw_parts(utf16_str, utf16_len as usize) };
    let path = String::from_utf16(slice).unwrap();

    std::thread::spawn(move || {
        let hwnd_ptr = hwnd as *mut std::ffi::c_void;
        let mut vst = vst_box::Box::from_path(&path);
        vst.init(48000.0, 48000 / 100);
        vst.show_editor_with_handle(hwnd_ptr);
    });
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn verify_vst_instance_by_path(
    utf16_str: *const u16,
    utf16_len: i32,
) -> *mut ByteBuffer {
    let slice = unsafe { std::slice::from_raw_parts(utf16_str, utf16_len as usize) };
    let str = String::from_utf16(slice).unwrap();

    let name = vst_box::verify_vst(&str).unwrap_or("MUEK_ERR".to_owned());

    let buf = ByteBuffer::from_vec(name.into_bytes());
    Box::into_raw(Box::new(buf))
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn cache_clip_data(
    utf16_str: *const u16, // clip id
    utf16_len: i32,
    data_ptr: *const f32,
    len: i32,
) {
    println!("cached clip data called");
    let slice = unsafe { std::slice::from_raw_parts(utf16_str, utf16_len as usize) };
    let str = String::from_utf16(slice).unwrap();

    let data_slice = unsafe { std::slice::from_raw_parts(data_ptr, len as usize) };
    let data_vec = data_slice.to_vec(); // copy

    println!(
        "caching clip id={} len={}",
        str,
        data_vec.len()
    );

    audio::cache_clip_data(&str, data_vec);
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn sync_all_clips(ptr: *const ClipProto, len: i32) {
    let slice = unsafe { std::slice::from_raw_parts(ptr, len as usize) };
    let mut engine_lock = AUDIO_ENGINE.lock().unwrap();
    let clip_caches_lock = CLIP_CACHES.read().unwrap();

    for item in slice {
        println!("sync clip id ptr={:?} len={} start_time={} end_time={}", item.clip_id, item.clip_id_len, item.start_time, item.end_time);
        let slice = unsafe { std::slice::from_raw_parts(item.clip_id, item.clip_id_len as usize) };
        let str = String::from_utf16(slice).unwrap();

        let samples = clip_caches_lock.get(&str).unwrap();
        
        let rendered_clip = RenderedClip {
            start_sample_idx: audio::beat_to_sample_idx(
                item.start_time,
                engine_lock.config.bpm,
                engine_lock.config.sample_rate,
                4,
                engine_lock.config.channels.try_into().unwrap(),
            ),
            end_sample_idx: audio::beat_to_sample_idx(
                item.end_time,
                engine_lock.config.bpm,
                engine_lock.config.sample_rate,
                4,
                engine_lock.config.channels.try_into().unwrap(),
            ),
            samples: samples.to_vec(),
        };
        engine_lock.rendered_clips.push(rendered_clip);
    }
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn spawn_audio_thread() {
    let engine_lock = AUDIO_ENGINE.lock().unwrap();
    engine_lock.spawn();
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn get_current_position_beat() -> f32 {
    let engine_lock = AUDIO_ENGINE.lock().unwrap();
    engine_lock.get_position_beat()
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn stream_play(beat:f32) {
    let mut engine_lock = AUDIO_ENGINE.lock().unwrap();
    engine_lock.play(beat);
}


#[unsafe(no_mangle)]
pub unsafe extern "C" fn stream_stop() -> bool {
    let mut engine_lock = AUDIO_ENGINE.lock().unwrap();
    engine_lock.stop()
}

#[unsafe(no_mangle)]
pub unsafe extern "C" fn set_position_beat(beat:f32) {
    let mut engine_lock = AUDIO_ENGINE.lock().unwrap();
    engine_lock.set_pos_beat(beat);
}
