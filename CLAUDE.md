# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Campus Lost & Found** — an ASP.NET Core 8 Web API for posting lost/found items and managing claims.
No authentication; uses string names (`OwnerName`, `RequesterName`) instead of user accounts by design.

## Commands

```bash
dotnet run          # Start API on http://0.0.0.0:5000 (PORT env var overrides port)
dotnet build        # Build only

# Tests — no external services required
dotnet test CampusLostAndFound.Tests/
dotnet test CampusLostAndFound.Tests/ --filter "FullyQualifiedName~GetListingByIdHandler"  # single class

# Open solution in IDE (contains main + test projects)
# open main.sln

# Docker
docker build -t main .
docker run -p 5000:5000 main
```

Use Swagger at `/swagger` for manual API testing (always enabled).

## Architecture

**MediatR CQRS** — controllers dispatch commands/queries; all business logic lives in handlers.

```
Commands/       — IRequest<T> records (one per operation)
Handlers/       — IRequestHandler<TRequest, TResult> implementations
Controllers/    — Thin HTTP layer; validates input, sends to IMediator, maps result to status code
Infrastructure/ — EmbeddingService, SemanticRerankService, N8nWebhookService
Mcp/            — McpToolDefinitions, McpToolRegistry, McpToolExecutor
Models/         — Listing and Claim EF entities
Dtos/           — C# records for all request/response contracts
Data/           — AppDbContext (PostgreSQL via Npgsql)
Services/       — IListingService/IClaimService exist but are not the main code path; handlers are
```

## Domain Model

**Listing** fields: `Id`, `OwnerName`, `Type`, `Title`, `Description`, `Category`, `Location`, `EventDate`, `PhotoUrl`, `Status`, `CreatedAt`, `UpdatedAt`, plus AI fields: `EmbeddingJson` (float[] as JSON), `AiTags`, `NormalizedLocation`, `AiSummary`.

**Claim** fields: `Id`, `ListingId`, `RequesterName`, `Message`, `Status`, `CreatedAt`, `DecidedAt`.

String enums:
- `Listing.Type`: `"Lost"` | `"Found"`
- `Listing.Status`: `"Open"` | `"Claimed"` | `"Closed"` | `"Hidden"`
- `Claim.Status`: `"Pending"` | `"Accepted"` | `"Rejected"`

## Database

- PostgreSQL via Npgsql; `ConnectionStrings:DefaultConnection`
- **No EF migrations** — schema via `EnsureCreated()` on startup; new columns added with raw `ALTER TABLE … ADD COLUMN IF NOT EXISTS`
- Table/column names are snake_case (`listings`, `claims`, `owner_name`, etc.)

## AI Subsystem

| Handler | Purpose |
|---|---|
| `RagSearchHandler` | Keyword filter → embedding query → cosine reranking → LLM-grounded answer |
| `SimilarListingsHandler` | k-nearest by embedding cosine similarity |
| `ClaimCheckHandler` | Scores claim message against its listing |
| `ModerationAnalyzeHandler` | Scans recent listings for policy violations |
| `FaqHandler` | Generates FAQ + trends from recent listing data |
| `AiAssistHandler` | Two-pass MCP tool loop; tools in `Mcp/McpToolRegistry.cs`, executed by `McpToolExecutor` |
| `ReindexListingsHandler` | Backfills `EmbeddingJson` for listings where it is null; runs on every startup |

MCP tools: `search_listings`, `get_listing`, `get_monthly_report`, `get_trends`. See `mcp.tools.json` and `docs/MCP.md`.

## Key Configuration (`appsettings.json`)

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `OpenAI:ApiKey` / `Ai:ApiKey` | Both aliases work |
| `Ai:AssistModel` / `Ai:ChatModel` | LLM model (default `gpt-4o-mini`) |
| `N8n:ClaimCreatedWebhookUrl` | Webhook URL fired on claim creation |
| `N8n:ListingCreatedWebhookUrl` | Webhook URL fired on listing creation |

CORS allows: `localhost:3000`, `localhost:5173`, `localhost:5174`, `https://campuslostfound-fe.onrender.com`.

## Adding a New Feature

1. Add a record in `Commands/` implementing `IRequest<TResult>`
2. Add a handler in `Handlers/` implementing `IRequestHandler<TCommand, TResult>`
3. Add a controller action calling `mediator.Send(new YourCommand(...))`
4. Add DTOs to `Dtos/Dtos.cs` if new shapes are needed

## Tests

`CampusLostAndFound.Tests/` uses InMemory EF, `FakeHttpMessageHandler`, and `Mock<IMediator>`. No external services needed.

- **Covered:** all listing/claim CRUD handlers, `McpToolExecutor`, controller input validation
- **Not covered (require OpenAI):** `RagSearchHandler`, `ClaimCheckHandler`, `ModerationAnalyzeHandler`, `FaqHandler`, `AiAssistHandler`, `SimilarListingsHandler`, `ReindexListingsHandler`
