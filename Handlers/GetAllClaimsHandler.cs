using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusLostAndFound.Handlers;

public class GetAllClaimsHandler(AppDbContext db) : IRequestHandler<GetAllClaimsQuery, IEnumerable<ClaimResponseDto>>
{
    public async Task<IEnumerable<ClaimResponseDto>> Handle(GetAllClaimsQuery request, CancellationToken cancellationToken)
    {
        var claims = await db.Claims.ToListAsync(cancellationToken);
        return claims.Select(Map);
    }

    private static ClaimResponseDto Map(Claim c) =>
        new(c.Id, c.ListingId, c.RequesterName, c.Message, c.Status, c.CreatedAt, c.DecidedAt);
}
