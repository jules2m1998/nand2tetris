#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
PROJECT_DIR="$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)"
SUBMISSION_DIR="$PROJECT_DIR/submission"
STAGE_DIR="$SUBMISSION_DIR/project9"
ZIP_PATH="$SUBMISSION_DIR/project9.zip"

mkdir -p "$SUBMISSION_DIR"
rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR/source"

required_files=(
  "Main.jack"
  "Player.jack"
  "Cactus.jack"
  "Main.vm"
  "Player.vm"
  "Cactus.vm"
  "README.md"
)

for file in "${required_files[@]}"; do
  if [[ ! -f "$PROJECT_DIR/$file" ]]; then
    echo "Missing required file: $PROJECT_DIR/$file" >&2
    exit 1
  fi
done

cp "$PROJECT_DIR"/Main.vm "$PROJECT_DIR"/Player.vm "$PROJECT_DIR"/Cactus.vm "$STAGE_DIR"/
cp "$PROJECT_DIR"/README.md "$STAGE_DIR"/

if [[ -f "$PROJECT_DIR/image.png" ]]; then
  cp "$PROJECT_DIR/image.png" "$STAGE_DIR"/
fi

cp "$PROJECT_DIR"/Main.jack "$PROJECT_DIR"/Player.jack "$PROJECT_DIR"/Cactus.jack "$STAGE_DIR/source"/

rm -f "$ZIP_PATH"
(
  cd "$STAGE_DIR"
  zip -rq "$ZIP_PATH" .
)

echo "Created submission archive: $ZIP_PATH"
