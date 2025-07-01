use tonic::{Request, Response, Status};

use crate::greet::greet::{greeter_server::Greeter, HelloReply, HelloRequest};


pub mod greet {
    tonic::include_proto!("greet");
}

#[derive(Debug, Default)]
pub struct MyGreeter {}

#[tonic::async_trait]
impl Greeter for MyGreeter {
    async fn say_hello(
        &self,
        request: Request<HelloRequest>,
    ) -> Result<Response<HelloReply>, Status> {
        println!("Got a request: {:?}", request);

        let reply = HelloReply {
            message: format!("Connect: {}", request.into_inner().name),
        };

        Ok(Response::new(reply))
    }
}