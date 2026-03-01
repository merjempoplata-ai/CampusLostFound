using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusLostAndFound.Handlers;

public class FaqHandler(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : IRequestHandler<FaqQuery, FaqResponseDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<FaqResponseDto> Handle(FaqQuery request, CancellationToken cancellationToken)
    {
        var since    = DateTime.UtcNow.AddDays(-request.Days);
        var listings = await db.Listings
            .Where(l => l.EventDate >= since)
            .ToListAsync(cancellationToken);

        var byCategory = listings.GroupBy(l => l.Category).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count());
        var byLocation = listings.GroupBy(l => l.Location).OrderByDescending(g => g.Count()).ToDictionary(g => g.Key, g => g.Count());
        var stats      = new FaqStatsDto(byCategory, byLocation, listings.Count(l => l.Type == "Lost"), listings.Count(l => l.Type == "Found"));

        if (listings.Count == 0)
        {
            return new FaqResponseDto(
            [
                new("What items are commonly lost on campus?", "No data available for the selected period."),
                new("What should I do if I lose something?", "Post a listing on this Lost & Found board and check the Found items section.")
            ], stats);
        }

        var faq = await GenerateFaqAsync(listings, request.Days, cancellationToken);
        return new FaqResponseDto(faq, stats);
    }

    private async Task<List<FaqItemDto>> GenerateFaqAsync(List<Models.Listing> listings, int days, CancellationToken ct)
    {
        var apiKey  = configuration["Ai:ApiKey"] ?? configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AI API key is not configured.");
        var baseUrl = configuration["Ai:ApiBaseUrl"] ?? "https://api.openai.com";
        var model   = configuration["Ai:ChatModel"] ?? "gpt-4o-mini";

        var summary = string.Join("\n", listings.Take(100).Select(l => $"{l.Type} | {l.Category} | {l.Location} | {l.Title}"));

        var systemPrompt = $$"""
            You are a campus lost-and-found assistant generating a data-grounded FAQ.
            Based ONLY on the listing data below (last {{days}} days), generate 5 to 8 useful FAQ entries.
            Return ONLY valid JSON in this exact format:
            {
              "faq": [
                {"q": "<question>", "a": "<answer>"}
              ]
            }

            Data (last {{days}} days):
            {{summary}}
            """;

        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = $"Generate a campus lost-and-found FAQ from the last {days} days of data." }
            },
            temperature = 0.3
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

        return ParseFaq(completion.Choices[0].Message.Content ?? "{}");
    }

    private static List<FaqItemDto> ParseFaq(string json)
    {
        var result = new List<FaqItemDto>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("faq", out var faq)) return result;

        foreach (var item in faq.EnumerateArray())
        {
            var q = item.TryGetProperty("q", out var qEl) ? qEl.GetString() ?? "" : "";
            var a = item.TryGetProperty("a", out var aEl) ? aEl.GetString() ?? "" : "";
            if (!string.IsNullOrWhiteSpace(q)) result.Add(new FaqItemDto(q, a));
        }

        return result;
    }

    private record ChatCompletion(Choice[] Choices);
    private record Choice(Message Message);
    private record Message(string? Content);
}
