// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    public static VSProjectContext? GetProjectContext(this TextDocumentIdentifier textDocumentIdentifier)
        => textDocumentIdentifier is VSTextDocumentIdentifier vsIdentifier
            ? vsIdentifier.ProjectContext
            : null;

    /// <summary>
    /// Returns a copy of the passed in <see cref="TextDocumentIdentifier"/> with the passed in <see cref="Uri"/>.
    /// </summary>
    public static TextDocumentIdentifier WithUri(this TextDocumentIdentifier textDocumentIdentifier, Uri uri)
    {
        var documentUri = new DocumentUri(uri);
        if (textDocumentIdentifier is VSTextDocumentIdentifier vsTdi)
        {
            return new VSTextDocumentIdentifier
            {
                DocumentUri = documentUri,
                ProjectContext = vsTdi.ProjectContext
            };
        }

        return new TextDocumentIdentifier
        {
            DocumentUri = documentUri
        };
    }

    public static RazorTextDocumentIdentifier? ToRazorTextDocumentIdentifier(this TextDocumentIdentifier textDocumentIdentifier)
    {
        return textDocumentIdentifier.DocumentUri.ParsedUri is Uri parsedUri
            ? new RazorTextDocumentIdentifier(parsedUri, (textDocumentIdentifier as VSTextDocumentIdentifier)?.ProjectContext?.Id)
            : null;
    }
}
