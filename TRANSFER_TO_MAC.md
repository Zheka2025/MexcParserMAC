# 📦 Як перенести проект на Mac

## Метод 1: Через ZIP (найпростіше)

### На Windows:
1. Правий клік на папку `MexcSetupApp.Maui`
2. **"Send to" → "Compressed (zipped) folder"**
3. Отримаєш `MexcSetupApp.Maui.zip`

### На Mac:
1. Перенеси ZIP на Mac (через email, USB, AirDrop, OneDrive)
2. Подвійний клік на ZIP → автоматично розпакується
3. Відкрий **Terminal**
4. Перейди в папку:
   ```bash
   cd ~/Downloads/MexcSetupApp.Maui
   # Або куди ти розпакував
   ```
5. Запусти скрипт:
   ```bash
   chmod +x build-mac.sh
   ./build-mac.sh
   ```

---

## Метод 2: Через OneDrive/Google Drive

### На Windows:
1. Скопіюй папку `MexcSetupApp.Maui` в OneDrive
2. Дочекайся синхронізації

### На Mac:
1. Встанови OneDrive (якщо немає)
2. Дочекайся синхронізації
3. Відкрий Terminal і перейди в папку:
   ```bash
   cd ~/OneDrive/MexcSetupApp.Maui
   chmod +x build-mac.sh
   ./build-mac.sh
   ```

---

## Метод 3: Через GitHub (для досвідчених)

### На Windows:
```powershell
cd C:\Users\pczhe\OneDrive\MexcSetupApp.Maui
git init
git add .
git commit -m "Initial MAUI version"
git remote add origin https://github.com/твій-username/mexc-setup.git
git push -u origin main
```

### На Mac:
```bash
git clone https://github.com/твій-username/mexc-setup.git
cd mexc-setup
chmod +x build-mac.sh
./build-mac.sh
```

---

## Метод 4: Через AirDrop (якщо є iPhone/Mac поряд)

### На Windows:
1. Запакуй в ZIP
2. Відправ на свій iPhone (через Telegram Self, Email)
3. З iPhone → AirDrop на Mac

### На Mac:
1. Отримай ZIP через AirDrop
2. Розпакуй
3. Запусти `build-mac.sh`

---

## ⚠️ ВАЖЛИВО:

Після копіювання на Mac треба дати права на виконання скрипту:
```bash
chmod +x build-mac.sh
```

Інакше отримаєш помилку "Permission denied".

---

## 🎯 Що потрібно перенести:

**Мінімум (вся папка):**
```
MexcSetupApp.Maui/
├── *.cs (всі C# файли)
├── *.xaml (всі XAML файли)
├── *.csproj (файл проекту)
├── Platforms/
├── Resources/
└── build-mac.sh
```

**НЕ треба переносити:**
- ❌ `bin/` папку
- ❌ `obj/` папку
- ❌ `.vs/` папку (якщо є)

Вони згенеруються автоматично при білді.

---

## 📏 Розмір проекту:

- Без `bin/obj`: ~100 KB (тільки вихідний код)
- З `bin/obj`: ~500 MB (після білда)
- Release `.app`: ~80-150 MB

---

## 🆘 Допомога:

Якщо щось не працює на Mac - надішли скріншот помилки!




