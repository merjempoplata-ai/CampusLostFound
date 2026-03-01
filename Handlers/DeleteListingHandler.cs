using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using MediatR;

namespace CampusLostAndFound.Handlers;

public class DeleteListingHandler(AppDbContext db) : IRequestHandler<DeleteListingCommand, bool>
{
    public async Task<bool> Handle(DeleteListingCommand request, CancellationToken cancellationToken)
    {
        var listing = await db.Listings.FindAsync([request.Id], cancellationToken);
        if (listing == null) return false;
        db.Listings.Remove(listing);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
