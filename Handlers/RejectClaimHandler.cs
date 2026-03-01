using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using MediatR;

namespace CampusLostAndFound.Handlers;

public class RejectClaimHandler(AppDbContext db) : IRequestHandler<RejectClaimCommand, bool>
{
    public async Task<bool> Handle(RejectClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await db.Claims.FindAsync([request.Id], cancellationToken);
        if (claim == null) return false;

        claim.Status    = "Rejected";
        claim.DecidedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
