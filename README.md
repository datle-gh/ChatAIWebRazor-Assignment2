# ChatAIWeb

ChatAIWeb is an ASP.NET Core MVC application that provides a RAG chatbot for Vietnamese study materials. The system lets Admins/Teachers upload PDF, DOCX, and PPTX files, split the content into chunks, generate embeddings, retrieve relevant context, and produce answers with source citations.

The project follows a 3-layer architecture for the PRN222/FPT course: `Presentation`, `BusinessLogic`, `DataAccess`, `BusinessObject`.

## Table of Contents

- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Quick Setup](#quick-setup)
- [AI/RAG Configuration](#airag-configuration)
- [Running the Application](#running-the-application)
- [Seed Accounts](#seed-accounts)
- [Project Architecture](#project-architecture)
- [Database](#database)
- [Python Services](#python-services)
- [Common Commands](#common-commands)
- [Troubleshooting](#troubleshooting)

## Key Features

- Login/logout by role: `Admin`, `Teacher`, `Student`.
- Manage subjects and users.
- Upload study materials in PDF, DOCX, PPTX formats.
- Extract text from documents, split into chunks, generate embeddings.
- RAG chatbot answers questions based on uploaded documents.
- Display citations by document, page/slide, chunk, and similarity score.
- Store conversation history by user/subject.
- Benchmark chat/RAG with an evaluation question set.
- Support for multiple embedding models:
  - `bge-m3` via Ollama.
  - `vinai/phobert-base` via the PhoBERT FastAPI service.
  - Fine-tuned PhoBERT model if an artifact is available.
- Support for Qdrant as a vector store, with SQL as fallback.
- Support for a RAGAS Python service to score RAG.

## Tech Stack

| Component | Technology |
| --- | --- |
| Backend web | ASP.NET Core MVC, .NET 8 |
| UI | Razor Views, Bootstrap, jQuery, SignalR |
| Database | SQL Server, Entity Framework Core |
| Auth | Cookie Authentication |
| LLM | Google Gemini, Fake LLM fallback |
| Embedding | Ollama `bge-m3`, PhoBERT FastAPI, Fake embedding fallback |
| Vector search | SQL cosine fallback, Qdrant REST API |
| File parsing | PdfPig, OpenXML |
| Benchmark | RAGAS service, LLM-as-judge fallback |
| Python services | FastAPI, transformers, sentence-transformers, ragas |

## Prerequisites

You need to install:

- .NET SDK 8.0 or later.
- SQL Server or SQL Server Express.
- Visual Studio 2022 or VS Code.
- Ollama if using the `bge-m3` embedding.
- Python 3.10+ if using PhoBERT/RAGAS/fine-tuning.
- Qdrant if you want to use the Qdrant vector store.

Quick check:

```powershell
dotnet --version
python --version
ollama --version
```

## Quick Setup

### 1. Clone or open the source code

```powershell
cd D:\Study_Research\Knowledge\IT\FPT\kì_7\PRN222\Assignments
```

If cloning from Git:

```powershell
git clone <repo-url> ChatAIWeb
cd ChatAIWeb
```

### 2. Restore and build

```powershell
dotnet restore ChatAIWeb.slnx
dotnet build ChatAIWeb.slnx
```

A successful build shows:

```text
Build succeeded.
```

### 3. Create the database

Open SQL Server Management Studio or Azure Data Studio, then run the file:

```text
ChatAIWeb_Database.sql
```

This file creates the `ChatAIWebDb` database, the required tables, and seeds demo data.

If you already have an old database, also run the Phase 2 migration script if the file exists in the project:

```text
Phase2_Database_Migration.sql
```

The application also has a startup initializer that can add some Phase 2 tables/columns automatically if it can connect to the database.

### 4. Check the connection string

Open the file:

```text
Presentation/appsettings.json
```

Default:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ChatAIWebDb;Trusted_Connection=True;TrustServerCertificate=true;Encrypt=true;"
}
```

If your machine hits a SQL Server encryption error, try changing it to:

```json
"DefaultConnection": "Server=localhost;Database=ChatAIWebDb;Trusted_Connection=True;TrustServerCertificate=true;Encrypt=false;"
```

If using SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=ChatAIWebDb;Trusted_Connection=True;TrustServerCertificate=true;Encrypt=false;"
```

### 5. Run the application

From the command line:

```powershell
dotnet run --project Presentation\Presentation.csproj --launch-profile https
```

Or open the solution in Visual Studio and click Run.

Default URLs:

- HTTPS: <https://localhost:7108>
- HTTP: <http://localhost:5039>

## AI/RAG Configuration

Configuration lives in:

```text
Presentation/appsettings.json
```

### Gemini LLM

Configuration section:

```json
"Llm": {
  "Provider": "Gemini",
  "Model": "gemini-2.5-flash",
  "Gemini": {
    "BaseUrl": "https://generativelanguage.googleapis.com",
    "Temperature": 0.2,
    "MaxOutputTokens": 1024
  }
}
```

The API key should be stored using User Secrets or an environment variable, not committed to the source code.

Example with User Secrets:

```powershell
cd Presentation
dotnet user-secrets set "Llm:Gemini:ApiKey" "<your-gemini-api-key>"
```

### Ollama bge-m3 embedding

Pull the model:

```powershell
ollama pull bge-m3
```

Warm up the embedding model:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:11434/api/embed" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"model":"bge-m3","input":"This is a warm-up test sentence for the embedding.","truncate":true}'
```

Check the loaded model:

```powershell
ollama ps
```

### Embedding models

Default in `appsettings.json`:

```json
"Embedding": {
  "Provider": "Ollama",
  "DefaultModel": "bge-m3",
  "DefaultModelKey": "bge-m3",
  "Models": [
    {
      "Key": "bge-m3",
      "Provider": "Ollama",
      "Model": "bge-m3",
      "BaseUrl": "http://localhost:11434",
      "Dimension": 1024,
      "Enabled": true,
      "IncludeInBenchmark": true
    },
    {
      "Key": "phobert-base",
      "Provider": "PhoBert",
      "Model": "vinai/phobert-base",
      "BaseUrl": "http://localhost:8001",
      "Dimension": 768,
      "Enabled": true,
      "IncludeInBenchmark": true
    }
  ]
}
```

If you have not started the PhoBERT service yet, you can temporarily disable the PhoBERT model:

```json
"Enabled": false
```

### Qdrant

Configuration:

```json
"VectorStore": {
  "Provider": "Qdrant",
  "DualWrite": true
},
"Qdrant": {
  "Host": "localhost",
  "Port": 6333,
  "UseHttps": false,
  "ApiKey": "",
  "CollectionPrefix": "chataiweb"
}
```

If Qdrant is not running, the application logs a warning and falls back to SQL vector search.

Run Qdrant with Docker:

```powershell
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

Or switch the provider to SQL:

```json
"VectorStore": {
  "Provider": "Sql",
  "DualWrite": false
}
```

## Running the Application

### With Visual Studio

1. Open `ChatAIWeb.slnx`.
2. Set the startup project to `Presentation`.
3. Select the `https` profile.
4. Click Run.
5. Open <https://localhost:7108>.

### With the terminal

```powershell
dotnet run --project Presentation\Presentation.csproj --launch-profile https
```

If you hit a port-in-use error:

```powershell
netstat -ano | Select-String ':7108|:5039'
```

Stop the process holding the port:

```powershell
Stop-Process -Id <PID> -Force
```

## Seed Accounts

The seed passwords are recorded in `ChatAIWeb_Database.sql`.

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@chataiweb.local` | `Admin@123` |
| Teacher demo | `teacher@chataiweb.local` | `Teacher@123` |
| Student demo | `student@chataiweb.local` | `Student@123` |

The seeded database also contains many other teachers and students.

## Project Architecture

```text
ChatAIWeb
├── BusinessObject
│   ├── Entities
│   └── Enums
├── DataAccess
│   ├── ChatAIWebDbContext.cs
│   └── Repositories
├── BusinessLogic
│   ├── DTOs
│   ├── Infrastructure
│   └── Services
├── Presentation
│   ├── Controllers
│   ├── Models
│   ├── Views
│   ├── wwwroot
│   └── Program.cs
└── python_services
    ├── phobert_service.py
    ├── ragas_service.py
    ├── finetune_embedding.py
    └── requirements.txt
```

### Presentation

Contains the ASP.NET Core MVC UI:

- Controllers.
- Razor views.
- ViewModels.
- Cookie authentication.
- Dependency injection in `Program.cs`.
- SignalR upload progress hub.

Controllers should be thin and only call services in `BusinessLogic`.

### BusinessLogic

Contains the business workflows:

- Document upload and indexing.
- Text extraction orchestration.
- Chunking.
- Embedding generation.
- Vector search.
- RAG chatbot.
- Citation mapping.
- RAGAS benchmark.
- System settings.

### DataAccess

Contains the EF Core DbContext and repositories:

- Query the database.
- CRUD.
- Include navigation data.
- Store chat, document, chunk, citation, and benchmark results.

### BusinessObject

Contains the shared entities and enums:

- `User`
- `Subject`
- `SubjectEnrollment`
- `Document`
- `DocumentChunk`
- `DocumentChunkEmbedding`
- `ChatSession`
- `ChatMessage`
- `Citation`
- `EvaluationQuestion`
- `RagasBenchmarkResult`

## RAG Pipeline

The flow always runs as:

```text
Upload document
→ Save file
→ Extract text
→ Split into chunks
→ Generate embeddings
→ Save SQL metadata
→ Upsert Qdrant if available
→ Mark document as Indexed

Student question
→ Generate question embedding
→ Search vector store
→ Filter by similarity threshold
→ Build prompt with retrieved chunks
→ Call LLM
→ Save chat message
→ Save citations
→ Return answer + sources
```

## Database

Main database: `ChatAIWebDb`.

Important tables:

| Table | Purpose |
| --- | --- |
| `Users` | Accounts and roles |
| `Subjects` | Subjects |
| `SubjectEnrollments` | Assign users to subjects |
| `Documents` | Uploaded document metadata |
| `DocumentChunks` | Text chunks from documents |
| `DocumentChunkEmbeddings` | Embeddings per chunk/model |
| `ChatSessions` | Chat sessions |
| `ChatMessages` | Questions/answers |
| `Citations` | Answer source citations |
| `EvaluationQuestions` | Benchmark question set |
| `RagasBenchmarkResults` | Benchmark results |

### Recreate the database from scratch

Be careful: this operation will drop the old database if the script contains `DROP DATABASE`/`DROP TABLE`.

1. Open `ChatAIWeb_Database.sql`.
2. Run it with SSMS/Azure Data Studio.
3. Verify the `ChatAIWebDb` database.
4. Run the app.

### Old database after Phase 2

If the old database is missing columns:

- `RunId`
- `VectorStore`
- `RetrievedContextsJson`
- the `DocumentChunkEmbeddings` table

Run:

```text
Phase2_Database_Migration.sql
```

## Python Services

The Python services are optional, but needed if you want to use PhoBERT/RAGAS/fine-tuning.

### Install dependencies

```powershell
cd python_services
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

### Run the PhoBERT service

```powershell
python phobert_service.py
```

Default:

- Health: <http://localhost:8001/health>
- Embed: `POST http://localhost:8001/embed`

Test:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:8001/embed" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"model":"vinai/phobert-base","texts":["Hello, this is a test sentence."]}'
```

### Run the RAGAS service

Requires a Gemini API key:

```powershell
$env:GOOGLE_API_KEY="<your-gemini-api-key>"
python ragas_service.py
```

Default:

- Health: <http://localhost:8002/health>
- Evaluate: `POST http://localhost:8002/evaluate`

If the RAGAS service is not running, the .NET service falls back to LLM-as-judge.

### Fine-tune the embedding

Export the dataset from SQL:

```powershell
python finetune_embedding.py `
  --connection-string "Driver={ODBC Driver 17 for SQL Server};Server=localhost;Database=ChatAIWebDb;Trusted_Connection=yes;TrustServerCertificate=yes;" `
  --subject-id 1 `
  --dataset artifacts/embedding_train.jsonl `
  --export-only
```

Train the model:

```powershell
python finetune_embedding.py `
  --dataset artifacts/embedding_train.jsonl `
  --base-model vinai/phobert-base `
  --output-dir models/phobert-finetuned `
  --epochs 1 `
  --batch-size 8
```

After you have the fine-tuned model, enable it in `appsettings.json`:

```json
{
  "Key": "phobert-finetuned",
  "Provider": "PhoBert",
  "Model": "models/phobert-finetuned",
  "Enabled": true
}
```

## Common Commands

| Command | Description |
| --- | --- |
| `dotnet restore ChatAIWeb.slnx` | Restore NuGet packages |
| `dotnet build ChatAIWeb.slnx` | Build the entire solution |
| `dotnet run --project Presentation\Presentation.csproj --launch-profile https` | Run the web app |
| `ollama pull bge-m3` | Download the embedding model |
| `ollama ps` | Check loaded Ollama models |
| `python python_services\phobert_service.py` | Run the PhoBERT embedding service |
| `python python_services\ragas_service.py` | Run the RAGAS service |

## Troubleshooting

### 1. `Failed to bind to address ... address already in use`

Port `7108` or `5039` is held by another process.

Check:

```powershell
netstat -ano | Select-String ':7108|:5039'
```

Stop the process:

```powershell
Stop-Process -Id <PID> -Force
```

### 2. SQL Server encryption error

Common error:

```text
The instance of SQL Server you attempted to connect to requires encryption but this machine does not support it.
```

Try changing the connection string:

```json
"Encrypt=false;TrustServerCertificate=true;"
```

### 3. RAGAS page reports invalid column

If you hit:

```text
Invalid column name 'RunId'
Invalid column name 'VectorStore'
Invalid column name 'RetrievedContextsJson'
```

Run:

```text
Phase2_Database_Migration.sql
```

Or restart the app so the startup initializer has a chance to update the schema automatically.

### 4. Ollama timeout when backfilling embeddings

The log looks like:

```text
Unable to backfill embedding ... TaskCanceledException
```

Common causes:

- Ollama is not running.
- The `bge-m3` model has not been pulled.
- The model is loading for the first time and is slow.
- The chunk is too long, or the machine lacks RAM/CPU/GPU.

Fix:

```powershell
ollama pull bge-m3
ollama ps
```

Warm up:

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:11434/api/embed" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"model":"bge-m3","input":"This is a warm-up test sentence for the embedding.","truncate":true}'
```

### 5. Qdrant is not running

If Qdrant is not running, the app falls back to SQL search. If you want to disable Qdrant:

```json
"VectorStore": {
  "Provider": "Sql",
  "DualWrite": false
}
```

### 6. Upload page shows only some subjects

The upload page gets the subject list according to the rule in `DocumentService.GetUploadSubjectOptionsAsync`.

In the current version:

- Admin sees all subjects.
- Teacher sees all subjects.
- Student cannot upload.

If you want to restrict teachers to only upload assigned subjects, change the service to use `GetUploadableByTeacherAsync`.

### 7. Build is locked by a DLL file

If the build reports:

```text
The file is locked by: Microsoft Visual Studio, Presentation
```

Stop the app running in Visual Studio, then build again.

## Security Notes

- Do not commit the Gemini API key, OpenAI key, or Qdrant API key.
- Store secrets with User Secrets in development.
- Use environment variables or a secret manager in production.

## Suggested Demo Flow

1. Log in as Admin or Teacher.
2. Go to `Document Library`.
3. Select `Add new document`.
4. Upload a PDF/DOCX/PPTX into a subject.
5. Wait for the extract/chunk/embed/index process to finish.
6. Go to the chat for that subject.
7. Ask a question related to the document you just uploaded.
8. Check the answer and citations.
9. Go to the RAGAS benchmark page to evaluate multiple embedding models.

## License

This project serves learning and course-demo purposes. Update the license if you need to make it public or deploy to production.
