using System.Text.Json.Serialization;

namespace CampusLostAndFound.Infrastructure;

public static class ToolRegistry
{
    private static readonly ToolDefinition SearchListings = new(
        Type: "function",
        Function: new(
            Name: "search_listings",
            Description: "Search for lost or found item listings on campus. Supports optional filtering by item type and free-text search across title, description, category, and location.",
            Parameters: new(
                Type: "object",
                Properties: new Dictionary<string, PropertyDefinition>
                {
                    ["type"]   = new("string",  "Filter by listing type.",                       Enum: new[] { "Lost", "Found" }),
                    ["search"] = new("string",  "Free-text search across title, description, category, and location."),
                    ["page"]   = new("integer", "Page number for pagination. Defaults to 1."),
                    ["limit"]  = new("integer", "Number of results per page. Defaults to 9.")
                },
                Required: Array.Empty<string>()
            )
        )
    );

    private static readonly ToolDefinition GetListing = new(
        Type: "function",
        Function: new(
            Name: "get_listing",
            Description: "Retrieve a single lost or found listing by its unique identifier.",
            Parameters: new(
                Type: "object",
                Properties: new Dictionary<string, PropertyDefinition>
                {
                    ["id"] = new("string", "The UUID of the listing to retrieve.")
                },
                Required: new[] { "id" }
            )
        )
    );

    private static readonly ToolDefinition GetMonthlyReport = new(
        Type: "function",
        Function: new(
            Name: "get_monthly_report",
            Description: "Get a summary report of lost and found listings for a given month and year.",
            Parameters: new(
                Type: "object",
                Properties: new Dictionary<string, PropertyDefinition>
                {
                    ["year"]  = new("integer", "The four-digit calendar year (e.g. 2025)."),
                    ["month"] = new("integer", "The month as a number from 1 (January) to 12 (December).")
                },
                Required: new[] { "year", "month" }
            )
        )
    );

    private static readonly ToolDefinition GetTrends = new(
        Type: "function",
        Function: new(
            Name: "get_trends",
            Description: "Get statistics and trends for lost and found items over a recent time period.",
            Parameters: new(
                Type: "object",
                Properties: new Dictionary<string, PropertyDefinition>
                {
                    ["days"] = new("integer", "Number of days to look back. Defaults to 30.")
                },
                Required: Array.Empty<string>()
            )
        )
    );

    public static IReadOnlyList<ToolDefinition> Tools { get; } = new[]
    {
        SearchListings,
        GetListing,
        GetMonthlyReport,
        GetTrends
    };
}

public record ToolDefinition(string Type, FunctionDefinition Function);
public record FunctionDefinition(string Name, string Description, ParameterSchema Parameters);
public record ParameterSchema(string Type, Dictionary<string, PropertyDefinition> Properties, string[] Required);
public record PropertyDefinition(
    string Type,
    string Description,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string[]? Enum = null);
