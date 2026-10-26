# StudySpace

StudySpace - навчальна вебсистема для спільної роботи з документами та знаннями. Пов'язані завдання курсу об'єднані навколо єдиної предметної області, а окремі компоненти, для яких потрібні самостійні рішення, розміщені поруч у межах одного репозиторію.

## Реалізовані можливості

- ASP.NET Core MVC, сутності робочих просторів, документів, блоків, вкладень і учасників, міграції EF Core, SQLite, CRUD, валідація та завантаження файлів.
- REST API зі Swagger, колекцією Postman, пагінацією, коректними HTTP-кодами, кешуванням, ізоляцією даних і повнотекстовим пошуком Lucene.
- Адаптивна головна сторінка на Bootstrap, SEO-ресурси, діаграми Chart.js, автодоповнення через jQuery та AJAX, карта Leaflet з OpenStreetMap і попередній перегляд файлів.
- Спільний блочний редактор на SignalR, збереження порядку блоків, відображення фокуса користувачів, порівняння способів обміну даними та спільна дошка Canvas.
- Автентифікація через cookie, необов'язковий Google OAuth, ролі власника, редактора і читача, плани Free та Premium, перевірка авторизації.
- Необов'язкові інтеграції з Telegram або webhook, Application Insights, Azure Blob Storage, RabbitMQ і зовнішнім сервісом обробки запитів.
- Помічник робочого простору через SSE з локальним режимом, що використовує вміст доступних документів.
- Окремий клієнт React та TypeScript, Azure Functions, фоновий обробник черги і MCP-сервер.
- Модульні, інтеграційні, авторизаційні та браузерні тести, GitHub Actions і Docker Compose.

## Структура репозиторію

```text
src/StudySpace.Core       Сутності предметної області
src/StudySpace.Data       Контекст EF Core, міграції та початкові дані
src/StudySpace.Web        MVC, REST API, SignalR і SSE
src/studyspace-spa        Окремий клієнт React та TypeScript
src/StudySpace.Functions  Окремий проєкт Azure Functions
src/StudySpace.Worker     Фоновий споживач повідомлень RabbitMQ
src/StudySpace.Mcp        MCP-сервер, що працює через stdio
tests                     Модульні, інтеграційні та браузерні тести
docs                      Інструкції та опис реалізації завдань
report                    Звіт у форматах LaTeX і PDF
```

## Локальний запуск

Потрібні .NET SDK 10, Node.js 24 або новіший і pnpm.

```powershell
dotnet restore StudySpace.slnx
dotnet run --project src/StudySpace.Web --urls http://localhost:5036
```

Після запуску відкрити `http://localhost:5036`. База SQLite оновлюється міграціями та наповнюється початковими даними автоматично. Для локальної роботи без входу використовується демонстраційний користувач `demo-user`. Сторінка `/Account/Login` дає змогу змінити локальний обліковий запис.

Окремий клієнт React запускається в іншому терміналі:

```powershell
cd src/studyspace-spa
pnpm install
pnpm dev
```

## Перевірка проєкту

```powershell
dotnet format StudySpace.slnx --verify-no-changes
dotnet build StudySpace.slnx --no-restore
dotnet test StudySpace.slnx --no-build
cd src/studyspace-spa
pnpm build
cd ../../tests/e2e
pnpm test
```

## Необов'язкові сервіси

Якщо облікові дані відсутні, зовнішні інтеграції вимкнені й не заважають локальному запуску. Секрети потрібно зберігати у змінних середовища або через .NET User Secrets.

| Можливість | Налаштування |
|---|---|
| Google OAuth | `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret` |
| Application Insights | `APPLICATIONINSIGHTS_CONNECTION_STRING` |
| Azure Blob Storage | `ConnectionStrings__BlobStorage` |
| Telegram | `Integrations__Telegram__BotToken`, `Integrations__Telegram__ChatId` |
| Webhook | `Integrations__WebhookUrl` |
| RabbitMQ | `RabbitMq__Host`, `RabbitMq__User`, `RabbitMq__Password` |
| Зовнішній сервіс помічника | `AI__ChatEndpoint`, `AI__ApiKey`, `AI__Model` |

Команда `docker compose up --build` запускає вебзастосунок, панель керування RabbitMQ і фоновий обробник черги.

## Окремі компоненти

- Azure Functions: проєкт `src/StudySpace.Functions`; `GET /api/health` доступний без ключа, а `GET /api/digest` захищений ключем функції.
- MCP: після запуску вебзастосунку виконати `dotnet run --project src/StudySpace.Mcp`. Приклад конфігурації міститься у `src/StudySpace.Mcp/mcp.example.json`.
- React SPA: виробнича збірка створюється у `src/studyspace-spa/dist`.

Докладні команди наведені в [інструкції із запуску](docs/SETUP.md), а відповідність завданням курсу - у [матриці реалізації](docs/DEFENCE.md).

## Звіт

Повний україномовний звіт розміщено в `report/WebTechnologiesLab`. Каталог містить початковий файл LaTeX, використані знімки екрана та готовий PDF.
