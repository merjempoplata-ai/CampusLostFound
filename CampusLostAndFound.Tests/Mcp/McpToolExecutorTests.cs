using CampusLostAndFound.Mcp;
using CampusLostAndFound.Models;
using CampusLostAndFound.Tests.Helpers;
using System.Text.Json;
using Xunit;

namespace CampusLostAndFound.Tests.Mcp;

/// <summary>
/// Tests for McpToolExecutor — all tools use InMemory DB seeded with known data.
/// </summary>
public class McpToolExecutorTests : IDisposable
{
    private readonly CampusLostAndFound.Data.AppDbContext _db;
    private readonly McpToolExecutor _executor;

    // Seeded listing IDs
    private readonly Guid _lostId;
    private readonly Guid _foundId;

    public McpToolExecutorTests()
    {
        _db = DbContextFactory.CreateInMemory();

        var now = DateTime.UtcNow;

        var lost = new Listing
        {
            Id          = Guid.NewGuid(),
            OwnerName   = "Alice",
            Type        = "Lost",
            Title       = "Blue Laptop",
            Description = "15-inch laptop",
            Category    = "Electronics",
            Location    = "Library",
            EventDate   = now.AddDays(-1),
            Status      = "Open",
            CreatedAt   = now,
            UpdatedAt   = now
        };

        var found = new Listing
        {
            Id          = Guid.NewGuid(),
            OwnerName   = "Bob",
            Type        = "Found",
            Title       = "Red Umbrella",
            Description = "Found near cafeteria",
            Category    = "Accessories",
            Location    = "Cafeteria",
            EventDate   = now.AddDays(-2),
            Status      = "Open",
            CreatedAt   = now,
            UpdatedAt   = now
        };

        _db.Listings.AddRange(lost, found);
        _db.SaveChanges();

        _lostId  = lost.Id;
        _foundId = found.Id;

        _executor = new McpToolExecutor(_db);
    }

    public void Dispose() => _db.Dispose();

    // ──────────────────────────────────────────
    //  search_listings
    // ──────────────────────────────────────────

    [Fact]
    public async Task SearchListings_returns_all_with_no_args()
    {
        var json   = await _executor.ExecuteToolAsync("search_listings", "{}", default);
        var result = JsonDocument.Parse(json).RootElement;

        Assert.Equal(2, result.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task SearchListings_filters_by_type()
    {
        var json   = await _executor.ExecuteToolAsync("search_listings", """{"type":"Lost"}""", default);
        var result = JsonDocument.Parse(json).RootElement;

        Assert.Equal(1, result.GetProperty("total").GetInt32());
        var item = result.GetProperty("items")[0];
        Assert.Equal("Lost", item.GetProperty("type").GetString());
    }

    [Fact]
    public async Task SearchListings_filters_by_search_keyword()
    {
        var json   = await _executor.ExecuteToolAsync("search_listings", """{"search":"laptop"}""", default);
        var result = JsonDocument.Parse(json).RootElement;

        Assert.Equal(1, result.GetProperty("total").GetInt32());
        var title = result.GetProperty("items")[0].GetProperty("title").GetString();
        Assert.Contains("Laptop", title, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────
    //  get_listing
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetListing_returns_serialized_listing_for_known_id()
    {
        var args = $$$"""{"id":"{{{_lostId}}}"}""";
        var json = await _executor.ExecuteToolAsync("get_listing", args, default);
        var doc  = JsonDocument.Parse(json).RootElement;

        Assert.Equal(_lostId.ToString(), doc.GetProperty("id").GetString());
        Assert.Equal("Blue Laptop", doc.GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetListing_returns_error_for_unknown_id()
    {
        var args = $$$"""{"id":"{{{Guid.NewGuid()}}}"}""";
        var json = await _executor.ExecuteToolAsync("get_listing", args, default);
        var doc  = JsonDocument.Parse(json).RootElement;

        Assert.Equal("Listing not found.", doc.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetListing_returns_error_for_non_guid_string()
    {
        var json = await _executor.ExecuteToolAsync("get_listing", """{"id":"not-a-guid"}""", default);
        var doc  = JsonDocument.Parse(json).RootElement;

        Assert.Equal("Invalid listing ID format.", doc.GetProperty("error").GetString());
    }

    // ──────────────────────────────────────────
    //  get_monthly_report
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetMonthlyReport_returns_correct_counts_for_seeded_month()
    {
        // Both seeded listings use EventDate = now.AddDays(-1/-2); get the current month/year
        var now   = DateTime.UtcNow;
        var args  = $$$"""{"year":{{{now.Year}}},"month":{{{now.Month}}}}""";
        var json  = await _executor.ExecuteToolAsync("get_monthly_report", args, default);
        var doc   = JsonDocument.Parse(json).RootElement;

        Assert.Equal(1, doc.GetProperty("lost").GetInt32());
        Assert.Equal(1, doc.GetProperty("found").GetInt32());
        Assert.Equal(2, doc.GetProperty("total").GetInt32());
    }

    // ──────────────────────────────────────────
    //  get_trends
    // ──────────────────────────────────────────

    [Fact]
    public async Task GetTrends_returns_by_category_and_by_location()
    {
        var json = await _executor.ExecuteToolAsync("get_trends", """{"days":30}""", default);
        var doc  = JsonDocument.Parse(json).RootElement;

        Assert.True(doc.TryGetProperty("by_category", out var byCategory));
        Assert.True(doc.TryGetProperty("by_location",  out var byLocation));
        // Both seeded items are within the last 30 days
        Assert.True(byCategory.EnumerateObject().Any());
        Assert.True(byLocation.EnumerateObject().Any());
    }

    // ──────────────────────────────────────────
    //  Unknown tool
    // ──────────────────────────────────────────

    [Fact]
    public async Task Unknown_tool_throws_InvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _executor.ExecuteToolAsync("unknown_tool", "{}", default));
    }
}
