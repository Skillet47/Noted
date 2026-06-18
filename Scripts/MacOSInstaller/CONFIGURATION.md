# Installer Configuration

This file contains customizable settings for the macOS installer build process.

## Application Settings

```bash
# Application name and version
APP_NAME="Noted"
BUNDLE_ID="com.companyname.noted"
VERSION="1.0.0"

# Build directories
BUILD_DIR="build"
OUTPUT_DIR="dist"

# Architecture (x64, arm64, or universal)
ARCHITECTURE="x64"
```

## Update These Values

### 1. Company Name

Replace `companyname` with your actual company name:

In [../Noted/Noted.csproj](../Noted/Noted.csproj):
```xml
<ApplicationId>com.yourcompany.noted</ApplicationId>
```

### 2. Version Number

Update version for each release:

In [../Noted/Noted.csproj](../Noted/Noted.csproj):
```xml
<ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
<ApplicationVersion>1</ApplicationVersion>
```

Then update in build scripts:
```bash
VERSION="1.0.0"
```

### 3. Code Signing (Required for distribution)

For production builds, you need:

**Apple Developer Program**
- Enroll at [developer.apple.com](https://developer.apple.com)
- Download Developer ID Certificate
- Cost: $99/year

**Get Team ID**
- Sign in to [developer.apple.com/account](https://developer.apple.com/account)
- Account → Membership
- Copy your Team ID (looks like: `ABC123DEFG`)

**Create App Password**
1. Go to [appleid.apple.com](https://appleid.apple.com)
2. Sign in
3. Security → App-specific passwords → Generate
4. Label it "Noted macOS Notarization"
5. Use the generated password in the notarization script

### 4. macOS Minimum Version

In [../Noted/Noted.csproj](../Noted/Noted.csproj):
```xml
<SupportedOSPlatformVersion Condition="...">15.0</SupportedOSPlatformVersion>
```

Current: macOS 15.0 (Sequoia). Adjust if you need to support older versions.

## Build Profiles

### Development Build
```bash
# Quick build for testing
./Scripts/MacOSInstaller/build-macos.sh x64
```

### Release Build
```bash
# Universal binary for all Macs
./Scripts/MacOSInstaller/build-universal.sh
```

### Distribution Build (with notarization)
```bash
# Full release with Apple approval
./Scripts/MacOSInstaller/build-universal.sh
./Scripts/MacOSInstaller/codesign-and-notarize.sh build/maccatalyst-x64/Noted.app ABC123DEFG your-email@example.com "app-password"
```

## Output Locations

- Development: `build/maccatalyst-x64/Noted.app`
- Release: `dist/Noted-1.0.0.dmg`
- Checksums: `dist/Noted-1.0.0.dmg.sha256`

## Customization Checklist

- [ ] Update `BUNDLE_ID` to your company domain
- [ ] Update `VERSION` for each release
- [ ] Create/update app icon (1024x1024 PNG minimum)
- [ ] Create custom DMG background (1024x768 PNG)
- [ ] Test build on both Intel and Apple Silicon Mac
- [ ] Obtain Apple Developer Program membership
- [ ] Get Team ID from Apple Developer account
- [ ] Create app-specific password for notarization
- [ ] Test DMG installer on clean Mac
- [ ] Test PKG installer if using that format
- [ ] Create release notes
- [ ] Upload to distribution channel

## Architecture Support

### macOS Versions Supported

- **Intel Macs (x64)**: macOS 10.15+
- **Apple Silicon (arm64)**: macOS 11.0+
- **Universal Binary**: Both

### Building for Specific Architectures

```bash
# Intel Macs only
./Scripts/MacOSInstaller/build-macos.sh x64

# Apple Silicon only
./Scripts/MacOSInstaller/build-macos.sh arm64

# Both architectures (creates one installer)
./Scripts/MacOSInstaller/build-universal.sh
```

## CI/CD Integration

For automated builds in GitHub Actions, Jenkins, etc.:

```bash
#!/bin/bash
set -e

# Build
# Restore dependencies before building
dotnet restore Noted.slnx --nologo

dotnet publish -f net10.0-maccatalyst -c Release \
    -p:RuntimeIdentifier=maccatalyst-x64 \
    -p:ApplicationVersion=1 \
    -p:ApplicationDisplayVersion=1.0.0 \
    -o build/maccatalyst-x64 \
    ./Noted/Noted.csproj

# Create DMG
./Scripts/MacOSInstaller/create-dmg.sh build/maccatalyst-x64/Noted.app 1.0.0 dist

# (Optional) Sign and notarize
if [ ! -z "$APPLE_ID" ]; then
    ./Scripts/MacOSInstaller/codesign-and-notarize.sh \
        build/maccatalyst-x64/Noted.app \
        $TEAM_ID \
        $APPLE_ID \
        $APP_PASSWORD
fi

# Upload artifacts
echo "Build artifacts in: dist/"
```

## Common Issues

**"Command not found" when running scripts**
```bash
chmod +x Scripts/MacOSInstaller/*.sh
```

**"No matching developer certificate" during signing**
```bash
# List your certificates
security find-identity -v -p codesigning
# Install the "Developer ID Application" certificate from developer.apple.com
```

**DMG creation fails**
```bash
# Unmount any existing volume
hdiutil detach /Volumes/Noted-*
# Then retry
./Scripts/MacOSInstaller/create-dmg.sh
```

## Support

For more information:
- [MAUI macOS Documentation](https://learn.microsoft.com/en-us/dotnet/maui/deployment/macos/)
- [Apple Code Signing](https://developer.apple.com/support/code-signing/)
- [Apple Notarization](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution/)
