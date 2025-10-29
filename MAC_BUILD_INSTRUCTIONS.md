# 🍎 Інструкція: Білд і запуск MEXC Setup App на macOS

## 📋 Вимоги:
- macOS 13.1 (Ventura) або новіше
- Доступ до Terminal
- ~5 GB вільного місця

---

## Крок 1️⃣: Встановити Homebrew (якщо немає)

Відкрий **Terminal** (⌘ + Space, введи "Terminal")

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

Після встановлення можливо треба додати в PATH (Terminal покаже команди).

---

## Крок 2️⃣: Встановити .NET SDK

```bash
# Встановити .NET 9
brew install dotnet

# Перевірити версію
dotnet --version
# Має бути 9.x.x
```

---

## Крок 3️⃣: Встановити MAUI Workload

```bash
# Встановити MAUI
dotnet workload install maui

# Перевірити
dotnet workload list
# Має показати "maui"
```

⏱️ **Це займе 5-10 хвилин** - завантажує багато файлів.

---

## Крок 4️⃣: Скопіювати проект на Mac

### Варіант A: Через USB/OneDrive
Скопіюй папку `MexcSetupApp.Maui` на Mac.

### Варіант B: Через GitHub
```bash
# Якщо проект на GitHub
git clone https://твій-репозиторій.git
cd MexcSetupApp.Maui
```

### Варіант C: Через ZIP
1. Запакуй папку `MexcSetupApp.Maui` в ZIP на Windows
2. Перенеси на Mac
3. Розпакуй

---

## Крок 5️⃣: Білд проекту

```bash
# Перейди в папку проекту
cd /path/to/MexcSetupApp.Maui

# Restore залежності
dotnet restore

# Білд для Mac
dotnet build -f net9.0-maccatalyst

# Якщо є помилки - напиши мені!
```

**Очікуваний вивід:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Крок 6️⃣: Запуск програми

### Варіант A: Через dotnet run
```bash
dotnet run -f net9.0-maccatalyst
```

### Варіант B: Запуск .app файлу напряму
```bash
open bin/Debug/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app
```

---

## Крок 7️⃣: Створення Release версії (для розповсюдження)

```bash
# Publish для Mac
dotnet publish -f net9.0-maccatalyst -c Release

# Готовий .app буде тут:
# bin/Release/net9.0-maccatalyst/maccatalyst-x64/publish/MexcSetupApp.Maui.app
```

**Цей .app файл можна:**
- Скопіювати в `/Applications`
- Відправити іншим юзерам Mac
- Запакувати в DMG (інсталятор)

---

## 🔧 Troubleshooting:

### Проблема: "command not found: dotnet"
```bash
# Додай в PATH
echo 'export PATH="/usr/local/share/dotnet:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

### Проблема: "workload install failed"
```bash
# Спробуй з sudo
sudo dotnet workload install maui
```

### Проблема: "The application cannot be opened"
```bash
# Дозволи запуск неперевірених додатків
xattr -cr bin/Debug/net9.0-maccatalyst/maccatalyst-x64/MexcSetupApp.Maui.app
```

### Проблема: Build помилки
```bash
# Очисти і перебілдь
dotnet clean
dotnet restore
dotnet build -f net9.0-maccatalyst
```

---

## 📱 Що працює на Mac:

✅ Вся логіка Telegram Parser  
✅ Вбудований браузер (WebView → WKWebView на Mac)  
✅ Окремі вікна для токенів  
✅ VIP перевірка  
✅ Всі кнопки та UI  

## ⚠️ Відмінності від Windows:

- 🔴🟡🟢 Кнопки вікна зліва (Mac style)
- 🌐 WebView використовує Safari engine (не Chromium)
- 📁 Config зберігається в `~/Library/Application Support/`

---

## 🚀 Quick Start (для досвідчених):

```bash
brew install dotnet
dotnet workload install maui
cd /path/to/MexcSetupApp.Maui
dotnet build -f net9.0-maccatalyst
dotnet run -f net9.0-maccatalyst
```

---

## 📞 Підтримка:

Якщо щось не працює - відправ скріншот помилки!

**Розробник:** @kovtun_evgenii  
**Версія:** 1.0 MAUI Cross-Platform



