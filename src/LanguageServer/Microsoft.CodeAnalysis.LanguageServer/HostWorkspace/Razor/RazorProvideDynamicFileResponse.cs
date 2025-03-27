// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

internal class RazorProvideDynamicFileResponse
{
    [JsonPropertyName("csharpDocument")]
    public required TextDocumentIdentifier CSharpDocument { get; set; }

    [JsonPropertyName("updates")]
    public RazorDynamicFileUpdate[]? Updates { get; set; }

    [JsonPropertyName("checksum")]
    public required string Checksum { get; set; }

    [JsonPropertyName("checksumAlgorithm")]
    public SourceHashAlgorithm ChecksumAlgorithm { get; set; }

    [JsonPropertyName("encodingCodePage")]
    public int? SourceEncodingCodePage { get; set; }
}
