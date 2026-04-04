#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
source_root="${1:-/Users/julesjuniormevaa/Downloads/03_Education/nand2tetris/projects/11}"
tools_root="${2:-/Users/julesjuniormevaa/Downloads/03_Education/nand2tetris/tools}"
fixture_root="$repo_root/tests/SyntaxAnalyser.Tests/Fixtures/Project11"
temp_root="$(mktemp -d "${TMPDIR:-/tmp}/project11-fixtures-XXXXXX")"

cleanup() {
  rm -rf "$temp_root"
}

trap cleanup EXIT

projects=(
  Average
  ComplexArrays
  ConvertToBin
  Pong
  Seven
  Square
)

rm -rf "$fixture_root"
mkdir -p "$fixture_root"

for project in "${projects[@]}"; do
  source_project="$source_root/$project"
  temp_project="$temp_root/$project"
  fixture_project="$fixture_root/$project"

  mkdir -p "$temp_project" "$fixture_project"

  cp "$source_project"/*.jack "$temp_project"/
  cp "$source_project"/*.jack "$fixture_project"/

  "$tools_root/JackCompiler.sh" "$temp_project"

  cp "$temp_project"/*.vm "$fixture_project"/
done

printf 'Imported Project 11 fixtures into %s\n' "$fixture_root"
