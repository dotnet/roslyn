// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>Describes a workspace folder</summary>
/// <remarks>Since LSP 3.6</remarks>
internal sealed class WorkspaceFolder
{
    /// <summary>
    /// The URI for this workspace folder.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    [JsonRequired]
    public DocumentUri DocumentUri { get; init; }

    [Obsolete("Use DocumentUri instead. This property will be removed in a future version.")]
    [JsonIgnore]
    public Uri Uri
    {
        get => DocumentUri.GetRequiredParsedUri();
        init => DocumentUri = new DocumentUri(value);
    }

    /// <summary>
    /// The name of the workspace folder used in the UI.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; }
}
