#!/bin/bash

# Universal Binary Builder for macOS
# Creates a universal binary supporting both x64 and ARM64

set -e

APP_NAME="Noted"
VERSION="1.0.0"
BUILD_DIR="build"
OUTPUT_DIR="dist"

# Directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}=== Building Universal Binary for macOS ===${NC}"

# Create output directories
mkdir -p "$BUILD_DIR" "$OUTPUT_DIR"

# Build for x64
echo -e "${YELLOW}Building for Intel (x64)...${NC}"
dotnet publish \
    -f net10.0-maccatalyst \
    -c Release \
    -p:RuntimeIdentifier=maccatalyst-x64 \
    -p:ApplicationVersion=1 \
    -p:ApplicationDisplayVersion=$VERSION \
    -o "$BUILD_DIR/maccatalyst-x64" \
    ./Noted/Noted.csproj

# Build for ARM64
echo -e "${YELLOW}Building for Apple Silicon (ARM64)...${NC}"
dotnet publish \
    -f net10.0-maccatalyst \
    -c Release \
    -p:RuntimeIdentifier=maccatalyst-arm64 \
    -p:ApplicationVersion=1 \
    -p:ApplicationDisplayVersion=$VERSION \
    -o "$BUILD_DIR/maccatalyst-arm64" \
    ./Noted/Noted.csproj

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

# Create universal binary
echo -e "${YELLOW}Creating universal binary...${NC}"
X64_APP="$BUILD_DIR/maccatalyst-x64/Noted.app"
ARM64_APP="$BUILD_DIR/maccatalyst-arm64/Noted.app"
UNIVERSAL_APP="$BUILD_DIR/Noted-universal.app"

# Copy x64 as base
cp -R "$X64_APP" "$UNIVERSAL_APP"

# Create universal binaries for executables
X64_BIN="$X64_APP/Contents/MacOS/Noted"
ARM64_BIN="$ARM64_APP/Contents/MacOS/Noted"
UNIVERSAL_BIN="$UNIVERSAL_APP/Contents/MacOS/Noted"

lipo -create "$X64_BIN" "$ARM64_BIN" -output "$UNIVERSAL_BIN"

echo -e "${GREEN}Universal binary created!${NC}"

# Create DMG with universal binary
echo -e "${YELLOW}Creating DMG installer...${NC}"
"$SCRIPT_DIR/create-dmg.sh" "$UNIVERSAL_APP" "$VERSION" "$OUTPUT_DIR"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}Universal macOS installer created successfully!${NC}"
    echo -e "${GREEN}Output: $OUTPUT_DIR/Noted-$VERSION.dmg${NC}"
else
    echo -e "${RED}DMG creation failed!${NC}"
    exit 1
fi

# Cleanup individual builds
echo -e "${YELLOW}Cleaning up individual builds...${NC}"
rm -rf "$BUILD_DIR/maccatalyst-x64" "$BUILD_DIR/maccatalyst-arm64" "$BUILD_DIR/Noted-universal.app"

echo -e "${GREEN}Build complete!${NC}"
