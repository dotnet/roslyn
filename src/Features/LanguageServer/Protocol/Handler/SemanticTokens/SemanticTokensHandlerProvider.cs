// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
    [ProvidesMethod(Methods.TextDocumentSemanticTokensFullName)]
    [ProvidesMethod(Methods.TextDocumentSemanticTokensFullDeltaName)]
    [ProvidesMethod(Methods.TextDocumentSemanticTokensRangeName)]
    internal class SemanticTokensHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensHandlerProvider()
        {
        }

        public override ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
        {
            var semanticTokensCache = new SemanticTokensCache();
            return ImmutableArray.Create<IRequestHandler>(
                new SemanticTokensHandler(semanticTokensCache),
                new SemanticTokensEditsHandler(semanticTokensCache),
                new SemanticTokensRangeHandler(semanticTokensCache));
        }
    }
}
