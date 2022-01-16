#!/usr/bin/env bash

docker build -t harepacker .

rm -rf xml
mkdir -p xml
rm -rf compiled_wz
mkdir -p compiled_wz

docker run -it \
  -v `pwd`/MapleStory:/harepacker/MapleStory \
  -v `pwd`/xml:/harepacker/xml \
  -v `pwd`/compiled_wz:/harepacker/compiled_wz \
  --rm \
  harepacker \
  bash -c "csc src/* && mono Program.exe"
