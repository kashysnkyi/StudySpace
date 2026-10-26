using System.Net.Http.Json;
using System.Text.Json;

var apiUrl = (Environment.GetEnvironmentVariable("STUDYSPACE_API_URL") ?? "http://localhost:5036").TrimEnd('/');
using var client = new HttpClient { BaseAddress = new Uri(apiUrl) };
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

string[] supportedVersions = ["2026-07-28", "2025-11-25", "2025-06-18", "2025-03-26", "2024-11-05"];
while (await Console.In.ReadLineAsync() is { } line)
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    try
    {
        using var request = JsonDocument.Parse(line);
        var root = request.RootElement;
        if (!root.TryGetProperty("id", out var id)) continue;
        var method = root.GetProperty("method").GetString();
        object result = method switch
        {
            "initialize" => Initialize(root),
            "server/discover" => Discover(),
            "ping" => new { },
            "tools/list" => new { tools = ToolDefinitions(), ttlMs = 60_000, cacheScope = "server" },
            "tools/call" => await CallToolAsync(root, client),
            _ => throw new RpcException(-32601, $"Method '{method}' was not found.")
        };
        await WriteAsync(new { jsonrpc = "2.0", id = JsonSerializer.Deserialize<object>(id.GetRawText()), result });
    }
    catch (RpcException exception)
    {
        await WriteAsync(new { jsonrpc = "2.0", id = (object?)null, error = new { code = exception.Code, message = exception.Message } });
    }
    catch (Exception exception)
    {
        await WriteAsync(new { jsonrpc = "2.0", id = (object?)null, error = new { code = -32603, message = exception.Message } });
    }
}

object Initialize(JsonElement request)
{
    var requested = request.TryGetProperty("params", out var parameters) && parameters.TryGetProperty("protocolVersion", out var version)
        ? version.GetString()
        : null;
    return new
    {
        protocolVersion = supportedVersions.Contains(requested) ? requested : "2025-11-25",
        capabilities = new { tools = new { listChanged = false } },
        serverInfo = new { name = "studyspace", version = "1.0.0", title = "StudySpace MCP Server" }
    };
}

object Discover() => new
{
    protocolVersion = "2026-07-28",
    serverInfo = new { name = "studyspace", version = "1.0.0", title = "StudySpace MCP Server" },
    capabilities = new { tools = new { } }
};

object[] ToolDefinitions() =>
[
    new { name = "list_documents", description = "List documents visible to the current StudySpace API identity.", inputSchema = new { type = "object", properties = new { limit = new { type = "integer", minimum = 1, maximum = 100 } } } },
    new { name = "search_documents", description = "Full-text search StudySpace documents through the Lucene-backed API.", inputSchema = new { type = "object", properties = new { query = new { type = "string", minLength = 3 } }, required = new[] { "query" } } },
    new { name = "get_document", description = "Read one StudySpace document with blocks and attachments.", inputSchema = new { type = "object", properties = new { id = new { type = "string", format = "uuid" } }, required = new[] { "id" } } }
];

async Task<object> CallToolAsync(JsonElement request, HttpClient httpClient)
{
    var parameters = request.GetProperty("params");
    var name = parameters.GetProperty("name").GetString();
    var arguments = parameters.TryGetProperty("arguments", out var args) ? args : default;
    object data = name switch
    {
        "list_documents" => await GetJsonAsync($"/api/documents?limit={ReadInt(arguments, "limit", 20)}"),
        "search_documents" => await GetJsonAsync($"/api/search/documents?term={Uri.EscapeDataString(ReadString(arguments, "query"))}"),
        "get_document" => await GetJsonAsync($"/api/documents/{ReadString(arguments, "id")}"),
        _ => throw new RpcException(-32602, $"Unknown tool '{name}'.")
    };
    var serialized = JsonSerializer.Serialize(data, jsonOptions);
    return new { content = new[] { new { type = "text", text = serialized } }, structuredContent = data, isError = false };

    async Task<object> GetJsonAsync(string path)
    {
        using var response = await httpClient.GetAsync(path);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode) throw new RpcException(-32000, $"StudySpace API returned {(int)response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<object>(body, jsonOptions) ?? new { };
    }
}

static int ReadInt(JsonElement arguments, string name, int fallback) =>
    arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed) ? Math.Clamp(parsed, 1, 100) : fallback;

static string ReadString(JsonElement arguments, string name) =>
    arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(name, out var value) && !string.IsNullOrWhiteSpace(value.GetString())
        ? value.GetString()!
        : throw new RpcException(-32602, $"Argument '{name}' is required.");

static async Task WriteAsync(object value)
{
    await Console.Out.WriteLineAsync(JsonSerializer.Serialize(value));
    await Console.Out.FlushAsync();
}

sealed class RpcException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}
