// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.SemanticSearch;

internal interface IRemoteSemanticSearchService
{
    internal interface ICallback
    {
        ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableDefinitionItem definition, CancellationToken cancellationToken);
        ValueTask OnUserCodeExceptionAsync(RemoteServiceCallbackId callbackId, UserCodeExceptionInfo exception, CancellationToken cancellationToken);
        ValueTask<ClassificationOptions> GetClassificationOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken);
        ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken);
        ValueTask ItemsCompletedAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken);
    }

    ValueTask<CompileQueryResult> CompileQueryAsync(string query, string language, string referenceAssembliesDir, CancellationToken cancellationToken);
    ValueTask<ExecuteQueryResult> ExecuteQueryAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, CompiledQueryId queryId, CancellationToken cancellationToken);
    ValueTask DiscardQueryAsync(CompiledQueryId queryId, CancellationToken cancellationToken);
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

        public ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).AddItemsAsync(itemCount, cancellationToken);

        public ValueTask ItemsCompletedAsync(RemoteServiceCallbackId callbackId, int itemCount, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).ItemsCompletedAsync(itemCount, cancellationToken);

        public ValueTask<ClassificationOptions> GetClassificationOptionsAsync(RemoteServiceCallbackId callbackId, string language, CancellationToken cancellationToken)
            => ((ServerCallback)GetCallback(callbackId)).GetClassificationOptionsAsync(language, cancellationToken);
    }

    internal sealed class ServerCallback(Solution solution, ISemanticSearchResultsDefinitionObserver observer)
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
                return await observer.GetClassificationOptionsAsync(solution.Services.GetLanguageServices(language), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return ClassificationOptions.Default;
            }
        }
    }

    public static async ValueTask<CompileQueryResult?> CompileQueryAsync(SolutionServices services, string query, string language, string referenceAssembliesDir, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            return null;
        }

        var result = await client.TryInvokeAsync<IRemoteSemanticSearchService, CompileQueryResult>(
            (service, cancellationToken) => service.CompileQueryAsync(query, language, referenceAssembliesDir, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return result.Value;
    }

    public static async ValueTask DiscardQueryAsync(SolutionServices services, CompiledQueryId queryId, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(client);

        await client.TryInvokeAsync<IRemoteSemanticSearchService>(
            (service, cancellationToken) => service.DiscardQueryAsync(queryId, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<ExecuteQueryResult> ExecuteQueryAsync(Solution solution, CompiledQueryId queryId, ISemanticSearchResultsDefinitionObserver results, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull(client);

        var serverCallback = new ServerCallback(solution, results);

        var result = await client.TryInvokeAsync<IRemoteSemanticSearchService, ExecuteQueryResult>(
            solution,
            (service, solutionInfo, callbackId, cancellationToken) => service.ExecuteQueryAsync(solutionInfo, callbackId, queryId, cancellationToken),
            callbackTarget: serverCallback,
            cancellationToken).ConfigureAwait(false);

        return result.Value;
    }
}
