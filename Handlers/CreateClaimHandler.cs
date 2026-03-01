using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Infrastructure;
using CampusLostAndFound.Models;
using MediatR;

namespace CampusLostAndFound.Handlers;

public class CreateClaimHandler(AppDbContext db, N8nWebhookService webhook)
    : IRequestHandler<CreateClaimCommand, ClaimResponseDto?>
{
    public async Task<ClaimResponseDto?> Handle(CreateClaimCommand request, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.FindAsync([request.ListingId], cancellationToken);
        if (listing == null) return null;

        var claim = new Claim
        {
            Id            = Guid.NewGuid(),
            ListingId     = request.ListingId,
            RequesterName = request.Dto.RequesterName,
            Message       = request.Dto.Message,
            Status        = "Pending",
            CreatedAt     = DateTime.UtcNow
        };
        db.Claims.Add(claim);
        await db.SaveChangesAsync(cancellationToken);

        await webhook.SendClaimCreatedAsync(new ClaimCreatedPayload(
            request.ListingId, listing.Title,
            claim.RequesterName, claim.Message, claim.CreatedAt));

        return Map(claim);
    }

    private static ClaimResponseDto Map(Claim c) =>
        new(c.Id, c.ListingId, c.RequesterName, c.Message, c.Status, c.CreatedAt, c.DecidedAt);
}
