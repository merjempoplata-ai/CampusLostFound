using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Infrastructure;
using CampusLostAndFound.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusLostAndFound.Handlers;

public class AiAssistHandler(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : IRequestHandler<AiAssistQuery, AssistResponseDto>
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AssistResponseDto> Handle(AiAssistQuery request, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? configuration["Ai:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var messages    = new List<Message> { new("user", Content: request.Message) };
        var toolCallLog = new List<ToolCallLogDto>();

        var firstResponse = await CallLlmAsync(apiKey, messages, ToolRegistry.Tools);
        var firstChoice   = firstResponse.Choices[0];

        if (firstChoice.FinishReason != "tool_calls" || firstChoice.Message.ToolCalls is null)
        {
            return new AssistResponseDto(firstChoice.Message.Content ?? string.Empty, toolCallLog, []);
        }

        messages.Add(firstChoice.Message);

        foreach (var toolCall in firstChoice.Message.ToolCalls)
        {
            var result = await ExecuteToolAsync(toolCall.Function.Name, toolCall.Function.Arguments, cancellationToken);
            messages.Add(new("tool", Content: result, ToolCallId: toolCall.Id));

            using var argsDoc = JsonDocument.Parse(toolCall.Function.Arguments);
            toolCallLog.Add(new ToolCallLogDto(toolCall.Function.Name, argsDoc.RootElement.Clone()));
        }

        var secondResponse = await CallLlmAsync(apiKey, messages, tools: null);
        var answer         = secondResponse.Choices[0].Message.Content ?? string.Empty;

        var allIds    = await db.Listings.Select(l => l.Id).ToListAsync(cancellationToken);
        var citations = allIds
            .Where(id => answer.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new AssistResponseDto(answer, toolCallLog, citations);
    }

    private async Task<CompletionResponse> CallLlmAsync(string apiKey, List<Message> messages, IReadOnlyList<ToolDefinition>? tools)
    {
        var model = configuration["Ai:AssistModel"] ?? configuration["Ai:ChatModel"] ?? "gpt-4o-mini";

        object requestBody = tools is not null
            ? new { model, messages, tools, tool_choice = "auto" }
            : (object)new { model, messages };

        using var httpClient = httpClientFactory.CreateClient();
        using var req        = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(req);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"OpenAI {(int)response.StatusCode}: {errorBody}");
        }

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CompletionResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize LLM response.");
    }

    private async Task<string> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken ct)
    {
        using var doc  = JsonDocument.Parse(argumentsJson);
        var args = doc.RootElement;

        return toolName switch
        {
            "search_listings"   => await ExecuteSearchListings(args, ct),
            "get_listing"       => await ExecuteGetListing(args, ct),
            "get_monthly_report"=> await ExecuteGetMonthlyReport(args, ct),
            "get_trends"        => await ExecuteGetTrends(args, ct),
            _ => throw new InvalidOperationException($"Unknown tool '{toolName}'.")
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

    private record CompletionResponse(Choice[] Choices);
    private record Choice(string FinishReason, Message Message);
    private record Message(
        string Role,
        string? Content     = null,
        ToolCall[]? ToolCalls = null,
        string? ToolCallId  = null);
    private record ToolCall(string Id, string Type, ToolCallFunction Function);
    private record ToolCallFunction(string Name, string Arguments);
}
