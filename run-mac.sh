#!/bin/bash

echo "🚀 Launching MEXC Setup App..."

# Перевірка чи є збілджена версія
if [ -d "bin/Debug/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app" ]; then
    echo "✅ Found Debug build"
    open bin/Debug/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app
elif [ -d "bin/Release/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app" ]; then
    echo "✅ Found Release build"
    open bin/Release/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app
else
    echo "❌ App not found. Building first..."
    dotnet build -f net9.0-maccatalyst
    
    if [ $? -eq 0 ]; then
        echo "✅ Build complete! Launching..."
        open bin/Debug/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app
    else
        echo "❌ Build failed. Run ./build-mac.sh for details."
        exit 1
    fi
fi


