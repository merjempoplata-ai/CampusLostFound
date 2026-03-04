using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
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
//  Shared factory helpers
// ─────────────────────────────────────────────────────────────

file static class Factory
{
    public static EmbeddingService ThrowingEmbedding()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler()); // throws
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OpenAI:ApiKey"] = "fake" })
            .Build();
        return new EmbeddingService(httpClient, config);
    }

    public static N8nWebhookService SilentWebhook()
    {
        var factory = new Mock<IHttpClientFactory>();
        var config  = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        return new N8nWebhookService(factory.Object, config, NullLogger<N8nWebhookService>.Instance);
    }

    public static Listing MakeListing(string type = "Lost", DateTime? eventDate = null) => new()
    {
        Id          = Guid.NewGuid(),
        OwnerName   = "Alice",
        Type        = type,
        Title       = "Test Item",
        Description = "A description",
        Category    = "Electronics",
        Location    = "Library",
        EventDate   = eventDate ?? DateTime.UtcNow,
        Status      = "Open",
        CreatedAt   = DateTime.UtcNow,
        UpdatedAt   = DateTime.UtcNow
    };
}

// ─────────────────────────────────────────────────────────────
//  GetListingByIdHandler
// ─────────────────────────────────────────────────────────────

public class GetListingByIdHandlerTests
{
    [Fact]
    public async Task Returns_dto_when_listing_exists()
    {
        using var db   = DbContextFactory.CreateInMemory();
        var listing    = Factory.MakeListing();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var handler = new GetListingByIdHandler(db);
        var result  = await handler.Handle(new GetListingByIdQuery(listing.Id), default);

        Assert.NotNull(result);
        Assert.Equal(listing.Id,    result.Id);
        Assert.Equal(listing.Title, result.Title);
    }

    [Fact]
    public async Task Returns_null_when_listing_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new GetListingByIdHandler(db);
        var result   = await handler.Handle(new GetListingByIdQuery(Guid.NewGuid()), default);

        Assert.Null(result);
    }
}

// ─────────────────────────────────────────────────────────────
//  GetListingsPagedHandler
// ─────────────────────────────────────────────────────────────

public class GetListingsPagedHandlerTests
{
    [Fact]
    public async Task Returns_all_listings_with_no_filters()
    {
        using var db = DbContextFactory.CreateInMemory();
        db.Listings.AddRange(Factory.MakeListing("Lost"), Factory.MakeListing("Found"));
        await db.SaveChangesAsync();

        var handler = new GetListingsPagedHandler(db);
        var result  = await handler.Handle(new GetListingsPagedQuery(1, 9, null, null), default);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task Filters_by_type_lost()
    {
        using var db = DbContextFactory.CreateInMemory();
        db.Listings.AddRange(Factory.MakeListing("Lost"), Factory.MakeListing("Found"));
        await db.SaveChangesAsync();

        var handler = new GetListingsPagedHandler(db);
        var result  = await handler.Handle(new GetListingsPagedQuery(1, 9, "Lost", null), default);

        Assert.Equal(1, result.Total);
        Assert.All(result.Items, i => Assert.Equal("Lost", i.Type));
    }

    [Fact]
    public async Task Filters_by_type_found()
    {
        using var db = DbContextFactory.CreateInMemory();
        db.Listings.AddRange(Factory.MakeListing("Lost"), Factory.MakeListing("Found"));
        await db.SaveChangesAsync();

        var handler = new GetListingsPagedHandler(db);
        var result  = await handler.Handle(new GetListingsPagedQuery(1, 9, "Found", null), default);

        Assert.Equal(1, result.Total);
        Assert.All(result.Items, i => Assert.Equal("Found", i.Type));
    }

    [Fact]
    public async Task Search_matches_title_and_description()
    {
        using var db = DbContextFactory.CreateInMemory();
        var match = Factory.MakeListing();
        match.Title = "Blue Laptop";
        var noMatch = Factory.MakeListing();
        noMatch.Title = "Red Umbrella";
        db.Listings.AddRange(match, noMatch);
        await db.SaveChangesAsync();

        var handler = new GetListingsPagedHandler(db);
        var result  = await handler.Handle(new GetListingsPagedQuery(1, 9, null, "laptop"), default);

        Assert.Equal(1, result.Total);
        Assert.Equal("Blue Laptop", result.Items.Single().Title);
    }

    [Fact]
    public async Task Pagination_returns_correct_page()
    {
        using var db = DbContextFactory.CreateInMemory();
        var dates = Enumerable.Range(0, 5)
            .Select(i => new DateTime(2025, 1, i + 1, 0, 0, 0, DateTimeKind.Utc))
            .ToArray();
        for (int i = 0; i < 5; i++)
        {
            var l = Factory.MakeListing();
            l.Title     = $"Item {i + 1}";
            l.EventDate = dates[i];
            db.Listings.Add(l);
        }
        await db.SaveChangesAsync();

        var handler = new GetListingsPagedHandler(db);
        // Page size 2, page 2 — sorted descending by EventDate so items 3 and 4
        var result = await handler.Handle(new GetListingsPagedQuery(2, 2, null, null), default);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task Results_ordered_by_event_date_descending()
    {
        using var db = DbContextFactory.CreateInMemory();
        var older = Factory.MakeListing(); older.EventDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newer = Factory.MakeListing(); newer.EventDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Listings.AddRange(older, newer);
        await db.SaveChangesAsync();

        var handler = new GetListingsPagedHandler(db);
        var result  = await handler.Handle(new GetListingsPagedQuery(1, 9, null, null), default);

        var items = result.Items.ToList();
        Assert.True(items[0].EventDate >= items[1].EventDate);
    }
}

// ─────────────────────────────────────────────────────────────
//  CreateListingHandler
// ─────────────────────────────────────────────────────────────

public class CreateListingHandlerTests
{
    [Fact]
    public async Task Persists_listing_with_status_open()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new CreateListingHandler(db, Factory.ThrowingEmbedding(), Factory.SilentWebhook());
        var dto      = new ListingCreateDto("Bob", "Lost", "Keys", "Car keys", "Keys", "Parking Lot",
                                            new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc), null);

        var result = await handler.Handle(new CreateListingCommand(dto), default);

        Assert.Equal("Open",        result.Status);
        Assert.Equal("Bob",         result.OwnerName);
        Assert.Equal("Keys",        result.Title);
        Assert.Single(db.Listings);
    }

    [Fact]
    public async Task Swallows_embedding_failure_and_still_saves()
    {
        using var db = DbContextFactory.CreateInMemory();
        // ThrowingEmbedding will throw; TrySetEmbeddingAsync swallows it
        var handler = new CreateListingHandler(db, Factory.ThrowingEmbedding(), Factory.SilentWebhook());
        var dto     = new ListingCreateDto("Alice", "Found", "Wallet", "Brown wallet", "Accessories",
                                           "Cafeteria", DateTime.UtcNow, null);

        var result = await handler.Handle(new CreateListingCommand(dto), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(db.Listings);
        // EmbeddingJson is null because embedding failed — that's expected
        Assert.Null(db.Listings.Single().EmbeddingJson);
    }
}

// ─────────────────────────────────────────────────────────────
//  UpdateListingHandler
// ─────────────────────────────────────────────────────────────

public class UpdateListingHandlerTests
{
    [Fact]
    public async Task Returns_null_when_listing_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new UpdateListingHandler(db, Factory.ThrowingEmbedding());
        var dto      = new ListingUpdateDto("X", "X", "X", "X", DateTime.UtcNow, null, "Open");

        var result = await handler.Handle(new UpdateListingCommand(Guid.NewGuid(), dto), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Updates_all_mutable_fields()
    {
        using var db = DbContextFactory.CreateInMemory();
        var listing  = Factory.MakeListing();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var handler = new UpdateListingHandler(db, Factory.ThrowingEmbedding());
        var newDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var dto     = new ListingUpdateDto("New Title", "New Desc", "Clothing", "Gym", newDate, "http://photo.url", "Closed");

        var result = await handler.Handle(new UpdateListingCommand(listing.Id, dto), default);

        Assert.NotNull(result);
        Assert.Equal("New Title", result.Title);
        Assert.Equal("Closed",    result.Status);
        Assert.Equal("Gym",       result.Location);
        Assert.Equal(newDate,     result.EventDate);
    }
}

// ─────────────────────────────────────────────────────────────
//  DeleteListingHandler
// ─────────────────────────────────────────────────────────────

public class DeleteListingHandlerTests
{
    [Fact]
    public async Task Returns_false_when_listing_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new DeleteListingHandler(db);

        var result = await handler.Handle(new DeleteListingCommand(Guid.NewGuid()), default);

        Assert.False(result);
    }

    [Fact]
    public async Task Removes_listing_and_returns_true()
    {
        using var db = DbContextFactory.CreateInMemory();
        var listing  = Factory.MakeListing();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var handler = new DeleteListingHandler(db);
        var result  = await handler.Handle(new DeleteListingCommand(listing.Id), default);

        Assert.True(result);
        Assert.Empty(db.Listings);
    }
}

// ─────────────────────────────────────────────────────────────
//  UpdateListingAiMetadataHandler
// ─────────────────────────────────────────────────────────────

public class UpdateListingAiMetadataHandlerTests
{
    [Fact]
    public async Task Returns_null_when_listing_missing()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new UpdateListingAiMetadataHandler(db);
        var dto      = new ListingAiMetadataDto("tags", "location", "summary");

        var result = await handler.Handle(new UpdateListingAiMetadataCommand(Guid.NewGuid(), dto), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Updates_ai_fields_and_persists()
    {
        using var db = DbContextFactory.CreateInMemory();
        var listing  = Factory.MakeListing();
        db.Listings.Add(listing);
        await db.SaveChangesAsync();

        var handler = new UpdateListingAiMetadataHandler(db);
        var dto     = new ListingAiMetadataDto("electronics,keys", "Main Library", "A summary.");

        await handler.Handle(new UpdateListingAiMetadataCommand(listing.Id, dto), default);

        var saved = db.Listings.Single();
        Assert.Equal("electronics,keys", saved.AiTags);
        Assert.Equal("Main Library",     saved.NormalizedLocation);
        Assert.Equal("A summary.",       saved.AiSummary);
    }
}

// ─────────────────────────────────────────────────────────────
//  GetMonthlyReportHandler
// ─────────────────────────────────────────────────────────────

public class GetMonthlyReportHandlerTests
{
    [Fact]
    public async Task Returns_correct_lost_and_found_counts()
    {
        using var db = DbContextFactory.CreateInMemory();
        var jan = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var lostA  = Factory.MakeListing("Lost",  jan); db.Listings.Add(lostA);
        var lostB  = Factory.MakeListing("Lost",  jan); db.Listings.Add(lostB);
        var found  = Factory.MakeListing("Found", jan); db.Listings.Add(found);
        await db.SaveChangesAsync();

        var handler = new GetMonthlyReportHandler(db);
        var result  = await handler.Handle(new GetMonthlyReportQuery(2025, 1), default);

        Assert.Equal(2, result.Lost);
        Assert.Equal(1, result.Found);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task Excludes_listings_outside_target_month()
    {
        using var db = DbContextFactory.CreateInMemory();
        var inMonth  = Factory.MakeListing("Lost", new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc));
        var beforeM  = Factory.MakeListing("Lost", new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
        var afterM   = Factory.MakeListing("Lost", new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        db.Listings.AddRange(inMonth, beforeM, afterM);
        await db.SaveChangesAsync();

        var handler = new GetMonthlyReportHandler(db);
        var result  = await handler.Handle(new GetMonthlyReportQuery(2025, 1), default);

        Assert.Equal(1, result.Total);
    }

    [Fact]
    public async Task Month_name_formatted_correctly()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler  = new GetMonthlyReportHandler(db);
        var result   = await handler.Handle(new GetMonthlyReportQuery(2025, 1), default);

        Assert.Contains("2025", result.Month);
        Assert.Contains("January", result.Month);
    }
}
