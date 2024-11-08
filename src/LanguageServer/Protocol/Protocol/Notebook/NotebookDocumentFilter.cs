// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A notebook document filter denotes a notebook document by
/// different properties.
/// <para>
/// NOTE: One or more of the properties NotebookType, Scheme and Pattern must be non-null
/// </para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentFilter">Language Server Protocol specification</see> for additional information.
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class NotebookDocumentFilter
{
    /// <summary>
    /// The type of the enclosing notebook. */
    /// </summary>
    [JsonPropertyName("notebookType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NotebookType { get; init; }

    /// <summary>
    /// A Uri scheme (<see cref="System.Uri.Scheme"/>) like <c>file</c> or <c>untitled</c>.
    /// </summary>
    [JsonPropertyName("scheme")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Scheme { get; init; }

    /// <summary>
    /// A glob pattern.
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pattern { get; init; }
}
