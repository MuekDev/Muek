fn main() {
    println!("cargo:warning=Building csbindgen...");
    csbindgen::Builder::default()
        .input_extern_file("src/lib.rs")
        .input_extern_file("src/protos/byte_buffer.rs")
        .input_extern_file("src/protos/tracks_proto.rs")
        .csharp_dll_name("muek_engine")
        .csharp_namespace("Muek.Engine")
        .csharp_class_name("MuekEngine")
        .generate_csharp_file(
            r"../Muek/Engine/MuekEngine.g.cs",
        )
        .unwrap();
}