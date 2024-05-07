// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Interface representing the text document registration options.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocumentRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
internal interface ITextDocumentRegistrationOptions
{
    /// <summary>
    /// Gets or sets the document filters for this registration option.
    /// </summary>
    public DocumentFilter[]? DocumentSelector { get; set; }
}
