// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>Describes a workspace folder</summary>
/// <remarks>Since LSP 3.6</remarks>
internal class WorkspaceFolder
{
    /// <summary>
    /// The URI for this workspace folder.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    [JsonRequired]
    public Uri Uri { get; init; }

    /// <summary>
    /// The name of the workspace folder used in the UI.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; }
}
