﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.Search.Data;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal sealed partial class RoslynSearchItemsSourceProvider
{
    /// <summary>
    /// Roslyn implementation of <see cref="ISearchItemsSource"/>.  This is the type actually responsible for
    /// calling into the underlying <see cref="NavigateToSearcher"/> and marshalling the results over to the ui.
    /// </summary>
    private sealed class RoslynSearchItemsSource : CodeSearchItemsSourceBase
    {
        private static readonly IImmutableSet<string> s_typeKinds = ImmutableHashSet<string>.Empty
            .Add(NavigateToItemKind.Class)
            .Add(NavigateToItemKind.Enum)
            .Add(NavigateToItemKind.Structure)
            .Add(NavigateToItemKind.Interface)
            .Add(NavigateToItemKind.Delegate)
            .Add(NavigateToItemKind.Module);
        private static readonly IImmutableSet<string> s_memberKinds = ImmutableHashSet<string>.Empty
            .Add(NavigateToItemKind.Constant)
            .Add(NavigateToItemKind.EnumItem)
            .Add(NavigateToItemKind.Field)
            .Add(NavigateToItemKind.Method)
            .Add(NavigateToItemKind.Property)
            .Add(NavigateToItemKind.Event);
        private static readonly IImmutableSet<string> s_allKinds = s_typeKinds.Union(s_memberKinds);

        private readonly RoslynSearchItemsSourceProvider _provider;

        public RoslynSearchItemsSource(RoslynSearchItemsSourceProvider provider)
        {
            _provider = provider;
        }

        public override async Task PerformSearchAsync(ISearchQuery searchQuery, ISearchCallback searchCallback, CancellationToken cancellationToken)
        {
            using var token = _provider._asyncListener.BeginAsyncOperation(nameof(PerformSearchAsync));

            try
            {
                var searchValue = searchQuery.QueryString.Trim();
                if (string.IsNullOrWhiteSpace(searchValue))
                    return;

                var includeTypeResults = searchQuery.FiltersStates.Any(f => f is { Key: "Types", Value: "True" });
                var includeMembersResults = searchQuery.FiltersStates.Any(f => f is { Key: "Members", Value: "True" });

                var kinds = (includeTypeResults, includeMembersResults) switch
                {
                    (true, false) => s_typeKinds,
                    (false, true) => s_memberKinds,
                    _ => s_allKinds,
                };

                // TODO(cyrusn): New aiosp doesn't seem to support only searching current document.
                var searchCurrentDocument = false;

                // Create a nav-to callback that will take results and translate them to aiosp results for the
                // callback passed to us.

                var searcher = NavigateToSearcher.Create(
                    _provider._workspace.CurrentSolution,
                    _provider._asyncListener,
                    new RoslynNavigateToSearchCallback(_provider, searchCallback),
                    searchValue,
                    kinds,
                    _provider._threadingContext.DisposalToken);

                await searcher.SearchAsync(searchCurrentDocument, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
            {
            }
        }
    }
}
