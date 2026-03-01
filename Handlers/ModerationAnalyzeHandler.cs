using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusLostAndFound.Handlers;

public class ModerationAnalyzeHandler(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : IRequestHandler<ModerationAnalyzeCommand, ModerationResponseDto>
{
    private const int LlmBatchSize = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<ModerationResponseDto> Handle(ModerationAnalyzeCommand request, CancellationToken cancellationToken)
    {
        var sinceDays   = request.Dto.SinceDays   < 1 ? 7   : request.Dto.SinceDays;
        var maxListings = request.Dto.MaxListings  < 1 ? 200 : request.Dto.MaxListings;
        var since       = DateTime.UtcNow.AddDays(-sinceDays);

        var listings = await db.Listings
            .Where(l => l.CreatedAt >= since)
            .OrderByDescending(l => l.CreatedAt)
            .Take(maxListings)
            .ToListAsync(cancellationToken);

        if (listings.Count == 0)
            return new ModerationResponseDto([], "No listings found in the specified time range.");

        var apiKey  = configuration["Ai:ApiKey"] ?? configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AI API key is not configured.");
        var baseUrl = configuration["Ai:ApiBaseUrl"] ?? "https://api.openai.com";
        var model   = configuration["Ai:ChatModel"] ?? "gpt-4o-mini";

        var allFlagged = new List<FlaggedListingDto>();
        for (int i = 0; i < listings.Count; i += LlmBatchSize)
        {
            var batch   = listings.Skip(i).Take(LlmBatchSize).ToList();
            var flagged = await AnalyzeBatchAsync(apiKey, baseUrl, model, batch, cancellationToken);
            allFlagged.AddRange(flagged);
        }

        var summary = allFlagged.Count == 0
            ? $"Analyzed {listings.Count} listing(s). No suspicious items detected."
            : $"Analyzed {listings.Count} listing(s). Flagged {allFlagged.Count}: "
              + $"{allFlagged.Count(f => f.Severity == "high")} high, "
              + $"{allFlagged.Count(f => f.Severity == "medium")} medium, "
              + $"{allFlagged.Count(f => f.Severity == "low")} low severity.";

        return new ModerationResponseDto(allFlagged, summary);
    }

    private async Task<List<FlaggedListingDto>> AnalyzeBatchAsync(
        string apiKey, string baseUrl, string model, List<Listing> batch, CancellationToken ct)
    {
        var listingContext = string.Join("\n", batch.Select(l =>
            $"[ID: {l.Id}] type={l.Type} | owner={l.OwnerName} | title={l.Title} | desc={l.Description} | category={l.Category} | location={l.Location}"));

        var systemPrompt = $$"""
            You are a campus lost-and-found moderation assistant. Be strict.
            Analyze the listings below and flag ANY that match one or more of these patterns:
            - Gibberish or keyboard-mash in title, description, owner name, or location
            - Placeholder or test text (e.g. "test", "string test", "lorem ipsum")
            - Suspiciously short or meaningless owner names
            - Repeated identical phrases within a single field
            - External links, URLs, or contact info that looks like a scam
            - Inappropriate or harmful content

            Return ONLY valid JSON in this exact format:
            {
              "flagged": [
                {"listingId": "<exact-guid-from-input>", "reason": "<specific reason>", "severity": "low|medium|high"}
              ]
            }
            If nothing matches return {"flagged": []}.
            Only use IDs that appear verbatim in the input.

            Listings:
            {{listingContext}}
            """;

        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = "Analyze these listings for suspicious or harmful content." }
            },
            temperature = 0
        });

        using var client = httpClientFactory.CreateClient();
        using var req    = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();

        var body       = await response.Content.ReadAsStringAsync(ct);
        var completion = JsonSerializer.Deserialize<ChatCompletion>(body, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize LLM response.");

        return ParseFlagged(completion.Choices[0].Message.Content ?? "{}", batch);
    }

    private static List<FlaggedListingDto> ParseFlagged(string json, List<Listing> batch)
    {
        var validIds = batch.Select(l => l.Id).ToHashSet();
        var result   = new List<FlaggedListingDto>();

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("flagged", out var flagged)) return result;

        foreach (var item in flagged.EnumerateArray())
        {
            if (!item.TryGetProperty("listingId", out var idEl)) continue;
            if (!Guid.TryParse(idEl.GetString(), out var id)) continue;
            if (!validIds.Contains(id)) continue;

            var reason   = item.TryGetProperty("reason",   out var r)  ? r.GetString()  ?? "" : "";
            var raw      = item.TryGetProperty("severity", out var sv) ? sv.GetString() ?? "low" : "low";
            var severity = raw is "high" or "medium" or "low" ? raw : "low";
            result.Add(new FlaggedListingDto(id, reason, severity));
        }

        return result;
    }

    private record ChatCompletion(Choice[] Choices);
    private record Choice(Message Message);
    private record Message(string? Content);
}
