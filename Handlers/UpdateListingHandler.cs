using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Infrastructure;
using CampusLostAndFound.Models;
using MediatR;
using System.Text.Json;

namespace CampusLostAndFound.Handlers;

public class UpdateListingHandler(AppDbContext db, EmbeddingService embedding)
    : IRequestHandler<UpdateListingCommand, ListingResponseDto?>
{
    public async Task<ListingResponseDto?> Handle(UpdateListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.FindAsync([request.Id], cancellationToken);
        if (listing == null) return null;

        var dto = request.Dto;
        listing.Title       = dto.Title;
        listing.Description = dto.Description;
        listing.Category    = dto.Category;
        listing.Location    = dto.Location;
        listing.EventDate   = dto.EventDate;
        listing.PhotoUrl    = dto.PhotoUrl;
        listing.Status      = dto.Status;
        listing.UpdatedAt   = DateTime.UtcNow;

        await TrySetEmbeddingAsync(listing);

        await db.SaveChangesAsync(cancellationToken);
        return Map(listing);
    }

    private async Task TrySetEmbeddingAsync(Listing listing)
    {
        try
        {
            var vec = await embedding.EmbedAsync($"{listing.Type} {listing.Title} {listing.Description} {listing.Category} {listing.Location}");
            listing.EmbeddingJson = JsonSerializer.Serialize(vec);
        }
        catch { /* non-critical */ }
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location, l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
}
