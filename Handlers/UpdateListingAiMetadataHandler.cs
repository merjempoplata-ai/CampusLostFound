using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using MediatR;

namespace CampusLostAndFound.Handlers;

public class UpdateListingAiMetadataHandler(AppDbContext db) : IRequestHandler<UpdateListingAiMetadataCommand, ListingResponseDto?>
{
    public async Task<ListingResponseDto?> Handle(UpdateListingAiMetadataCommand request, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.FindAsync([request.Id], cancellationToken);
        if (listing == null) return null;

        listing.AiTags             = request.Dto.AiTags;
        listing.NormalizedLocation = request.Dto.NormalizedLocation;
        listing.AiSummary          = request.Dto.Summary;
        listing.UpdatedAt          = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return Map(listing);
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location, l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
}
