// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A filter to describe in which file operation requests or notifications
/// the server is interested in.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileOperationFilter">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class FileOperationFilter
{
    /// <summary>
    /// A Uri like `file` or `untitled`.
    /// </summary>
    [JsonPropertyName("scheme")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Scheme { get; init; }

    /// <summary>
    /// The actual file operation pattern.
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonRequired]
    public FileOperationPattern Pattern { get; init; }
}
