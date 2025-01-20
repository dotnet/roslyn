// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A notebook cell text document filter denotes a cell text document by
/// different properties.
/// 
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookCellTextDocumentFilter">Language Server Protocol specification</see> for additional information.
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class NotebookCellTextDocumentFilter
{
    /// <summary>
    /// A filter that matches against the notebook
    /// containing the notebook cell. If a string
    /// value is provided it matches against the
    /// notebook type. '*' matches every notebook.
    /// </summary>
    [JsonPropertyName("notebook")]
    [JsonRequired]
    public SumType<string, NotebookDocumentFilter> Notebook { get; init; }

    /// <summary>
    /// A language id like `python`.
    /// 
    /// Will be matched against the language id of the
    /// notebook cell document. '*' matches every language.
    /// </summary>
    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language { get; init; }
}
