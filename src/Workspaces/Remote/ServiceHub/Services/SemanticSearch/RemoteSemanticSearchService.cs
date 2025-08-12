// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.CodeAnalysis.Text;

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
            await OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnSyntaxNodeFoundAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var definition = await SemanticSearchDefinitionItemFactory.CreateAsync(document, node, cancellationToken).ConfigureAwait(false);
            await OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnValueFoundAsync(Solution solution, object value, CancellationToken cancellationToken)
        {
            var definition = SemanticSearchDefinitionItemFactory.Create(value.ToString() ?? "<null>");
            await OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnLocationFoundAsync(Solution solution, Location location, CancellationToken cancellationToken)
        {
            var definition = await SemanticSearchDefinitionItemFactory.CreateAsync(solution, location, cancellationToken).ConfigureAwait(false);
            if (definition == null)
            {
                return;
            }

            await OnDefinitionFoundAsync(definition, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            var dehydratedDefinition = SerializableDefinitionItem.Dehydrate(id: 0, definition);
            await callback.InvokeAsync((callback, cancellationToken) => callback.OnDefinitionFoundAsync(callbackId, dehydratedDefinition, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.OnUserCodeExceptionAsync(callbackId, exception, cancellationToken), cancellationToken);

        public ValueTask OnDocumentUpdatedAsync(DocumentId documentId, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.OnDocumentUpdatedAsync(callbackId, documentId, changes, cancellationToken), cancellationToken);

        public ValueTask OnLogMessageAsync(string message, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.OnLogMessageAsync(callbackId, message, cancellationToken), cancellationToken);

        public ValueTask OnTextFileUpdatedAsync(string filePath, string? newContent, CancellationToken cancellationToken)
            => callback.InvokeAsync((callback, cancellationToken) => callback.OnTextFileUpdatedAsync(callbackId, filePath, newContent, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Remote API.
    /// </summary>
    public ValueTask<CompileQueryResult> CompileQueryAsync(
        string query,
        string? targetLanguage,
        string referenceAssembliesDir,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(cancellationToken =>
        {
            var services = GetWorkspaceServices();
            var service = GetRequiredService<ISemanticSearchQueryService>();
            var result = service.CompileQuery(services, query, targetLanguage, referenceAssembliesDir, TraceLogger, cancellationToken);

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
        QueryExecutionOptions options,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var service = GetRequiredService<ISemanticSearchQueryService>();
            var clientCallbacks = new ClientCallbacks(callback, callbackId);

            return await service.ExecuteQueryAsync(solution, queryId, observer: clientCallbacks, options, TraceLogger, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
