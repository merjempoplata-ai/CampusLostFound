# replit.md

## Overview

Campus Lost & Found is a university project backend API built with ASP.NET Core 8. The application provides a platform for users to post lost or found items on campus and manage claims on those items. This is a minimal, single-project skeleton designed to run on Replit's free tier without authentication, authorization, or Docker dependencies.

## User Preferences

Preferred communication style: Simple, everyday language.

## System Architecture

### Framework & Runtime
- **ASP.NET Core 8 Web API**: Single-project structure for simplicity and Replit compatibility
- **Target Framework**: .NET 8.0

### Project Structure
The codebase follows a standard layered architecture:
- `/Controllers` - API endpoint handlers
- `/Services` - Business logic layer
- `/Data` - Database context and data access
- `/Models` - Entity definitions (Listing, Claim)
- `/Dtos` - Data transfer objects for API requests/responses

### Data Layer
- **ORM**: Entity Framework Core 8.0
- **Database**: SQLite (file-based at `campus.db`)
- **Connection String**: Configured in `appsettings.json` under `ConnectionStrings:DefaultConnection`

### Entity Models
**Listing**:
- Represents lost or found items
- Key fields: Id (Guid), OwnerName, Type (Lost/Found), Title, Description, Category, Location, EventDate, PhotoUrl, Status (Open/Claimed/Closed/Hidden), CreatedAt, UpdatedAt

**Claim**:
- Represents claims on listings
- Key fields: Id (Guid), ListingId, RequesterName, Message, Status (Pending/Accepted/Rejected), CreatedAt, DecidedAt

### API Design
- RESTful endpoints for Listings CRUD operations
- Claims management endpoints
- Swagger/OpenAPI documentation enabled in Development mode

### Design Decisions
- **No Authentication**: Simplified for university project scope; uses `OwnerName` and `RequesterName` strings instead of user accounts
- **SQLite over PostgreSQL**: Chosen for zero-configuration, file-based storage that works on Replit free tier
- **Single Project**: Avoids multi-project complexity for easier deployment and maintenance

## External Dependencies

### NuGet Packages
| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.0 | SQLite database provider for EF Core |
| Microsoft.EntityFrameworkCore.Design | 8.0.0 | EF Core tooling for migrations |
| Swashbuckle.AspNetCore | 6.5.0 | Swagger/OpenAPI documentation generation |

### Database
- **SQLite**: Local file database (`campus.db`)
- Connection string pattern supports easy migration to PostgreSQL if needed

### API Documentation
- **Swagger UI**: Available at `/swagger` in Development environment
- Auto-generated from controller endpoints