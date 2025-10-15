# Oleg bot

## Компилировать вручную

```bash
# For 64-bit Windows
dotnet publish -c Release -r win-x64 --self-contained true

# For 64-bit Linux
dotnet publish -c Release --self-contained true
```

## Настройки (appsettings.json)

Для настройки бота необходимо заполнить все параметры в файле `appsettings.json`. Ниже приведено подробное описание каждого раздела.

**DeepSeek**
- **ApiKey** — Ваш API-ключ, полученный в личном кабинете DeepSeek.

**OpenAI**
- **ApiKey** — Ваш API-ключ из панели управления OpenAI.

**Telegram**
- **BotToken** — Токен вашего бота, который вы получили у [@BotFather](https://t.me/botfather).
- **CommandHandle** — Команда, которая активирует основную функцию бота. Например, `/start`.
- **CommandDescription** — Описание команды, которое видят пользователи в интерфейсе Telegram.

**UMoney** (для обработки платежей)
- **ClientId** — Идентификатор вашего приложения в личном кабинете UMoney (YooMoney).
- **PrivateToken** — Секретный API-ключ для доступа к платежам.
- **PaymentTimeoutInMinutes** — Время в минутах, в течение которого платеж считается действительным (после этого времени оплата просрочивается).
- **PollingIntervalInSeconds** — Как часто бот проверяет статус платежей (в секундах).
- **ReturnUrl** — URL, на который пользователь возвращается после оплаты (рекомендуется не менять, если у вас нет особых требований).

**Smtp** (для отправки email-уведомлений)
- **Server** — Адрес SMTP-сервера вашей почты. Например, для Gmail: `smtp.gmail.com`.
- **Port** — Порт для подключения. Обычно это `587` для защищенного соединения.
- **FromAddress** — Email-адрес, с которого будут отправляться письма.
- **Password** — Пароль от почты или специальный пароль для приложений.
- **Unsubscribe** — Email-адрес, на который можно отправлять запросы на отписку от рассылки.

```json
{
  "DeepSeek": {
    "ApiKey": "<your-deepseek-api-key>"
  },
  "OpenAI": {
    "ApiKey": "<your-openai-api-key>"
  },
  "Telegram": {
    "BotToken": "<your-bot-token>",
    "CommandHandle": "/start",
    "CommandDescription": "<your-command-description>"
  },
  "UMoney": {
    "ShopId": "<your-shop-id>",
    "ApiKeySecret": "<your-api-key-secret>",
    "PaymentTimeoutInMinutes": 10,
    "PollingIntervalInSeconds": 5
  },
  "Smtp": {
    "Server": "mail.gmail.com",
    "Port": 587,
    "FromAddress": "<your-mail-address>",
    "Password": "<your-email-password>",
    "Unsubscribe": " <your-mail-address>"
  }
}
```

## Дополнительная информация

- [Зарегистрируйте приложение с yoowallet](https://yoomoney.ru/myservices/new) (https://yoomoney.ru/myservices/new)