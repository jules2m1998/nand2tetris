#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_file="$script_dir/../VMTranslator/VMTranslator.csproj"
install_dir="${INSTALL_DIR:-$HOME/.local/bin}"
app_dir="${APP_DIR:-$HOME/.local/share/vm-translator}"

cleanup() {
  :
}

trap cleanup EXIT

mkdir -p "$install_dir"
mkdir -p "$app_dir"

dotnet publish "$project_file" \
  -c Release \
  --self-contained false \
  -o "$app_dir"

cat > "$install_dir/vm-translator" <<EOF
#!/usr/bin/env bash
exec "$app_dir/vm-translator" "\$@"
EOF

chmod 755 "$install_dir/vm-translator"

printf 'Installed vm-translator to %s/vm-translator\n' "$install_dir"
printf 'Application files published to %s\n' "$app_dir"

case ":$PATH:" in
  *":$install_dir:"*) ;;
  *)
    printf 'Add %s to your PATH to run `vm-translator` directly.\n' "$install_dir"
    ;;
esac
