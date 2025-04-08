// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters sent from a server to a client for the workspace/applyEdit request.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#applyWorkspaceEditParams">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class ApplyWorkspaceEditParams
{
    /// <summary>
    /// An optional label of the workspace edit. This label is
    /// presented in the user interface for example on an undo
    /// stack to undo the workspace edit.
    /// </summary>
    [JsonPropertyName("label")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label
    {
        get;
        set;
    }

    /// <summary>
    /// The edits to apply.
    /// </summary>
    [JsonPropertyName("edit")]
    [JsonRequired]
    public WorkspaceEdit Edit
    {
        get;
        set;
    }
}
