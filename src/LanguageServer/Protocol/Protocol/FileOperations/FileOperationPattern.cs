// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A filter to describe in which file operation requests or notifications
/// the server is interested in.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileOperationPattern">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class FileOperationPattern
{
    /// <summary>
    /// The glob pattern to match. Glob patterns can have the following syntax:
    /// <list type="bullet">
    /// <item><c>*</c> to match one or more characters in a path segment</item>
    /// <item><c>?</c> to match on one character in a path segment</item>
    /// <item><c>**</c> to match any number of path segments, including none</item>
    /// <item><c>{}</c> to group sub patterns into an OR expression.
    /// (e.g. <c>**​/*.{ts,js}</c>matches all TypeScript and JavaScript files)</item>
    /// <item><c>[]</c>to declare a range of characters to match in a path segment
    /// (e.g., <c>example.[0-9]</c> to match on <c>example.0</c>, <c>example.1</c>, …)</item>
    /// <item><c>[!...]</c> to negate a range of characters to match in a path segment
    /// (e.g., <c>example.[!0-9]</c> to match on <c>example.a</c>, <c>example.b</c>, but not <c>example.0</c>)</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("glob")]
    [JsonRequired]
    public string Glob { get; init; }

    /// <summary>
    /// Whether to match files or folders with this pattern.
    /// Matches both if undefined.
    /// </summary>
    [JsonPropertyName("matches")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationPatternKind? Matches { get; init; }

    /// <summary>
    /// Additional options used during matching.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationPatternOptions? Options { get; init; }
}
