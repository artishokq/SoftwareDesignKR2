# Контрольная работа №2 КПО - Синхронное межсервисное взаимодействие
---

Набор из трёх микросервисов на .NET 9 для загрузки, хранения и анализа текстовых отчётов студентов.

## Состав проекта

1. **ApiGateway**  
   - маршрутизация запросов через YARP  
   - порты: `5200:80`  
   - swagger: `http://localhost:5200/swagger/index.html`

2. **FileStoringService**  
   - хранение файлов, вычисление SHA256-хеша, предотвращение дубликатов  
   - хранит записи в In-Memory DB + файлы на диск в папке `uploads`  
   - порты: `5101:80`  
   - swagger: `http://localhost:5101/swagger/index.html`

3. **FileAnalysisService**  
   - подсчёт абзацев/слов/символов
   - кеширование результатов в памяти  
   - порты: `5102:80`  
   - swagger: `http://localhost:5102/swagger/index.html`

---

## Архитектура микросервисов

Система состоит из трёх независимых сервисов, каждый из которых отвечает за свою область:

---

### 1. API Gateway  
- **APIGatewayController.cs** — центральная точка входа для клиента. 
- **YARP Reverse Proxy** настроен в `appsettings.json` для перенаправления запросов к остальным сервисам по путям `/api/gateway/files/...` и `/api/gateway/analysis/...`.  
- Принимает файл от пользователя, передаёт его на File Storing Service и возвращает обратно сгенерированный идентификатор.  
- Получив идентификатор, перенаправляет запрос на анализ к File Analysis Service и отдаёт результаты пользователю.  
- Обеспечивает прозрачный доступ к функциям загрузки, скачивания и анализа через единый URL.

---

### 2. File Storing Service  
- **FileStoringController.cs** — все операции, связанные с физическим хранением отчётов.  
- **FileHashingService.cs** — вычисляет SHA-256 хеш содержимого и не позволяет сохранять дубликаты.  
- **FileService.cs** — сохраняет файл в локальную директорию, заносит запись в базу данных и выдаёт метаданные по запросу.  
- Поддерживает три ключевых сценария:  
  1. Приём нового файла и выдачу его уникального `fileId`.  
  2. Отдачу содержимого файла по `fileId`.  
  3. Выдачу информации об имени, размере и времени загрузки (metadata).

---

### 3. File Analysis Service  
- **AnalysisController.cs** — служит точкой входа для анализа: принимает `fileId` и возвращает готовый `AnalysisResult`.  
- **AnalysisService.cs** — основная бизнес-логика анализа:  
  - через встроенный `HttpClient` (настраивается в `Program.cs`) запрашивает у File Storing Service содержимое файла;  
  - считает число абзацев, слов и символов;  
  - строит «вектор частотности слов» для каждого отчёта;  
  - сохраняет результаты в память для повторных запросов.  
- **Models**:  
  - `AnalysisRequest` — оболочка `{ Guid FileId }` для POST-запроса;  
  - `AnalysisResult` — содержит `FileId`, `Paragraphs`, `Words`, `Characters` и список `Similarity`;  
  - `Similarity` — пары `{ OtherFileId, Score }`.  

---

### Основные принципы  
- **Разделение ответственности**: каждый сервис решает узкую задачу.  
- **Масштабируемость**: при росте нагрузки можно независимо разворачивать и увеличивать любой из сервисов.  
- **Однородные контракты**: все API документированы через Swagger и следуют OpenAPI-конвенциям.  
- **Простота интеграции**: внешнему клиенту достаточно работать с единственной точкой входа — API Gateway.  

---

## API Endpoints

Все сервисы подняты локально и доступны по следующим портам:

- **API Gateway**: `http://localhost:5200`  
- **File Storing Service**: `http://localhost:5101`  
- **File Analysis Service**: `http://localhost:5102`  

---

### API Gateway (порт 5200)

- **POST** `/api/gateway/files`  
  — загружает текстовый файл, возвращает `fileId`  
- **POST** `/api/gateway/analysis`  
  — запускает анализ для переданного в теле JSON `{ "fileId": "<GUID>" }`  
- **GET**  `/api/gateway/files/{fileId}`  
  — скачивает ранее загруженный файл  
- **GET**  `/api/gateway/analysis/{fileId}`  
  — возвращает результаты анализа (кол-во абзацев, слов, символов и список похожих файлов)

---

### File Storing Service (порт 5101)

- **POST** `/api/files/upload`  
  — загрузка файла, сохранение на диск и в БД, проверка дубликатов  
- **GET**  `/api/files/{fileId}/download`  
  — скачивание файла по его идентификатору  
- **GET**  `/api/files/{fileId}/metadata`  
  — получение метаданных: оригинальное имя, размер и время загрузки

---

### File Analysis Service (порт 5102)

- **POST** `/api/analysis`  
  — принимает JSON `{ "fileId": "<GUID>" }`, анализирует текст (абзацы, слова, символы, плагиат)  
- **GET**  `/api/analysis/{fileId}`  
  — возвращает сохранённый результат анализа

---
## Запуск локально (без Docker)

В каждом из трёх каталогов выполните:
```bash
cd ApiGateway
dotnet run
# слушает http://localhost:5200

cd ../FileStoringService
dotnet run
# слушает http://localhost:5101

cd ../FileAnalysisService
dotnet run
# слушает http://localhost:5102
```
---
## Запуск в Docker Compose

1. Скомпилировать проект:
```bash
docker-compose build
```

2. Запустить проект:
```bash
docker-compose up -d
```
3. Swagger UI:
- Gateway: http://localhost:5200/swagger/index.html
- FileStore: http://localhost:5101/swagger/index.html
- FileAnalysis: http://localhost:5102/swagger/index.html




---
## Тестирование

### Через Swagger  
1. Перейдите в интерфейс по адресу:  
   `http://localhost:5200/swagger/index.html`  
2. В разделе **Gateway** найдите метод **POST /api/gateway/files**, нажмите **Try it out**, прикрепите файл (тестовые файлы лежат в `test files`) и выполните запрос.  
3. Скопируйте возвращённый `fileId`.  
4. Вызовите **POST /api/gateway/analysis**, подставив тот же `fileId` в JSON-теле.  
5. Чтобы скачать загруженный файл, используйте **GET /api/gateway/files/{fileId}**.

---

### С помощью curl  
```bash
# Загрузка файла
curl -X POST http://localhost:5200/api/gateway/files \
     -F file=@./test-files/sample1.txt

# Анализ загруженного файла
curl -X POST http://localhost:5200/api/gateway/analysis \
     -H "Content-Type: application/json" \
     -d '{"fileId":"<fileId>"}'

# Скачивание файла
curl http://localhost:5200/api/gateway/files/<fileId> \
     -o downloaded.txt
```

### Юнит-тесты
Чтобы проверить покрытие, запустите тестовые проекты отдельно:

```bash
Tests/FileStoringServiceTests/FileServiceTests
Tests/FileAnalysisServiceTests/AnalysisServiceTests
Tests/ApiGatewayTests/GatewayControllerTests
```

- Покрытие тестами более 65%
<img width="465" alt="tests" src="https://github.com/user-attachments/assets/466d1c46-0c44-4545-9c53-bbd435b9996a" />
