#!/bin/bash

# DMG Creator Script for macOS app distribution
# Usage: ./create-dmg.sh <app_path> <version> <output_dir>

set -e

APP_PATH="${1:-.}"
VERSION="${2:-1.0.0}"
OUTPUT_DIR="${3:-.}"
APP_NAME="Noted"
DMG_NAME="$APP_NAME-$VERSION"
DMG_FILE="$OUTPUT_DIR/$DMG_NAME.dmg"
TEMP_DMG="$OUTPUT_DIR/temp-$DMG_NAME.dmg"
MOUNT_POINT="/Volumes/$DMG_NAME"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# Clean up any previous mounts
echo -e "${YELLOW}Cleaning up previous mounts...${NC}"
if [ -d "$MOUNT_POINT" ]; then
    hdiutil detach "$MOUNT_POINT" 2>/dev/null || true
fi

# Remove existing DMG
if [ -f "$DMG_FILE" ]; then
    rm "$DMG_FILE"
fi

if [ -f "$TEMP_DMG" ]; then
    rm "$TEMP_DMG"
fi

# Create temporary DMG
echo -e "${YELLOW}Creating temporary DMG...${NC}"
hdiutil create -volname "$DMG_NAME" \
    -srcfolder "$APP_PATH" \
    -ov -format UDRW \
    "$TEMP_DMG"

# Mount the DMG
echo -e "${YELLOW}Mounting DMG...${NC}"
hdiutil attach "$TEMP_DMG" -mountpoint "$MOUNT_POINT"

# Copy background image if it exists
if [ -f "Assets/dmg-background.png" ]; then
    echo -e "${YELLOW}Setting DMG background...${NC}"
    cp "Assets/dmg-background.png" "$MOUNT_POINT/.background/dmg-background.png"
fi

# Set DMG appearance and layout
echo -e "${YELLOW}Configuring DMG appearance...${NC}"
osascript << EOF
tell application "Finder"
    tell disk "$DMG_NAME"
        open
        
        set current view of container window to icon view
        set toolbar visible of container window to false
        set statusbar visible of container window to false
        
        -- Set window size and position
        set the bounds of container window to {400, 100, 885, 430}
        
        -- Create Applications folder alias
        make new alias file at container window to POSIX file "/Applications" with properties {name:"Applications"}
        
        set position of item "$APP_NAME.app" of container window to {100, 100}
        set position of item "Applications" of container window to {310, 100}
        
        set icon size of icon view options of container window to 96
        
        close
        open
        update without registering applications
    end tell
end tell
EOF

# Unmount and convert to compressed
echo -e "${YELLOW}Finalizing DMG...${NC}"
hdiutil detach "$MOUNT_POINT"
hdiutil convert "$TEMP_DMG" -format UDZO -imagekey zlib-level=9 -o "$DMG_FILE"
rm "$TEMP_DMG"

# Create checksum
echo -e "${YELLOW}Creating checksum...${NC}"
shasum -a 256 "$DMG_FILE" > "$OUTPUT_DIR/$DMG_NAME.dmg.sha256"

echo -e "${GREEN}DMG created successfully!${NC}"
echo -e "${GREEN}File: $DMG_FILE${NC}"
echo -e "${GREEN}Size: $(du -h "$DMG_FILE" | cut -f1)${NC}"
