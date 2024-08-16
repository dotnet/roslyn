// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal interface IRemoteSemanticSearchService
{
    internal interface ICallback
    {
        ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableDefinitionItem definition, CancellationToken cancellationToken);
        ValueTask OnUserCodeExceptionAsync(RemoteServiceCallbackId callbackId, UserCodeExceptionInfo exception, CancellationToken cancellationToken);
        ValueTask OnCompilationFailureAsync(RemoteServiceCallbackId callbackId, ImmutableArray<QueryCompilationError> errors, CancellationToken cancellationToken);
        ValueTask<ClassificationOptions> GetClassificationOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken);
        ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken);
        ValueTask ItemsCompletedAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken);
    }

    ValueTask<ExecuteQueryResult> ExecuteQueryAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, string language, string query, string referenceAssembliesDir, CancellationToken cancellationToken);
}

internal static class RemoteSemanticSearchServiceProxy
{
    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteSemanticSearchService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CallbackDispatcher() : RemoteServiceCallbackDispatcher, IRemoteSemanticSearchService.ICallback
    {
        public ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableDefinitionItem definition, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).OnDefinitionFoundAsync(definition, cancellationToken);

        public ValueTask OnUserCodeExceptionAsync(RemoteServiceCallbackId callbackId, UserCodeExceptionInfo exception, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).OnUserCodeExceptionAsync(exception, cancellationToken);

        public ValueTask OnCompilationFailureAsync(RemoteServiceCallbackId callbackId, ImmutableArray<QueryCompilationError> errors, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).OnCompilationFailureAsync(errors, cancellationToken);

        public ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).AddItemsAsync(itemCount, cancellationToken);

        public ValueTask ItemsCompletedAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).ItemsCompletedAsync(itemCount, cancellationToken);

        public ValueTask<ClassificationOptions> GetClassificationOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).GetClassificationOptionsAsync(language, cancellationToken);
    }

    internal sealed class ServerCallback(Solution solution, ISemanticSearchResultsObserver observer, OptionsProvider<ClassificationOptions> classificationOptions)
    {
        public async ValueTask OnDefinitionFoundAsync(SerializableDefinitionItem definition, CancellationToken cancellationToken)
        {
            try
            {
                var rehydratedDefinition = await definition.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                await observer.OnDefinitionFoundAsync(rehydratedDefinition, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }

        public async ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
        {
            try
            {
                await observer.OnUserCodeExceptionAsync(exception, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }

        public async ValueTask OnCompilationFailureAsync(ImmutableArray<QueryCompilationError> errors, CancellationToken cancellationToken)
        {
            try
            {
                await observer.OnCompilationFailureAsync(errors, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }

        public async ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
        {
            try
            {
                await observer.AddItemsAsync(itemCount, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }

        public async ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
        {
            try
            {
                await observer.ItemsCompletedAsync(itemCount, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
            }
        }

        public async ValueTask<ClassificationOptions> GetClassificationOptionsAsync(string language, CancellationToken cancellationToken)
        {
            try
            {
                return await classificationOptions.GetOptionsAsync(solution.Services.GetLanguageServices(language), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ClassificationOptions.Default;
            }
        }
    }

    public static async ValueTask<ExecuteQueryResult> ExecuteQueryAsync(Solution solution, string language, string query, string referenceAssembliesDir, ISemanticSearchResultsObserver results, OptionsProvider<ClassificationOptions> classificationOptions, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            return new ExecuteQueryResult(FeaturesResources.Semantic_search_only_supported_on_net_core);
        }

        var serverCallback = new ServerCallback(solution, results, classificationOptions);

        var result = await client.TryInvokeAsync<IRemoteSemanticSearchService, ExecuteQueryResult>(
            solution,
            (service, solutionInfo, callbackId, cancellationToken) => service.ExecuteQueryAsync(solutionInfo, callbackId, language, query, referenceAssembliesDir, cancellationToken),
            callbackTarget: serverCallback,
            cancellationToken).ConfigureAwait(false);

        return result.Value;
    }
}
