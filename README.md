# Антиплагиат — система хранения и анализа студенческих работ

Домашняя работа №3 по курсу «Конструирование программного обеспечения».
Синхронное межсервисное взаимодействие.

Проект представляет собой микросервисную 
информационную систему, которая организует хранение домашних работ, 
присланных студентами, и формирует отчет по каждой работе 
по результатам проверки на антиплагиат.

---


## Архитектура системы

Серверная часть реализована как микросервисная архитектура с четким разделением ответственности. Используется синхронное межсервисное взаимодействие по HTTP.

Система состоит из трех сервисов:

### API Gateway (SD3-APIGateway)

Центральный сервис-посредник, принимающий все запросы от клиентов и маршрутизирующий их к соответствующим микросервисам.

Ответственность:
- прием запросов от внешних клиентов
- маршрутизация запросов к File Storing Service и File Analysis Service
- агрегация ответов от микросервисов
- возврат единого ответа клиенту

API Gateway не содержит бизнес-логики и не работает с базой данных напрямую. Он использует именованные HTTP-клиенты (через IHttpClientFactory) для обращения к внутренним сервисам.

### File Storing Service (SD3-FileAnalysisService)

Отвечает только за хранение и выдачу файлов.

Ответственность:
- прием файла от API Gateway
- валидация файла
- сохранение файла на диск сервера
- запись метаданных файла в СУБД
- выдача метаданных файла по запросу

Файл сохраняется на диске контейнера. В базу данных записывается 
только путь к файлу.

### File Analysis Service (SD3-FileAnalysisService)

Отвечает только за проведение анализа, хранение отчетов и выдачу отчетов.

Ответственность:
- получение информации о файле для анализа
- проведение проверки на плагиат
- формирование отчета об анализе
- сохранение отчета в СУБД
- генерация облака слов для файлов формата .txt (через quickchart.io)
- выдача отчетов по запросу

### Схема взаимодействия

Микросервисы взаимодействуют через API Gateway. Данные между сервисами передаются либо через приватные DTO, либо через JSON, в зависимости от того, где это удобнее в конкретном сценарии.

---


## Сценарии межсервисного взаимодействия

### Сценарий: загрузка и анализ файла

Последовательность вызовов:

1. Клиент отправляет POST-запрос на API Gateway с файлом, studentId и assignmentId
2. API Gateway валидирует наличие файла
3. API Gateway перенаправляет файл в File Storing Service
4. File Storing Service сохраняет файл на диск и создает запись FileMetadata в базе данных
5. File Storing Service возвращает API Gateway метаданные сохраненного файла
6. API Gateway перенаправляет запрос на анализ в File Analysis Service, передавая идентификатор файла
7. File Analysis Service выполняет проверку на плагиат (побайтовое сравнение с ранее загруженными файлами)
8. File Analysis Service создает отчет AnalysisReport и сохраняет его в базу данных
9. Если файл имеет формат .txt, File Analysis Service отправляет запрос к quickchart.io для генерации облака слов и сохраняет путь к изображению
10. File Analysis Service возвращает API Gateway отчет об анализе
11. API Gateway агрегирует результаты и возвращает клиенту ответ

### Сценарий: получение аналитики по заданию

Последовательность вызовов:

1. Клиент отправляет GET-запрос на API Gateway с идентификатором задания
2. API Gateway перенаправляет запрос в File Analysis Service
3. File Analysis Service извлекает из базы данных все отчеты по данному заданию
4. File Analysis Service возвращает список отчетов с флагами плагиата
5. API Gateway возвращает результат клиенту

### Формат обмена данными

Между сервисами данные передаются в формате JSON. Для типизации данных используются DTO-объекты и Value Objects (StudentId, AssignmentId, FileName, FilePath, ContentType, WordCloudPath), обеспечивающие валидацию на уровне доменной модели.

---

## Технологический стек

### Язык и платформа
- C# 12
- .NET 8.0
- ASP.NET Core 8.0

### База данных и ORM
- PostgreSQL 16
- Entity Framework Core 8.0.10

### Контейнеризация
- Docker
- Docker Compose

### Документация API
- Swagger

### Внешние сервисы
- quickchart.io Word Cloud API (для визуализации облака слов)

### Архитектурные подходы
- Микросервисная архитектура
- паттерн API Gateway
- Domain-Driven Design (Value Objects)
- Синхронное межсервисное взаимодействие по HTTP

---

## Структура проекта


```text
Anti-plagiarism-system/
├── SD3-APIGateway/ # Сервис-посредник
│ ├── Controllers/
│ │         └── GatewayController.cs # Маршрутизация запросов к микросервисам
│ ├── Properties/
│ │         └── launchSettings.json # Настройки запуска
│ ├── Program.cs # Точка входа, DI-контейнер, HTTP-клиенты, Swagger
│ ├── appsettings.json # Конфигурация
│ ├── appsettings.Development.json
│ ├── Dockerfile # Контейнеризация сервиса
│ └── SD3-APIGateway.csproj # Файл проекта .NET
│
├── SD3-FileStoringService/ # Сервис хранения и выдачи файлов
│ ├── Controllers/
│ │         └── FilesController.cs # Загрузка файлов, выдача метаданных
│ ├── Infrastructure/
│ │         └── FileStorageDbContext.cs # Контекст БД
│ ├── Migrations/
│ ├── Models/
│ │         ├── FileMetadata.cs # Доменная модель метаданных файла
│ │         └── Values/ # Value Objects
│ │         ├── AssigmentID.cs
│ │         ├── ContentType.cs
│ │         ├── FileName.cs
│ │         ├── FilePath.cs
│ │         └── StudentID.cs
│ ├── Properties/
│ │         └── launchSettings.json
│ ├── Program.cs # Точка входа, миграции при старте, Swagger
│ ├── appsettings.json # Строка подключения к PostgreSQL
│ ├── appsettings.Development.json
│ ├── Dockerfile # Контейнеризация, создание /app/uploads
│ └── SD3-FileStoringService.csproj
│
├── SD3-FileAnalysisService/ # Сервис анализа, отчетов и облака слов
│ ├── Controllers/
│ │         └── AnalysisController.cs # Запуск анализа, выдача отчетов
│ ├── Infrastructure/
│ │         └── FileAnalysisDbContext.cs # Контекст БД
│ ├── Migrations/
│ ├── Models/
│ │         ├── AnalysisReport.cs # Доменная модель отчета об анализе
│ │         ├── FileMetadata.cs # Метаданные файла
│ │         └── Values/ # Value Objects
│ │         ├── AssigmentID.cs
│ │         ├── ContentType.cs
│ │         ├── FileName.cs
│ │         ├── FilePath.cs
│ │         ├── StudentID.cs
│ │         └── WordCloudPath.cs # Путь к облаку слов
│ ├── Properties/
│ │         └── launchSettings.json
│ ├── Program.cs # Точка входа, миграции при старте, Swagger
│ ├── appsettings.json # Строка подключения к PostgreSQL
│ ├── appsettings.Development.json
│ ├── Dockerfile # Контейнеризация сервиса
│ └── SD3-FileAnalysisService.csproj
│
├── SD3.sln
├── docker-compose.yml # Оркестрация всех сервисов и БД
├── .dockerignore
├── .gitattributes
├── .gitignore
└── README.md

```

---

## API документация

Все микросервисы предоставляют интерактивную документацию API через Swagger UI. Swagger подключен в каждом сервисе в файле Program.cs.

### Доступ к Swagger UI

| Сервис | URL Swagger UI |
|--------|----------------|
| API Gateway | http://localhost:8080/swagger |
| File Storing Service | http://localhost:8081/swagger |
| File Analysis Service | http://localhost:8082/swagger |


---

## Установка и запуск

### Предварительные требования

- Docker Desktop (версия 20.10 или выше) с поддержкой Docker Compose
- Git

### Запуск системы

1. Клонируйте репозиторий и перейдите в директорию проекта:

~~~
git clone https://github.com/Restmine20/Anti-plagiarism-system
cd Anti-plagiarism-system
~~~

2. Запустите все сервисы:

~~~
docker compose up --build
~~~

3. После запуска сервисы доступны по адресам:

| Сервис | URL |
|--------|-----|
| API Gateway | http://localhost:8080 |
| File Storing Service | http://localhost:8081 |
| File Analysis Service | http://localhost:8082 |

### Остановка системы

~~~
docker compose down
~~~

Для остановки с удалением данных

~~~
docker compose down -v
~~~

---


## Визуализация: облако слов

В качестве дополнительной функциональности реализована визуализация присланной работы в виде облака слов.

### Описание

- Облако слов генерируется с помощью внешнего API quickchart.io (документация: https://quickchart.io/documentation/word-cloud-api/).
- Визуализация выполняется только для файлов формата .txt.
- Для файлов других форматов (PDF, DOCX и т.д.) облако слов не создается, поле WordCloudPath в отчете остается пустым.
- Результат генерации (изображение) сохраняется, путь к нему записывается в отчет.

### Интеграция

File Analysis Service отправляет HTTP-запрос к API quickchart.io с текстовым содержимым файла. В ответ сервис получает изображение облака слов, которое сохраняется и привязывается к отчету об анализе.
