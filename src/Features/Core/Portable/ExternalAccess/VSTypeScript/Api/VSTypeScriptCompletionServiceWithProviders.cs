// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal abstract class VSTypeScriptCompletionServiceWithProviders : CompletionService
    {
        internal VSTypeScriptCompletionServiceWithProviders(Workspace workspace)
            : base(workspace.Services.SolutionServices)
        {
        }

        internal sealed override CompletionRules GetRules(CompletionOptions options)
            => GetRulesImpl();

        internal abstract CompletionRules GetRulesImpl();

        private static readonly ObjectPool<List<VSTypeScriptCompletionItemMatchResult>> s_listOfTSMatchResultPool = new(factory: () => new());

        internal sealed override void FilterItems(
           Document document,
           IReadOnlyList<MatchResult> matchResults,
           string filterText,
           IList<MatchResult> builder)
        {
            var tsMatchResults = s_listOfTSMatchResultPool.Allocate();
            var tsFilteredMatchResults = s_listOfTSMatchResultPool.Allocate();

            try
            {
                tsMatchResults.AddRange(matchResults.Where(static r => r.ShouldBeConsideredMatchingFilterText).Select(static r => new VSTypeScriptCompletionItemMatchResult(r)));
                FilterItemsImpl(document, tsMatchResults, filterText, tsFilteredMatchResults);

                builder.AddRange(tsMatchResults.Select(static r => r.MatchResult));
            }
            finally
            {
                // Don't call ClearAndFree, which resets the capacity to a default value.
                tsMatchResults.Clear();
                tsFilteredMatchResults.Clear();
                s_listOfTSMatchResultPool.Free(tsMatchResults);
                s_listOfTSMatchResultPool.Free(tsFilteredMatchResults);
            }
        }

        // TODO: make it abstract once TS implemented this.
        internal virtual void FilterItemsImpl(
            Document document,
            IReadOnlyList<VSTypeScriptCompletionItemMatchResult> matchResults,
            string filterText,
            IList<VSTypeScriptCompletionItemMatchResult> builder)
        {
            using var _1 = ArrayBuilder<CompletionItem>.GetInstance(matchResults.Count, out var itemBuilder);
            using var _2 = PooledDictionary<CompletionItem, VSTypeScriptCompletionItemMatchResult>.GetInstance(out var map);

            foreach (var result in matchResults)
            {
                itemBuilder.Add(result.CompletionItem);
                map.Add(result.CompletionItem, result);
            }

#pragma warning disable RS0030 // Do not used banned APIs
            var filteredItems = FilterItems(document, itemBuilder.ToImmutable(), filterText);
#pragma warning restore RS0030 // Do not used banned APIs

            builder.AddRange(filteredItems.Select(item => map[item]));
        }

        internal readonly struct VSTypeScriptCompletionItemMatchResult
        {
            public readonly MatchResult MatchResult;

            public VSTypeScriptCompletionItemMatchResult(MatchResult matchResult)
            {
                MatchResult = matchResult;
            }

            public CompletionItem CompletionItem => MatchResult.CompletionItem;
        }
    }
}
