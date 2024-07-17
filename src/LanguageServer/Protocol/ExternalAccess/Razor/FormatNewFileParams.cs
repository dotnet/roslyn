// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

internal sealed record FormatNewFileParams
{
    [JsonPropertyName("document")]
    public required TextDocumentIdentifier Document { get; init; }

    [JsonPropertyName("project")]
    public required TextDocumentIdentifier Project { get; init; }

    [JsonPropertyName("contents")]
    public required string Contents { get; init; }
}
