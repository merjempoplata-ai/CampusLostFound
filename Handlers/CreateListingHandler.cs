using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Infrastructure;
using CampusLostAndFound.Models;
using MediatR;
using System.Text.Json;

namespace CampusLostAndFound.Handlers;

public class CreateListingHandler(AppDbContext db, EmbeddingService embedding, N8nWebhookService webhook)
    : IRequestHandler<CreateListingCommand, ListingResponseDto>
{
    public async Task<ListingResponseDto> Handle(CreateListingCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var listing = new Listing
        {
            Id          = Guid.NewGuid(),
            OwnerName   = dto.OwnerName,
            Type        = dto.Type,
            Title       = dto.Title,
            Description = dto.Description,
            Category    = dto.Category,
            Location    = dto.Location,
            EventDate   = dto.EventDate,
            PhotoUrl    = dto.PhotoUrl,
            Status      = "Open",
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        };

        await TrySetEmbeddingAsync(listing);

        db.Listings.Add(listing);
        await db.SaveChangesAsync(cancellationToken);

        _ = webhook.SendListingCreatedAsync(new ListingCreatedPayload(
            listing.Id, listing.Type, listing.Title,
            listing.Description, listing.Category,
            listing.Location, listing.EventDate));

        return Map(listing);
    }

    private async Task TrySetEmbeddingAsync(Listing listing)
    {
        try
        {
            var vec = await embedding.EmbedAsync($"{listing.Type} {listing.Title} {listing.Description} {listing.Category} {listing.Location}");
            listing.EmbeddingJson = JsonSerializer.Serialize(vec);
        }
        catch { /* non-critical — backfill via POST /api/ai/reindex/listings */ }
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location, l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
}
