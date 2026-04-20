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
| POST | `/api/messages` | Store message, get cover text |
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
- Google AI Studio API key (for Gemini LLM)

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
dotnet user-secrets set "Gemini:ApiKey" "your-google-ai-studio-api-key"
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

### Gemini LLM

| Setting | Description |
|---------|-------------|
| `Gemini:ApiKey` | Google AI Studio API key (required) |
| `Gemini:ModelName` | Model to use (default: `gemini-3-flash-preview`) |

### Environment Variables (Production)

| Variable | Description |
|----------|-------------|
| `Database__Provider` | `PostgreSQL` or `InMemory` |
| `Database__ConnectionString` | PostgreSQL connection string |
| `Jwt__Key` | 64-character hex key for JWT signing |
| `AdminUser__Username` | Admin username (default: `admin`) |
| `AdminUser__Password` | Admin password |
| `AdminUser__TelegramId` | Admin Telegram ID |
| `Gemini__ApiKey` | Google AI Studio API key |
| `Gemini__ModelName` | Gemini model name (optional) |

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
export Gemini__ApiKey="your-google-ai-studio-api-key"
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
