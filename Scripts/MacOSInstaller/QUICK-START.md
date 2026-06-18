# Quick Start: Building your macOS Installer

## For Beginners - Start Here! 🚀

### 1. Simple DMG Installer (5 minutes)

Open Terminal and run:

```bash
cd ~/Developer/Repos/Noted
./Scripts/MacOSInstaller/build.sh
```

Select option `1` or `3`:
- **Option 1**: DMG for Intel Macs
- **Option 3**: Universal DMG (works on both Intel & Apple Silicon)

Your installer will be in `dist/Noted-1.0.0.dmg`

---

## Step-by-Step Guide

### Option A: GUI Builder (Easiest)

```bash
./Scripts/MacOSInstaller/build.sh
```

Follow the interactive menu:
1. Choose build type
2. Let it build automatically
3. Find your DMG in `dist/` folder

### Option B: Command Line (Fastest)

**For Intel Macs:**
```bash
./Scripts/MacOSInstaller/build-macos.sh x64
```

**For Apple Silicon Macs:**
```bash
./Scripts/MacOSInstaller/build-macos.sh arm64
```

**For Both (Universal):**
```bash
./Scripts/MacOSInstaller/build-universal.sh
```

---

## What You Get

### Default Build Output

```
dist/
├── Noted-1.0.0.dmg           ← Double-click to install
└── Noted-1.0.0.dmg.sha256    ← For verification
```

### How Users Install

1. Download `Noted-1.0.0.dmg`
2. Double-click to open
3. Drag "Noted" app to "Applications" folder
4. Done! 🎉

---

## Customization

### Change App Icon

Replace this file with your 1024x1024 PNG:
```
Noted/Resources/AppIcon/appicon.png
```

### Change Company Name

Edit [Noted/Noted.csproj](../Noted/Noted.csproj), find:
```xml
<ApplicationId>com.companyname.noted</ApplicationId>
```

Change `companyname` to your company name.

### Custom DMG Background

Create a 1024x768 PNG and save as:
```
Assets/dmg-background.png
```

The installer will use it automatically.

---

## Next Steps

After creating your installer:

1. **Test It** - Double-click the DMG and try installing
2. **Sign It** (optional) - For distribution:
   ```
   ./Scripts/MacOSInstaller/build.sh
   Select option 5: Code Sign & Notarize
   ```
3. **Share It** - Upload to GitHub, your website, etc.

---

## Troubleshooting

### "Permission Denied" Error
```bash
chmod +x Scripts/MacOSInstaller/*.sh
```

### Build Fails
```bash
# Clean and rebuild
rm -rf build dist
# Restore dependencies before rebuilding
dotnet restore Noted.slnx --nologo
./Scripts/MacOSInstaller/build.sh
```

### Can't Find Notarization Option
You need:
- Apple Developer account ($99/year)
- Developer ID Certificate
 - See [README.md](README.md) for details

---

## File Reference

| Script | Purpose |
|--------|---------|
| `build.sh` | Interactive menu - start here! |
| `build-macos.sh` | Build DMG for one architecture |
| `build-universal.sh` | Build DMG for all Macs |
| `create-dmg.sh` | Create DMG from built app |
| `create-pkg-installer.sh` | Create traditional PKG installer |
| `codesign-and-notarize.sh` | Sign & notarize for distribution |

---

## Documentation

- **[README.md](README.md)** - Full detailed guide
- **[CONFIGURATION.md](CONFIGURATION.md)** - Advanced settings
- **[../../Noted/Noted.csproj](../Noted/Noted.csproj)** - Project settings

---

## Support

If you're stuck:

1. Check the **[README.md](README.md)** troubleshooting section
2. View the full docs in **[CONFIGURATION.md](CONFIGURATION.md)**
3. Run: `./Scripts/MacOSInstaller/build.sh` → option 7 (View configuration)

---

Happy building! 🎉
