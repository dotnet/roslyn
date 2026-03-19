// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Interface for request/notification params that apply to a document
/// </summary>
internal interface ITextDocumentParams
{
    /// <summary>
    /// The identifier of the document.
    /// </summary>
    // NOTE: these JSON attributes are not inherited, they are here as a reference for implementations
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument { get; }
}
