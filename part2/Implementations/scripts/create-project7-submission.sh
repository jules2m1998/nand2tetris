#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../../.." && pwd)"
source_dir="$repo_root/part2/Implementations/VMTranslator"
submission_root="$repo_root/part2/Implementations/submission"
staging_dir="$submission_root/project7"
zip_path="$submission_root/project7.zip"

rm -rf "$staging_dir"
mkdir -p "$staging_dir"

cp "$source_dir/Program.cs" "$staging_dir/Program.cs"
cp "$source_dir/Services/HackVmLineTranslator.cs" "$staging_dir/HackVmLineTranslator.cs"
cp "$source_dir/Abstractions/IVMLineTranslator.cs" "$staging_dir/IVMLineTranslator.cs"

cat > "$staging_dir/VMTranslator.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>VMTranslator</AssemblyName>
    <RootNamespace>VMTranslator</RootNamespace>
  </PropertyGroup>
</Project>
EOF

cat > "$staging_dir/lang.txt" <<'EOF'
c# debug
EOF

rm -f "$zip_path"
(
  cd "$staging_dir"
  zip -q "$zip_path" Program.cs HackVmLineTranslator.cs IVMLineTranslator.cs VMTranslator.csproj lang.txt
)

printf 'Created submission package at %s\n' "$zip_path"
