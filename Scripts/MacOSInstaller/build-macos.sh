#!/bin/bash

# macOS Installer Build Script for Noted
# This script builds and packages the Noted app for macOS distribution

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
APP_NAME="Noted"
BUNDLE_ID="com.companyname.noted"
VERSION="1.0.0"
BUILD_DIR="build"
OUTPUT_DIR="dist"
ARCH="${1:-x64}"  # x64 or arm64, default to x64

# Directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo -e "${GREEN}=== Building $APP_NAME for macOS ===${NC}"
echo "Architecture: $ARCH"
echo "Version: $VERSION"

# Create output directories
mkdir -p "$BUILD_DIR" "$OUTPUT_DIR"

# Build the macOS app
echo -e "${YELLOW}Building macOS app...${NC}"
dotnet publish \
    -f net10.0-maccatalyst \
    -c Release \
    -p:RuntimeIdentifier=maccatalyst-$ARCH \
    -p:ApplicationVersion=1 \
    -p:ApplicationDisplayVersion=$VERSION \
    -o "$BUILD_DIR/maccatalyst-$ARCH" \
    ./Noted/Noted.csproj

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

echo -e "${GREEN}Build completed successfully!${NC}"
# Determine produced .app location (publish may put it in project publish folder)
APP_CANDIDATE="$BUILD_DIR/maccatalyst-$ARCH/Noted.app"
if [ ! -d "$APP_CANDIDATE" ]; then
    # Try common publish output inside the project
    APP_CANDIDATE=$(find ./Noted/bin/Release -type d -name "$APP_NAME.app" -print -quit || true)
fi

if [ -z "$APP_CANDIDATE" ] || [ ! -d "$APP_CANDIDATE" ]; then
    echo -e "${RED}Could not find the generated .app bundle. Expected at either:\n  $BUILD_DIR/maccatalyst-$ARCH/Noted.app\nor inside Noted/bin/Release/...${NC}"
    exit 1
fi

echo -e "${YELLOW}App location: $APP_CANDIDATE${NC}"

# Ensure app is copied into the build output folder for packaging
TARGET_APP_DIR="$BUILD_DIR/maccatalyst-$ARCH/Noted.app"
rm -rf "$TARGET_APP_DIR"
mkdir -p "$(dirname "$TARGET_APP_DIR")"
echo -e "${YELLOW}Copying .app to build output: $TARGET_APP_DIR${NC}"
if [ "$APP_CANDIDATE" = "$TARGET_APP_DIR" ]; then
    echo -e "${YELLOW}App is already in target build directory; skipping copy.${NC}"
else
    rm -rf "$TARGET_APP_DIR"
    mkdir -p "$(dirname "$TARGET_APP_DIR")"
    cp -R "$APP_CANDIDATE" "$TARGET_APP_DIR"
fi

# Create DMG
echo -e "${YELLOW}Creating DMG installer...${NC}"
"$SCRIPT_DIR/create-dmg.sh" "$TARGET_APP_DIR" "$VERSION" "$OUTPUT_DIR"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}macOS installer created successfully!${NC}"
    echo -e "${GREEN}Output: $OUTPUT_DIR/Noted-$VERSION.dmg${NC}"
else
    echo -e "${RED}DMG creation failed!${NC}"
    exit 1
fi
