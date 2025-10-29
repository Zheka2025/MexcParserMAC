# MEXC Setup & Parser - MAUI Edition

Cross-platform застосунок для парсингу Telegram каналів та автоматичного відкриття MEXC futures.

## 🌍 Підтримувані платформи:

- ✅ **Windows 10/11**
- ✅ **macOS 13.1+** (Ventura and newer)

---

## 🚀 Швидкий старт:

### Windows:
```powershell
cd C:\path\to\MexcSetupApp.Maui
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0
```

### macOS:
```bash
cd /path/to/MexcSetupApp.Maui
dotnet build -f net9.0-maccatalyst
dotnet run -f net9.0-maccatalyst
```

**Детальні інструкції для Mac:** читай `MAC_BUILD_INSTRUCTIONS.md`

---

## 📦 Залежності:

- .NET 9.0
- WTelegramClient 4.3.11
- Newtonsoft.Json 13.0.4
- Microsoft.Maui.Controls 9.0.111

---

## ✨ Функції:

- 🔐 Telegram авторизація
- 📊 Парсинг каналів (listing/delisting)
- 🌐 Вбудований браузер (WebView)
- 🪟 Множинні вікна для токенів
- 🔍 Гнучка система фільтрів
- 📌 Вікна завжди поверх інших
- 👁️ Маскування чутливих даних

---

## 🛠️ Структура проекту:

```
MexcSetupApp.Maui/
├── App.xaml.cs              # Головний додаток
├── MainPage.xaml            # Головне вікно UI
├── MainPage.xaml.cs         # Головна логіка
├── MexcWebViewPage.xaml     # Вбудований браузер
├── TelegramParser.cs        # Парсер Telegram (без змін з WPF)
├── Config.cs                # Конфігурація
├── Platforms/               # Platform-specific код
│   ├── Windows/             # Windows код
│   └── MacCatalyst/         # Mac код
└── Resources/               # Ресурси (іконки, шрифти)
```

---

## 🔒 Security:

- VIP перевірка перед запуском парсера
- Session файли захищені
- Config зберігається локально

---

## 📄 Ліцензія:

Розроблено **@kovtun_evgenii** для ком'юніті **Tether VIP**

---

## 🆘 Проблеми?

1. Перевір що .NET 9 встановлений: `dotnet --version`
2. Перевір що MAUI встановлений: `dotnet workload list`
3. Перечитай інструкції в `MAC_BUILD_INSTRUCTIONS.md`



