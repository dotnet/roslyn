// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteFindUsagesService
    {
        public Task FindReferencesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectIdArg,
            SerializableFindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(symbolAndProjectIdArg.ProjectId);

                    var symbol = await symbolAndProjectIdArg.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    if (symbol == null)
                        return;

                    var context = new RemoteFindUsageContext(solution, EndPoint, cancellationToken);
                    await AbstractFindUsagesService.FindReferencesAsync(
                        context, symbol, project, options.Rehydrate()).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task FindImplementationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectIdArg,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var project = solution.GetProject(symbolAndProjectIdArg.ProjectId);

                    var symbol = await symbolAndProjectIdArg.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);
                    if (symbol == null)
                        return;

                    var context = new RemoteFindUsageContext(solution, EndPoint, cancellationToken);
                    await AbstractFindUsagesService.FindImplementationsAsync(
                        symbol, project, context).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private class RemoteFindUsageContext : IFindUsagesContext, IStreamingProgressTracker
        {
            private readonly Solution _solution;
            private readonly RemoteEndPoint _endPoint;
            private readonly Dictionary<DefinitionItem, int> _definitionItemToId = new Dictionary<DefinitionItem, int>();

            public CancellationToken CancellationToken { get; }

            public RemoteFindUsageContext(Solution solution, RemoteEndPoint endPoint, CancellationToken cancellationToken)
            {
                _solution = solution;
                _endPoint = endPoint;
                CancellationToken = cancellationToken;
            }

            #region IStreamingProgressTracker

            public Task AddItemsAsync(int count)
                => _endPoint.InvokeAsync(nameof(AddItemsAsync), new object[] { count }, CancellationToken);

            public Task ItemCompletedAsync()
                => _endPoint.InvokeAsync(nameof(ItemCompletedAsync), Array.Empty<object>(), CancellationToken);

            #endregion

            #region IFindUsagesContext

            public IStreamingProgressTracker ProgressTracker => this;

            public async ValueTask ReportMessageAsync(string message)
                => await _endPoint.InvokeAsync(nameof(ReportMessageAsync), new object[] { message }, CancellationToken).ConfigureAwait(false);

            public async ValueTask ReportProgressAsync(int current, int maximum)
                => await _endPoint.InvokeAsync(nameof(ReportProgressAsync), new object[] { current, maximum }, CancellationToken).ConfigureAwait(false);

            public async ValueTask SetSearchTitleAsync(string title)
                => await _endPoint.InvokeAsync(nameof(SetSearchTitleAsync), new object[] { title }, CancellationToken).ConfigureAwait(false);

            public async ValueTask OnDefinitionFoundAsync(DefinitionItem definition)
            {
                var id = GetOrAddDefinitionItemId(definition);
                await _endPoint.InvokeAsync(nameof(OnDefinitionFoundAsync),
                    new object[]
                    {
                        SerializableDefinitionItem.Dehydrate(id, definition),
                    },
                    CancellationToken).ConfigureAwait(false);
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

            public async ValueTask OnReferenceFoundAsync(SourceReferenceItem reference)
            {
                var definitionItem = GetOrAddDefinitionItemId(reference.Definition);
                await _endPoint.InvokeAsync(nameof(OnReferenceFoundAsync),
                    new object[]
                    {
                        SerializableSourceReferenceItem.Dehydrate(definitionItem, reference),
                    },
                    CancellationToken).ConfigureAwait(false);
            }

            #endregion
        }
    }
}
