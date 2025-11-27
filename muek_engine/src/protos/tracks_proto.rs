#[repr(C)]
pub struct TrackProto {
    pub track_id: i32,
}

#[repr(C)]
pub struct ClipProto {
    pub clip_id: *const u16,
    pub clip_id_len: i32,
    pub start_time: f32,
    pub end_time: f32,
}