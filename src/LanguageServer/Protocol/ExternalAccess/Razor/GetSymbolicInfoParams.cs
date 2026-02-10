// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

internal sealed record GetSymbolicInfoParams

{
    [JsonPropertyName("document")]
    public required TextDocumentIdentifier Document { get; init; }

    [JsonPropertyName("project")]
    public required TextDocumentIdentifier Project { get; init; }

    [JsonPropertyName("hostDocumentVersion")]
    public required int HostDocumentVersion { get; init; }

    [JsonPropertyName("generatedDocumentRanges")]
    public required Range[] GeneratedDocumentRanges { get; init; }
}

internal sealed record MemberSymbolicInfo
{
    public required MethodSymbolicInfo[] Methods { get; set; }
    public required AttributeSymbolicInfo[] Attributes { get; set; }
}

internal sealed record MethodSymbolicInfo
{
    public required string Name { get; set; }

    public required string ReturnType { get; set; }

    public required string[] ParameterTypes { get; set; }
}

internal sealed record AttributeSymbolicInfo
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required bool IsValueType { get; set; }
    public required bool IsWrittenTo { get; set; }
}
