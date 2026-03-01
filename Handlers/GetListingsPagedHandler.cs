using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Handlers;

public class GetListingsPagedHandler(AppDbContext db) : IRequestHandler<GetListingsPagedQuery, PaginatedListingsResponseDto>
{
    public async Task<PaginatedListingsResponseDto> Handle(GetListingsPagedQuery request, CancellationToken cancellationToken)
    {
        var query = db.Listings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Type))
            query = query.Where(l => l.Type == request.Type);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var lower = request.Search.ToLower();
            query = query.Where(l =>
                l.Title.ToLower().Contains(lower) ||
                l.Description.ToLower().Contains(lower) ||
                l.Category.ToLower().Contains(lower) ||
                l.Location.ToLower().Contains(lower));
        }

        query = query.OrderByDescending(l => l.EventDate);

        var total = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)request.Limit);

        var items = await query
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return new PaginatedListingsResponseDto(items.Select(Map), total, totalPages, request.Page);
    }

    private static ListingResponseDto Map(Listing l) =>
        new(l.Id, l.OwnerName, l.Type, l.Title, l.Description, l.Category, l.Location, l.EventDate, l.PhotoUrl, l.Status, l.CreatedAt, l.UpdatedAt);
}
