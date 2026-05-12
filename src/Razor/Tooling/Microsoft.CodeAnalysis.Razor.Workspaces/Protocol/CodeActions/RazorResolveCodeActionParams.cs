// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;

internal record RazorResolveCodeActionParams(
    [property: JsonPropertyName("identifier")] TextDocumentIdentifier Identifier,
    [property: JsonPropertyName("hostDocumentVersion")] int HostDocumentVersion,
    [property: JsonPropertyName("languageKind")] RazorLanguageKind LanguageKind,
    [property: JsonPropertyName("codeAction")] CodeAction CodeAction);
