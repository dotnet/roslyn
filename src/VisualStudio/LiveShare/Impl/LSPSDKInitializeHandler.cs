// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Handle the initialize request and report the capabilities of the server.
    /// TODO Once the client side code is migrated to LSP client, this can be removed.
    /// </summary>
    [ExportLspRequestHandler(LiveShareConstants.RoslynLSPSDKContractName, LSP.Methods.InitializeName)]
    internal class LSPSDKInitializeHandler : ILspRequestHandler<LSP.InitializeParams, LSP.InitializeResult, Solution>
    {
        public Task<LSP.InitializeResult> HandleAsync(LSP.InitializeParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var result = new LSP.InitializeResult
            {
                Capabilities = new LSP.ServerCapabilities()
            };

            return Task.FromResult(result);
        }
    }
}
