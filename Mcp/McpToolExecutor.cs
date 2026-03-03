using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusLostAndFound.Mcp;

/// <summary>
/// MCP-style tool executor — receives a tool name + JSON arguments from the LLM,
/// executes the corresponding data-access logic, and returns a JSON result string
/// that is appended to the conversation before the second LLM pass.
/// </summary>
public class McpToolExecutor(AppDbContext db)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Dispatches a tool call by name and returns a JSON string result.
    /// Called once per tool_call entry in the LLM's first response.
    /// </summary>
    public Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(argumentsJson);
        // Clone so the element is valid after doc disposal (non-async dispatch path).
        var args = doc.RootElement.Clone();

        return toolName switch
        {
            "search_listings"    => ExecuteSearchListings(args, ct),
            "get_listing"        => ExecuteGetListing(args, ct),
            "get_monthly_report" => ExecuteGetMonthlyReport(args, ct),
            "get_trends"         => ExecuteGetTrends(args, ct),
            _ => throw new InvalidOperationException($"Unknown MCP tool '{toolName}'.")
        };
    }

    private async Task<string> ExecuteSearchListings(JsonElement args, CancellationToken ct)
    {
        var type   = args.TryGetProperty("type",   out var t) ? t.GetString() : null;
        var search = args.TryGetProperty("search", out var s) ? s.GetString() : null;
        var page   = args.TryGetProperty("page",   out var p) ? p.GetInt32()  : 1;
        var limit  = args.TryGetProperty("limit",  out var l) ? l.GetInt32()  : 9;

        var query = db.Listings.AsQueryable();
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(lower) ||
                x.Description.ToLower().Contains(lower) ||
                x.Category.ToLower().Contains(lower) ||
                x.Location.ToLower().Contains(lower));
        }
        query = query.OrderByDescending(x => x.EventDate);

        var total      = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(total / (double)limit);
        var items      = await query.Skip((page - 1) * limit).Take(limit).ToListAsync(ct);
        var result     = new PaginatedListingsResponseDto(items.Select(Map), total, totalPages, page);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private async Task<string> ExecuteGetListing(JsonElement args, CancellationToken ct)
    {
        if (!Guid.TryParse(args.GetProperty("id").GetString(), out var id))
            return """{"error": "Invalid listing ID format."}""";

        var listing = await db.Listings.FindAsync([id], ct);
        return listing is null
            ? """{"error": "Listing not found."}"""
            : JsonSerializer.Serialize(Map(listing), JsonOptions);
    }

    private async Task<string> ExecuteGetMonthlyReport(JsonElement args, CancellationToken ct)
    {
        var year  = args.GetProperty("year").GetInt32();
        var month = args.GetProperty("month").GetInt32();
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = start.AddMonths(1);

        var listings = await db.Listings
            .Where(l => l.EventDate >= start && l.EventDate < end)
            .ToListAsync(ct);

        var result = new MonthlyReportDto(
            $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} {year}",
            listings.Count(l => l.Type == "Lost"),
            listings.Count(l => l.Type == "Found"),
            listings.Count);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private async Task<string> ExecuteGetTrends(JsonElement args, CancellationToken ct)
    {
        var days  = args.TryGetProperty("days", out var d) ? d.GetInt32() : 30;
        var since = DateTime.UtcNow.AddDays(-days);

        var listings = await db.Listings.Where(l => l.EventDate >= since).ToListAsync(ct);

        return JsonSerializer.Serialize(new
        {
            days,
            lostCount  = listings.Count(l => l.Type == "Lost"),
            foundCount = listings.Count(l => l.Type == "Found"),
            byCategory = listings.GroupBy(l => l.Category).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count()),
            byLocation = listings.GroupBy(l => l.Location).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count())
        }, JsonOptions);
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location,
            l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
}
