// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.ColorPresentation;

internal class DelegatedDocumentColorParams : DocumentColorParams
{
    [JsonPropertyName("_razor_hostDocumentVersion")]
    public int HostDocumentVersion { get; set; }
}
