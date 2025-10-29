#!/bin/bash

# MEXC Setup App - Automatic Release Builder for macOS
# Usage: ./create-release.sh [version]

VERSION="${1:-1.0}"
APP_NAME="MexcSetupApp"
OUTPUT_DIR="releases"

echo ""
echo "╔══════════════════════════════════════════════╗"
echo "║   MEXC Setup App - Release Builder v$VERSION    ║"
echo "╚══════════════════════════════════════════════╝"
echo ""

# Перевірка що на Mac
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ This script must run on macOS!"
    exit 1
fi

# Створити output папку
mkdir -p "$OUTPUT_DIR"

# 1. CLEAN
echo "🧹 Step 1/5: Cleaning previous builds..."
dotnet clean -c Release > /dev/null 2>&1
rm -rf bin/Release
rm -rf obj/Release
echo "   ✅ Clean complete"
echo ""

# 2. RESTORE
echo "📦 Step 2/5: Restoring dependencies..."
dotnet restore > /dev/null 2>&1
echo "   ✅ Dependencies restored"
echo ""

# 3. BUILD
echo "🔨 Step 3/5: Building Release for macOS..."
dotnet publish -f net9.0-maccatalyst -c Release -p:CreatePackage=false

if [ $? -ne 0 ]; then
    echo "   ❌ Build failed! Check errors above."
    exit 1
fi
echo "   ✅ Build complete"
echo ""

APP_PATH="bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app"

if [ ! -d "$APP_PATH" ]; then
    echo "❌ App not found at: $APP_PATH"
    exit 1
fi

APP_SIZE=$(du -sh "$APP_PATH" | cut -f1)
echo "   📏 App size: $APP_SIZE"
echo ""

# 4. CREATE ZIP
echo "📦 Step 4/5: Creating ZIP archive..."
cd bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/
zip -r -q "$APP_NAME-Mac-v$VERSION.zip" MexcSetupApp.Maui.app
ZIP_PATH="../../../../../$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.zip"
mkdir -p "../../../../../$OUTPUT_DIR"
mv "$APP_NAME-Mac-v$VERSION.zip" "$ZIP_PATH"
cd ../../../../../

ZIP_SIZE=$(du -sh "$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.zip" | cut -f1)
echo "   ✅ ZIP created ($ZIP_SIZE)"
echo ""

# 5. CREATE DMG
echo "📀 Step 5/5: Creating DMG installer..."

if command -v create-dmg &> /dev/null; then
    # Красивий DMG з UI
    create-dmg \
      --volname "MEXC Setup & Parser v$VERSION" \
      --window-pos 200 120 \
      --window-size 800 400 \
      --icon-size 100 \
      --icon "MexcSetupApp.Maui.app" 200 190 \
      --hide-extension "MexcSetupApp.Maui.app" \
      --app-drop-link 600 185 \
      --no-internet-enable \
      "$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.dmg" \
      "bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/" \
      > /dev/null 2>&1
    
    if [ -f "$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.dmg" ]; then
        DMG_SIZE=$(du -sh "$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.dmg" | cut -f1)
        echo "   ✅ DMG created ($DMG_SIZE) with UI"
    fi
else
    # Простий DMG
    echo "   ℹ️  'create-dmg' not found. Creating simple DMG..."
    echo "   To get fancy DMG: brew install create-dmg"
    
    hdiutil create \
      -volname "MEXC Setup v$VERSION" \
      -srcfolder "bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app" \
      -ov -format UDZO \
      "$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.dmg" \
      > /dev/null 2>&1
    
    if [ -f "$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.dmg" ]; then
        DMG_SIZE=$(du -sh "$OUTPUT_DIR/$APP_NAME-Mac-v$VERSION.dmg" | cut -f1)
        echo "   ✅ DMG created ($DMG_SIZE)"
    fi
fi

echo ""
echo "╔══════════════════════════════════════════════╗"
echo "║           🎉 RELEASE COMPLETE! 🎉           ║"
echo "╚══════════════════════════════════════════════╝"
echo ""
echo "📁 Release files location: ./$OUTPUT_DIR/"
echo ""
ls -lh "$OUTPUT_DIR"/"$APP_NAME-Mac-v$VERSION".* 2>/dev/null | while read -r line; do
    SIZE=$(echo "$line" | awk '{print $5}')
    NAME=$(echo "$line" | awk '{print $NF}')
    EXT="${NAME##*.}"
    
    case $EXT in
        zip)
            echo "   📦 $(basename "$NAME") - $SIZE"
            echo "      → For: Direct download, easy sharing"
            ;;
        dmg)
            echo "   📀 $(basename "$NAME") - $SIZE"
            echo "      → For: Professional distribution, App Store-like"
            ;;
    esac
done

echo ""
echo "📤 Distribution options:"
echo "   1. Upload to cloud (Drive, Dropbox)"
echo "   2. GitHub Releases: gh release create v$VERSION $OUTPUT_DIR/*"
echo "   3. Share via Telegram channel"
echo "   4. Host on website"
echo ""
echo "👥 Installation for users:"
echo "   - ZIP: Extract → Drag to Applications"
echo "   - DMG: Open → Drag to Applications"
echo ""
echo "⚠️  IMPORTANT: Users will need to allow app in Security settings"
echo "   (Right-click → Open, or System Settings → Privacy & Security)"
echo ""
echo "📝 Share 'USER_INSTALL_INSTRUCTIONS.md' with users!"
echo ""




