# AccountService

## Содержание

- [Описание](#описание)
- [Технологии](#технологии)
- [Требования](#требования)
- [Запуск в Docker Compose](#запуск-в-docker-compose)
- [Структура проекта](#структура-проекта)
- [Конфигурация Keycloak](#конфигурация-keycloak)
- [Аутентификация](#аутентификация)
- [База данных](#база-данных)
- [Планировщик задач](#планировщик-задач)
- [Конфигурация RabbitMQ](#конфигурация-rabbitmq)
- [Событийная архитектура](#событийная-архитектура)
- [Описание API](#описание-api)
- [Тесты](#тесты)
  
---

## Описание

Микросервис для управления банковскими счетами в розничном банке. Сервис предоставляет REST API для создания, редактирования, удаления счетов, регистрации транзакций, переводов между счетами, получения выписок и ежедневного начисления процентов для дебиторских счетов. Основные сценарии включают открытие текущих и депозитных счетов, пополнение счетов, переводы и мониторинг баланса. Сервис реализует асинхронное взаимодействие через доменные события.

---

## Технологии

- .NET 9 / ASP.NET Core Web API

- AutoMapper — для автоматического сопоставления моделей

- FluentValidation — для валидации входящих данных
 
- MediatR — реализация паттерна CQRS / Mediator

- Hangfire — для выполнения запланированных задач

- Swagger / OpenAPI — генерация Swagger UI для документации API

- FluentMigrator — для управления миграциями базы данных

- Dapper — ORM для взаимодействия с базой данных

- PostgreSQL — для хранения данных

- Docker / Docker Compose — развертывание проекта в контейнере

- Keycloak — OAuth 2.0 сервис аутентификации/авторизации

- RabbitMQ —  для обмена сообщениями между сервисами

---

## Требования

.NET SDK 9 или новее

Visual Studio 2022

Docker Desktop (или Docker Engine + Docker Compose)

---

## Запуск в Docker Compose

Для быстрого старта всех сервисов используется Docker Compose. Он поднимает контейнеры AccountService, PostgreSQL, RabbitMQ и Keycloak. Также создает named volumes для Postgres и RabbitMQ. Keycloack запускает в режиме dev с импортом realm из файла ./configs/realm-export.json.

1. Клонируйте репозиторий:
```bash
git clone https://github.com/turbo821/AccountService.git
cd AccountService
```
1. Запустите сервисы через Docker Compose в Visual Studio или с помощью CLI:
```bash
docker-compose up -d
```
1. Ждём старта сервисов. Проверка через health check гарантирует готовность PostgreSQL и RabbitMQ.
2. Доступ к сервисам:
- Swagger UI: http://localhost:80/swagger
- Hangfire Dashboard: http://localhost:80/hangfire
- Keycloak Admin: http://localhost:8080 (вход в панель admin : admin)
- RabbitMQ Management UI: http://localhost:15672 (вход в панель guest : guest)

---

## Структура проекта
Используется Vertical Slice Architecture (VSA). Приложение структурируется по определенным функциям (срезам).

- **`AccountService/Application`**
  * Содержит общие модели для всего приложения.
- **`AccountService/Background`**
  * Содержит сервисы, запущенные в фоновом режиме.
- **`AccountService/Controllers`**
  * Содержит контролеры API.
- **`AccountService/Extensions`**
  * Содержит классы расширения.
- **`AccountService/Features`**
  * Содержит список директорий для каждого среза, содержащий общий функционал для конкретной сущности.
- **`AccountService/Features/Accounts`**
  * Содержит модели, интерфейсы и компоненты MediatR, связанные с `Account`.
- **`AccountService/Infrastructure`**
  * Содержит реализацию сервисов, связанных со внешними источниками.
- **`AccountService/Infrastructure/Persistence`**
  * Содержит классы миграции БД и класс репозитория для взаимодействия с БД из приложения.
- **`AccountService/Infrastructure/Consumers`**
  * Содержит классы потребителей, которые обрабатывают поступаемые события.
- **`AccountService/Middlewares`**
  * Содержит middlewares, используемые для обработки запросов/ответов.
- **`AccountService.Tests`**
  * Содержит модульные и интеграционный тесты.

---

## Конфигурация Keycloak

Сервис использует предварительно настроенный Keycloak:
- Realm: `auth-service`
- Клиент: `account-api`
- Тестовый пользователь: `tom` / `pass123`

Для кастомной настройки:
1. Откройте админ-панель: http://localhost:8080
2. Импортируйте конфигурацию из `./keycloak/realm-export.json`

---

## Аутентификация
API поддерживает два способа авторизации:
1. **OAuth2 через Keycloak**  
   - Использует implicit flow для Swagger UI  
   - Тестовый пользователь: `tom` / `pass123`  
   - Client: `account-api`
   - Клиент вводится в Keycloak OAuth2.0 после чего редиректится в Keycloak форму входа, где нужно ввести тестовые данные

2. **JWT токен**  
   - Получает токен через `/auth/token` 
   - Вводится в поле Keycloak JWT 
   - Автоматически передается в заголовке `Authorization: Bearer <токен>`

---

## База данных
База данных PostgreSQL содержит две схемы public(для основных данных) и hangfire(для запланированных задач). Есть две таблицы: accounts для хранения информации о счетах и transactions для хранения информации о транзакциях. Для оптимизации имеются индексы. Также есть две функции: accrue_interest(account_id uuid) для зачисления процента счету по id и accrue_interest_all() для зачисления ежедневных процентов по вкладам, используя первую функцию. Для доменных событий имеются таблицы: outbox_messages для хранения созданных сервисом событий, inbox_consumed для хранения обработанных потребителями событий (реализация идемпотентности), audit_events для хранения всех пришедших сообщений и inbox_dead_letters для событий в карантине.

---

## Планировщик задач
В качестве планировщика задач используется Hangfire. Джоб accrue-interest-daily начисляет проценты по вкладам ежедневно, вызывая в коде специальный сервис, который исполняет функцию БД по каждому дебитовому счёту и создает событие InterestAccrued. Джоб outbox-processor каждые 20 секунд выбирает по 100 событий из outbox_messages, публикует их в очередь и отмечает как обработанные (at‑least‑once доставка). Планировщик доступен по пути `/hangfire`. 

---

## Конфигурация RabbitMQ

Для микросервиса используется брокер RabbitMQ с паттерном **Transactional Outbox**. Конфигурация (exchange, queues, binds) задается в коде при старте приложения.

### Exchange

- **`account.events`** — тип `topic`.

### Очереди и маршрутизация

| Очередь                  | Routing key                       | Назначение                                |
|---------------------------|----------------------------------|-------------------------------------------|
| `account.crm`             | `account.*`                      | CRM-система, события по счетам            |
| `account.notifications`   | `money.*`                        | Уведомления о денежных операциях          |
| `account.antifraud`       | `client.*`                       | Антифрод сервис, блокировка/разблокировка |
| `account.audit`           | `#`                              | Хранение всех событий для аудита          |

### Примеры маршрутизации событий

| Событие                        | Exchange / Routing key            |
|--------------------------------|----------------------------------|
| `AccountOpened`                 | `account.events` / `account.opened` |
| `MoneyCredited` / `MoneyDebited` / `TransferCompleted` | `account.events` / `money.*` |
| `ClientBlocked` / `ClientUnblocked` | `account.events` / `client.*` |

---

## Событийная архитектура
Сервис использует **Event-driven архитектуру** с паттерном **Transactional Outbox** для надежной доставки сообщений.

### Transactional Outbox

- Все события записываются в таблицу `outbox_messages` вместе с бизнес-транзакцией.
- OutboxDispatcher (Hangfire background job) периодически публикует события в RabbitMQ.
- При недоступности RabbitMQ события остаются в outbox до успешной публикации.

### Consumers

- ConsumersHostedService подписывается на очереди и обрабатывает события в фоне.
- Примеры:
  - `AuditConsumer` — помещает события в таблицу аудита (`inbox_consumed`).
  - `AntifraudConsumer` — обрабатывает события типа `ClientBlocked` / `ClientUnblocked`.
- Обеспечивается иденпотентность: события с одинаковым `eventId` не обрабатываются повторно.

### Доменные события

#### Публикуемые события:
| Событие | Описание |
|---------|----------|
| AccountOpened | Создание нового счета |
| AccountClosed | Закрытие счета |
| MoneyDebited / MoneyCredited | Списание / пополнение средств |
| ClientBlocked / ClientUnblocked | Блокировка / разблокировка клиента |
| TransferCompleted | Завершение перевода между счетами |
| InterestAccrued | Начисления процентов за определённый период. |

#### Публикуемые события:
| Событие | Описание |
|---------|----------|
| ClientBlocked | Блокировка клиента (счета заморожены) |
| ClientUnblocked | Снятие блокировки клиента (счета разморожены) |

Каждое событие содержит:
- `eventId` — уникальный идентификатор события
- `occurredAt` — время события
- `meta` — метаданные (version, sourse correlationId, causationId)
- payload — данные события (например, Id счета, сумма, валюта)

---

## Описание API

### 1. Создание счёта
*   **Endpoint:** `POST /accounts`
*   **Требуется авторизация** 
*   **Описание:** Создаёт новый банковский счёт с нулевым балансом.
*   **Тело запроса:**
```json
{
  "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Deposit",
  "currency": "RUB",
  "interestRate": 2
}
``` 
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {
    "accountId": "11329a35-0c15-4191-aff7-c59c8e395614"
  },
  "error": null
}
```

### 2. Получение списка счётов
*   **Endpoint:** `GET /accounts?ownerId={ownerId?}`
*   **Требуется авторизация** 
*   **Описание:** Возвращает список всех открытых счетов (опционально для конкретного владельца).
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": [
    {
      "id": "55dcfa45-03e0-49ae-9cce-44b167251328",
      "ownerId": "f6af2260-9c81-4178-98f5-696742700fa6",
      "type": "Checking",
      "currency": "USD",
      "balance": 1000,
      "interestRate": null,
      "openedAt": "2025-07-28T00:52:18.5161679Z"
    }
  ],
  "error": null
}
```

### 3. Получение счёта по ID
*   **Endpoint:** `GET /accounts/{id}`
*   **Требуется авторизация** 
*   **Описание:** Возвращает информацию о конкретном счёте по его ID.
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {
    "id": "55dcfa45-03e0-49ae-9cce-44b167251328",
    "ownerId": "f6af2260-9c81-4178-98f5-696742700fa6",
    "type": "Checking",
    "currency": "USD",
    "balance": 1000,
    "interestRate": null,
    "openedAt": "2025-07-28T00:52:18.5161679Z"
  },
  "error": null
}
```

### 4. Изменение счета по ID
*   **Endpoint:** `PUT /accounts/{id}`
*   **Требуется авторизация** 
*   **Описание:** Изменяет данные счёта по его ID.
*   **Тело запроса:**
```json
{
  "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "type": "Deposit",
  "currency": "string",
  "balance": 300,
  "interestRate": 16,
  "openedAt": "2025-07-30"
}
```
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {},
  "error": null
}
```

### 5. Изменение процентной ставки счёта
*   **Endpoint:** `PATCH /accounts/{id}/interest-rate`
*   **Требуется авторизация** 
*   **Описание:** Обновляет процентную ставку по счёту.
*   **Тело запроса:**
```json
{
  "interestRate": 3
}
```
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {},
  "error": null
}
```

### 6. Закрытие счёта (мягкое удаление)
*   **Endpoint:** `DELETE /accounts/{id}`
*   **Требуется авторизация** 
*   **Описание:** Закрывает счёт по его ID. Альтернатива мягкого удаления.
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {
    "accountId": "55dcfa45-03e0-49ae-9cce-44b167251328"
  },
  "error": null
}
```

### 7. Регистрация транзакции по счёту
*   **Endpoint:** `POST /accounts/{accountId}/transactions`
*   **Требуется авторизация** 
*   **Описание:** Регистрирует транзакцию по счёту (пополнение или списание).
*   **Тело запроса:**
```json
{
  "amount": 100,
  "currency": "USD",
  "type": "Debit",
  "description": "hello"
}
```
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {
    "transactionId": "dad8ebfd-52d5-4c5f-b70d-527c5502ecf2"
  },
  "error": null
}
```

### 8. Выполнение перевода между счётами
*   **Endpoint:** `POST /accounts/transfer`
*   **Требуется авторизация** 
*   **Описание:** Переводит средства между двумя счетами. Регистрация по одной транзакций (пополнение или списание) для обоих счетов.
*   **Тело запроса:**
```json
{
  "fromAccountId": "0e294688-657f-4f3f-a59a-1d5de8c48bde",
  "toAccountId": "cb69a295-b13f-43d8-bca5-c8a99b5f04ff",
  "amount": 5000,
  "currency": "RUB",
  "description": "happy birthday"
}
```
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": [
    {
      "transactionId": "0e294688-657f-4f3f-a59a-1d5de8c48bde"
    }, 
    {
      "transactionId": "cb69a295-b13f-43d8-bca5-c8a99b5f04ff"
    },
  ],
  "error": null
}
```

### 9. Выдача выписки клиенту по счету
*   **Endpoint:** `GET /accounts/{accountId}/transactions?fromDate={fromDate?}&toDate={toDate?}`
*   **Требуется авторизация** 
*   **Описание:** Возвращает выписку по счету за определенный период.
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {
    "accountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "type": "Checking",
    "balance": 0,
    "transactions": [
      {
        "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "counterpartyAccountId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "amount": 0,
        "currency": "string",
        "type": "Debit",
        "description": "string",
        "timestamp": "2025-07-28T01:09:26.544Z"
      }
    ]
  },
  "error": null
}
```

### 10. Проверка наличия счёта у клиента
*   **Endpoint:** `GET /accounts/check-owner/{ownerId}`
*   **Требуется авторизация** 
*   **Описание:** Проверяет наличие счетов у клиента.
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {
    "ownerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "accountExists": true,
    "accountCount": 1
  },
  "error": null
}
```

### 11. Получение токена доступа (используются данные тестового пользователя)
*   **Endpoint:** `GET /auth/token`
*   **Без авторизации**
*   **Описание:** Получает AccessToken для входа. 
*   **Используемые данные:**
```
client_id: account-api
username: tom
password: pass123
```
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJSUzI1Ni...."
  },
  "error": null
}
```

### 12. Проверка подключение в RabbitMQ
*   **Endpoint:** `GET /health/live`
*   **Без авторизации**
*   **Описание:** Проверяет подключение к RabbitMQ. 
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "available": true
}
```

### 13. Проверка отставания Outbox и RabbitMQ
*   **Endpoint:** `GET /health/ready`
*   **Без авторизации**
*   **Описание:** Проверяет сколько событий в Outbox еще не опубликовал RabbitMQ. 
*   **Тело запроса:** Пустое
*   **Успешный ответ:** `200 OK`.
```json
{
  "status": "Healthy",
  "pendingOutboxMessages": 0
}
```

---

## Тесты
Тесты расположены в `AccountService.Tests`. Покрытие включает как модульные, так и интеграционные тесты.

### Модульные тесты
- **Unit Tests** (xUnit) находятся в директории `UnitTests`.
- Основное покрытие:
  - **UnitTests.AccountTests** — проверка бизнес-логики выполнения транзакций.
  - **UnitTests.Handlers** — проверка корректной работы MediatR-обработчиков с маппингом между DTO и моделями домена.
- Цель модульных тестов — убедиться, что отдельные компоненты работают корректно без зависимости от внешних сервисов и БД.

### Интеграционные тесты
- **Integration Tests** (xUnit + Testcontainers‑DotNet) проверяют взаимодействие компонентов в среде, максимально приближенной к реальной.
- Основные сценарии:
  1. **ClientBlockedPreventsDebit_ThenUnblockedAllowsDebit**  
     Проверяет, что событие ClientBlocked блокирует клиента и его дебиторские операции и что событие ClientUnblocked снимает блокировку и позволяет проводить транзакции.
  2. **OutboxPublishesAfterFailure**  
     Имитирует временную недоступность RabbitMQ, а после проверяет, что событие публикуется после восстановления;
  3. **Transfer_ShouldMaintainTotalBalance_After50ParallelTransfers**  
     Проверяет целостность суммарного баланса при 50 параллельных переводах через HTTP API. Ожидаемые коды ответов: 200 (Ok) или 409 (Conflict) при конфликтующих операциях.
  4. **TransferEmitsSingleEvent**  
     Проверяет, что при 50 параллельных переводах генерируется ровно 50 доменных событий в Outbox / RabbitMQ.
- Цель интеграционных тестов — убедиться, что бизнес-логика, Outbox, публикация и обработка событий работают корректно в связке.


---