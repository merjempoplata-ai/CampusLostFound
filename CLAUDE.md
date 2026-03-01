# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Campus Lost & Found** — an ASP.NET Core 8 Web API for posting lost/found items and managing claims.
No authentication; uses string names (`OwnerName`, `RequesterName`) instead of user accounts by design.

## Commands

```bash
dotnet run          # Start API on http://0.0.0.0:5000 (PORT env var overrides port)
dotnet build        # Build only

# Docker
docker build -t main .
docker run -p 5000:5000 main
```

No automated tests. Use Swagger at `/swagger` for manual API testing (always enabled, not just dev).

## Architecture

The app uses **MediatR CQRS** — controllers dispatch commands/queries; all business logic lives in handlers.

```
Commands/   — IRequest<T> records (one per operation)
Handlers/   — IRequestHandler<TRequest, TResult> implementations
Controllers/— Thin HTTP layer; only validates input, sends to IMediator, maps results
Infrastructure/ — Plain services not going through MediatR
Models/     — Listing and Claim EF entities
Dtos/       — C# records for request/response contracts
Data/       — AppDbContext (PostgreSQL via Npgsql)
```

**Infrastructure services** (registered in `Program.cs`, injected directly into handlers):
- `EmbeddingService` — Calls OpenAI `text-embedding-ada-002`; registered as `AddHttpClient<EmbeddingService>()`
- `SemanticRerankService` — Cosine similarity scoring over candidate listings
- `N8nWebhookService` — POSTs to N8n webhook on claim creation

## Domain Model

**Listing** fields: `Id`, `OwnerName`, `Type`, `Title`, `Description`, `Category`, `Location`, `EventDate`, `PhotoUrl`, `Status`, `CreatedAt`, `UpdatedAt`, plus AI fields: `EmbeddingJson` (float[] as JSON), `AiTags`, `NormalizedLocation`, `AiSummary`.

**Claim** fields: `Id`, `ListingId`, `RequesterName`, `Message`, `Status`, `CreatedAt`, `DecidedAt`.

String enums (not C# enums):
- `Listing.Type`: `"Lost"` | `"Found"`
- `Listing.Status`: `"Open"` | `"Claimed"` | `"Closed"` | `"Hidden"`
- `Claim.Status`: `"Pending"` | `"Accepted"` | `"Rejected"`

## Database

- PostgreSQL via Npgsql; connection string at `ConnectionStrings:DefaultConnection`
- **No EF migrations** — schema is created with `EnsureCreated()` on startup; new columns are added via raw `ALTER TABLE … ADD COLUMN IF NOT EXISTS`
- Table names are snake_case (`listings`, `claims`); multi-word column names are also snake_case
- SQLite package is present but not used

## AI Subsystem

The AI layer is built into Handlers (not a separate service layer):

| Handler | Purpose |
|---|---|
| `RagSearchHandler` | Keyword filter → embedding query → cosine reranking → LLM-grounded answer |
| `SimilarListingsHandler` | Finds k nearest listings by embedding cosine similarity |
| `ClaimCheckHandler` | Scores a claim message against its listing; returns issues + suggestions |
| `ModerationAnalyzeHandler` | Scans recent listings for policy violations |
| `FaqHandler` | Generates FAQ + trends from recent listing data |
| `AiAssistHandler` | OpenAI tool-use loop (single round-trip, two LLM calls); tools defined in `ToolRegistry.cs` |
| `ReindexListingsHandler` | Generates embeddings for all listings where `EmbeddingJson` is null |

**AI tools** available to `AiAssistHandler`: `search_listings`, `get_listing`, `get_monthly_report`, `get_trends`.

AI model is configurable: `Ai:AssistModel` → `Ai:ChatModel` → fallback `"gpt-4o-mini"`.

On every startup, `ReindexListingsCommand` runs automatically — it only indexes listings with null `EmbeddingJson`, so it is safe to call repeatedly.

## API Routes

| Method | Path | Handler |
|---|---|---|
| GET | `/api/listings` | `GetListingsPagedHandler` — pagination + type/search filters |
| GET | `/api/listings/{id}` | `GetListingByIdHandler` |
| POST | `/api/listings` | `CreateListingHandler` — also triggers embedding generation |
| PUT | `/api/listings/{id}` | `UpdateListingHandler` |
| DELETE | `/api/listings/{id}` | `DeleteListingHandler` |
| PATCH | `/api/listings/{id}/ai-metadata` | `UpdateListingAiMetadataHandler` — written back by n8n |
| GET | `/api/listings/report` | `GetMonthlyReportHandler` — requires `year` + `month` query params |
| GET | `/api/ai/search` | `RagSearchHandler` — `query` param required |
| POST | `/api/ai/reindex/listings` | `ReindexListingsHandler` |
| GET | `/api/ai/similar/{listingId}` | `SimilarListingsHandler` — optional `k` param (default 6) |
| POST | `/api/ai/claim-check` | `ClaimCheckHandler` |
| POST | `/api/ai/moderation/analyze` | `ModerationAnalyzeHandler` |
| GET | `/api/ai/faq` | `FaqHandler` — optional `days` param (default 30) |
| POST | `/api/ai/assist` | `AiAssistHandler` — free-form campus assistant |
| GET | `/api/claims` | `GetAllClaimsHandler` |
| POST | `/api/listings/{id}/claims` | `CreateClaimHandler` — also fires N8n webhook |
| POST | `/api/claims/{id}/accept` | `AcceptClaimHandler` |
| POST | `/api/claims/{id}/reject` | `RejectClaimHandler` |

## Key Configuration (`appsettings.json`)

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `OpenAI:ApiKey` / `Ai:ApiKey` | OpenAI API key (both aliases work) |
| `Ai:AssistModel` / `Ai:ChatModel` | LLM model override (default `gpt-4o-mini`) |
| `N8n:WebhookUrl` | N8n webhook URL for claim events |

CORS allows: `localhost:3000`, `localhost:5173`, `localhost:5174`, `https://campuslostfound-fe.onrender.com`.

## Adding a New Feature

The pattern for every new operation:
1. Add a record in `Commands/` implementing `IRequest<TResult>`
2. Add a handler in `Handlers/` implementing `IRequestHandler<TCommand, TResult>`
3. Add a controller action that calls `mediator.Send(new YourCommand(...))`
4. Add DTOs to `Dtos/Dtos.cs` if new request/response shapes are needed