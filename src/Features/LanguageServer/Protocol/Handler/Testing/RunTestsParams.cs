// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;

[DataContract]
internal record RunTestsParams(
    [property: System.Text.Json.Serialization.JsonPropertyName("textDocument")] LSP.TextDocumentIdentifier TextDocument,
    [property: System.Text.Json.Serialization.JsonPropertyName("range")] LSP.Range Range,
    [property: System.Text.Json.Serialization.JsonPropertyName("attachDebugger")] bool AttachDebugger,
    [property: System.Text.Json.Serialization.JsonPropertyName("runSettingsPath")] string? RunSettingsPath
) : LSP.IPartialResultParams<RunTestsPartialResult>
{
    [System.Text.Json.Serialization.JsonPropertyName(LSP.Methods.PartialResultTokenName)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<RunTestsPartialResult>? PartialResultToken { get; set; }
}
