// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteFindUsagesService : BrokeredServiceBase, IRemoteFindUsagesService
    {
        internal sealed class Factory : FactoryBase<IRemoteFindUsagesService, IRemoteFindUsagesService.ICallback>
        {
            protected override IRemoteFindUsagesService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteFindUsagesService.ICallback> callback)
                => new RemoteFindUsagesService(arguments, callback);
        }

        private readonly RemoteCallback<IRemoteFindUsagesService.ICallback> _callback;

        public RemoteFindUsagesService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteFindUsagesService.ICallback> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        public ValueTask FindReferencesAsync(
            PinnedSolutionInfo solutionInfo,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var project = solution.GetProject(symbolAndProjectId.ProjectId);

                var symbol = await symbolAndProjectId.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                    return;

                var context = new RemoteFindUsageContext(_callback, callbackId);
                await AbstractFindUsagesService.FindReferencesAsync(
                    context, symbol, project, options, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask FindImplementationsAsync(
            PinnedSolutionInfo solutionInfo,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var project = solution.GetProject(symbolAndProjectId.ProjectId);

                var symbol = await symbolAndProjectId.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);
                if (symbol == null)
                    return;

                var context = new RemoteFindUsageContext(_callback, callbackId);
                await AbstractFindUsagesService.FindImplementationsAsync(
                    context, symbol, project, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private sealed class RemoteFindUsageContext : IFindUsagesContext, IStreamingProgressTracker
        {
            private readonly RemoteCallback<IRemoteFindUsagesService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;
            private readonly Dictionary<DefinitionItem, int> _definitionItemToId = new();

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

            public ValueTask<FindUsagesOptions> GetOptionsAsync(string language, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.GetOptionsAsync(_callbackId, language, cancellationToken), cancellationToken);

            public ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.ReportMessageAsync(_callbackId, message, cancellationToken), cancellationToken);

            public ValueTask ReportInformationalMessageAsync(string message, CancellationToken cancellationToken)
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

            public ValueTask OnReferenceFoundAsync(SourceReferenceItem reference, CancellationToken cancellationToken)
            {
                var definitionItem = GetOrAddDefinitionItemId(reference.Definition);
                var dehydratedReference = SerializableSourceReferenceItem.Dehydrate(definitionItem, reference);
                return _callback.InvokeAsync((callback, cancellationToken) => callback.OnReferenceFoundAsync(_callbackId, dehydratedReference, cancellationToken), cancellationToken);
            }

            #endregion
        }
    }
}
