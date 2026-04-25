// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal record CodeActionRequestInfo(
    [property: JsonPropertyName("languageKind")] RazorLanguageKind LanguageKind,
    [property: JsonPropertyName("csharpRequest")] VSCodeActionParams? CSharpRequest);
