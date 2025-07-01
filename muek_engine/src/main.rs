use tonic::transport::Server;

use crate::{
    audio::{
        AudioProxy,
        audio_proto::audio_proxy_proto_server::AudioProxyProtoServer,
    },
    greet::{MyGreeter, greet::greeter_server::GreeterServer},
};

mod audio;
mod greet;
mod decode;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let addr = "[::1]:50051".parse()?;
    let greeter = MyGreeter::default();
    let audio_engine = AudioProxy::default();

    Server::builder()
        .add_service(GreeterServer::new(greeter))
        .add_service(AudioProxyProtoServer::new(audio_engine))
        .serve(addr)
        .await?;

    Ok(())
}
