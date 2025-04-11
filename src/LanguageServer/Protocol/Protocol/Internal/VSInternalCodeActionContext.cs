// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters sent from the client to the server for the textDocument/codeAction request.
/// </summary>
internal sealed class VSInternalCodeActionContext : CodeActionContext
{
    /// <summary>
    /// Gets or sets the range of the current selection in the document for which the command was invoked.
    /// If there is no selection this would be a Zero-length range for the caret position.
    /// </summary>
    [JsonPropertyName("_vs_selectionRange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Range? SelectionRange
    {
        get;
        set;
    }
}
