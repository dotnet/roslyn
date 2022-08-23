// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal class RequestContextFactory : IRequestContextFactory<RequestContext>, ILspService
{
    private readonly ILspServices _lspServices;

    public RequestContextFactory(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public Task<RequestContext> CreateRequestContextAsync(IQueueItem<RequestContext> queueItem, CancellationToken cancellationToken)
    {
        var clientCapabilitiesManager = _lspServices.GetRequiredService<IClientCapabilitiesManager>();
        var clientCapabilities = clientCapabilitiesManager.TryGetClientCapabilities();
        var logger = _lspServices.GetRequiredService<IRoslynLspLogger>();
        var serverInfoProvider = _lspServices.GetRequiredService<ServerInfoProvider>();

        TextDocumentIdentifier? textDocumentIdentifier;
        if (queueItem.TextDocumentIdentifier is TextDocumentIdentifier t)
        {
            textDocumentIdentifier = t;
        }
        else if (queueItem.TextDocumentIdentifier is Uri uri)
        {
            textDocumentIdentifier = new TextDocumentIdentifier
            {
                Uri = uri,
            };
        }
        else if (queueItem.TextDocumentIdentifier is null)
        {
            textDocumentIdentifier = null;
        }
        else
        {
            throw new NotImplementedException($"TextDocument in an unrecognized type for method: {queueItem.MethodName}");
        }

        return RequestContext.CreateAsync(
            queueItem.MutatesDocumentState,
            textDocumentIdentifier,
            serverInfoProvider.ServerKind,
            clientCapabilities,
            serverInfoProvider.SupportedLanguages,
            _lspServices,
            logger,
            cancellationToken);
    }
}
