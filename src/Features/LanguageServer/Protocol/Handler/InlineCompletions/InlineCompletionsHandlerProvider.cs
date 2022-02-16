// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlineCompletions;

[ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
[ProvidesMethod(VSInternalMethods.TextDocumentInlineCompletionName)]
internal class InlineCompletionsHandlerProvider : AbstractRequestHandlerProvider
{
    private readonly XmlSnippetParser _snippetParser;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InlineCompletionsHandlerProvider(XmlSnippetParser xmlSnippetParser)
    {
        _snippetParser = xmlSnippetParser;
    }

    public override ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
    {
        return ImmutableArray.Create<IRequestHandler>(new InlineCompletionsHandler(_snippetParser));
    }
}
