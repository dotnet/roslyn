// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Utilities;

using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal sealed partial class ItemManager : IAsyncCompletionItemManager2
    {
        public const string AggressiveDefaultsMatchingOptionName = "AggressiveDefaultsMatchingOption";

        private readonly RecentItemsManager _recentItemsManager;
        private readonly IGlobalOptionService _globalOptions;

        internal ItemManager(RecentItemsManager recentItemsManager, IGlobalOptionService globalOptions)
        {
            _recentItemsManager = recentItemsManager;
            _globalOptions = globalOptions;
        }

        public Task<ImmutableArray<VSCompletionItem>> SortCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken cancellationToken)
        {
            var stopwatch = SharedStopwatch.StartNew();
            var items = SortCompletionitems(data, cancellationToken).ToImmutableArray();

            AsyncCompletionLogger.LogItemManagerSortTicksDataPoint(stopwatch.Elapsed);
            return Task.FromResult(items);
        }

        public Task<CompletionList<VSCompletionItem>> SortCompletionItemListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionInitialDataSnapshot data,
            CancellationToken cancellationToken)
        {
            var stopwatch = SharedStopwatch.StartNew();
            var itemList = session.CreateCompletionList(SortCompletionitems(data, cancellationToken));

            AsyncCompletionLogger.LogItemManagerSortTicksDataPoint(stopwatch.Elapsed);
            return Task.FromResult(itemList);
        }

        private static SegmentedList<VSCompletionItem> SortCompletionitems(AsyncCompletionSessionInitialDataSnapshot data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = new SegmentedList<VSCompletionItem>(data.InitialItemList.Count);
            foreach (var item in data.InitialItemList)
            {
                CompletionItemData.GetOrAddDummyRoslynItem(item);
                items.Add(item);
            }

            items.Sort(VSItemComparer.Instance);
            return items;
        }

        public async Task<FilteredCompletionModel?> UpdateCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
        {
            var stopwatch = SharedStopwatch.StartNew();
            try
            {
                var sessionData = CompletionSessionData.GetOrCreateSessionData(session);

                // As explained in more details in the comments for `CompletionSource.GetCompletionContextAsync`, expanded items might
                // not be provided upon initial trigger of completion to reduce typing delays, even if they are supposed to be included by default.
                // While we do not expect to run in to this scenario very often, we'd still want to minimize the impact on user experience of this feature
                // as best as we could when it does occur. So the solution we came up with is this: if we decided to not include expanded items (because the
                // computation is running too long,) we will let it run in the background as long as the completion session is still active. Then whenever
                // any user input that would cause the completion list to refresh, we will check the state of this background task and add expanded items as part
                // of the update if they are available.
                // There is a `CompletionContext.IsIncomplete` flag, which is only supported in LSP mode at the moment. Therefore we opt to handle the checking
                // and combining the items in Roslyn until the `IsIncomplete` flag is fully supported in classic mode.

                if (sessionData.CombinedSortedList is not null)
                {
                    // Always use the previously saved combined list if available.
                    data = new AsyncCompletionSessionDataSnapshot(sessionData.CombinedSortedList, data.Snapshot, data.Trigger, data.InitialTrigger, data.SelectedFilters,
                        data.IsSoftSelected, data.DisplaySuggestionItem, data.Defaults);
                }
                else if (sessionData.ExpandedItemsTask != null)
                {
                    var task = sessionData.ExpandedItemsTask;
                    if (task.Status == TaskStatus.RanToCompletion)
                    {
                        // Make sure the task is removed when Adding expanded items,
                        // so duplicated items won't be added in subsequent list updates.
                        sessionData.ExpandedItemsTask = null;

                        var (expandedContext, _) = await task.ConfigureAwait(false);
                        if (expandedContext.ItemList.Count > 0)
                        {
                            // Here we rely on the implementation detail of `CompletionItem.CompareTo`, which always put expand items after regular ones.
                            var combinedItemList = session.CreateCompletionList(data.InitialSortedItemList.Concat(expandedContext.ItemList));

                            // Add expanded items into a combined list, and save it to be used for future updates during the same session.
                            sessionData.CombinedSortedList = combinedItemList;
                            var combinedFilterStates = FilterSet.CombineFilterStates(expandedContext.Filters, data.SelectedFilters);

                            data = new AsyncCompletionSessionDataSnapshot(combinedItemList, data.Snapshot, data.Trigger, data.InitialTrigger, combinedFilterStates,
                                data.IsSoftSelected, data.DisplaySuggestionItem, data.Defaults);
                        }

                        AsyncCompletionLogger.LogSessionWithDelayedImportCompletionIncludedInUpdate();
                    }
                }

                var updater = new CompletionListUpdater(session.ApplicableToSpan, sessionData, data, _recentItemsManager, _globalOptions);
                return updater.UpdateCompletionList(session, cancellationToken);
            }
            finally
            {
                AsyncCompletionLogger.LogItemManagerUpdateDataPoint(stopwatch.Elapsed, isCanceled: cancellationToken.IsCancellationRequested);
            }
        }

        private sealed class VSItemComparer : IComparer<VSCompletionItem>
        {
            public static VSItemComparer Instance { get; } = new();

            private VSItemComparer()
            {
            }

            public int Compare(VSCompletionItem? x, VSCompletionItem? y)
            {
                if (x is null && y is null)
                    return 0;

                var xRoslyn = x is not null ? CompletionItemData.GetOrAddDummyRoslynItem(x) : null;
                var yRoslyn = y is not null ? CompletionItemData.GetOrAddDummyRoslynItem(y) : null;

                // Sort by default comparer of Roslyn CompletionItem
                return Comparer<RoslynCompletionItem>.Default.Compare(xRoslyn, yRoslyn);
            }
        }
    }
}
