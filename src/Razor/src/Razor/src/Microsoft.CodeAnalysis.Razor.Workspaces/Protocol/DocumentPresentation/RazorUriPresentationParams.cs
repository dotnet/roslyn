// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DocumentPresentation;

/// <summary>
/// Class representing the parameters sent for a textDocument/_vs_uriPresentation request, plus
/// a host document version.
/// </summary>
internal class RazorUriPresentationParams : UriPresentationParams, IRazorPresentationParams
{
    [JsonPropertyName("kind")]
    public RazorLanguageKind Kind { get; set; }

    [JsonPropertyName("hostDocumentVersion")]
    public int HostDocumentVersion { get; set; }
}
