#!/bin/sh

echo "Publish START"

app_name="AWSHelper"
pub_dir=./bin/publish

rm -rf $pub_dir -f -r -v

dotnet publish --self-contained -c release -r linux-x64 -o $pub_dir/linux-x64 &
dotnet publish -c release -r win-x64 -o $pub_dir/win-x64 &
wait $(jobs -p)

zip -r $pub_dir/$app_name-linux-x64.zip $pub_dir/linux-x64/* &
zip -r $pub_dir/$app_name-win-x64.zip $pub_dir/win-x64/* &
wait $(jobs -p)

echo "Publish DONE"