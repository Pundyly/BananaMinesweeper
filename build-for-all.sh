#!/bin/bash
set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/Minesweeper.Desktop"
OUT="$ROOT/publish"

rm -rf "$OUT"

for rid in win-x64 linux-x64 osx-x64 osx-arm64; do
  echo "=== Publishing $rid ==="
  dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -o "$OUT/$rid"

done

echo "Ready $OUT"
