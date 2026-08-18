// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

/// <summary>
/// Represents a position in a document. If <see cref="LanguageKind"/> is Razor then the position will be
/// in the host document, otherwise it will be in the corresponding generated document.
/// </summary>
internal readonly record struct DocumentPositionInfo(
    [property: JsonPropertyName("languageKind")] RazorLanguageKind LanguageKind,
    [property: JsonPropertyName("position")] Position Position,
    [property: JsonPropertyName("hostDocumentIndex")] int HostDocumentIndex);

