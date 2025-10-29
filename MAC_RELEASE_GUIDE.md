# 🚀 Mac Release Guide - Як дати програму іншим юзерам

## 📦 Етап 1: Створення Release Build

### На Mac виконай:

```bash
cd /path/to/MexcSetupApp.Maui

# Release білд для Mac (універсальний - x64 + ARM64)
dotnet publish -f net9.0-maccatalyst -c Release -p:CreatePackage=false

# Готовий .app буде тут:
# bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app
```

**Розмір:** ~80-150 MB

---

## 📂 Етап 2: Що робити з .app файлом

### Варіант A: Просто ZIP (найпростіше) ✅

```bash
cd bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/

# Створити ZIP
zip -r MexcSetupApp-Mac.zip MexcSetupApp.Maui.app

# Тепер можна відправляти MexcSetupApp-Mac.zip
```

**Юзери роблять:**
1. Завантажують ZIP
2. Розпаковують (подвійний клік)
3. Перетягують `MexcSetupApp.Maui.app` в папку **Applications**
4. Запускають

### ⚠️ ПРОБЛЕМА: macOS Security

При першому запуску macOS скаже: **"Cannot open app from unidentified developer"**

**Рішення для юзерів:**
1. Правий клік на app → **"Open"**
2. Або: **System Settings → Privacy & Security → "Open Anyway"**

---

## 🔐 Етап 3: Code Signing (опціонально, але краще)

### Що це:
Підпис від Apple - юзери не побачать попередження security.

### Вимоги:
- ❗ **Apple Developer Account** ($99/рік)
- Mac з Xcode
- Developer Certificate

### Як підписати:

```bash
# 1. Отримай свій signing identity
security find-identity -v -p codesigning

# 2. Підпиши app
codesign --deep --force --verify --verbose \
  --sign "Developer ID Application: Your Name (TEAMID)" \
  bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app

# 3. Перевір підпис
codesign --verify --deep --strict --verbose=2 \
  bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app
```

**З підписом:**
- ✅ Юзери одразу можуть запустити
- ✅ Не треба "Open Anyway"
- ✅ Виглядає професійно

**Без підпису:**
- ⚠️ Попередження при першому запуску
- ✅ Але працює після "Open Anyway"

---

## 📀 Етап 4: Створення DMG інсталятора (як професіонали)

### Що це:
`.dmg` - стандартний інсталятор для Mac (як `.exe` для Windows).

### Спосіб 1: Вручну (простіше)

```bash
# 1. Створи DMG з папкою
hdiutil create -volname "MEXC Setup" \
  -srcfolder bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app \
  -ov -format UDZO MexcSetupApp-Mac-v1.0.dmg

# 2. Готово! Тепер є MexcSetupApp-Mac-v1.0.dmg
```

**Юзери роблять:**
1. Подвійний клік на `.dmg`
2. Перетягують app в папку Applications
3. Закривають DMG
4. Запускають з Applications

### Спосіб 2: З красивим UI (складніше)

Використати інструмент `create-dmg`:

```bash
# Встановити
brew install create-dmg

# Створити красивий DMG
create-dmg \
  --volname "MEXC Setup & Parser" \
  --window-pos 200 120 \
  --window-size 800 400 \
  --icon-size 100 \
  --icon "MexcSetupApp.Maui.app" 200 190 \
  --hide-extension "MexcSetupApp.Maui.app" \
  --app-drop-link 600 185 \
  "MexcSetupApp-Mac-v1.0.dmg" \
  "bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/"
```

Отримаєш красивий DMG з іконками та стрілкою "перетягни сюди".

---

## 🌐 Етап 5: Розповсюдження

### Куди завантажити:

1. **Google Drive / Dropbox**
   - Завантаж DMG або ZIP
   - Дай посилання юзерам
   
2. **GitHub Releases**
   ```bash
   # Створи release на GitHub
   gh release create v1.0 MexcSetupApp-Mac-v1.0.dmg
   ```
   
3. **Telegram канал**
   - Відправ DMG/ZIP файл
   - Юзери завантажують напряму

4. **Власний сайт**
   - Hosted де завгодно
   - Кнопка "Download for Mac"

---

## ⚡ Автоматичний Release скрипт

Створю для тебе скрипт який все робить автоматично!

```bash
#!/bin/bash
# release-mac.sh

VERSION="1.0"
APP_NAME="MexcSetupApp"

echo "📦 Creating Mac Release v$VERSION..."

# 1. Clean build
dotnet clean
dotnet publish -f net9.0-maccatalyst -c Release

# 2. Create ZIP
cd bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/
zip -r "$APP_NAME-Mac-v$VERSION.zip" MexcSetupApp.Maui.app
mv "$APP_NAME-Mac-v$VERSION.zip" ../../../../../

# 3. Create DMG (if create-dmg installed)
cd ../../../../../
if command -v create-dmg &> /dev/null; then
    create-dmg \
      --volname "$APP_NAME v$VERSION" \
      --window-size 800 400 \
      --icon-size 100 \
      --app-drop-link 600 185 \
      "$APP_NAME-Mac-v$VERSION.dmg" \
      "bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/"
fi

echo "✅ Release files created:"
ls -lh *-Mac-v*.{zip,dmg} 2>/dev/null

echo ""
echo "📤 Ready to distribute!"
```

---

## 📊 Порівняння варіантів:

| Метод | Розмір | Складність | Професійність |
|-------|--------|------------|---------------|
| **ZIP** | ~80 MB | Легко ⭐ | ⭐⭐ |
| **ZIP + Code Sign** | ~80 MB | Середньо ⭐⭐ | ⭐⭐⭐⭐ |
| **DMG простий** | ~80 MB | Середньо ⭐⭐ | ⭐⭐⭐ |
| **DMG + UI** | ~80 MB | Складно ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **DMG + Sign** | ~80 MB | Складно ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |

---

## 🎯 Моя рекомендація:

### Для початку (без Apple Developer):
**→ Просто ZIP файл**
- Швидко
- Працює
- Юзери просто розпаковують і перетягують в Applications

### Для серйозного релізу:
**→ DMG + Code Signing**
- Професійно
- Безпечно
- Юзери довіряють більше

---

## 💰 Вартість:

- **ZIP без підпису:** безкоштовно ✅
- **Code Signing:** $99/рік (Apple Developer Program)
- **Notarization:** включено в Apple Developer

---

## 🔥 Що я можу зробити ЗАРАЗ:

1. **Створити release-mac.sh** - автоматичний скрипт
2. **Інструкцію для юзерів** - як встановити твій app
3. **Створити .pkg installer** - альтернатива DMG

**Створити release скрипт?** 🤔




