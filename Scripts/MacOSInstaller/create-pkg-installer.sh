#!/bin/bash

# PKG Installer Creator Script for macOS
# Creates a standard macOS .pkg installer for the Noted app

set -e

APP_PATH="${1:-.}"
VERSION="${2:-1.0.0}"
OUTPUT_DIR="${3:-.}"
APP_NAME="Noted"
PKG_NAME="$APP_NAME-$VERSION"
BUNDLE_ID="com.companyname.noted"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${GREEN}=== Creating macOS Package Installer ===${NC}"

# Create temporary directory for packaging
TEMP_DIR="$(mktemp -d)"
PACKAGE_DIR="$TEMP_DIR/package"
SCRIPTS_DIR="$TEMP_DIR/scripts"

mkdir -p "$PACKAGE_DIR/Applications" "$SCRIPTS_DIR"

# Copy app to Applications folder
echo -e "${YELLOW}Preparing app for packaging...${NC}"
cp -R "$APP_PATH" "$PACKAGE_DIR/Applications/"

# Create postinstall script
cat > "$SCRIPTS_DIR/postinstall" << 'POSTINSTALL_EOF'
#!/bin/bash
# Post-installation script
APP_PATH="/Applications/Noted.app"

# Make sure the app is executable
chmod +x "$APP_PATH/Contents/MacOS/Noted"

# Update the database of LaunchServices to recognize the app
/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "$APP_PATH"

# Set proper permissions
chown -R root:wheel "$APP_PATH"

exit 0
POSTINSTALL_EOF

chmod +x "$SCRIPTS_DIR/postinstall"

# Create preinstall script (optional)
cat > "$SCRIPTS_DIR/preinstall" << 'PREINSTALL_EOF'
#!/bin/bash
# Pre-installation script
# Add any pre-installation checks here

exit 0
PREINSTALL_EOF

chmod +x "$SCRIPTS_DIR/preinstall"

# Create the package
echo -e "${YELLOW}Building package...${NC}"
pkgbuild \
    --root "$PACKAGE_DIR" \
    --scripts "$SCRIPTS_DIR" \
    --identifier "$BUNDLE_ID" \
    --version "$VERSION" \
    --ownership recommended \
    "$OUTPUT_DIR/$PKG_NAME.pkg"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}Package created successfully!${NC}"
    echo -e "${GREEN}File: $OUTPUT_DIR/$PKG_NAME.pkg${NC}"
    echo -e "${GREEN}Size: $(du -h "$OUTPUT_DIR/$PKG_NAME.pkg" | cut -f1)${NC}"
    
    # Create checksum
    shasum -a 256 "$OUTPUT_DIR/$PKG_NAME.pkg" > "$OUTPUT_DIR/$PKG_NAME.pkg.sha256"
else
    echo -e "${RED}Package creation failed!${NC}"
    exit 1
fi

# Cleanup
rm -rf "$TEMP_DIR"

echo -e "${GREEN}Done!${NC}"
