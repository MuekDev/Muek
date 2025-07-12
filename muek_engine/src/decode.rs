use std::{fs::File, io::Read};

use infer::audio::is_mp3;
use symphonia::core::sample::u24;

/// samples, channels, sample_rate
pub fn symphonia_decode(path: &str) -> Option<(Vec<f32>, usize, u32)> {
    use symphonia::core::{
        audio::{AudioBufferRef, Signal},
        codecs::DecoderOptions,
        formats::FormatOptions,
        io::MediaSourceStream,
        meta::MetadataOptions,
    };

    let file = Box::new(File::open(path).unwrap());
    let mss = MediaSourceStream::new(file, Default::default());

    let probed = symphonia::default::get_probe()
        .format(
            &Default::default(),
            mss,
            &FormatOptions::default(),
            &MetadataOptions::default(),
        )
        .unwrap();

    let mut format = probed.format;
    let track = format.default_track().unwrap();
    let codec_params = &track.codec_params.clone();
    let mut decoder = symphonia::default::get_codecs()
        .make(&codec_params, &DecoderOptions::default())
        .unwrap();

    let mut samples = vec![];
    let mut sample_rate = codec_params.sample_rate.unwrap_or(44100);
    let mut channels = 1;

    println!("[symphonia_decode] 开始解码");

    loop {
        let packet = match format.next_packet() {
            Ok(p) => p,
            Err(_) => break,
        };

        let decoded = decoder.decode(&packet).unwrap();

        match decoded {
            AudioBufferRef::F32(buf) => {
                sample_rate = buf.spec().rate;
                channels = buf.spec().channels.count();

                for frame_idx in 0..buf.frames() {
                    for ch in 0..channels {
                        let sample = buf.chan(ch)[frame_idx];
                        samples.push(sample);
                    }
                }
            }
            AudioBufferRef::S16(buf) => {
                sample_rate = buf.spec().rate;
                channels = buf.spec().channels.count();

                for frame_idx in 0..buf.frames() {
                    for ch in 0..channels {
                        let sample = buf.chan(ch)[frame_idx] as f32 / i16::MAX as f32;
                        samples.push(sample);
                    }
                }
            }
            AudioBufferRef::U8(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        let raw = cow.chan(ch)[frame_idx];
                        let norm = raw as f32 / u8::MAX as f32;
                        let sample = norm * 2.0 - 1.0;
                        samples.push(sample);
                    }
                }
            }

            AudioBufferRef::U16(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        let raw = cow.chan(ch)[frame_idx];
                        let norm = raw as f32 / u16::MAX as f32;
                        let sample = norm * 2.0 - 1.0;
                        samples.push(sample);
                    }
                }
            }

            AudioBufferRef::U24(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        let raw = cow.chan(ch)[frame_idx].0;
                        let norm = raw as f32 / 16_777_215.0;
                        let sample = norm * 2.0 - 1.0;
                        samples.push(sample);
                    }
                }
            }

            AudioBufferRef::U32(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        let raw = cow.chan(ch)[frame_idx];
                        let norm = raw as f32 / u32::MAX as f32;
                        let sample = norm * 2.0 - 1.0;
                        samples.push(sample);
                    }
                }
            }
            AudioBufferRef::S8(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        let sample = cow.chan(ch)[frame_idx] as f32 / i8::MAX as f32;
                        samples.push(sample)
                    }
                }
            }
            AudioBufferRef::S24(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        let raw_val = cow.chan(ch)[frame_idx].0;
                        let sample = raw_val as f32 / 8_388_608.0; // 2^23
                        samples.push(sample);
                    }
                }
            }

            AudioBufferRef::S32(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        let sample = cow.chan(ch)[frame_idx] as f32 / i32::MAX as f32;
                        samples.push(sample)
                    }
                }
            }
            AudioBufferRef::F64(cow) => {
                sample_rate = cow.spec().rate;
                channels = cow.spec().channels.count();

                for frame_idx in 0..cow.frames() {
                    for ch in 0..channels {
                        // TODO: 这里精度被降低了
                        let sample = cow.chan(ch)[frame_idx] as f32 / f64::MAX as f32;
                        samples.push(sample)
                    }
                }
            }
        }
    }

    println!(
        "[symphonia_decode] DONE，采样率：{}, 通道数：{}",
        sample_rate, channels
    );
    Some((samples, channels, sample_rate))
}

/// Returns (samples: Vec<f32>, channels, sample_rate)
pub fn minimp3_decode(path: &str) -> Option<(Vec<f32>, usize, u32)> {
    use minimp3::{Decoder, Error, Frame};
    use std::fs::File;

    println!("[minimp3] 开始解码");

    let mut decoder = Decoder::new(File::open(path).ok()?);

    let mut all_samples = Vec::new();
    let mut channels = None;
    let mut sample_rate = None;

    loop {
        match decoder.next_frame() {
            Ok(Frame {
                data,
                sample_rate: sr,
                channels: ch,
                ..
            }) => {
                if channels.is_none() {
                    channels = Some(ch);
                } else if channels != Some(ch) {
                    eprintln!("Warning: inconsistent channel count detected");
                    // TODO: return None or handle resampling
                }

                if sample_rate.is_none() {
                    sample_rate = Some(sr as usize);
                } else if sample_rate != Some(sr as usize) {
                    eprintln!("Warning: inconsistent sample rate detected");
                    // TODO: return None or handle resampling
                }

                // Convert i16 to f32
                all_samples.extend(data.iter().map(|s| *s as f32 / i16::MAX as f32));
            }
            Err(Error::Eof) => break,
            Err(e) => {
                eprintln!("Decode error: {:?}", e);
                return None;
            }
        }
    }

    println!("[minimp3] 解码完成");

    Some((
        all_samples,
        channels.unwrap_or(1),
        sample_rate.unwrap_or(44100).try_into().unwrap(),
    ))
}

/// Returns (samples: Vec<f32>, channels, sample_rate)
pub fn auto_decode(path: &str) -> Option<(Vec<f32>, usize, u32)> {
    let mut buf = [0u8; 8192];
    let mut file = match File::open(path) {
        Ok(f) => f,
        Err(_) => return None,
    };

    if file.read(&mut buf).is_err() {
        return None;
    }

    if is_mp3(&buf) {
        minimp3_decode(path)
    } else {
        symphonia_decode(path)
    }
}
