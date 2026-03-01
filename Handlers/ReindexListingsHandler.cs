using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CampusLostAndFound.Handlers;

public class ReindexListingsHandler(AppDbContext db, EmbeddingService embedding)
    : IRequestHandler<ReindexListingsCommand, int>
{
    private const int BatchSize = 10;

    public async Task<int> Handle(ReindexListingsCommand request, CancellationToken cancellationToken)
    {
        var unindexed = await db.Listings
            .Where(l => l.EmbeddingJson == null)
            .ToListAsync(cancellationToken);

        var indexed = 0;
        for (int i = 0; i < unindexed.Count; i += BatchSize)
        {
            var batch = unindexed.Skip(i).Take(BatchSize);
            foreach (var listing in batch)
            {
                var vec = await embedding.EmbedAsync($"{listing.Type} {listing.Title} {listing.Description} {listing.Category} {listing.Location}");
                listing.EmbeddingJson = JsonSerializer.Serialize(vec);
                indexed++;
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        return indexed;
    }
}
