# Dating — Service + Telegram Bot

**Repository:** https://github.com/TimeBean/dating  
**Author:** TimeBean

Monorepository containing a Web API, a Telegram bot, and shared contracts. The project is designed to store and process user profiles with conversational data entry via Telegram.

---

## Contents

- [Quickstart](#quickstart)
- [Configuration](#configuration)
- [Repository structure](#repository-structure)
- [API](#api)
- [Telegram bot](#telegram-bot)
- [Database and migrations](#database-and-migrations)
- [Local development](#local-development)
- [License](#license)

---

## Quickstart

```bash
# Clone the repository
git clone https://github.com/TimeBean/dating.git
cd dating

# Restore dependencies and build
dotnet restore
dotnet build Dating.sln

# Run the API
dotnet run --project DatingAPI --urls "http://localhost:5000"

# Run the Telegram bot (in a separate terminal)
dotnet run --project DatingTelegramBot
```

---

## Configuration

### API

Configuration files:

- `DatingAPI/appsettings.json`
- `DatingAPI/appsettings.Development.json`

Example `appsettings` snippet:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=dating;Username=postgres;Password=postgres"
  }
}
```

### Telegram bot

Environment variables used by the bot:

- `TELEGRAM_BOT_TOKEN` — Telegram bot token  
- `API_BASE_URL` — base URL of the API

Example (Linux/macOS):

```bash
export TELEGRAM_BOT_TOKEN="<token>"
export API_BASE_URL="http://localhost:5000"
```

---

## Repository structure

```text
.
├── DatingAPI              # ASP.NET Core Web API
├── DatingAPIWrapper       # Client wrapper for the API
├── DatingContracts        # DTOs and shared contracts
├── DatingTelegramBot      # Telegram bot and dialog flows
├── StepAnalyzer           # Visualization of dialog steps
├── Dating.sln
├── global.json
└── LICENSE
```

### Key components

- **DatingAPI** — HTTP endpoints, domain models, `DbContext`
- **DatingAPIWrapper** — typed client for calling the API
- **DatingContracts** — DTOs and dialog state definitions
- **DatingTelegramBot** — commands, dialog steps, session storage

---

## API

Primary endpoints:

| Method | Path          | Description               |
|-------:|---------------|---------------------------|
| GET    | `/users`      | Retrieve all users        |
| GET    | `/users/{id}` | Retrieve a user by ID     |
| POST   | `/users`      | Create a new user         |
| PUT    | `/users/{id}` | Update an existing user   |
| DELETE | `/users/{id}` | Delete a user             |

Example request:

```bash
curl -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Ivan","age":30,"place":"Rostov-on-Don","description":"Description"}'
```

---

## Telegram bot

The Telegram bot implements a step-by-step conversational flow for collecting and updating user profile data.

Main areas:

- `Commands` — startup and service commands
- `DialogSteps` — dialog steps (name, age, place, description)
- `Repositories` — session and user data persistence
- `ObjectStores` — S3 / MinIO-compatible storage backends

Start the bot:

```bash
dotnet run --project DatingTelegramBot
```

---

## Database and migrations

The project uses Entity Framework Core.

```bash
# Install EF tools (once)
dotnet tool install --global dotnet-ef

# Create a migration
dotnet ef migrations add InitialCreate \
  --project DatingAPI \
  --startup-project DatingAPI

# Apply migrations
dotnet ef database update \
  --project DatingAPI \
  --startup-project DatingAPI
```

---

## Local development

Run the API with hot reload:

```bash
dotnet watch run --project DatingAPI
```

Run the Telegram bot in a separate terminal.

---

## License

See the LICENSE file in the repository.
