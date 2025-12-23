#!/usr/bin/fish

cargo build

set source "target/debug/libmuek_engine.so"
set destination "../Muek/"

cp -f $source $destination
