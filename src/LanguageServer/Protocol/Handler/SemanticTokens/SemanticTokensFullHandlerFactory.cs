// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

[ExportCSharpVisualBasicLspServiceFactory(typeof(SemanticTokensFullHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SemanticTokensFullHandlerFactory(IGlobalOptionService globalOptions) : ILspServiceFactory
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var semanticTokensRefreshQueue = lspServices.GetRequiredService<SemanticTokensRefreshQueue>();
        return new SemanticTokensFullHandler(_globalOptions, semanticTokensRefreshQueue);
    }
}
