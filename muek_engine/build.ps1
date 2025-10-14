cargo build

$source = "target/debug/muek_engine.dll"
$destination ="../Muek"

Copy-Item -Path $source -Destination $destination -Force
