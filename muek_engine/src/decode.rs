use std::fs::File;

/// samples, channels, sample_rate
pub fn symphonia_decode(path: &str) -> (Vec<f32>, usize, u32) {
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
            _ => {
                println!("[symphonia_decode] 不支持的音频格式");
                continue;
            }
        }
    }

    println!(
        "[symphonia_decode] 采样率：{}, 通道数：{}",
        sample_rate, channels
    );
    (samples, channels, sample_rate)
}

/// Returns (samples: Vec<f32>, channels, sample_rate)
pub fn minimp3_decode(path: &str) -> Option<(Vec<f32>, usize, u32)> {
    use minimp3::{Decoder, Error, Frame};
    use std::fs::File;

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

    Some((
        all_samples,
        channels.unwrap_or(1),
        sample_rate.unwrap_or(44100).try_into().unwrap(),
    ))
}
