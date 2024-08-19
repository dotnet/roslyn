// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Roslyn.LanguageServer.Protocol;

internal sealed record RazorComponentInfoParams

{
    [JsonPropertyName("document")]
    public required TextDocumentIdentifier Document { get; init; }

    [JsonPropertyName("newDocument")]
    public required TextDocumentIdentifier NewDocument { get; init; }

    [JsonPropertyName("project")]
    public required TextDocumentIdentifier Project { get; init; }

    [JsonPropertyName("hostDocumentVersion")]
    public required int HostDocumentVersion { get; init; }

    [JsonPropertyName("newContents")]
    public required string NewContents { get; init; }
}

// Not sure where to put these two records
internal sealed record RazorComponentInfo
{
    public required List<MethodInsideRazorElementInfo> Methods { get; set; }
    public required List<SymbolInsideRazorElementInfo> Fields { get; set; }
}


internal sealed record MethodInsideRazorElementInfo
{
    public required string Name { get; set; }

    public required string ReturnType { get; set; }

    public required List<string> ParameterTypes { get; set; }
}

internal sealed record SymbolInsideRazorElementInfo
{
    public required string Name { get; set; }
    public required string Type { get; set; }
}
