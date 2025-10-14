# Oleg bot

## Компилировать вручную

```bash
# For 64-bit Windows
dotnet publish -c Release -r win-x64 --self-contained true

# For 64-bit Linux
dotnet publish -c Release --self-contained true
```

## Настройка (Gitlab)

- Создайте новую учетную запись с помощью https://gitlab.com
- Создайте новый пустой проект.
- В вашем проекте нажмите на значок +, затем создайте новый файл с именем `.gitlab-ci.yml` вставьте следующую конфигурацию и commit файл.

```yaml
pages:
  stage: deploy
  environment: production
  script:
    - mkdir .public
    - cp -r ./* .public
    - rm -rf public
    - mv .public public
  artifacts:
    paths:
      - public
  rules:
    - if: $CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH
```

> Обратите внимание, что в бесплатном gitlab на каждый репозиторий предоставляется максимум 10Gb, либо платите за премиум, либо, когда репозиторий заполнится, замените его на новый в конфигурации "appsettings.json".


## Настройки (appsettings.json)

**OpenAI**

- ApiKey => Токен API в панели управления open ai manager

**Telegram**

- AllowedUsers => Имена пользователей, которые могут использовать бота без @
- BotToken => Токен, сгенерированный @botfather
- CommandHandle => Что вызывает команду bot по умолчанию /image

**Gitlab**

- Host => По умолчанию используется официальный gitlab.com, но если вы самостоятельно размещаете gitlab, вы можете изменить URL
- ProjectId => Зайдите в свой проект, затем в настройки, затем в общие, там должен быть ваш идентификатор проекта
- PrivateToken => В левом верхнем углу выберите свой профиль, затем нажмите "Настройки", там будет "Access Token". Предоставьте доступ всем.
- PageUrl => Зайдите в свой проект, затем в deploy, затем pages, после "Your Pages site is live at..." должен быть ваш URL-адрес. **Должно закончиться словами /**

```json
{
  "OpenAI": {
    "ApiKey": "<your-openai-api-key>"
  },
  "Telegram": {
    "AllowedUsers": ["mathieuauclair"],
    "BotToken": "<your-bot-token>",
    "CommandHandle": "/image"
  },
  "Gitlab": {
    "Host": "https://gitlab.com",
    "ProjectId": "<project-id>",
    "PrivateToken": "<gitlab-user-api-token>",
    "PageUrl": "<page-url-github-pages>"
  }
}
```

## Дополнительная информация

- [Зарегистрируйте приложение с yoowallet](https://yoomoney.ru/myservices/new) (https://yoomoney.ru/myservices/new)