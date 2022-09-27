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
        private static readonly ObjectPool<List<MatchResult>> s_listOfMatchResultPool = new(factory: () => new());

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
                tsMatchResults.AddRange(matchResults.Where(static r => r.ShouldBeConsideredMatchingFilterText)
                    .Select(static r => new VSTypeScriptCompletionItemMatchResult(r)));

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

        // Override this to bypass the expensive call to public FilterItems method.
        internal virtual void FilterItemsImpl(
            Document document,
            IReadOnlyList<VSTypeScriptCompletionItemMatchResult> tsMatchResults,
            string filterText,
            IList<VSTypeScriptCompletionItemMatchResult> builder)
        {
            using var _1 = ArrayBuilder<CompletionItem>.GetInstance(tsMatchResults.Count, out var itemsBuilder);
            using var _2 = PooledDictionary<CompletionItem, VSTypeScriptCompletionItemMatchResult>.GetInstance(out var map);

            foreach (var result in tsMatchResults)
            {
                itemsBuilder.Add(result.CompletionItem);
                map.Add(result.CompletionItem, result);
            }

#pragma warning disable RS0030 // Do not used banned APIs
            var filteredItems = FilterItems(document, itemsBuilder.ToImmutable(), filterText);
#pragma warning restore RS0030 // Do not used banned APIs

            var helper = CompletionHelper.GetHelper(document);
            builder.AddRange(filteredItems.Select(item => map[item]));
        }

        internal static void FilterItemsImplDefault(
            Document document,
            IReadOnlyList<VSTypeScriptCompletionItemMatchResult> tsMatchResults,
            string filterText,
            IList<VSTypeScriptCompletionItemMatchResult> tsBuilder)
        {
            var matchResults = s_listOfMatchResultPool.Allocate();
            var builder = s_listOfMatchResultPool.Allocate();

            try
            {
                matchResults.AddRange(tsMatchResults.Select(static r => r.MatchResult));
                FilterItemsDefault(CompletionHelper.GetHelper(document), matchResults, filterText, builder);
                tsBuilder.AddRange(builder.Select(static r => new VSTypeScriptCompletionItemMatchResult(r)));
            }
            finally
            {
                matchResults.Clear();
                builder.Clear();
                s_listOfMatchResultPool.Free(matchResults);
                s_listOfMatchResultPool.Free(builder);
            }
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
