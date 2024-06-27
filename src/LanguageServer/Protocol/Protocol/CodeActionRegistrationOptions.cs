// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class representing the registration options for code action support.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
internal class CodeActionRegistrationOptions : CodeActionOptions, ITextDocumentRegistrationOptions
{
    /// <summary>
    /// Gets or sets the document filters for this registration option.
    /// </summary>
    [JsonPropertyName("documentSelector")]
    public DocumentFilter[]? DocumentSelector
    {
        get;
        set;
    }
}