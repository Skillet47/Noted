# macOS Installer Guide for Noted

This guide explains how to build and create macOS installers for the Noted application.

## Prerequisites

- macOS 13.0 or later
- Xcode Command Line Tools: `xcode-select --install`
- .NET 10.0 SDK (or latest): `dotnet --version`
- Bash shell

### Verify Prerequisites

```bash
# Check Xcode tools
xcode-select -p

# Check .NET SDK
dotnet --version

# Check macOS version
sw_vers
```

## Build Options

### 1. Simple DMG Installer (Recommended for most users)

**Best for:** Direct distribution, simple installation process

```bash
chmod +x Scripts/MacOSInstaller/build-macos.sh
./Scripts/MacOSInstaller/build-macos.sh x64
# or for Apple Silicon:
# ./Scripts/MacOSInstaller/build-macos.sh arm64
```

**Output:** `dist/Noted-1.0.0.dmg`

**Installation:** Users double-click the DMG, drag the app to Applications folder.

### 2. Universal Binary (Single installer for all Macs)

**Best for:** Maximum compatibility, supports both Intel and Apple Silicon

```bash
chmod +x Scripts/MacOSInstaller/build-universal.sh
./Scripts/MacOSInstaller/build-universal.sh
```

**Output:** `dist/Noted-1.0.0.dmg` (universal binary)

### 3. PKG Installer

**Best for:** Automated deployments, software management systems

```bash
chmod +x Scripts/create-pkg-installer.sh

# First build the app:
# Ensure dependencies are restored before publishing
dotnet restore Noted.slnx --nologo

dotnet publish -f net10.0-maccatalyst -c Release -p:RuntimeIdentifier=maccatalyst-x64 \
    -p:ApplicationVersion=1 -p:ApplicationDisplayVersion=1.0.0 -o build/maccatalyst-x64 ./Noted/Noted.csproj

# Then create the PKG:
./Scripts/create-pkg-installer.sh build/maccatalyst-x64/Noted.app 1.0.0 dist
```

**Output:** `dist/Noted-1.0.0.pkg`

**Installation:** Users can install via double-click or command line:
```bash
sudo installer -pkg Noted-1.0.0.pkg -target /
```

## Code Signing and Notarization

For distribution outside the Mac App Store, your app must be notarized by Apple.

### Prerequisites for Signing

1. **Apple Developer Account** with enrollment in the Developer Program
2. **Developer ID Certificate** (not App Store)
3. **App-specific Password** from [appleid.apple.com](https://appleid.apple.com)

### Get Your Information

1. Find your **Team ID**: [developer.apple.com/account/#!/membership/](https://developer.apple.com/account/#!/membership/)
2. Create **App Password**:
   - Visit [appleid.apple.com](https://appleid.apple.com)
   - Sign in → Security → App-specific passwords → Generate

### Sign and Notarize

```bash
chmod +x Scripts/MacOSInstaller/codesign-and-notarize.sh

./Scripts/MacOSInstaller/codesign-and-notarize.sh \
    build/maccatalyst-x64/Noted.app \
    ABC123DEFG \
    your-apple-id@example.com \
    "your-app-specific-password"
```

**Parameters:**
- `build/maccatalyst-x64/Noted.app` - Path to your built app
- `ABC123DEFG` - Your Apple Team ID
- `your-apple-id@example.com` - Your Apple ID email
- `your-app-specific-password` - App-specific password (not your Apple ID password)

### What Notarization Does

- **Validates** your code doesn't contain known malware
- **Verifies** code signing is correct
- **Staples** notarization ticket to your app
- **Allows** Gatekeeper to recognize your app as safe

## Customization

### Version Number

Update in [Noted/Noted.csproj](../Noted/Noted.csproj):

```xml
<ApplicationDisplayVersion>1.0.1</ApplicationDisplayVersion>
<ApplicationVersion>2</ApplicationVersion>
```

### App Icon

Update the app icon in:
- [Resources/AppIcon/appicon.png](../Noted/Resources/AppIcon/appicon.png)

The icon should be at least 1024x1024 pixels.

### DMG Background

Create a 1024x768 PNG image and save as:
```
Assets/dmg-background.png
```

The DMG creation script will automatically use this as the background.

### Bundle Identifier

Update in [Noted/Noted.csproj](../Noted/Noted.csproj):

```xml
<ApplicationId>com.yourcompany.noted</ApplicationId>
```

Update in [Noted/Platforms/MacCatalyst/Entitlements.plist](../Noted/Platforms/MacCatalyst/Entitlements.plist) if making any entitlement changes.

## Build Artifacts

### Generated Files

- `build/maccatalyst-{arch}/` - Intermediate build output
- `dist/Noted-{version}.dmg` - DMG installer
- `dist/Noted-{version}.dmg.sha256` - Checksum for verification
- `dist/Noted-{version}.pkg` - PKG installer (if created)

### Clean Build

```bash
rm -rf build dist
dotnet clean
```

## Troubleshooting

### Build Fails with "Missing Entitlements"

Make sure [Noted/Platforms/MacCatalyst/Entitlements.plist](../Noted/Platforms/MacCatalyst/Entitlements.plist) exists and is properly formatted.

### DMG Creation Fails

- Check that you have write permissions to the `dist` directory
- Ensure no other DMG with the same name is mounted
- Try: `hdiutil detach /Volumes/Noted-*` to unmount any existing DMG

### Code Signing Fails

```bash
# List your certificates
security find-identity -v -p codesigning

# Must see "Developer ID Application" certificate
```

### Notarization Times Out

The notarization can take several minutes. The script includes `--wait` flag but you can check status later:

```bash
xcrun notarytool log <submission-id> \
    --apple-id your-apple-id@example.com \
    --password "app-password" \
    --team-id ABC123DEFG
```

### App Won't Launch After Installation

1. Check permissions:
   ```bash
   ls -la /Applications/Noted.app/Contents/MacOS/
   ```

2. Check for quarantine attribute:
   ```bash
   xattr /Applications/Noted.app
   ```

3. Remove quarantine if present:
   ```bash
   xattr -d com.apple.quarantine /Applications/Noted.app
   ```

## Distribution

### On Your Website

1. Create release in GitHub or your website
2. Upload DMG and SHA256 checksum
3. Users can verify: `shasum -a 256 -c Noted-1.0.0.dmg.sha256`

### On Mac App Store

If distributing via Mac App Store:
1. Enroll in Developer Program
2. Use Xcode to create App Store build
3. Submit via App Store Connect

## Build Orchestration

For full pipeline with all steps:

```bash
# Quick start - single architecture DMG
./Scripts/MacOSInstaller/build-macos.sh x64

# Advanced - universal binary with code signing
./Scripts/MacOSInstaller/build-universal.sh
# Then sign and notarize:
./Scripts/MacOSInstaller/codesign-and-notarize.sh <app_path> <team_id> <apple_id> <app_password>
```

## Next Steps

1. **Customize** version, icons, and branding
2. **Test** the installer on your Mac
3. **Sign and Notarize** for distribution
4. **Create** release notes
5. **Distribute** via your preferred channel

## Resources

- [Microsoft MAUI macOS Deployment](https://learn.microsoft.com/dotnet/maui/deployment/macos/)
- [Apple Code Signing Guide](https://developer.apple.com/support/code-signing/)
- [Apple Notarization Guide](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)
- [DMG Creation Best Practices](https://el.media.mit.edu/bitcasts/dmg/)
