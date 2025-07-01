fn main() -> Result<(), Box<dyn std::error::Error>> {
    tonic_build::compile_protos("../protos/greet.proto")?;
    tonic_build::compile_protos("../protos/audio.proto")?;
    Ok(())
}