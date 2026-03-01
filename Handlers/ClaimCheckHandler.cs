using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using MediatR;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusLostAndFound.Handlers;

public class ClaimCheckHandler(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : IRequestHandler<ClaimCheckCommand, ClaimCheckResponseDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<ClaimCheckResponseDto> Handle(ClaimCheckCommand request, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.FindAsync([request.Dto.ListingId], cancellationToken)
            ?? throw new KeyNotFoundException($"Listing {request.Dto.ListingId} not found.");

        var apiKey  = configuration["Ai:ApiKey"] ?? configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AI API key is not configured.");
        var baseUrl = configuration["Ai:ApiBaseUrl"] ?? "https://api.openai.com";
        var model   = configuration["Ai:ChatModel"] ?? "gpt-4o-mini";

        var systemPrompt = $$"""
            You are a campus lost-and-found assistant evaluating claim message quality.
            Assess the claim message against the listing details below.
            Return ONLY valid JSON in this exact format:
            {
              "score": <integer 0-100>,
              "issues": ["<issue1>", "<issue2>"],
              "suggestions": ["<suggestion1>", "<suggestion2>"],
              "improvedMessage": "<rewritten claim message>"
            }
            Rules:
            - Do not invent facts not present in the listing or claim.
            - Score higher when the claimant provides specific distinguishing marks, proof, time, or location details.
            - List issues: what is vague or missing.
            - List suggestions: ask for specifics.
            - ImprovedMessage: rewrite the claim incorporating the suggested improvements.

            Listing:
            Title: {{listing.Title}}
            Type: {{listing.Type}}
            Description: {{listing.Description}}
            Category: {{listing.Category}}
            Location: {{listing.Location}}
            """;

        var requestBody = JsonSerializer.Serialize(new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = $"Claim message: {request.Dto.ClaimMessage}" }
            },
            temperature = 0
        });

        using var client = httpClientFactory.CreateClient();
        using var req    = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(req, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body       = await response.Content.ReadAsStringAsync(cancellationToken);
        var completion = JsonSerializer.Deserialize<ChatCompletion>(body, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize LLM response.");

        return ParseResponse(completion.Choices[0].Message.Content
            ?? throw new InvalidOperationException("LLM returned empty content."));
    }

    private static ClaimCheckResponseDto ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var score       = root.TryGetProperty("score", out var s) ? s.GetInt32() : 0;
        var issues      = root.TryGetProperty("issues", out var i) ? i.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : [];
        var suggestions = root.TryGetProperty("suggestions", out var sg) ? sg.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : [];
        var improved    = root.TryGetProperty("improvedMessage", out var im) ? im.GetString() ?? "" : "";
        return new ClaimCheckResponseDto(score, issues, suggestions, improved);
    }

    private record ChatCompletion(Choice[] Choices);
    private record Choice(Message Message);
    private record Message(string? Content);
}
