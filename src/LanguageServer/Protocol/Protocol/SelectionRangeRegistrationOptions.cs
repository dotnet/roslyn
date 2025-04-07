// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Subclass of <see cref="DeclarationOptions"/> that allows scoping the registration.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#selectionRangeRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal class SelectionRangeRegistrationOptions : DeclarationOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
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
