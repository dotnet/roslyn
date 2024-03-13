// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[DataContract]
internal sealed record FormatNewFileParams
{
    [System.Text.Json.Serialization.JsonPropertyName("document")]
    public required TextDocumentIdentifier Document { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("project")]
    public required TextDocumentIdentifier Project { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("contents")]
    public required string Contents { get; init; }
}
