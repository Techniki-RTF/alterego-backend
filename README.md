# Alter Ego

- Backend API for Alter Ego application

## How it Works

1. **User A** types a real message → LLM generates innocent cover text → sends cover to Telegram
2. **User B** receives cover message → decodes via plugin → sees original message
3. Messages are matched by cover text hash + timestamp (works with different message IDs in p2p chats)

## API Endpoints

### Public
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/status` | Health check |
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/refresh` | Refresh tokens |
| POST | `/api/auth/logout` | Logout |

### Protected (requires JWT)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/messages/mask` | Generate cover text by dialogue context |
| POST | `/api/messages` | Store sent message (id/date/original/cover) |
| POST | `/api/messages/decode` | Decode cover text to original |
| GET | `/api/messages/{dialogId}` | Get messages (cursor pagination) |
| GET | `/api/messages/{dialogId}/updates` | Long-polling for new messages |
| POST | `/api/messages/{dialogId}/context/reset` | Reset dialogue context |
| POST | `/api/llm/generate` | Generate cover text only (test purposes) |

### Admin Only
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List all users |
| POST | `/api/users` | Create user |
| GET | `/api/users/{id}` | Get user |
| PUT | `/api/users/{id}/password` | Set password |
| PUT | `/api/users/{id}/role` | Change role |
| DELETE | `/api/users/{id}` | Delete user |

## Requirements

- .NET 10.0 SDK
- PostgreSQL 16+ (for production)
- Docker (optional, for running PostgreSQL)
- API key for your chosen LLM provider

## Development Setup

### 1. Clone and restore

```bash
cd src
dotnet restore
```

### 2. Configure user secrets

```bash
cd src/AlterEgo.Api
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=5432;Database=alterego;Username=postgres;Password=postgres"
dotnet user-secrets set "AdminUser:Password" "password"
dotnet user-secrets set "Jwt:Key" "ABA8F7F09B6C2F7A42E63BD62873CA051A98BF1FB09B5EC006036E3B444134F42"
dotnet user-secrets set "Llm:ApiKey" "your-api-key"
```

### 3. Run PostgreSQL (optional)

Skip this if using InMemory database for development.

```bash
docker compose -f infra/docker-compose.yml up -d
```

### 4. Run the API

```bash
cd src/AlterEgo.Api
dotnet run
```

API will be available at `http://localhost:5089`. Swagger UI at `/swagger`.

## Configuration

### Database Provider

Set in `appsettings.json` or `appsettings.Development.json`:

| Provider | Description |
|----------|-------------|
| `InMemory` | In-memory database (default for Development) |
| `PostgreSQL` | PostgreSQL database (default for Production) |

### LLM Provider

| Setting | Description |
|---------|-------------|
| `Llm:Provider` | Provider name (required, see table below) |
| `Llm:ApiKey` | API key for the chosen provider (required) |
| `Llm:ModelName` | Model to use (required) |

Available providers:

| `Llm:Provider` | Where to get API key | Example model |
|----------------|----------------------|---------------|
| `OpenAi` | platform.openai.com | `gpt-5.4-mini` |
| `Anthropic` | console.anthropic.com | `claude-haiku-4-5` |
| `Google` | aistudio.google.com | `gemini-3.1-flash-lite` |
| `Groq` | console.groq.com | `llama-3.1-8b-instant` |
| `Mistral` | console.mistral.ai | `mistral-small-4` |
| `DeepSeek` | platform.deepseek.com | `deepseek-v4-flash` |
| `XAi` | console.x.ai | `grok-4.3` |
| `Cohere` | dashboard.cohere.com | `command-r-plus` |
| `OpenRouter` | openrouter.ai | `meta-llama/llama-3.1-8b-instruct` |
| `Perplexity` | perplexity.ai/settings/api | `sonar` |
| `AzureOpenAi` | portal.azure.com | `gpt-5.4-mini` |

### Environment Variables (Production)

| Variable | Description |
|----------|-------------|
| `Database__Provider` | `PostgreSQL` or `InMemory` |
| `Database__ConnectionString` | PostgreSQL connection string |
| `Jwt__Key` | 64-character hex key for JWT signing |
| `AdminUser__Username` | Admin username (default: `admin`) |
| `AdminUser__Password` | Admin password |
| `AdminUser__TelegramId` | Admin Telegram ID |
| `Llm__Provider` | Provider name (e.g. `Google`, `OpenAi`, `Anthropic`) |
| `Llm__ApiKey` | API key for the chosen provider |
| `Llm__ModelName` | Model name |

## Production Deployment

### 1. Build

```bash
cd src/AlterEgo.Api
dotnet publish -c Release -o ./publish
```

### 2. Set environment variables

```bash
export Database__Provider="PostgreSQL"
export Database__ConnectionString="Host=db;Port=5432;Database=alterego;Username=alterego;Password=secure-password"
export Jwt__Key="ABA8F7F09B6C2F7A42E63BD62873CA051A98BF1FB09B5EC006036E3B444134F42"
export AdminUser__Password="password"
export Llm__Provider="Google"
export Llm__ApiKey="your-api-key"
export Llm__ModelName="gemini-2.0-flash-001"
```

### 3. Run

```bash
cd publish
dotnet AlterEgo.Api.dll
```

Database migrations run automatically on startup.

## Generate JWT Key

```bash
openssl rand -hex 32
```
