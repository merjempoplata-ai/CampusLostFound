using System.Text.Json.Serialization;

namespace CampusLostAndFound.Mcp;

// MCP-style type definitions — schemas used when registering tools with the LLM.

public record ToolDefinition(string Type, FunctionDefinition Function);
public record FunctionDefinition(string Name, string Description, ParameterSchema Parameters);
public record ParameterSchema(string Type, Dictionary<string, PropertyDefinition> Properties, string[] Required);
public record PropertyDefinition(
    string Type,
    string Description,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string[]? Enum = null);
