﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Subclass of <see cref="TypeDefinitionOptions"/> that allows scoping the registration.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#typeDefinitionRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class TypeDefinitionRegistrationOptions : TypeDefinitionOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonPropertyName("documentSelector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentFilter[]? DocumentSelector { get; set; }

    /// <summary>
    /// The id used to register the request. The id can be used to deregister the request again.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
}
