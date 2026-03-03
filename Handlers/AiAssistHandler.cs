using CampusLostAndFound.Commands;
using CampusLostAndFound.Data;
using CampusLostAndFound.Dtos;
using CampusLostAndFound.Mcp;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CampusLostAndFound.Handlers;

public class AiAssistHandler(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    McpToolExecutor mcpToolExecutor)
    : IRequestHandler<AiAssistQuery, AssistResponseDto>
{
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AssistResponseDto> Handle(AiAssistQuery request, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenAI:ApiKey"] ?? configuration["Ai:ApiKey"]
            ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var messages    = new List<Message> { new("user", Content: request.Message) };
        var toolCallLog = new List<ToolCallLogDto>();

        // --- MCP Step 1: First LLM pass — model selects which tools to call ---
        var firstResponse = await CallLlmAsync(apiKey, messages, McpToolRegistry.Tools);
        var firstChoice   = firstResponse.Choices[0];

        if (firstChoice.FinishReason != "tool_calls" || firstChoice.Message.ToolCalls is null)
        {
            return new AssistResponseDto(firstChoice.Message.Content ?? string.Empty, toolCallLog, []);
        }

        messages.Add(firstChoice.Message);

        // --- MCP Step 2: Execute each requested tool via McpToolExecutor ---
        foreach (var toolCall in firstChoice.Message.ToolCalls)
        {
            var result = await mcpToolExecutor.ExecuteToolAsync(
                toolCall.Function.Name, toolCall.Function.Arguments, cancellationToken);
            messages.Add(new("tool", Content: result, ToolCallId: toolCall.Id));

            using var argsDoc = JsonDocument.Parse(toolCall.Function.Arguments);
            toolCallLog.Add(new ToolCallLogDto(toolCall.Function.Name, argsDoc.RootElement.Clone()));
        }

        // --- MCP Step 3: Second LLM pass — model generates final answer using tool results ---
        var secondResponse = await CallLlmAsync(apiKey, messages, tools: null);
        var answer         = secondResponse.Choices[0].Message.Content ?? string.Empty;

        var allIds    = await db.Listings.Select(l => l.Id).ToListAsync(cancellationToken);
        var citations = allIds
            .Where(id => answer.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new AssistResponseDto(answer, toolCallLog, citations);
    }

    private async Task<CompletionResponse> CallLlmAsync(string apiKey, List<Message> messages, IReadOnlyList<ToolDefinition>? tools)
    {
        var model = configuration["Ai:AssistModel"] ?? configuration["Ai:ChatModel"] ?? "gpt-4o-mini";

        object requestBody = tools is not null
            ? new { model, messages, tools, tool_choice = "auto" }
            : (object)new { model, messages };

        using var httpClient = httpClientFactory.CreateClient();
        using var req        = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(req);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"OpenAI {(int)response.StatusCode}: {errorBody}");
        }

        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<CompletionResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize LLM response.");
    }

    private record CompletionResponse(Choice[] Choices);
    private record Choice(string FinishReason, Message Message);
    private record Message(
        string Role,
        string? Content     = null,
        ToolCall[]? ToolCalls = null,
        string? ToolCallId  = null);
    private record ToolCall(string Id, string Type, ToolCallFunction Function);
    private record ToolCallFunction(string Name, string Arguments);
}
