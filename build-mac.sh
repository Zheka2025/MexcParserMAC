#!/bin/bash

echo "🍎 MEXC Setup App - Mac Build Script"
echo "======================================"
echo ""

# Перевірка .NET
echo "📌 Checking .NET installation..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET not found!"
    echo "Installing .NET via Homebrew..."
    
    if ! command -v brew &> /dev/null; then
        echo "Installing Homebrew first..."
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    fi
    
    brew install dotnet
fi

dotnet --version
echo ""

# Перевірка MAUI workload
echo "📌 Checking MAUI workload..."
if ! dotnet workload list | grep -q "maui"; then
    echo "Installing MAUI workload..."
    dotnet workload install maui
fi
echo ""

# Очистка попереднього білда
echo "🧹 Cleaning previous build..."
dotnet clean
echo ""

# Restore залежностей
echo "📦 Restoring dependencies..."
dotnet restore
echo ""

# Білд
echo "🔨 Building for macOS..."
dotnet build -f net9.0-maccatalyst -c Release
echo ""

# Перевірка успіху
if [ $? -eq 0 ]; then
    echo "✅ Build SUCCESS!"
    echo ""
    echo "📍 App location:"
    echo "   bin/Release/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app"
    echo ""
    echo "🚀 To run the app:"
    echo "   open bin/Release/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app"
    echo ""
    echo "Or run now? (y/n)"
    read -r response
    if [[ "$response" =~ ^[Yy]$ ]]; then
        echo "🚀 Launching app..."
        open bin/Release/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app
    fi
else
    echo "❌ Build FAILED!"
    echo "Check errors above and try again."
    exit 1
fi




