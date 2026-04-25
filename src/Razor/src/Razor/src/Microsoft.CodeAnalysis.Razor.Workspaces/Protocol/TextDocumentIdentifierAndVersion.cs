// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

/// <summary>
/// A serializable pairing of <see cref="TextDocumentIdentifier"/> and a version. This
/// should be used over <see cref="VersionedTextDocumentIdentifier"/> because when serializing
/// and deserializing that class, if the <see cref="TextDocumentIdentifier"/> is a <see cref="VSTextDocumentIdentifier"/>
/// it will lose the project context information.
/// </summary>
internal record class TextDocumentIdentifierAndVersion(
    [property: JsonPropertyName("textDocumentIdentifier")] TextDocumentIdentifier TextDocumentIdentifier,
    [property: JsonPropertyName("version")] int Version);
