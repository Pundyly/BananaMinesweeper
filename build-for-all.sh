#!/bin/bash
set -euo pipefail

# Directory where this script lives (solution root)
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$ROOT/Minesweeper.Desktop"
OUT="$ROOT/publish"
NAME="BananaMinesweeper"
RIDS=(win-x64 linux-x64 osx-x64 osx-arm64)

# Check required tools
command -v dotnet >/dev/null 2>&1 || { echo "Error: dotnet is not installed."; exit 1; }
command -v zip >/dev/null 2>&1 || { echo "Error: zip is not installed."; exit 1; }
command -v tar >/dev/null 2>&1 || { echo "Error: tar is not installed."; exit 1; }

echo "Cleaning old output..."
rm -rf "$OUT"
mkdir -p "$OUT"

for rid in "${RIDS[@]}"; do
  echo
  echo "=== Publishing $rid ==="
  dotnet publish "$PROJECT" -c Release -r "$rid" --self-contained true \
    -p:PublishSingleFile=true -o "$OUT/$rid"

  echo "--- Creating archive for $rid ---"
  if [[ "$rid" == win-* ]]; then
    # Windows: zip
    (cd "$OUT" && zip -rq "$NAME-$rid.zip" "$rid")
  else
    # Linux/macOS: tar.gz (keeps executable permissions)
    tar -czf "$OUT/$NAME-$rid.tar.gz" -C "$OUT" "$rid"
  fi
done

echo
echo "Done! Archives are in: $OUT"
ls -lh "$OUT" | grep -E "\.(zip|tar\.gz)$" || true
