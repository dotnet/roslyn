// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.SemanticSearch;

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

        public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            var dehydratedDefinition = SerializableDefinitionItem.Dehydrate(id: 0, definition);
            return callback.InvokeAsync((callback, cancellationToken) => callback.OnDefinitionFoundAsync(callbackId, dehydratedDefinition, cancellationToken), cancellationToken);
        }

        public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.OnUserCodeExceptionAsync(callbackId, exception, cancellationToken), cancellationToken);

        public ValueTask OnCompilationFailureAsync(ImmutableArray<QueryCompilationError> errors, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.OnCompilationFailureAsync(callbackId, errors, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<ExecuteQueryResult> ExecuteQueryAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        string language,
        string query,
        string referenceAssembliesDir,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var service = solution.Services.GetLanguageServices(language).GetService<ISemanticSearchService>();
            if (service == null)
            {
                return new ExecuteQueryResult(FeaturesResources.Semantic_search_only_supported_on_net_core);
            }

            var clientCallbacks = new ClientCallbacks(callback, callbackId);

            return await service.ExecuteQueryAsync(solution, query, referenceAssembliesDir, clientCallbacks, clientCallbacks, TraceLogger, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
