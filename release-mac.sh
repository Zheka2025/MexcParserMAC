#!/bin/bash

VERSION="1.0"
APP_NAME="MexcSetupApp"

echo "📦 Creating Mac Release v$VERSION..."
echo "======================================"
echo ""

# Перевірка що на Mac
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ This script must run on macOS!"
    exit 1
fi

# Clean попереднього білда
echo "🧹 Cleaning previous builds..."
dotnet clean -c Release
echo ""

# Publish
echo "🔨 Building Release version..."
dotnet publish -f net9.0-maccatalyst -c Release -p:CreatePackage=false

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo ""
echo "✅ Build complete!"
echo ""

# Шлях до app
APP_PATH="bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app"

if [ ! -d "$APP_PATH" ]; then
    echo "❌ App not found at: $APP_PATH"
    exit 1
fi

# Отримати розмір
APP_SIZE=$(du -sh "$APP_PATH" | cut -f1)
echo "📏 App size: $APP_SIZE"
echo ""

# Створити ZIP
echo "📦 Creating ZIP archive..."
cd bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/
zip -r -q "$APP_NAME-Mac-v$VERSION.zip" MexcSetupApp.Maui.app
ZIP_SIZE=$(du -sh "$APP_NAME-Mac-v$VERSION.zip" | cut -f1)
mv "$APP_NAME-Mac-v$VERSION.zip" ../../../../../
cd ../../../../../
echo "✅ ZIP created: $APP_NAME-Mac-v$VERSION.zip ($ZIP_SIZE)"
echo ""

# Створити DMG (якщо є create-dmg)
if command -v create-dmg &> /dev/null; then
    echo "📀 Creating DMG installer..."
    
    create-dmg \
      --volname "MEXC Setup & Parser" \
      --window-pos 200 120 \
      --window-size 800 400 \
      --icon-size 100 \
      --icon "MexcSetupApp.Maui.app" 200 190 \
      --hide-extension "MexcSetupApp.Maui.app" \
      --app-drop-link 600 185 \
      --no-internet-enable \
      "$APP_NAME-Mac-v$VERSION.dmg" \
      "bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/" \
      2>/dev/null
    
    if [ -f "$APP_NAME-Mac-v$VERSION.dmg" ]; then
        DMG_SIZE=$(du -sh "$APP_NAME-Mac-v$VERSION.dmg" | cut -f1)
        echo "✅ DMG created: $APP_NAME-Mac-v$VERSION.dmg ($DMG_SIZE)"
    fi
else
    echo "ℹ️  'create-dmg' not found. Install it for DMG creation:"
    echo "   brew install create-dmg"
    echo ""
    echo "📦 Creating simple DMG without UI..."
    
    # Простий DMG без красивого UI
    hdiutil create -volname "MEXC Setup v$VERSION" \
      -srcfolder "bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app" \
      -ov -format UDZO "$APP_NAME-Mac-v$VERSION.dmg" > /dev/null
    
    if [ -f "$APP_NAME-Mac-v$VERSION.dmg" ]; then
        DMG_SIZE=$(du -sh "$APP_NAME-Mac-v$VERSION.dmg" | cut -f1)
        echo "✅ Simple DMG created: $APP_NAME-Mac-v$VERSION.dmg ($DMG_SIZE)"
    fi
fi

echo ""
echo "================================================"
echo "🎉 Release files ready for distribution!"
echo "================================================"
echo ""
echo "📁 Files created:"
ls -lh "$APP_NAME-Mac-v$VERSION".* 2>/dev/null | awk '{print "   " $9 " - " $5}'
echo ""
echo "📤 How to distribute:"
echo "   1. Upload to Google Drive / Dropbox"
echo "   2. Create GitHub Release"
echo "   3. Share via Telegram"
echo "   4. Host on your website"
echo ""
echo "📝 Installation instructions for users:"
echo "   - ZIP: Extract and drag to Applications folder"
echo "   - DMG: Open and drag to Applications folder"
echo ""
echo "⚠️  Users may need to allow app in Security settings!"
echo ""

