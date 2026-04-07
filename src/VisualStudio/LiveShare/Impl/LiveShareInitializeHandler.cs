// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Shims;

internal class LiveShareInitializeHandler : ILspRequestHandler<InitializeParams, InitializeResult, Solution>
{
    private static readonly InitializeResult s_initializeResult = new()
    {
        Capabilities = new ServerCapabilities
        {
            CodeActionProvider = false,
            ReferencesProvider = true,
            RenameProvider = false,
        }
    };

    public Task<InitializeResult> HandleAsync(InitializeParams param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        => Task.FromResult(s_initializeResult);
}

[ExportLspRequestHandler(LiveShareConstants.RoslynContractName, Methods.InitializeName)]
[Obsolete("Used for backwards compatibility with old liveshare clients.")]
internal sealed class RoslynInitializeHandlerShim : LiveShareInitializeHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RoslynInitializeHandlerShim()
    {
    }
}

[ExportLspRequestHandler(LiveShareConstants.CSharpContractName, Methods.InitializeName)]
internal sealed class CSharpInitializeHandlerShim : LiveShareInitializeHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpInitializeHandlerShim()
    {
    }
}

[ExportLspRequestHandler(LiveShareConstants.VisualBasicContractName, Methods.InitializeName)]
internal sealed class VisualBasicInitializeHandlerShim : LiveShareInitializeHandler
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualBasicInitializeHandlerShim()
    {
    }
}
