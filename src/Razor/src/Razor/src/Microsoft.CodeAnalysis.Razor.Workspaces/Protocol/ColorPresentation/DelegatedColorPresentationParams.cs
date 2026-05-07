// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.ColorPresentation;

internal class DelegatedColorPresentationParams : ColorPresentationParams
{
    [JsonPropertyName("_vs_requiredHostDocumentVersion")]
    public int RequiredHostDocumentVersion { get; set; }
}
