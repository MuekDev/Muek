use crate::byte_buffer::ByteBuffer;

type VecF32 = *const f32;

mod byte_buffer;

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
        let s= unsafe { std::slice::from_raw_parts(item.value, item.len as usize) };
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