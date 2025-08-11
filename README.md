# AccountService

## Содержание

- [Описание](#описание)
- [Технологии](#технологии)
- [Требования](#требования)
- [Как запустить](#как-запустить)
- [Структура проекта](#структура-проекта)
- [Конфигурация Keycloak](#конфигурация-keycloak)
- [Аутентификация](#аутентификация)
- [База данных](#база-данных)
- [Планировщик задач](#планировщик-задач)
- [Описание API](#описание-api)
- [Тесты](#тесты)
  
---

## Описание

Микросервис для управления банковскими счетами в розничном банке. Сервис предоставляет REST API для создания, редактирования, удаления счетов, регистрации транзакций, переводов между счетами, получения выписок и ежедневного начисления процентов для дебиторских счетов. Основные сценарии включают открытие текущих и депозитных счетов, пополнение счетов, переводы и мониторинг баланса.

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

---

## Требования

.NET SDK 9 или новее

Visual Studio 2022

Docker Desktop (или Docker Engine + Docker Compose)

---

## Как запустить

### Запуск в Docker Compose
1. Клонируйте репозиторий:
```bash
git clone https://github.com/turbo821/AccountService.git
cd AccountService
```
2. Запустите сервисы через Docker Compose:
```bash
docker-compose up -d
```
3. Немного подождите, сервисы Keycloak и PostgreSQL требуют время для старта.
4. Откройте Swagger UI по адресу http://localhost:80/swagger для тестирования API.
5. По адресу http://localhost:80/hangfire будет доступен Hangfire для теста Cron-Jobs.
   
### Запуск Docker Compose в Visual Studio
1.  Клонируйте репозиторий, запустите файл AccountService.sln.
2.  Запустите профиль Docker-Compose в Visual Studio.
3. Немного подождите, сервисы Keycloak и PostgreSQL требуют время для старта.  
4. Автоматически откроется Swagger UI по адресу http://localhost:80/swagger для тестирования API.
5. По адресу http://localhost:80/hangfire будет доступен Hangfire для теста Cron-Jobs.
   
---

## Структура проекта
Используется Vertical Slice Architecture (VSA). Приложение структурируется по определенным функциям (срезам).

- **`AccountService/Application`**
  * Содержит общие модели для всего приложения.
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
База данных PostgreSQL содержит две схемы public(для основных данных) и hangfire(для запланированных задач). Есть две таблицы: accounts для хранения информации о счетах и transactions для хранения информации о транзакциях. Для оптимизации имеются индексы. Также есть две функции: accrue_interest(account_id uuid) для зачисления процента счету по id и accrue_interest_all() для зачисления ежедневных процентов по вкладам, используя первую функцию. 

---

## Планировщик задач
В качестве планировщика задач используется Hangfire. Для начисления процентов по вкладам в коде Hangfire вызывает специальных сервис, который исполняет функцию БД accrue_interest_all(). Планировщик доступен по пути `/hangfire`.

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

## Тесты
Тесты расположены в AccountService.Tests. 

### Модульные тесты
Модульные тесты (xUnit) расположены в директории UnitTests. Проверяют бизнес логику в классе Account, а также проверяют обработчики MediatR вместе с маппером MappingProfile.

### Интеграционный тест
Иинтеграционный тест (xUnit + Testcontainers‑DotNet): на параллельную обработку запросов - ParallelTransferTests запускает 50 параллельных переводов при вызове метода http API и проверяет сохранность суммарного баланса по итогу выполнения операций. На время работы запускается postgres в тестовом контейнере с миграциями. Запросы приходят с кодами 200 (Ok) и 409 (Conflict).

---
