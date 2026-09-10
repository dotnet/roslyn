// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo;

/// <summary>
/// Optional expanded API for Navigate-To.  An <see cref="IFSharpNavigateToSearchService"/> that also implements this
/// takes part in the search that runs while the solution is still loading; one that does not is skipped until the
/// solution is fully loaded, and so contributes nothing to that search.
/// </summary>
internal interface IFSharpAdvancedNavigateToSearchService : IFSharpNavigateToSearchService
{
    /// <summary>
    /// Searches the documents inside <paramref name="projects"/> for symbols that match <paramref
    /// name="searchPattern"/>. Results should be reported from a previously computed cache (even if that cache is out
    /// of date) to produce results as quickly as possible.  This is called for every project of the solution while it
    /// loads, so it must not wait on anything that only becomes available once the project has loaded.
    /// </summary>
    /// <remarks>
    /// All the projects passed are for F#.  Similarly, all the <paramref name="priorityDocuments"/> belong to these
    /// projects.  <paramref name="onProjectCompleted"/> must be called exactly once per project, whether or not
    /// anything was found in it.
    /// </remarks>
    Task SearchCachedDocumentsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<FSharpNavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken);
}
