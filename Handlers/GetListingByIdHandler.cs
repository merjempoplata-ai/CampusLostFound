using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using MediatR;

namespace CampusLostAndFound.Handlers;

public class GetListingByIdHandler(AppDbContext db) : IRequestHandler<GetListingByIdQuery, ListingResponseDto?>
{
    public async Task<ListingResponseDto?> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.FindAsync([request.Id], cancellationToken);
        return listing == null ? null : Map(listing);
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location, l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
}
