using CampusLostAndFound.Models;
using System.Text.Json;

namespace CampusLostAndFound.Infrastructure;

public class SemanticRerankService
{
    public IReadOnlyList<Listing> Rerank(float[] queryEmbedding, IEnumerable<Listing> candidates, int topK = 8)
    {
        return candidates
            .Where(l => l.EmbeddingJson is not null)
            .Select(l =>
            {
                var vec   = JsonSerializer.Deserialize<float[]>(l.EmbeddingJson!);
                var score = vec is null ? 0f : CosineSimilarity(queryEmbedding, vec);
                return (listing: l, score);
            })
            .OrderByDescending(x => x.score)
            .Take(topK)
            .Select(x => x.listing)
            .ToList();
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0f;

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot  += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        var denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        return denom == 0 ? 0f : (float)(dot / denom);
    }
}
