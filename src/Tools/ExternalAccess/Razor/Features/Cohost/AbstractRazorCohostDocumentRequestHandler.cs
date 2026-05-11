// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal abstract class AbstractRazorCohostDocumentRequestHandler<TRequestType, TResponseType> : AbstractRazorCohostRequestHandler<TRequestType, TResponseType>, ITextDocumentIdentifierHandler<TRequestType, TextDocumentIdentifier?>
{
    TextDocumentIdentifier? ITextDocumentIdentifierHandler<TRequestType, TextDocumentIdentifier?>.GetTextDocumentIdentifier(TRequestType request)
        => GetRazorTextDocumentIdentifier(request);

    protected abstract TextDocumentIdentifier? GetRazorTextDocumentIdentifier(TRequestType request);
}
