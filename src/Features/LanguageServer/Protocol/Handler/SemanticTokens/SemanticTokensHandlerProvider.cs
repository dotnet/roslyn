// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [ExportLspRequestHandlerProvider, Shared]
    internal class SemanticTokensHandlerProvider : AbstractRequestHandlerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensHandlerProvider()
        {
        }

        protected override IEnumerable<IRequestHandler> InitializeHandlers()
        {
            var semanticTokensCache = new SemanticTokensCache();
            return ImmutableArray.Create<IRequestHandler>(
                new SemanticTokensHandler(semanticTokensCache),
                new SemanticTokensEditsHandler(semanticTokensCache),
                new SemanticTokensRangeHandler(semanticTokensCache));
        }
    }
}
