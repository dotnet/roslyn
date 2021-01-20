// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
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

                var context = new RemoteFindUsageContext(_callback, callbackId, cancellationToken);
                await AbstractFindUsagesService.FindReferencesAsync(
                    context, symbol, project, options).ConfigureAwait(false);
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

                var context = new RemoteFindUsageContext(_callback, callbackId, cancellationToken);
                await AbstractFindUsagesService.FindImplementationsAsync(
                    symbol, project, context).ConfigureAwait(false);
            }, cancellationToken);
        }

        private sealed class RemoteFindUsageContext : IFindUsagesContext, IStreamingProgressTracker
        {
            private readonly RemoteCallback<IRemoteFindUsagesService.ICallback> _callback;
            private readonly RemoteServiceCallbackId _callbackId;
            private readonly Dictionary<DefinitionItem, int> _definitionItemToId = new();

            public CancellationToken CancellationToken { get; }

            public RemoteFindUsageContext(RemoteCallback<IRemoteFindUsagesService.ICallback> callback, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
            {
                _callback = callback;
                _callbackId = callbackId;
                CancellationToken = cancellationToken;
            }

            #region IStreamingProgressTracker

            public ValueTask AddItemsAsync(int count)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.AddItemsAsync(_callbackId, count), CancellationToken);

            public ValueTask ItemCompletedAsync()
                => _callback.InvokeAsync((callback, cancellationToken) => callback.ItemCompletedAsync(_callbackId), CancellationToken);

            #endregion

            #region IFindUsagesContext

            public IStreamingProgressTracker ProgressTracker => this;

            public ValueTask ReportMessageAsync(string message)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.ReportMessageAsync(_callbackId, message), CancellationToken);

            [Obsolete]
            public ValueTask ReportProgressAsync(int current, int maximum)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.ReportProgressAsync(_callbackId, current, maximum), CancellationToken);

            public ValueTask SetSearchTitleAsync(string title)
                => _callback.InvokeAsync((callback, cancellationToken) => callback.SetSearchTitleAsync(_callbackId, title), CancellationToken);

            public ValueTask OnDefinitionFoundAsync(DefinitionItem definition)
            {
                var id = GetOrAddDefinitionItemId(definition);
                var dehydratedDefinition = SerializableDefinitionItem.Dehydrate(id, definition);
                return _callback.InvokeAsync((callback, cancellationToken) => callback.OnDefinitionFoundAsync(_callbackId, dehydratedDefinition), CancellationToken);
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

            public ValueTask OnReferenceFoundAsync(SourceReferenceItem reference)
            {
                var definitionItem = GetOrAddDefinitionItemId(reference.Definition);
                var dehydratedReference = SerializableSourceReferenceItem.Dehydrate(definitionItem, reference);
                return _callback.InvokeAsync((callback, cancellationToken) => callback.OnReferenceFoundAsync(_callbackId, dehydratedReference), CancellationToken);
            }

            #endregion
        }
    }
}
