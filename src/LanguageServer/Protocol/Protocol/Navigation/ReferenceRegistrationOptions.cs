// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Subclass of <see cref="ReferenceOptions"/> that allows scoping the registration.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#referenceRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class ReferenceRegistrationOptions : ReferenceOptions, ITextDocumentRegistrationOptions
{
    /// <summary>
    /// A document selector to identify the scope of the registration. If set to
    /// null the document selector provided on the client side will be used.
    /// </summary>
    [JsonPropertyName("documentSelector")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentFilter[]? DocumentSelector { get; set; }
}
