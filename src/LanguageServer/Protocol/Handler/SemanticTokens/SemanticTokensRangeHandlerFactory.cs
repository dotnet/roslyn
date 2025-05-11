// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

[ExportCSharpVisualBasicLspServiceFactory(typeof(SemanticTokensRangeHandler)), Shared]
internal sealed class SemanticTokensRangeHandlerFactory : ILspServiceFactory
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SemanticTokensRangeHandlerFactory(
        IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var semanticTokensRefreshQueue = lspServices.GetRequiredService<SemanticTokensRefreshQueue>();
        return new SemanticTokensRangeHandler(_globalOptions, semanticTokensRefreshQueue);
    }
}
