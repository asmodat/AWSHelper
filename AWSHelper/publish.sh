#!/bin/sh

echo "Publish START"

rm -rf "$PWD/bin/publish" -f -r -v

dotnet publish --self-contained -c Release -r linux-x64 -o ./bin/publish/linux-x64
dotnet publish --self-contained -c Release -r win-x64 -o ./bin/publish/win-x64


zip -r -j ./bin/publish/AWSHelper-linux-x64.zip ./bin/publish/linux-x64/*.*
zip -r -j ./bin/publish/AWSHelper-win-x64.zip ./bin/publish/win-x64/*.*

echo "Publish DONE"