// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal abstract class AbstractRazorCohostDocumentRequestHandler<TRequestType, TResponseType> : AbstractRazorCohostRequestHandler<TRequestType, TResponseType>, ITextDocumentIdentifierHandler<TRequestType, TextDocumentIdentifier?>
{
    TextDocumentIdentifier? ITextDocumentIdentifierHandler<TRequestType, TextDocumentIdentifier?>.GetTextDocumentIdentifier(TRequestType request)
    {
        var razorIdentifier = GetRazorTextDocumentIdentifier(request);
        if (razorIdentifier == null)
        {
            return null;
        }

        var textDocumentIdentifier = new VSTextDocumentIdentifier
        {
            Uri = razorIdentifier.Value.Uri,
        };

        if (razorIdentifier.Value.ProjectContextId != null)
        {
            textDocumentIdentifier.ProjectContext = new VSProjectContext
            {
                Id = razorIdentifier.Value.ProjectContextId
            };
        }

        return textDocumentIdentifier;
    }

    protected abstract RazorTextDocumentIdentifier? GetRazorTextDocumentIdentifier(TRequestType request);
}

/// <summary>
/// Custom type containing information in a <see cref="VSProjectContext"/> to avoid coupling LSP protocol versions.
/// </summary>
internal record struct RazorTextDocumentIdentifier(Uri Uri, string? ProjectContextId);
