// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteFindUsagesService(in BrokeredServiceBase.ServiceConstructionArguments arguments, RemoteCallback<IRemoteFindUsagesService.ICallback> callback)
    : BrokeredServiceBase(arguments), IRemoteFindUsagesService
{
    internal sealed class Factory : FactoryBase<IRemoteFindUsagesService, IRemoteFindUsagesService.ICallback>
    {
        protected override IRemoteFindUsagesService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteFindUsagesService.ICallback> callback)
            => new RemoteFindUsagesService(arguments, callback);
    }

    public ValueTask FindReferencesAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        SerializableSymbolAndProjectId symbolAndProjectId,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(symbolAndProjectId.ProjectId);

            var symbol = await symbolAndProjectId.TryRehydrateAsync(
                solution, cancellationToken).ConfigureAwait(false);

            if (symbol == null)
                return;

            var context = new RemoteFindUsageContext(callback, callbackId);
            var classificationOptions = GetClientOptionsProvider<ClassificationOptions, IRemoteFindUsagesService.ICallback>(callback, callbackId);

            await AbstractFindUsagesService.FindReferencesAsync(
                context, symbol, project, options, classificationOptions, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    public ValueTask FindImplementationsAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        SerializableSymbolAndProjectId symbolAndProjectId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var project = solution.GetRequiredProject(symbolAndProjectId.ProjectId);

            var symbol = await symbolAndProjectId.TryRehydrateAsync(
                solution, cancellationToken).ConfigureAwait(false);
            if (symbol == null)
                return;

            var context = new RemoteFindUsageContext(callback, callbackId);
            var classificationOptions = GetClientOptionsProvider<ClassificationOptions, IRemoteFindUsagesService.ICallback>(callback, callbackId);

            await AbstractFindUsagesService.FindImplementationsAsync(
                context, symbol, project, classificationOptions, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    private sealed class RemoteFindUsageContext : IFindUsagesContext, IStreamingProgressTracker
    {
        private readonly RemoteCallback<IRemoteFindUsagesService.ICallback> _callback;
        private readonly RemoteServiceCallbackId _callbackId;
        private readonly Dictionary<DefinitionItem, int> _definitionItemToId = [];

        public RemoteFindUsageContext(RemoteCallback<IRemoteFindUsagesService.ICallback> callback, RemoteServiceCallbackId callbackId)
        {
            _callback = callback;
            _callbackId = callbackId;
        }

        #region IStreamingProgressTracker

        public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.AddItemsAsync(_callbackId, count, cancellationToken), cancellationToken);

        public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.ItemsCompletedAsync(_callbackId, count, cancellationToken), cancellationToken);

        #endregion

        #region IFindUsagesContext

        public IStreamingProgressTracker ProgressTracker => this;

        public ValueTask ReportNoResultsAsync(string message, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.ReportMessageAsync(_callbackId, message, cancellationToken), cancellationToken);

        public ValueTask ReportMessageAsync(string message, NotificationSeverity severity, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.ReportInformationalMessageAsync(_callbackId, message, cancellationToken), cancellationToken);

        public ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.SetSearchTitleAsync(_callbackId, title, cancellationToken), cancellationToken);

        public ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
        {
            var id = GetOrAddDefinitionItemId(definition);
            var dehydratedDefinition = SerializableDefinitionItem.Dehydrate(id, definition);
            return _callback.InvokeAsync((callback, cancellationToken) => callback.OnDefinitionFoundAsync(_callbackId, dehydratedDefinition, cancellationToken), cancellationToken);
        }

        private int GetOrAddDefinitionItemId(DefinitionItem item)
        {
            lock (_definitionItemToId)
            {
                if (!_definitionItemToId.TryGetValue(item, out var id))
                {
                    id = _definitionItemToId.Count;
                    _definitionItemToId.Add(item, id);
                }

                return id;
            }
        }

        public async ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SerializableSourceReferenceItem>.GetInstance(out var dehydrated);
            await foreach (var reference in references)
            {
                var dehydratedReference = SerializableSourceReferenceItem.Dehydrate(
                    GetOrAddDefinitionItemId(reference.Definition), reference);
                dehydrated.Add(dehydratedReference);
            }

            var dehydratedReferences = dehydrated.ToImmutableAndClear();
            await _callback.InvokeAsync((callback, cancellationToken) => callback.OnReferencesFoundAsync(
                _callbackId, dehydratedReferences, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
