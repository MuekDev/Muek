pub enum MuekEvent {
    SendAudioBuffer(Vec<f32>, Vec<f32>),
    CreateNewPlugin(String),
}