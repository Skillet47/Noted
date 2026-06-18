#!/bin/bash

# Code Signing and Notarization Script for macOS
# This script signs the app and submits it for notarization

set -e

APP_PATH="${1:-.}"
TEAM_ID="${2:-}"
APPLE_ID="${3:-}"
APP_PASSWORD="${4:-}"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}=== Code Signing and Notarization ===${NC}"

# Validate inputs
if [ -z "$TEAM_ID" ] || [ -z "$APPLE_ID" ] || [ -z "$APP_PASSWORD" ]; then
    echo -e "${RED}Error: Missing required parameters${NC}"
    echo "Usage: $0 <app_path> <team_id> <apple_id> <app_password>"
    echo ""
    echo "Parameters:"
    echo "  app_path: Path to the .app bundle"
    echo "  team_id: Your Apple Team ID (e.g., ABC123DEFG)"
    echo "  apple_id: Your Apple ID email"
    echo "  app_password: App-specific password from appleid.apple.com"
    exit 1
fi

# Step 1: Sign the app
echo -e "${YELLOW}Code signing the app...${NC}"
codesign --deep --force --verify --verbose \
    --sign "Developer ID Application: $TEAM_ID" \
    "$APP_PATH"

if [ $? -ne 0 ]; then
    echo -e "${RED}Code signing failed!${NC}"
    exit 1
fi

echo -e "${GREEN}Code signing completed!${NC}"

# Step 2: Create ZIP for notarization
echo -e "${YELLOW}Creating ZIP archive for notarization...${NC}"
ZIP_FILE="$(dirname "$APP_PATH")/$(basename "$APP_PATH" .app)-notarize.zip"
ditto -c -k --keepParent "$APP_PATH" "$ZIP_FILE"

echo -e "${GREEN}ZIP created: $ZIP_FILE${NC}"

# Step 3: Submit for notarization
echo -e "${YELLOW}Submitting for notarization...${NC}"
NOTARIZE_RESPONSE=$(xcrun notarytool submit "$ZIP_FILE" \
    --apple-id "$APPLE_ID" \
    --password "$APP_PASSWORD" \
    --team-id "$TEAM_ID" \
    --wait)

SUBMISSION_ID=$(echo "$NOTARIZE_RESPONSE" | grep "id:" | awk '{print $2}')

if [ -z "$SUBMISSION_ID" ]; then
    echo -e "${RED}Notarization submission failed!${NC}"
    echo "$NOTARIZE_RESPONSE"
    exit 1
fi

echo -e "${GREEN}Notarization submission successful!${NC}"
echo "Submission ID: $SUBMISSION_ID"

# Step 4: Check notarization status
echo -e "${YELLOW}Checking notarization status...${NC}"
xcrun notarytool log "$SUBMISSION_ID" \
    --apple-id "$APPLE_ID" \
    --password "$APP_PASSWORD" \
    --team-id "$TEAM_ID"

# Step 5: Staple the notarization ticket
echo -e "${YELLOW}Stapling notarization ticket...${NC}"
xcrun stapler staple "$APP_PATH"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}Notarization completed successfully!${NC}"
    echo -e "${GREEN}App is ready for distribution: $APP_PATH${NC}"
else
    echo -e "${RED}Stapling failed!${NC}"
    exit 1
fi

# Cleanup
rm -f "$ZIP_FILE"

echo -e "${GREEN}Done!${NC}"
