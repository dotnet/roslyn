// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static VSProjectContext? GetProjectContext(this TextDocumentIdentifier textDocumentIdentifier)
        => textDocumentIdentifier is TextDocumentIdentifier vsIdentifier
            ? vsIdentifier.ProjectContext
            : null;

    /// <summary>
    /// Returns a copy of the passed in <see cref="TextDocumentIdentifier"/> with the passed in <see cref="DocumentUri"/>.
    /// </summary>
    public static TextDocumentIdentifier WithUri(this TextDocumentIdentifier textDocumentIdentifier, DocumentUri uri)
    {
        if (textDocumentIdentifier is TextDocumentIdentifier vsTdi)
        {
            return new TextDocumentIdentifier
            {
                Uri = uri,
                ProjectContext = vsTdi.ProjectContext
            };
        }

        return new TextDocumentIdentifier
        {
            Uri = uri
        };
    }
}
