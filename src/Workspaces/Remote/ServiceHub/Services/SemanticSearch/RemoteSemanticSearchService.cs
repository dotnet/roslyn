// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.SemanticSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteSemanticSearchService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments,
    RemoteCallback<IRemoteSemanticSearchService.ICallback> callback)
    : BrokeredServiceBase(arguments), IRemoteSemanticSearchService
{
    internal sealed class Factory : FactoryBase<IRemoteSemanticSearchService, IRemoteSemanticSearchService.ICallback>
    {
        protected override IRemoteSemanticSearchService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteSemanticSearchService.ICallback> callback)
            => new RemoteSemanticSearchService(arguments, callback);
    }

    private sealed class ClientCallbacks(
        RemoteCallback<IRemoteSemanticSearchService.ICallback> callback,
        RemoteServiceCallbackId callbackId) : ISemanticSearchResultsObserver, OptionsProvider<ClassificationOptions>
    {
        public ValueTask<ClassificationOptions> GetOptionsAsync(CodeAnalysis.Host.LanguageServices languageServices, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.GetClassificationOptionsAsync(callbackId, languageServices.Language, cancellationToken), cancellationToken);

        public ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.AddItemsAsync(callbackId, itemCount, cancellationToken), cancellationToken);

        public ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.ItemsCompletedAsync(callbackId, itemCount, cancellationToken), cancellationToken);

        public async ValueTask OnSymbolFoundAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var definition = await SemanticSearchDefinitionItemFactory.CreateAsync(solution, symbol, classificationOptions: this, cancellationToken).ConfigureAwait(false);
            var dehydratedDefinition = SerializableDefinitionItem.Dehydrate(id: 0, definition);

            await callback.InvokeAsync((callback, cancellationToken) => callback.OnDefinitionFoundAsync(callbackId, dehydratedDefinition, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.OnUserCodeExceptionAsync(callbackId, exception, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<CompileQueryResult> CompileQueryAsync(
        string query,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(cancellationToken =>
        {
            var services = GetWorkspaceServices();
            var service = GetRequiredService<ISemanticSearchQueryService>();
            var result = service.CompileQuery(services, query, TraceLogger, cancellationToken);

            return ValueTask.FromResult(result);
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask DiscardQueryAsync(CompiledQueryId queryId, CancellationToken cancellationToken)
    {
        return RunServiceAsync(cancellationToken =>
        {
            var service = GetRequiredService<ISemanticSearchQueryService>();
            service.DiscardQuery(queryId);

            return default;
        }, cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<ExecuteQueryResult> ExecuteQueryAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        CompiledQueryId queryId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var service = GetRequiredService<ISemanticSearchQueryService>();
            var clientCallbacks = new ClientCallbacks(callback, callbackId);

            return await service.ExecuteQueryAsync(solution, queryId, observer: clientCallbacks, TraceLogger, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
