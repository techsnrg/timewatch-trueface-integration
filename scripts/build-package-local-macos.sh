#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  scripts/build-package-local-macos.sh --sdk-zip PATH [--output PATH]

Builds the Windows connector locally on macOS and creates a target-PC zip.

Requirements:
  - .NET 8 SDK installed on this Mac
  - TrueFace_SDK.zip available locally

Example:
  scripts/build-package-local-macos.sh \
    --sdk-zip "/Users/nikhil/Library/Mobile Documents/com~apple~CloudDocs/Downloads/TrueFace_SDK.zip"
EOF
}

SDK_ZIP=""
OUTPUT="dist/TrueFaceConnector-TargetPC.zip"

while [[ $# -gt 0 ]]; do
  case "$1" in
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

if [[ ! -f "$SDK_ZIP" ]]; then
  echo "SDK zip not found: $SDK_ZIP" >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  cat >&2 <<'EOF'
dotnet was not found on this Mac.

Install the .NET 8 SDK for macOS first:
https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Then rerun this script.
EOF
  exit 1
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="$ROOT_DIR/dist/local-win-build"
PUBLISH_DIR="$BUILD_DIR/publish"

rm -rf "$BUILD_DIR"
mkdir -p "$PUBLISH_DIR" "$(dirname "$ROOT_DIR/$OUTPUT")"

echo "Publishing Windows connector locally..."
dotnet publish "$ROOT_DIR/connector/TrueFaceConnector/TrueFaceConnector.csproj" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o "$PUBLISH_DIR"

echo "Packaging with TrueFace SDK DLLs..."
"$ROOT_DIR/scripts/package-trueface-connector-macos.sh" \
  --publish-dir "$PUBLISH_DIR" \
  --sdk-zip "$SDK_ZIP" \
  --output "$OUTPUT"

echo ""
echo "Done. Send this zip to the target Windows PC:"
echo "$ROOT_DIR/$OUTPUT"
