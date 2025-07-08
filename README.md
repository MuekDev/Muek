# Muek

宇宙超级无敌第一DAW

## How 2 编译

- 装Rider，Rust
- 下载 [protoc](https://github.com/protocolbuffers/protobuf/releases)，把压缩包里的`bin`里的那啥拿出来
- 新建系统环境变量 `PROTOC`，值类似: `C:\env\protoc\bin\protoc.exe`
- 进入 [muek_engine](muek_engine) 文件夹，cd进这里或者用编辑器打开，在终端中执行 `cargo run`，保持后台运行
- 用Rider打开[Muek.sln](Muek.sln)，运行