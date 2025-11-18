// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the registration options for inline completion support.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#inlineCompletionRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class VSInternalInlineCompletionRegistrationOptions : VSInternalInlineCompletionOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// Gets or sets the document filters for this registration option.
    /// </summary>
    [JsonPropertyName("documentSelector")]
    public DocumentFilter[]? DocumentSelector
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the id used to register the request.  The id can be used to deregister the request again.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id
    {
        get;
        set;
    }
}
