# MCP-Style Tool Calling in Campus Lost & Found

## What Is MCP-Style Tool Calling?

The `/api/ai/assist` endpoint implements **MCP-style tool calling** — a pattern where the LLM is given a declared set of tools (functions with typed schemas), decides at runtime which tools to invoke, and receives their results before producing a final answer.

This mirrors the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) pattern:
- **Tools** are declared upfront with names, descriptions, and JSON-Schema parameter definitions.
- The LLM selects tools autonomously based on the user's question.
- The host (this backend) executes the selected tools and feeds results back to the LLM.
- The LLM synthesizes a final answer grounded in real data.

In this project, the pattern is implemented using **OpenAI's native tool-calling API** (`tool_choice: "auto"`) with the same structural separation that MCP enforces.

---

## Registered Tools

All tools are declared in `Mcp/McpToolRegistry.cs` and documented in `mcp.tools.json`.

### `search_listings`
Search for lost or found item listings on campus.

| Parameter | Type    | Required | Description |
|-----------|---------|----------|-------------|
| `type`    | string  | No       | Filter by listing type: `"Lost"` or `"Found"` |
| `search`  | string  | No       | Free-text search across title, description, category, and location |
| `page`    | integer | No       | Page number (default: 1) |
| `limit`   | integer | No       | Results per page (default: 9) |

Returns: paginated listing results.

---

### `get_listing`
Retrieve a single listing by its UUID.

| Parameter | Type   | Required | Description |
|-----------|--------|----------|-------------|
| `id`      | string | Yes      | UUID of the listing |

Returns: full listing object or an error message.

---

### `get_monthly_report`
Get a summary of lost and found activity for a specific month.

| Parameter | Type    | Required | Description |
|-----------|---------|----------|-------------|
| `year`    | integer | Yes      | Four-digit year (e.g. 2025) |
| `month`   | integer | Yes      | Month number 1–12 |

Returns: counts of lost, found, and total listings for that month.

---

### `get_trends`
Get category/location statistics over a recent time window.

| Parameter | Type    | Required | Description |
|-----------|---------|----------|-------------|
| `days`    | integer | No       | Days to look back (default: 30) |

Returns: lost/found counts, breakdown by category and location.

---

## Request / Response Flow

```
POST /api/ai/assist
  Body: { "message": "Are there any lost laptops near the library?" }

[Step 1 — First LLM Pass]
  → Backend sends user message + McpToolRegistry.Tools to OpenAI
  ← OpenAI responds with finish_reason="tool_calls"
    tool_calls: [{ name: "search_listings", arguments: { "search": "laptop", "type": "Lost" } }]

[Step 2 — Tool Execution]
  → AiAssistHandler calls McpToolExecutor.ExecuteToolAsync("search_listings", ...)
  ← McpToolExecutor queries the database and returns a JSON result string
  → Result is appended to the conversation as a "tool" role message

[Step 3 — Second LLM Pass]
  → Backend sends full conversation (user + tool_calls + tool results) back to OpenAI
  ← OpenAI generates a final natural-language answer grounded in the tool results

[Response to client]
  {
    "answer": "Yes, there are 2 lost laptops reported near the library...",
    "toolCallLog": [{ "tool": "search_listings", "args": { "search": "laptop", "type": "Lost" } }],
    "citations": ["<listing-uuid-1>", "<listing-uuid-2>"]
  }
```

---

## Code Structure

```
Mcp/
├── McpToolDefinitions.cs   — Type records: ToolDefinition, FunctionDefinition, ParameterSchema, PropertyDefinition
├── McpToolRegistry.cs      — Declares the four tools and exposes McpToolRegistry.Tools
└── McpToolExecutor.cs      — Dispatches tool calls to DB queries; returns JSON result strings

Handlers/
└── AiAssistHandler.cs      — Orchestrates the 3-step MCP flow using the above classes

mcp.tools.json              — Machine-readable tool manifest (for grading / inspection)
```

---

## Adding a New Tool

1. Add the `ToolDefinition` entry to `Mcp/McpToolRegistry.cs` and include it in `Tools`.
2. Add a `case "your_tool_name"` branch in `McpToolExecutor.ExecuteToolAsync`.
3. Implement the private `ExecuteYourTool` method in `McpToolExecutor`.
4. Update `mcp.tools.json` to document the new tool.
