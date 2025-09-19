// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// <see cref="VSGetProjectContextsParams" /> represents the parameter that is sent
/// with the 'textDocument/_vs_getProjectContexts' request.
/// </summary>
internal sealed class VSGetProjectContextsParams
{
    /// <summary>
    /// Gets or sets the document for which project contexts are queried.
    /// </summary>
    [JsonPropertyName("_vs_textDocument")]
    public TextDocumentItem TextDocument
    {
        get;
        set;
    }
}
