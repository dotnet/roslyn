// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare;

/// <summary>
/// Handle the initialize request and report the capabilities of the server.
/// TODO Once the client side code is migrated to LSP client, this can be removed.
/// </summary>
[ExportLspRequestHandler(LiveShareConstants.RoslynLSPSDKContractName, LSP.Methods.InitializeName)]
internal sealed class LSPSDKInitializeHandler : ILspRequestHandler<LSP.InitializeParams, LSP.InitializeResult, Solution>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LSPSDKInitializeHandler()
    {
    }

    public async Task<LSP.InitializeResult> HandleAsync(LSP.InitializeParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
    {
        var result = new LSP.InitializeResult
        {
            Capabilities = new LSP.ServerCapabilities()
        };

        return result;
    }
}
