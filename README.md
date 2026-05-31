# RetroMask

A collaborative retrospective platform built with ASP.NET Core 8.0. RetroMask enables agile teams to run structured retrospective sessions with real-time voting, anonymous feedback, icebreaker games, AI-driven insights, and personal growth tracking.

## Architecture

```
RetroMask/
├── RetroMask.Domain/           # Entities, enums, interfaces (no dependencies)
├── RetroMask.Application/      # DTOs, service interfaces, validation, mapping
├── RetroMask.Infrastructure/   # EF Core, Identity, service implementations, AI
├── RetroMask.API/              # Controllers, middleware, SignalR hubs
└── RetroMask.Tests/            # Unit + integration tests (xUnit, Moq, FluentAssertions)
```

**Patterns:** Clean Architecture, Unit of Work + Generic Repository, CQRS-lite (commands via services, queries via DTOs), soft-delete with global query filters, `ApiResponse<T>` envelope.

## Features

| Module | Description |
|---|---|
| **Auth** | JWT (HS256) with access/refresh tokens, role-based (User/Admin/SuperAdmin), password reset, avatar upload |
| **Teams** | Create/manage teams, invite by email, role-based membership (Owner/Admin/Member) |
| **Sessions** | Full lifecycle (Draft → Active → Paused → Completed), 5 default phases, anonymous mask assignment |
| **Phases** | Activate/complete/skip/extend/advance/reorder phases with SignalR broadcasting |
| **Discussion Points** | CRUD with tags, emoji reactions, threaded comments, pin/unpin |
| **Voting** | Up/down votes per point, per-session vote budget enforcement, close voting |
| **Icebreaker Games** | 8 built-in game types, start/answer/complete flow, leaderboard scoring |
| **AI Analysis** | GPT-4o-mini powered insights, summaries, clustering, sentiment analysis, recommendations |
| **Feedback** | Anonymous peer feedback with tone classification (Praise/Constructive/General), toxic content filtering |
| **Action Items** | Task assignment with priority/status tracking, progress updates, assignee notifications |
| **Reports** | Session/team reports, export to Markdown/HTML/PDF, shareable links |
| **Notifications** | Real-time via SignalR + persisted, unread count, mark read/all-read |
| **Insights** | Personal engagement scoring, participation trends, monthly growth snapshots |
| **File Storage** | Upload images/PDFs/attachments (max 10 MB), local disk storage with public URLs |

## Tech Stack

- **Runtime:** .NET 8.0
- **Database:** SQL Server (EF Core 8.0 + Migrations)
- **Authentication:** ASP.NET Core Identity + JWT Bearer
- **Real-time:** SignalR (`/hubs/session`)
- **AI:** OpenAI GPT-4o-mini (graceful fallback when unavailable)
- **Validation:** FluentValidation 11.x
- **Mapping:** AutoMapper 16.x
- **Logging:** Serilog (Console + File sinks)
- **Testing:** xUnit, Moq, FluentAssertions 8.x, EF Core InMemory
- **Docs:** Swagger/OpenAPI with XML comments

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, or full)

### Configuration

> **Important:** Never commit real secrets to `appsettings.json`. Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development and environment variables for production.

1. **Initialize User Secrets:**
   ```bash
   cd RetroMask.API
   dotnet user-secrets init
   ```

2. **Database** - Update the connection string:
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=RetroMaskDb;Trusted_Connection=True;TrustServerCertificate=True"
   ```

3. **JWT** - Set a strong signing key (minimum 32 characters):
   ```bash
   dotnet user-secrets set "JwtSettings:SecretKey" "your-256-bit-secret"
   ```

4. **OpenAI (optional)** - Set your API key for AI features:
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "sk-your-key"
   ```
   AI features gracefully degrade when no key is configured.

5. **Email (optional)** - SMTP is disabled by default (`"UseSmtp": false`). Enable and configure for password reset and team invitation emails.

### Run

```bash
cd RetroMask.API
dotnet run
```

The API starts at `https://localhost:5001` (or `http://localhost:5000`). The database is automatically created and migrated on first run. A default SuperAdmin account is seeded:
- **Email:** `admin@retromask.com`
- **Password:** `Admin@123456`

### Swagger UI

Navigate to `https://localhost:5001/swagger` to explore all endpoints. Click "Authorize" and enter `Bearer {your-token}` to test authenticated endpoints.

## API Overview

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new account |
| POST | `/api/auth/login` | Login, get JWT tokens |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Revoke refresh token |
| GET | `/api/auth/me` | Get current user profile |
| POST | `/api/auth/avatar` | Upload avatar image |

### Teams
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/teams` | List my teams |
| POST | `/api/teams` | Create a team |
| POST | `/api/teams/{id}/invite` | Invite member by email |
| POST | `/api/teams/invitations/{token}/accept` | Accept invitation |

### Sessions
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sessions` | Create session (5 default phases) |
| POST | `/api/sessions/{id}/start` | Start session |
| POST | `/api/sessions/{id}/join` | Join session |
| POST | `/api/sessions/{id}/complete` | Complete session |

### Phases
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/sessions/{id}/phases` | List phases |
| POST | `/api/sessions/{id}/phases/advance` | Advance to next phase |
| POST | `/api/sessions/{id}/phases/{phaseId}/extend` | Extend phase timer |

### Discussion & Voting
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/phases/{phaseId}/points` | Create discussion point |
| POST | `/api/points/{pointId}/votes` | Cast vote (Up/Down) |
| POST | `/api/points/{pointId}/votes/close` | Close voting |

### AI Analysis
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/sessions/{id}/ai/insights` | Get AI insights |
| POST | `/api/sessions/{id}/ai/summary` | Generate summary |
| POST | `/api/sessions/{id}/ai/clusters` | Cluster points |
| POST | `/api/sessions/{id}/ai/sentiment` | Analyze sentiment |

### Feedback & Action Items
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/feedback` | Send anonymous feedback |
| POST | `/api/actionitems` | Create action item |
| POST | `/api/actionitems/{id}/updates` | Add progress update |

### Notifications & Insights
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/notifications` | List notifications |
| GET | `/api/notifications/unread-count` | Get unread count |
| GET | `/api/insights/me` | Personal insights |
| GET | `/api/insights/me/growth` | Growth snapshots |

## Real-time (SignalR)

Connect to `/hubs/session` for real-time events:
- `SessionStarted` - Session has begun
- `PhaseChanged` - Active phase transition
- `GameStarted` / `GameCompleted` - Icebreaker events
- `VotesUpdated` - Vote count changed
- `NotificationReceived` - New notification for user

## Testing

```bash
dotnet test
```

**60 tests** covering:
- **Unit tests (48):** Session lifecycle, voting (budget enforcement, close, remove), action items, feedback (tone classification, toxic filtering), icebreaker games, notifications, personal insights
- **Integration tests (12):** Full HTTP pipeline via `WebApplicationFactory` - auth flow, notifications, insights, team CRUD

## Project Structure

```
RetroMask.Domain/
  Entities/          # 12 entity groups: Identity, Teams, Sessions, Discussion,
                     # Voting, Game, ActionItems, Feedback, Notifications,
                     # Insights, Files, AI
  Enums/             # SessionStatus, PhaseStatus, VoteType, ActionItemPriority, etc.

RetroMask.Application/
  Dtos/              # Request/Response DTOs per feature
  Services/          # Service interfaces (ISessionService, IVotingService, etc.)
  Mapping/           # AutoMapper profiles
  Validators/        # FluentValidation rules
  Common/            # ApiResponse<T>, PagedResult<T>, exceptions

RetroMask.Infrastructure/
  Persistence/       # DbContext, UnitOfWork, GenericRepository, migrations
  Services/          # Service implementations (12 services)
  Identity/          # IdentitySeeder, JWT token service
  Realtime/          # SignalR SessionHub + ISessionBroadcaster

RetroMask.API/
  Controllers/       # 14 controllers (Auth, Teams, Sessions, Phases, Points,
                     # Voting, AI, ActionItems, Feedback, Reports,
                     # Notifications, Game, Insights, Files)
  Middleware/        # Global exception handling
  Authorization/     # CurrentUser from JWT claims
```

## License

This project was built as a graduation project.
