use std::{
    ffi::{CString, c_char},
    sync::{Arc, Mutex, mpsc::Sender},
    thread,
};

use once_cell::sync::Lazy;
use winit::{
    event_loop::EventLoop,
    platform::windows::EventLoopBuilderExtWindows,
};

use crate::{byte_buffer::ByteBuffer, muek_event::MuekEvent, winit_app::App};

type VecF32 = *const f32;

mod byte_buffer;
mod muek_event;
mod vst_box;
mod winit_app;

#[repr(C)]
pub struct MyClassRepr {
    pub id: i32,
    pub value: VecF32,
    pub len: i32,
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
pub extern "C" fn my_add(x: i32, y: i32) -> i32 {
    x + y
}

#[unsafe(no_mangle)]
pub extern "C" fn alloc_u8_string() -> *mut ByteBuffer {
    let str = format!("foo bar baz");
    let buf = ByteBuffer::from_vec(str.into_bytes());
    Box::into_raw(Box::new(buf))
}

#[unsafe(no_mangle)]
pub extern "C" fn get_string_length(ptr: *const ByteBuffer) -> i32 {
    let buf = unsafe { &*ptr };
    buf.len() as i32
}

#[unsafe(no_mangle)]
#[allow(unused_must_use)]
pub unsafe extern "C" fn free_c_string(str: *mut c_char) {
    unsafe { CString::from_raw(str) };
}

static EVENT_LOOP_SENDER: Lazy<Arc<Mutex<Option<Sender<String>>>>> =
    Lazy::new(|| Arc::new(Mutex::new(None)));

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
pub extern "C" fn run_vst_instance_by_path_with_handle(
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
