using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Infrastructure;
using CampusLostAndFound.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CampusLostAndFound.Handlers;

public class SimilarListingsHandler(AppDbContext db, EmbeddingService embedding, SemanticRerankService rerank)
    : IRequestHandler<SimilarListingsQuery, IEnumerable<ListingResponseDto>>
{
    public async Task<IEnumerable<ListingResponseDto>> Handle(SimilarListingsQuery request, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.FindAsync([request.ListingId], cancellationToken);
        if (listing == null) return [];

        float[] queryEmbedding;
        if (listing.EmbeddingJson != null)
        {
            queryEmbedding = JsonSerializer.Deserialize<float[]>(listing.EmbeddingJson)
                ?? throw new InvalidOperationException("Failed to deserialize stored embedding.");
        }
        else
        {
            var text = $"{listing.Type} {listing.Title} {listing.Description} {listing.Category} {listing.Location}";
            queryEmbedding = await embedding.EmbedAsync(text);
        }

        var candidates = await db.Listings
            .Where(l => l.EmbeddingJson != null && l.Id != request.ListingId)
            .OrderByDescending(l => l.EventDate)
            .Take(100)
            .ToListAsync(cancellationToken);

        return rerank.Rerank(queryEmbedding, candidates, request.K).Select(Map);
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location,
            l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
}
