// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class RequestContextFactory : AbstractRequestContextFactory<RequestContext>, ILspService
{
    private readonly ILspServices _lspServices;

    public RequestContextFactory(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public override async Task<RequestContextInfo<RequestContext>> CreateRequestContextAsync<TRequestParam>(QueueItem<RequestContext> queueItem, IMethodHandler methodHandler, TRequestParam requestParam, CancellationToken cancellationToken)
    {
        var clientCapabilitiesManager = _lspServices.GetRequiredService<IInitializeManager>();
        var clientCapabilities = clientCapabilitiesManager.TryGetClientCapabilities();
        var logger = _lspServices.GetRequiredService<ILspLogger>();
        var serverInfoProvider = _lspServices.GetRequiredService<ServerInfoProvider>();

        if (clientCapabilities is null && queueItem.MethodName != Methods.InitializeName)
        {
            throw new InvalidOperationException($"ClientCapabilities was null for a request other than {Methods.InitializeName}.");
        }

        TextDocumentIdentifier? textDocumentIdentifier;
        var textDocumentIdentifierHandler = methodHandler as ITextDocumentIdentifierHandler;
        if (textDocumentIdentifierHandler is ITextDocumentIdentifierHandler<TRequestParam, TextDocumentIdentifier> tHandler)
        {
            textDocumentIdentifier = tHandler.GetTextDocumentIdentifier(requestParam);
        }
        else if (textDocumentIdentifierHandler is ITextDocumentIdentifierHandler<TRequestParam, TextDocumentIdentifier?> nullHandler)
        {
            textDocumentIdentifier = nullHandler.GetTextDocumentIdentifier(requestParam);
        }
        else if (textDocumentIdentifierHandler is ITextDocumentIdentifierHandler<TRequestParam, TextDocumentItem> uHandler)
        {
            var textDocumentItem = uHandler.GetTextDocumentIdentifier(requestParam);
            textDocumentIdentifier = new TextDocumentIdentifier
            {
                DocumentUri = textDocumentItem.DocumentUri,
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
        LspSolutionContextPreference solutionContextPreference;
        if (methodHandler is ISolutionRequiredHandler requiredHandler)
        {
            requiresLSPSolution = requiredHandler.RequiresLSPSolution;
            solutionContextPreference = requiresLSPSolution && textDocumentIdentifier is not null
                ? requiredHandler.SolutionContextPreference
                : LspSolutionContextPreference.NoPreference;
        }
        else
        {
            throw new InvalidOperationException($"{nameof(IMethodHandler)} implementation {methodHandler.GetType()} does not implement {nameof(ISolutionRequiredHandler)}");
        }

        if (methodHandler.MutatesSolutionState && solutionContextPreference != LspSolutionContextPreference.NoPreference)
        {
            throw new InvalidOperationException(
                $"Mutating handler {methodHandler.GetType()} must declare {nameof(LspSolutionContextPreference)}.{nameof(LspSolutionContextPreference.NoPreference)}.");
        }

        if (solutionContextPreference == LspSolutionContextPreference.NoPreference)
        {
            var requestContext = await RequestContext.CreateAsync(
                methodHandler.MutatesSolutionState,
                requiresLSPSolution,
                textDocumentIdentifier,
                serverInfoProvider.ServerKind,
                clientCapabilities,
                serverInfoProvider.SupportedLanguages,
                _lspServices,
                logger,
                queueItem.MethodName,
                LspSolutionContextPreference.NoPreference,
                OnDemandProjectLoadResult.Empty,
                trackedDocuments: null,
                cancellationToken).ConfigureAwait(false);

            return new RequestContextInfo<RequestContext>(requestContext);
        }

        var onDemandProjectLoader = _lspServices.GetService<IOnDemandProjectLoader>();
        var loadOperation = solutionContextPreference == LspSolutionContextPreference.Workspace
            ? onDemandProjectLoader?.GetWorkspaceLoadOperation() ?? OnDemandProjectLoadOperation.Completed
            : onDemandProjectLoader?.StartLoading(textDocumentIdentifier!.DocumentUri) ?? OnDemandProjectLoadOperation.Completed;
        var trackedDocuments = _lspServices.GetRequiredService<LspWorkspaceManager>().GetTrackedLspText();

        // Never resolves a document/solution: the framework only observes this value if PrepareContextAsync
        // is never invoked, so there is no reason to pay for the real (potentially expensive) resolution here.
        var placeholderContext = await CreateContextAsync(requiresSolution: false, cancellationToken).ConfigureAwait(false);

        return new RequestContextInfo<RequestContext>(placeholderContext, PrepareContextAsync);

        Task<RequestContext> CreateContextAsync(bool requiresSolution, CancellationToken ct)
            => RequestContext.CreateAsync(
                mutatesSolutionState: false,
                requiresSolution,
                textDocumentIdentifier,
                serverInfoProvider.ServerKind,
                clientCapabilities,
                serverInfoProvider.SupportedLanguages,
                _lspServices,
                logger,
                queueItem.MethodName,
                LspSolutionContextPreference.NoPreference,
                OnDemandProjectLoadResult.Empty,
                trackedDocuments: null,
                ct);

        async Task<RequestContext> PrepareContextAsync(CancellationToken preparationCancellationToken)
        {
            try
            {
                var loadResult = await loadOperation.WaitAsync(solutionContextPreference, preparationCancellationToken).ConfigureAwait(false);
                return await RequestContext.CreateAsync(
                    mutatesSolutionState: false,
                    requiresLSPSolution,
                    textDocumentIdentifier,
                    serverInfoProvider.ServerKind,
                    clientCapabilities,
                    serverInfoProvider.SupportedLanguages,
                    _lspServices,
                    logger,
                    queueItem.MethodName,
                    solutionContextPreference,
                    loadResult,
                    trackedDocuments,
                    preparationCancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (preparationCancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (FatalError.ReportAndCatch(exception))
            {
                logger.LogException(exception);
                // Fall back to the cache-based resolution used by non-deferred requests instead of the cheap placeholder.
                return await CreateContextAsync(requiresSolution: requiresLSPSolution, preparationCancellationToken).ConfigureAwait(false);
            }
        }
    }
}
