#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
source_dir="$repo_root/submission/project10-java"
zip_path="$repo_root/submission/project10.zip"

rm -f "$zip_path"

(
  cd "$source_dir"
  zip -q "$zip_path" JackAnalyzer.java CompilerEngine.java Tokenizer.java Token.java TokenType.java XmlWriter.java lang.txt
)

printf 'Created submission package at %s\n' "$zip_path"
