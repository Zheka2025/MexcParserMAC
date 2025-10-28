# 📥 Інструкція для користувачів Mac - Встановлення MEXC Setup App

## 🎯 Для кого: Юзери Mac які отримали програму

---

## Спосіб 1: Якщо отримав ZIP файл

### Крок 1: Завантаж і розпакуй
1. Завантаж `MexcSetupApp-Mac-v1.0.zip`
2. Подвійний клік на ZIP → автоматично розпакується
3. Побачиш файл `MexcSetupApp.Maui.app`

### Крок 2: Встанови
1. Перетягни `MexcSetupApp.Maui.app` в папку **Applications**
   - Або натисни `⌘ + Shift + A` щоб відкрити Applications
2. Готово!

### Крок 3: Запусти
1. Відкрий **Applications**
2. Знайди **MexcSetupApp.Maui**
3. Подвійний клік

### ⚠️ Якщо побачиш попередження:
```
"MexcSetupApp.Maui cannot be opened because it is from an unidentified developer"
```

**Рішення:**
1. Правий клік (або Control + клік) на app
2. Вибери **"Open"** з меню
3. Натисни **"Open"** в діалозі
4. Програма запуститься

**АБО:**
1. Відкрий **System Settings**
2. **Privacy & Security**
3. Прокрути вниз до "Security"
4. Натисни **"Open Anyway"** біля назви програми
5. Запусти app знову

---

## Спосіб 2: Якщо отримав DMG файл

### Крок 1: Відкрий DMG
1. Подвійний клік на `MexcSetupApp-Mac-v1.0.dmg`
2. Відкриється вікно з іконкою програми

### Крок 2: Встанови
1. Перетягни іконку програми в папку **Applications**
2. Дочекайся копіювання

### Крок 3: Вийди з DMG
1. Клік правою кнопкою на DMG в Finder sidebar
2. **"Eject"**

### Крок 4: Запусти
1. Відкрий **Applications**
2. Знайди **MexcSetupApp.Maui**
3. Подвійний клік

Якщо попередження - див. вище ⚠️

---

## ✅ Перевірка що працює:

Після запуску маєш побачити:
- 📝 Поля для Telegram налаштувань (API ID, API HASH, Phone...)
- 🔘 Кнопки (Connect Telegram, Open MEXC Login...)
- ▶️ Parser Control (Start/Stop Parser)
- 📋 Logs вікно
- ℹ️ Status внизу

---

## 🔧 Налаштування:

1. Введи **Telegram API credentials** (api_id, api_hash, phone)
2. Введи **Channel IDs** (listing/delisting)
3. Натисни **"Save Config"**
4. Натисни **"Connect Telegram"** → введи код із Telegram
5. Натисни **"Start Parser"**

---

## ❓ Проблеми?

### "App is damaged and can't be opened"
```bash
# Відкрий Terminal і виконай:
xattr -cr /Applications/MexcSetupApp.Maui.app
```

### "App crashes on startup"
Перевір що macOS 13.1 (Ventura) або новіше:
- 🍎 → **About This Mac** → версія має бути 13.1+

### Інші проблеми:
Звернися до розробника: **@kovtun_evgenii**

---

## 🗑️ Видалення:

1. Відкрий **Applications**
2. Перетягни **MexcSetupApp.Maui** в Trash
3. Очисти Trash

Config файли зберігаються тут:
```
~/Library/Application Support/com.mexcsetup.maui/
```

---

## 💡 Корисні поради:

- 🔐 Дані зберігаються локально (session файли, config)
- 🌐 WebView використовує Safari engine
- 🪟 Можна відкрити багато вікон одночасно
- 📌 Вікна MEXC автоматично поверх всіх

---

**Розроблено @kovtun_evgenii для ком'юніті Tether VIP**

