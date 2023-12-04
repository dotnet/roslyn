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

    public Task<RequestContext> CreateRequestContextAsync<TRequestParam>(IQueueItem<RequestContext> queueItem, TRequestParam requestParam, CancellationToken cancellationToken)
    {
        var clientCapabilitiesManager = _lspServices.GetRequiredService<IInitializeManager>();
        var clientCapabilities = clientCapabilitiesManager.TryGetClientCapabilities();
        var logger = _lspServices.GetRequiredService<ILspServiceLogger>();
        var serverInfoProvider = _lspServices.GetRequiredService<ServerInfoProvider>();

        if (clientCapabilities is null && queueItem.MethodName != Methods.InitializeName)
        {
            throw new InvalidOperationException($"ClientCapabilities was null for a request other than {Methods.InitializeName}.");
        }

        TextDocumentIdentifier? textDocumentIdentifier;
        var textDocumentIdentifierHandler = queueItem.MethodHandler as ITextDocumentIdentifierHandler;
        if (textDocumentIdentifierHandler is ITextDocumentIdentifierHandler<TRequestParam, TextDocumentIdentifier> tHandler)
        {
            textDocumentIdentifier = tHandler.GetTextDocumentIdentifier(requestParam);
        }
        else if (textDocumentIdentifierHandler is ITextDocumentIdentifierHandler<TRequestParam, TextDocumentIdentifier?> nullHandler)
        {
            textDocumentIdentifier = nullHandler.GetTextDocumentIdentifier(requestParam);
        }
        else if (textDocumentIdentifierHandler is ITextDocumentIdentifierHandler<TRequestParam, Uri> uHandler)
        {
            var uri = uHandler.GetTextDocumentIdentifier(requestParam);
            textDocumentIdentifier = new TextDocumentIdentifier
            {
                Uri = uri,
            };
        }
        else if (textDocumentIdentifierHandler is null)
        {
            textDocumentIdentifier = null;
        }
        else
        {
            throw new NotImplementedException($"TextDocumentIdentifier in an unrecognized type for method: {queueItem.MethodName}");
        }

        bool requiresLSPSolution;
        if (queueItem.MethodHandler is ISolutionRequiredHandler requiredHandler)
        {
            requiresLSPSolution = requiredHandler.RequiresLSPSolution;
        }
        else
        {
            throw new InvalidOperationException($"{nameof(IMethodHandler)} implementation {queueItem.MethodHandler.GetType()} does not implement {nameof(ISolutionRequiredHandler)}");
        }

        return RequestContext.CreateAsync(
            queueItem.MutatesServerState,
            requiresLSPSolution,
            textDocumentIdentifier,
            serverInfoProvider.ServerKind,
            clientCapabilities,
            serverInfoProvider.SupportedLanguages,
            _lspServices,
            logger,
            queueItem.MethodName,
            cancellationToken);
    }
}
