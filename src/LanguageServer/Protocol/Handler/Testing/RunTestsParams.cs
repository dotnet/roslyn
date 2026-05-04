// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

internal sealed record RunTestsParams(
    [property: JsonPropertyName("textDocument")] LSP.TextDocumentIdentifier TextDocument,
    [property: JsonPropertyName("range")] LSP.Range Range,
    [property: JsonPropertyName("attachDebugger")] bool AttachDebugger,
    [property: JsonPropertyName("runSettingsPath")] string? RunSettingsPath
) : LSP.IPartialResultParams<RunTestsPartialResult>
{
    [JsonPropertyName(LSP.Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<RunTestsPartialResult>? PartialResultToken { get; set; }
}
