#!/bin/bash
# Render one video end to end: frames -> silent mp4, audio -> wav, mux -> final/<name>.mp4
set -e
cd "$(dirname "$0")"
N=$1; W=${2:-4}
node render.js "$N" --workers "$W" 2>&1 | grep --line-buffered -v "GL Driver"
python3 audio.py "out/$N.audio.json" "out/$N.wav"
mkdir -p final
ffmpeg -v error -y -i "out/$N.silent.mp4" -i "out/$N.wav" -c:v copy -c:a aac -b:a 192k -shortest -movflags +faststart "final/$N.mp4"
echo "FINAL final/$N.mp4"
