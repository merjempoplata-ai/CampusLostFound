using CampusLostAndFound.Commands;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Handlers;
using CampusLostAndFound.Infrastructure;
using CampusLostAndFound.Models;
using CampusLostAndFound.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CampusLostAndFound.Tests.Handlers;

// ─────────────────────────────────────────────────────────────
//  Shared factory helpers (claim-specific)
// ─────────────────────────────────────────────────────────────

file static class ClaimFactory
{
    public static N8nWebhookService SilentWebhook()
    {
        var factory = new Mock<IHttpClientFactory>();
        var config  = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        return new N8nWebhookService(factory.Object, config, NullLogger<N8nWebhookService>.Instance);
    }

    public static Listing MakeListing() => new()
    {
        Id          = Guid.NewGuid(),
        OwnerName   = "Owner",
        Type        = "Lost",
        Title       = "Lost Bag",
        Description = "A backpack",
        Category    = "Bags",
        Location    = "Library",
        EventDate   = DateTime.UtcNow,
        Status      = "Open",
        CreatedAt   = DateTime.UtcNow,
        UpdatedAt   = DateTime.UtcNow
    };

    public static Claim MakeClaim(Guid listingId) => new()
    {
        Id            = Guid.NewGuid(),
        ListingId     = listingId,
        RequesterName = "Requester",
        Message       = "This is mine.",
        Status        = "Pending",
        CreatedAt     = DateTime.UtcNow
    };
}

// ─────────────────────────────────────────────────────────────
//  GetAllClaimsHandler
// ─────────────────────────────────────────────────────────────

public class GetAllClaimsHandlerTests
{
    [Fact]
    public async Task Returns_all_persisted_claims()
    {
        using var db = DbContextFactory.CreateInMemory();
        var listing  = ClaimFactory.MakeListing();
        db.Listings.Add(listing);
        db.Claims.Add(ClaimFactory.MakeClaim(listing.Id));
        db.Claims.Add(ClaimFactory.MakeClaim(listing.Id));
        await db.SaveChangesAsync();

        var handler = new GetAllClaimsHandler(db);
        var result  = await handler.Handle(new GetAllClaimsQuery(), default);

        Assert.Equal(2, result.Count());
    }
}

// ─────────────────────────────────────────────────────────────
//  CreateClaimHandler
// ─────────────────────────────────────────────────────────────

public class CreateClaimHandlerTests
{
    [Fact]
    public async Task Returns_null_when_listing_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new CreateClaimHandler(db, ClaimFactory.SilentWebhook());
        var dto      = new ClaimCreateDto("Bob", "I think this is mine.");

        var result = await handler.Handle(new CreateClaimCommand(Guid.NewGuid(), dto), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Creates_claim_with_pending_status_and_persists()
    {
        using var db = DbContextFactory.CreateInMemory();
        var listing  = ClaimFactory.MakeListing();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var handler = new CreateClaimHandler(db, ClaimFactory.SilentWebhook());
        var dto     = new ClaimCreateDto("Carol", "Pretty sure it's mine.");

        var result = await handler.Handle(new CreateClaimCommand(listing.Id, dto), default);

        Assert.NotNull(result);
        Assert.Equal("Pending",    result.Status);
        Assert.Equal(listing.Id,   result.ListingId);
        Assert.Equal("Carol",      result.RequesterName);
        Assert.Single(db.Claims);
    }
}

// ─────────────────────────────────────────────────────────────
//  AcceptClaimHandler
// ─────────────────────────────────────────────────────────────

public class AcceptClaimHandlerTests
{
    [Fact]
    public async Task Returns_false_when_claim_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new AcceptClaimHandler(db);

        var result = await handler.Handle(new AcceptClaimCommand(Guid.NewGuid()), default);

        Assert.False(result);
    }

    [Fact]
    public async Task Returns_false_when_listing_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var orphanClaim = ClaimFactory.MakeClaim(Guid.NewGuid()); // listing doesn't exist in DB
        db.Claims.Add(orphanClaim);
        await db.SaveChangesAsync();

        var handler = new AcceptClaimHandler(db);
        var result  = await handler.Handle(new AcceptClaimCommand(orphanClaim.Id), default);

        Assert.False(result);
    }

    [Fact]
    public async Task Sets_claim_accepted_and_listing_claimed()
    {
        using var db = DbContextFactory.CreateInMemory();
        var listing  = ClaimFactory.MakeListing();
        var claim    = ClaimFactory.MakeClaim(listing.Id);
        db.Listings.Add(listing);
        db.Claims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new AcceptClaimHandler(db);
        var result  = await handler.Handle(new AcceptClaimCommand(claim.Id), default);

        Assert.True(result);
        Assert.Equal("Accepted", db.Claims.Single().Status);
        Assert.Equal("Claimed",  db.Listings.Single().Status);
        Assert.NotNull(db.Claims.Single().DecidedAt);
    }
}

// ─────────────────────────────────────────────────────────────
//  RejectClaimHandler
// ─────────────────────────────────────────────────────────────

public class RejectClaimHandlerTests
{
    [Fact]
    public async Task Returns_false_when_claim_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new RejectClaimHandler(db);

        var result = await handler.Handle(new RejectClaimCommand(Guid.NewGuid()), default);

        Assert.False(result);
    }

    [Fact]
    public async Task Sets_claim_rejected_and_records_decided_at()
    {
        using var db = DbContextFactory.CreateInMemory();
        var listing  = ClaimFactory.MakeListing();
        var claim    = ClaimFactory.MakeClaim(listing.Id);
        db.Listings.Add(listing);
        db.Claims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new RejectClaimHandler(db);
        var result  = await handler.Handle(new RejectClaimCommand(claim.Id), default);

        Assert.True(result);
        Assert.Equal("Rejected", db.Claims.Single().Status);
        Assert.NotNull(db.Claims.Single().DecidedAt);
    }
}
