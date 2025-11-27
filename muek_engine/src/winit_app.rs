use std::collections::HashMap;
use std::mem::transmute;

use raw_window_handle::{HasRawWindowHandle, RawWindowHandle};
use vst::plugin::Plugin;
use winit::application::ApplicationHandler;
use winit::dpi::PhysicalSize;
use winit::event::WindowEvent;
use winit::event_loop::ActiveEventLoop;
use winit::window::{Window, WindowAttributes, WindowId};

use crate::muek_event::MuekEvent;
use crate::vst_box;

#[derive(Default)]
pub struct App {
    windows: HashMap<WindowId, Window>,
    plugins: Vec<Box<vst_box::Box>>,
}

impl ApplicationHandler<MuekEvent> for App {
    fn resumed(&mut self, _event_loop: &ActiveEventLoop) {}

    fn user_event(&mut self, event_loop: &ActiveEventLoop, event: MuekEvent) {
        match event {
            MuekEvent::SendAudioBuffer(_items, _items1) => {}
            MuekEvent::CreateNewPlugin(path) => {
                let mut plugin = vst_box::Box::from_path(&path);

                plugin.init(48000.0, 48000 / 100);

                let mut editor_view = plugin.plugin.get_editor().unwrap();

                let plugin_name = plugin.plugin.get_info().name;

                self.plugins.push(Box::new(plugin));

                let (width, height) = editor_view.size();

                let window_attributes = WindowAttributes::default()
                    .with_inner_size(PhysicalSize::new(width, height))
                    .with_title(format!("[MUEK DEV] {}", plugin_name));

                let window = event_loop.create_window(window_attributes).unwrap();

                let handle = window.raw_window_handle();

                let handle_ptr = match handle {
                    Ok(RawWindowHandle::Win32(handle)) => handle.hwnd,
                    _ => panic!("don't know this platform"),
                };

                self.windows.insert(window.id(), window);

                unsafe {
                    //    let mut handle_ptr = 0x001D036C as *mut c_void;
                    editor_view.open(transmute(handle_ptr));
                }
            }
        }
    }

    fn window_event(&mut self, _event_loop: &ActiveEventLoop, id: WindowId, event: WindowEvent) {
        match event {
            WindowEvent::CloseRequested => {
                println!("The close button was pressed; stopping");
                // event_loop.exit();
                self.windows.remove(&id);
            }
            WindowEvent::RedrawRequested => {
                // Redraw the application.
                //
                // It's preferable for applications that do not render continuously to render in
                // this event rather than in AboutToWait, since rendering in here allows
                // the program to gracefully handle redraws requested by the OS.

                // Draw.

                // Queue a RedrawRequested event.
                //
                // You only need to call this if you've determined that you need to redraw in
                // applications which do not always need to. Applications that redraw continuously
                // can render here instead.
                println!("RedrawRequested\n");
                // if self.window.is_some() {
                // let window = self.window.as_ref().unwrap();
                // self.window.as_ref().unwrap().request_redraw();
                // }
            }
            _ => (),
        }
    }
}
