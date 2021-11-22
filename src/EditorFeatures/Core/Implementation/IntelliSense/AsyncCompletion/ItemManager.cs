// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Utilities;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal partial class ItemManager : IAsyncCompletionItemManager
    {
        private readonly RecentItemsManager _recentItemsManager;
        private readonly IGlobalOptionService _globalOptions;
        public const string AggressiveDefaultsMatchingOptionName = "AggressiveDefaultsMatchingOption";

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
            if (session.TextView.Properties.TryGetProperty(CompletionSource.TargetTypeFilterExperimentEnabled, out bool isTargetTypeFilterEnabled) && isTargetTypeFilterEnabled)
            {
                AsyncCompletionLogger.LogSessionHasTargetTypeFilterEnabled();

                // This method is called exactly once, so use the opportunity to set a baseline for telemetry.
                if (data.InitialList.Any(i => i.Filters.Any(f => f.DisplayText == FeaturesResources.Target_type_matches)))
                {
                    AsyncCompletionLogger.LogSessionContainsTargetTypeFilter();
                }
            }

            if (session.TextView.Properties.TryGetProperty(CompletionSource.TypeImportCompletionEnabled, out bool isTypeImportCompletionEnabled) && isTypeImportCompletionEnabled)
            {
                AsyncCompletionLogger.LogSessionWithTypeImportCompletionEnabled();
            }

            // Sort by default comparer of Roslyn CompletionItem
            var sortedItems = data.InitialList.OrderBy(GetOrAddRoslynCompletionItem).ToImmutableArray();
            return Task.FromResult(sortedItems);
        }

        public Task<FilteredCompletionModel?> UpdateCompletionListAsync(
            IAsyncCompletionSession session,
            AsyncCompletionSessionDataSnapshot data,
            CancellationToken cancellationToken)
        {
            var updater = new CompletionListUpdater(session, data, _recentItemsManager, _globalOptions, cancellationToken);
            return Task.FromResult(updater.UpdateCompletionList());
        }

        private static RoslynCompletionItem GetOrAddRoslynCompletionItem(VSCompletionItem vsItem)
        {
            if (!vsItem.Properties.TryGetProperty(CompletionSource.RoslynItem, out RoslynCompletionItem roslynItem))
            {
                roslynItem = RoslynCompletionItem.Create(
                    displayText: vsItem.DisplayText,
                    filterText: vsItem.FilterText,
                    sortText: vsItem.SortText,
                    displayTextSuffix: vsItem.Suffix);

                vsItem.Properties.AddProperty(CompletionSource.RoslynItem, roslynItem);
            }

            return roslynItem;
        }
    }
}
