// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(SemanticTokensRangesHandler)), Shared]
    internal sealed class SemanticTokensRangesHandlerFactory : ILspServiceFactory
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangesHandlerFactory(
            IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var semanticTokensRefreshQueue = lspServices.GetRequiredService<SemanticTokensRefreshQueue>();
            return new SemanticTokensRangesHandler(_globalOptions, semanticTokensRefreshQueue);
        }
    }
}
