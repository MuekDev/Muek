use std::mem::transmute;
use std::os::raw::c_void;
use std::path::Path;
use std::sync::{Arc, Mutex};

use raw_window_handle::{HasRawWindowHandle, RawWindowHandle};
use vst::host::{Host, PluginInstance, PluginLoader};
use vst::plugin::Plugin;
use winit::dpi::PhysicalSize;
use winit::event_loop::EventLoop;
use winit::window::WindowAttributes;
use windows::Win32::UI::WindowsAndMessaging::{
    DispatchMessageW, GetMessageW, MSG, TranslateMessage,
};

struct HostHandle;

const SAMPLE_RATE: usize = 48000;
const BLOCK_SIZE: usize = SAMPLE_RATE / 100;

impl Host for HostHandle {
    fn automate(&self, index: i32, value: f32) {
        println!("Parameter {} had its value changed to {}", index, value);
    }
    fn begin_edit(&self, _index: i32) {
        println!("update_display")
    }
}

pub struct Box {
    pub host: Arc<Mutex<HostHandle>>,
    pub plugin: PluginInstance,
    pub loader: PluginLoader<HostHandle>,
}

impl Box {
    pub fn from_path(path: &str) -> Self {
        let path = Path::new(path);

        let host: Arc<Mutex<HostHandle>> = Arc::new(Mutex::new(HostHandle));

        println!("Loading {}...", path.to_str().unwrap());

        // Load the plugin
        let mut loader = PluginLoader::load(path, Arc::clone(&host))
            .unwrap_or_else(|e| panic!("Failed to load plugin: {}", e));

        // Create an instance of the plugin
        let plugin = loader.instance().unwrap();

        // Get the plugin information
        let info = plugin.get_info();

        println!(
            "Loaded '{}':\n\t\
            Vendor: {}\n\t\
            Presets: {}\n\t\
            Parameters: {}\n\t\
            VST ID: {}\n\t\
            Version: {}\n\t\
            Initial Delay: {} samples",
            info.name,
            info.vendor,
            info.presets,
            info.parameters,
            info.unique_id,
            info.version,
            info.initial_delay
        );

        Box {
            host,
            plugin,
            loader,
        }
    }

    pub fn init(&mut self, sample_rate: f32, block_size: i64) {
        let plugin = &mut self.plugin;
        plugin.init();

        plugin.set_sample_rate(sample_rate);
        plugin.set_block_size(block_size);

        plugin.resume();

        println!("Initialized instance!");
    }

    pub fn show_editor(&mut self, event_loop: &EventLoop<(Vec<f32>, Vec<f32>)>) {
        let plugin = &mut self.plugin;

        let mut editor_view = plugin.get_editor().unwrap();

        let (window_width, window_height) = editor_view.size();

        let _plugin_name = plugin.get_info().name.clone();

        let window_attributes = WindowAttributes::default()
            .with_inner_size(PhysicalSize::new(window_width, window_height));

        // let window = WindowBuilder::new()
        //     .with_inner_size(PhysicalSize::new(window_width, window_height))
        //     .with_resizable(false)
        //     .with_title("sout VST testing host - ".to_owned() + &plugin_name)
        //     .build(&event_loop)
        //     .unwrap();

        let window = event_loop.create_window(window_attributes).unwrap();

        let handle = window.raw_window_handle();

        let handle_ptr = match handle {
            Ok(RawWindowHandle::Win32(handle)) => handle.hwnd,
            _ => panic!("don't know this platform"),
        };

        unsafe {
            //    let mut handle_ptr = 0x001D036C as *mut c_void;
            editor_view.open(transmute(handle_ptr));
        }

        println!("Opened editor window!");
    }

    pub fn show_editor_with_handle(&mut self, raw_window_handle: *mut c_void) {
        let plugin = &mut self.plugin;

        let mut editor_view = plugin.get_editor().unwrap();

        unsafe {
            editor_view.open(transmute(raw_window_handle));
        }

        unsafe {
            let mut msg = MSG::default();
            while GetMessageW(&mut msg, None, 0, 0).into() {
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
        }
    }
}

pub fn verify_vst(path: &str) -> anyhow::Result<String> {
    let path = Path::new(&path);

    let host: Arc<Mutex<HostHandle>> = Arc::new(Mutex::new(HostHandle));

    println!("Loading {}...", path.to_str().unwrap());

    // Load the plugin
    let mut loader = PluginLoader::load(path, Arc::clone(&host))
        .unwrap_or_else(|e| panic!("Failed to load plugin: {}", e));

    // Create an instance of the plugin
    let plugin = loader.instance()?;

    // Get the plugin information
    let info = plugin.get_info();

    Ok(info.name)
}
