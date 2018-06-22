#!/bin/sh

echo "Install START"
current_dir=$PWD

source ./publish.sh

cd "$current_dir"
pub_dir=./bin/publish/win-x64
install_dir="/d/Program Files/AWSHelper-win-x64"

rm "$install_dir"/* -f -v
mv "$pub_dir"/* "$install_dir" -f -v

echo "Install DONE"