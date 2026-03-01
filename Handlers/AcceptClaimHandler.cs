using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using MediatR;

namespace CampusLostAndFound.Handlers;

public class AcceptClaimHandler(AppDbContext db) : IRequestHandler<AcceptClaimCommand, bool>
{
    public async Task<bool> Handle(AcceptClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await db.Claims.FindAsync([request.Id], cancellationToken);
        if (claim == null) return false;

        var listing = await db.Listings.FindAsync([claim.ListingId], cancellationToken);
        if (listing == null) return false;

        claim.Status    = "Accepted";
        claim.DecidedAt = DateTime.UtcNow;
        listing.Status  = "Claimed";

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
