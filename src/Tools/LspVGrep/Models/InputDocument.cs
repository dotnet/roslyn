using System.Text.Json;
using System.Text.Json.Serialization;

namespace LspVGrepTool.Models;

internal sealed class InputDocument
{
    [JsonPropertyName("directory")]
    public string? Directory { get; init; }

    [JsonPropertyName("output")]
    public string? Output { get; init; }

    [JsonPropertyName("queries")]
    public List<QueryDefinitionDto>? Queries { get; init; }
}

internal sealed class QueryDefinitionDto
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; init; }
}
