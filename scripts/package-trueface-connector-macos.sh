#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  scripts/package-trueface-connector-macos.sh --publish-zip PATH --sdk-zip PATH [--output PATH]
  scripts/package-trueface-connector-macos.sh --publish-dir PATH --sdk-zip PATH [--output PATH]

Creates a final plug-and-play Windows zip containing:
  - published TrueFaceConnector.exe app files
  - install.bat / uninstall.bat
  - TrueFace SDK x64 runtime DLLs

Example:
  scripts/package-trueface-connector-macos.sh \
    --publish-zip ~/Downloads/TrueFaceConnector-Windows.zip \
    --sdk-zip "/Users/nikhil/Library/Mobile Documents/com~apple~CloudDocs/Downloads/TrueFace_SDK.zip"
EOF
}

PUBLISH_ZIP=""
PUBLISH_DIR=""
SDK_ZIP=""
OUTPUT="dist/TrueFaceConnector-TargetPC.zip"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --publish-zip)
      PUBLISH_ZIP="${2:-}"
      shift 2
      ;;
    --publish-dir)
      PUBLISH_DIR="${2:-}"
      shift 2
      ;;
    --sdk-zip)
      SDK_ZIP="${2:-}"
      shift 2
      ;;
    --output)
      OUTPUT="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ -z "$SDK_ZIP" ]]; then
  echo "Missing --sdk-zip" >&2
  usage
  exit 1
fi

if [[ -z "$PUBLISH_ZIP" && -z "$PUBLISH_DIR" ]]; then
  echo "Missing --publish-zip or --publish-dir" >&2
  usage
  exit 1
fi

if [[ -n "$PUBLISH_ZIP" && -n "$PUBLISH_DIR" ]]; then
  echo "Use only one of --publish-zip or --publish-dir" >&2
  usage
  exit 1
fi

if [[ ! -f "$SDK_ZIP" ]]; then
  echo "SDK zip not found: $SDK_ZIP" >&2
  exit 1
fi

WORKDIR="$(mktemp -d /tmp/trueface-package.XXXXXX)"
trap 'rm -rf "$WORKDIR"' EXIT

PACKAGE_DIR="$WORKDIR/TrueFaceConnector"
SDK_DIR="$WORKDIR/sdk"
mkdir -p "$PACKAGE_DIR" "$SDK_DIR" "$(dirname "$OUTPUT")"

if [[ -n "$PUBLISH_ZIP" ]]; then
  if [[ ! -f "$PUBLISH_ZIP" ]]; then
    echo "Publish zip not found: $PUBLISH_ZIP" >&2
    exit 1
  fi
  unzip -q "$PUBLISH_ZIP" -d "$PACKAGE_DIR"
else
  if [[ ! -d "$PUBLISH_DIR" ]]; then
    echo "Publish directory not found: $PUBLISH_DIR" >&2
    exit 1
  fi
  cp -R "$PUBLISH_DIR"/. "$PACKAGE_DIR"/
fi

unzip -q "$SDK_ZIP" -d "$SDK_DIR"

SDK_RUNTIME_DIR="$(find "$SDK_DIR" -type d -path '*TrueFace_SDK/AccessDemo2s/bin/x64Release' | head -n 1)"
if [[ -z "$SDK_RUNTIME_DIR" ]]; then
  SDK_RUNTIME_DIR="$(find "$SDK_DIR" -type d -path '*TrueFace_SDK/AccessDemo2s/bin/x64Debug' | head -n 1)"
fi

if [[ -z "$SDK_RUNTIME_DIR" ]]; then
  echo "Could not find TrueFace SDK x64Release/x64Debug runtime DLL folder inside SDK zip." >&2
  exit 1
fi

DLLS=(
  "dhnetsdk.dll"
  "dhconfigsdk.dll"
  "dhplay.dll"
  "avnetsdk.dll"
  "Infra.dll"
  "RenderEngine.dll"
  "IvsDrawer.dll"
  "StreamConvertor.dll"
  "ImageAlg.dll"
  "NetSDKCS.dll"
)

for dll in "${DLLS[@]}"; do
  if [[ -f "$SDK_RUNTIME_DIR/$dll" ]]; then
    cp "$SDK_RUNTIME_DIR/$dll" "$PACKAGE_DIR/"
  else
    echo "Warning: missing SDK DLL: $dll" >&2
  fi
done

cp connector/install.bat "$PACKAGE_DIR/" 2>/dev/null || true
cp connector/uninstall.bat "$PACKAGE_DIR/" 2>/dev/null || true

cat > "$PACKAGE_DIR/READ-ME-FIRST.txt" <<'EOF'
TrueFace Connector package

On the target Windows PC:

1. Extract this zip.
2. Edit appsettings.json.
3. Right-click install.bat.
4. Choose "Run as administrator".

The installer copies files to C:\TrueFaceConnector and starts the Windows service.
EOF

if [[ ! -f "$PACKAGE_DIR/TrueFaceConnector.exe" ]]; then
  echo "Warning: TrueFaceConnector.exe was not found in the package. Check the publish artifact." >&2
fi

(cd "$WORKDIR" && zip -qr "$OLDPWD/$OUTPUT" "TrueFaceConnector")

echo "Created package:"
echo "$OUTPUT"
