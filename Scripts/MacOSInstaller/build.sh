#!/bin/bash

# Main Build Orchestrator for macOS Noted App
# This script guides you through the entire build process

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"

# Make all scripts executable
chmod +x "$SCRIPT_DIR"/*.sh

# Banner
echo -e "${BLUE}"
echo "╔════════════════════════════════════════════════════════════╗"
echo "║          Noted macOS Installer Build Orchestrator          ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo -e "${NC}"

# Menu
show_menu() {
    echo ""
    echo -e "${YELLOW}Select build option:${NC}"
    echo ""
    echo "  1) Quick DMG Installer (Intel 64-bit)"
    echo "  2) Quick DMG Installer (Apple Silicon)"
    echo "  3) Universal DMG Installer (Intel + Apple Silicon)"
    echo "  4) Create PKG Installer"
    echo "  5) Code Sign & Notarize (for distribution)"
    echo "  6) Clean build artifacts"
    echo "  7) View configuration"
    echo "  8) Exit"
    echo ""
    read -p "Enter choice (1-8): " choice
}

# Function to build DMG for specific architecture
build_dmg() {
    local arch=$1
    echo ""
    echo -e "${YELLOW}Building DMG installer for $arch...${NC}"
    
    # Make sure we're in the project root
    cd "$PROJECT_ROOT"

    # Ensure dependencies and assets are restored before building
    echo -e "${YELLOW}Running dotnet restore for solution...${NC}"
    dotnet restore "$PROJECT_ROOT/Noted.slnx" --nologo
    
    "$SCRIPT_DIR/build-macos.sh" "$arch"
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ DMG created successfully!${NC}"
        ls -lh dist/Noted-*.dmg 2>/dev/null || true
    else
        echo -e "${RED}✗ DMG creation failed!${NC}"
        exit 1
    fi
}

# Function to build universal binary
build_universal() {
    echo ""
    echo -e "${YELLOW}Building universal DMG installer...${NC}"
    
    cd "$PROJECT_ROOT"

    # Ensure dependencies and assets are restored before building
    echo -e "${YELLOW}Running dotnet restore for solution...${NC}"
    dotnet restore "$PROJECT_ROOT/Noted.slnx" --nologo

    "$SCRIPT_DIR/build-universal.sh"
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Universal DMG created successfully!${NC}"
        ls -lh dist/Noted-*.dmg 2>/dev/null || true
    else
        echo -e "${RED}✗ Universal build failed!${NC}"
        exit 1
    fi
}

# Function to create PKG
create_pkg() {
    echo ""
    echo -e "${YELLOW}Creating PKG installer...${NC}"
    
    # First, check if we need to build the app
    if [ ! -f "$PROJECT_ROOT/build/maccatalyst-x64/Noted.app/Contents/MacOS/Noted" ]; then
        echo -e "${YELLOW}Restoring dependencies before build...${NC}"
        dotnet restore "$PROJECT_ROOT/Noted.slnx" --nologo
        echo -e "${YELLOW}Building app first...${NC}"
        "$SCRIPT_DIR/build-macos.sh" x64
    fi
    
    cd "$PROJECT_ROOT"
    
    "$SCRIPT_DIR/create-pkg-installer.sh" \
        "build/maccatalyst-x64/Noted.app" \
        "1.0.0" \
        "dist"
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ PKG created successfully!${NC}"
        ls -lh dist/Noted-*.pkg 2>/dev/null || true
    else
        echo -e "${RED}✗ PKG creation failed!${NC}"
        exit 1
    fi
}

# Function to sign and notarize
sign_and_notarize() {
    echo ""
    echo -e "${YELLOW}Code Signing and Notarization Setup${NC}"
    echo ""
    echo "Before proceeding, you need:"
    echo "  • Apple Developer Program membership"
    echo "  • Developer ID Certificate installed"
    echo "  • Team ID (from developer.apple.com)"
    echo "  • Apple ID email"
    echo "  • App-specific password (from appleid.apple.com)"
    echo ""
    
    read -p "Do you have all prerequisites? (y/n): " has_prereqs
    if [[ ! "$has_prereqs" =~ ^[Yy]$ ]]; then
        echo "Please set up prerequisites first. See README.md for instructions."
        return
    fi
    
    # Get information
    read -p "Enter your Team ID (e.g., ABC123DEFG): " team_id
    read -p "Enter your Apple ID email: " apple_id
    read -sp "Enter app-specific password: " app_password
    echo ""
    
    if [ -z "$team_id" ] || [ -z "$apple_id" ] || [ -z "$app_password" ]; then
        echo -e "${RED}Missing required information!${NC}"
        return
    fi
    
    # Get app path
    APP_PATH="build/maccatalyst-x64/Noted.app"
    if [ ! -d "$PROJECT_ROOT/$APP_PATH" ]; then
        echo -e "${YELLOW}Restoring dependencies before build...${NC}"
        dotnet restore "$PROJECT_ROOT/Noted.slnx" --nologo
        echo -e "${YELLOW}Building app first...${NC}"
        "$SCRIPT_DIR/build-macos.sh" x64
    fi
    
    cd "$PROJECT_ROOT"
    
    echo -e "${YELLOW}Starting code signing and notarization...${NC}"
    "$SCRIPT_DIR/codesign-and-notarize.sh" \
        "$APP_PATH" \
        "$team_id" \
        "$apple_id" \
        "$app_password"
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Code signing and notarization completed!${NC}"
    else
        echo -e "${RED}✗ Code signing and notarization failed!${NC}"
        exit 1
    fi
}

# Function to clean artifacts
clean_build() {
    echo ""
    echo -e "${YELLOW}Cleaning build artifacts...${NC}"
    
    cd "$PROJECT_ROOT"
    
    read -p "Are you sure? This will delete build/ and dist/ directories (y/n): " confirm
    if [[ "$confirm" =~ ^[Yy]$ ]]; then
        rm -rf build dist
        echo -e "${YELLOW}Running dotnet restore before clean...${NC}"
        dotnet restore "$PROJECT_ROOT/Noted.slnx" --nologo || true
        dotnet clean --nologo 2>/dev/null || true
        echo -e "${GREEN}✓ Clean complete!${NC}"
    else
        echo "Cancelled."
    fi
}

# Function to show configuration
show_config() {
    echo ""
    echo -e "${BLUE}=== Configuration ===${NC}"
    echo ""
    echo "Script Directory: $SCRIPT_DIR"
    echo "Project Root: $PROJECT_ROOT"
    echo ""
    
    if [ -f "$SCRIPT_DIR/CONFIGURATION.md" ]; then
        echo "Configuration File: $SCRIPT_DIR/CONFIGURATION.md"
        echo ""
        echo "Key settings:"
        echo "  • Bundle ID: com.companyname.noted"
        echo "  • Minimum macOS: 15.0"
        echo "  • Supported architectures: x64, arm64"
        echo ""
        echo "To customize, edit:"
        echo "  • $SCRIPT_DIR/CONFIGURATION.md"
        echo "  • $PROJECT_ROOT/Noted/Noted.csproj"
    fi
}

# Main loop
while true; do
    show_menu
    
    case $choice in
        1)
            build_dmg "x64"
            ;;
        2)
            build_dmg "arm64"
            ;;
        3)
            build_universal
            ;;
        4)
            create_pkg
            ;;
        5)
            sign_and_notarize
            ;;
        6)
            clean_build
            ;;
        7)
            show_config
            ;;
        8)
            echo ""
            echo -e "${GREEN}Thank you for using Noted macOS Installer Builder!${NC}"
            echo ""
            exit 0
            ;;
        *)
            echo -e "${RED}Invalid choice!${NC}"
            ;;
    esac
done
