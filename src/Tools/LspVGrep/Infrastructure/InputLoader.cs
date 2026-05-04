using System.Text.Json;
using LspVGrepTool.Models;

namespace LspVGrepTool.Infrastructure;

internal static class InputLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static async Task<InputDocument> LoadAsync(string inputPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input JSON file was not found: '{inputPath}'.", inputPath);
        }

        await using var stream = File.OpenRead(inputPath);
        var input = await JsonSerializer.DeserializeAsync<InputDocument>(stream, SerializerOptions, cancellationToken);
        if (input is null)
        {
            throw new InvalidDataException($"Input JSON file '{inputPath}' did not contain a valid document.");
        }

        return input;
    }
}
