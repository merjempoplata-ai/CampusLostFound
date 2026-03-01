using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Infrastructure;
using CampusLostAndFound.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusLostAndFound.Handlers;

public class RagSearchHandler(
    AppDbContext db,
    EmbeddingService embedding,
    SemanticRerankService rerank,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
    : IRequestHandler<RagSearchQuery, RagResponseDto>
{
    private const string Endpoint      = "https://api.openai.com/v1/chat/completions";
    private const string Model         = "gpt-4o-mini";
    private const int    CandidateCount = 100;
    private const int    TopK           = 8;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<RagResponseDto> Handle(RagSearchQuery request, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var candidates = await GetCandidatesAsync(request.Query, cancellationToken);
        var queryEmbedding = await embedding.EmbedAsync(request.Query);
        var reranked = rerank.Rerank(queryEmbedding, candidates, TopK);
        var matches  = reranked.Select(Map).ToList();
        var answer   = await GetGroundedAnswerAsync(apiKey, request.Query, reranked);

        var validIds  = reranked.Select(l => l.Id).ToHashSet();
        var citations = validIds
            .Where(id => answer.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new RagResponseDto(answer, matches, citations);
    }

    private async Task<List<Listing>> GetCandidatesAsync(string query, CancellationToken ct)
    {
        var lower = query.ToLower();
        var candidates = await db.Listings
            .Where(l => l.EmbeddingJson != null && (
                l.Title.ToLower().Contains(lower) ||
                l.Description.ToLower().Contains(lower) ||
                l.Category.ToLower().Contains(lower) ||
                l.Location.ToLower().Contains(lower)))
            .OrderByDescending(l => l.EventDate)
            .Take(CandidateCount)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            candidates = await db.Listings
                .Where(l => l.EmbeddingJson != null)
                .OrderByDescending(l => l.EventDate)
                .Take(CandidateCount)
                .ToListAsync(ct);
        }

        return candidates;
    }

    private async Task<string> GetGroundedAnswerAsync(string apiKey, string userQuery, IReadOnlyList<Listing> listings)
    {
        var context = string.Join("\n", listings.Select(l => $"[ID: {l.Id}] {l.Title} — {l.Description}"));
        var systemPrompt = $$"""
            You are a campus lost-and-found assistant.
            Answer the user's question using ONLY the listings provided below.
            Do not use any external knowledge.
            If the answer cannot be determined from the listings, say so clearly.
            When referencing a listing, include its ID in the format [ID: <id>].

            Listings:
            {{context}}
            """;

        var body = JsonSerializer.Serialize(new
        {
            model    = Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userQuery }
            },
            temperature = 0
        });

        using var httpClient = httpClientFactory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        var json       = await response.Content.ReadAsStringAsync();
        var completion = JsonSerializer.Deserialize<ChatCompletion>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize LLM response.");

        return completion.Choices[0].Message.Content?.Trim() ?? string.Empty;
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location,
            l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);

    private record ChatCompletion(Choice[] Choices);
    private record Choice(Message Message);
    private record Message(string? Content);
}
