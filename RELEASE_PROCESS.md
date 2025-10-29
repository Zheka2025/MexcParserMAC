# 🎯 RELEASE PROCESS - Покрокова інструкція

## 📋 Що треба зробити щоб дати програму юзерам Mac:

---

## ✅ КРОК 1: Білд на Mac (обов'язково)

**На Mac комп'ютері виконай:**

```bash
# Автоматичний спосіб (рекомендується):
cd /path/to/MexcSetupApp.Maui
chmod +x create-release.sh
./create-release.sh 1.0
```

**АБО вручну:**
```bash
dotnet publish -f net9.0-maccatalyst -c Release
```

**Результат:**
- ✅ `releases/MexcSetupApp-Mac-v1.0.zip` (~80 MB)
- ✅ `releases/MexcSetupApp-Mac-v1.0.dmg` (~80 MB)

---

## ✅ КРОК 2: Вибери формат розповсюдження

### Варіант A: ZIP (простіше для юзерів)
**Плюси:**
- Менший розмір
- Швидше завантажується
- Легко розпакувати

**Мінуси:**
- Менш професійно виглядає
- Юзери мають самі перетягнути в Applications

### Варіант B: DMG (професійніше)
**Плюси:**
- Виглядає як "справжня" програма
- Є UI з інструкцією (перетягни сюди)
- Стандарт для Mac

**Мінуси:**
- Трохи більший розмір
- Довше створювати

**🎯 Рекомендація: DMG для публічного релізу, ZIP для тестування**

---

## ✅ КРОК 3: Завантаж на хостинг

### Опція 1: Google Drive (безкоштовно, просто)

1. Зайди на [drive.google.com](https://drive.google.com)
2. Завантаж DMG або ZIP
3. Правий клік → **"Get shareable link"**
4. Скопіюй посилання

**Приклад посилання:**
```
https://drive.google.com/file/d/1abc...xyz/view?usp=sharing
```

Юзери клікають → завантажують → встановлюють.

### Опція 2: Dropbox

1. Завантаж файл на Dropbox
2. Створи shared link
3. Додай `?dl=1` в кінці для прямого завантаження

**Приклад:**
```
https://www.dropbox.com/s/abc123/MexcSetupApp-Mac-v1.0.dmg?dl=1
```

### Опція 3: GitHub Releases (для відкритого коду)

```bash
# Створити GitHub release
gh release create v1.0 \
  releases/MexcSetupApp-Mac-v1.0.dmg \
  releases/MexcSetupApp-Mac-v1.0.zip \
  --title "MEXC Setup App v1.0" \
  --notes "Initial Mac release"
```

### Опція 4: Telegram канал (пряма роздача)

1. Відправ DMG/ZIP в Telegram канал
2. Юзери завантажують прямо з Telegram
3. **Обмеження:** файли до 2 GB (твій ~80 MB - ОК)

---

## ✅ КРОК 4: Інструкція для юзерів

**Відправ разом з файлом:**
- 📄 `USER_INSTALL_INSTRUCTIONS.md` (як встановити)

**Або напиши коротко:**

```
📥 Як встановити:
1. Завантаж MexcSetupApp-Mac-v1.0.dmg
2. Подвійний клік на DMG
3. Перетягни app в папку Applications
4. Запусти з Applications

⚠️ При першому запуску:
- Правий клік на app → "Open"
- Натисни "Open" в попередженні
```

---

## ✅ КРОК 5: Апдейти (нові версії)

Коли робиш зміни:

```bash
# На Mac
cd /path/to/MexcSetupApp.Maui

# Новий реліз з новим номером версії
./create-release.sh 1.1

# Завантаж нові файли на хостинг
# Повідом юзерів про апдейт
```

---

## 📊 Що отримають юзери:

### Після встановлення:

✅ **Головне вікно:**
- Telegram налаштування (API ID, API HASH, Phone)
- Channel IDs (Listing/Delisting)
- Кнопки Connect, Save, Hide
- Parser controls
- Logs

✅ **Вбудований браузер:**
- MEXC Login відкривається в окремому вікні
- Токени відкриваються кожен в своєму вікні
- WebView використовує Safari engine
- Вікна поверх всіх

✅ **Працює автономно:**
- Config зберігається локально
- Session файли
- Cookies для MEXC

---

## 🔐 Code Signing (опціонально, для професійного релізу)

### Якщо є Apple Developer Account ($99/рік):

```bash
# 1. Підписати app
codesign --deep --force --verify --verbose \
  --sign "Developer ID Application: Your Name (TEAM123)" \
  --options runtime \
  bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app

# 2. Notarize (щоб Mac не показував попередження)
xcrun notarytool submit \
  releases/MexcSetupApp-Mac-v1.0.dmg \
  --apple-id your@email.com \
  --team-id TEAM123 \
  --password "app-specific-password"

# 3. Staple notarization
xcrun stapler staple releases/MexcSetupApp-Mac-v1.0.dmg
```

**З підписом і notarization:**
- ✅ Юзери НЕ бачать попередження
- ✅ Може розповсюджуватись через Mac App Store
- ✅ Виглядає професійно

**Без підпису (твій випадок):**
- ⚠️ Попередження "unidentified developer"
- ✅ Працює після "Open Anyway"
- ✅ Безкоштовно

---

## 🎯 ПІДСУМОК:

### ДЛЯ ТЕБЕ (на Windows зараз):

1. ✅ Проект готовий
2. ✅ Інструкції створені
3. ✅ Скрипти готові
4. 📤 Перенеси папку `MexcSetupApp.Maui` на Mac
5. 🔨 Запусти `./create-release.sh` на Mac
6. 📦 Отримаєш ZIP і DMG
7. ☁️ Завантаж на Drive/Dropbox
8. 📣 Дай посилання юзерам + інструкцію

### ДЛЯ ЮЗЕРІВ:

1. 📥 Завантажують DMG/ZIP
2. 📁 Встановлюють в Applications
3. ▶️ Запускають (після "Open Anyway")
4. ✅ Користуються програмою

---

## 📞 Підтримка:

**Розробник:** @kovtun_evgenii  
**Версія:** 1.0 MAUI Cross-Platform  
**Платформи:** Windows 10/11, macOS 13.1+

---

## 🔥 Pro Tip:

Якщо немаєш доступу до Mac - можеш:
- Попросити друга з Mac запустити скрипт
- Використати **GitHub Actions** (я можу налаштувати - білдить на Mac автоматично!)
- Орендувати Mac в хмарі (MacStadium, MacinCloud)




