using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CampusLostAndFound.Infrastructure;

public class EmbeddingService(HttpClient httpClient, IConfiguration configuration)
{
    private const string Endpoint = "https://api.openai.com/v1/embeddings";
    private const string Model = "text-embedding-ada-002";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<float[]> EmbedAsync(string text)
    {
        var apiKey = configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var requestBody = JsonSerializer.Serialize(new { model = Model, input = text });

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<EmbeddingResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize OpenAI embedding response.");

        return result.Data[0].Embedding;
    }

    private record EmbeddingResponse(EmbeddingData[] Data);
    private record EmbeddingData(float[] Embedding);
}
