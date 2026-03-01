using System.Text;
using System.Text.Json;

namespace CampusLostAndFound.Infrastructure;

public class N8nWebhookService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<N8nWebhookService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SendClaimCreatedAsync(ClaimCreatedPayload payload)
    {
        var url = configuration["N8n:ClaimCreatedWebhookUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("N8n:ClaimCreatedWebhookUrl is not configured. Skipping webhook.");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            using var client   = httpClientFactory.CreateClient();
            using var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("n8n webhook responded with {StatusCode}. Claim creation is unaffected.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "n8n webhook call failed. Claim creation is unaffected.");
        }
    }

    public async Task SendListingCreatedAsync(ListingCreatedPayload payload)
    {
        var url = configuration["N8n:ListingCreatedWebhookUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogWarning("N8n:ListingCreatedWebhookUrl is not configured. Skipping webhook.");
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            using var client   = httpClientFactory.CreateClient();
            using var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("n8n listing webhook responded with {StatusCode}. Listing creation is unaffected.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "n8n listing webhook call failed. Listing creation is unaffected.");
        }
    }
}

public record ClaimCreatedPayload(Guid ListingId, string ListingTitle, string ClaimantName, string Message, DateTime CreatedAt);
public record ListingCreatedPayload(Guid ListingId, string Type, string Title, string Description, string Category, string Location, DateTime EventDate);
