﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Interface for registration options that can be scoped to particular text documents.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal interface ITextDocumentRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// <see langword="null"/> the document selector provided on the client side will be used.
    /// </summary>
    // NOTE: these JSON attributes are not inherited, they are here as a reference for implementations
    [JsonPropertyName("documentSelector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentFilter[]? DocumentSelector { get; set; }
}
