// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript;

[ExportLanguageService(typeof(INavigateToSearchService), InternalLanguageNames.TypeScript), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VSTypeScriptNavigateToSearchService(
    [Import(AllowDefault = true)] IVSTypeScriptNavigateToSearchService? searchService) : INavigateToSearchService
{
    private readonly IVSTypeScriptNavigateToSearchService? _searchService = searchService;

    public IImmutableSet<string> KindsProvided => _searchService?.KindsProvided ?? ImmutableHashSet<string>.Empty;

    public bool CanFilter => _searchService?.CanFilter ?? false;

    public async Task SearchDocumentAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        CancellationToken cancellationToken)
    {
        if (_searchService != null)
        {
            var results = await _searchService.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
            if (results.Length > 0)
                await onResultsFound(results.SelectAsArray(Convert)).ConfigureAwait(false);
        }
    }

    public async Task SearchProjectsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        bool searchGeneratedCode,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(projects.IsEmpty);
        Contract.ThrowIfTrue(projects.Select(p => p.Language).Distinct().Count() != 1);

        using var _ = PooledHashSet<Project>.GetInstance(out var processedProjects);

        foreach (var group in priorityDocuments.GroupBy(d => d.Project))
            await ProcessProjectAsync(group.Key).ConfigureAwait(false);

        foreach (var project in projects)
            await ProcessProjectAsync(project).ConfigureAwait(false);

        return;

        async Task ProcessProjectAsync(Project project)
        {
            if (processedProjects.Add(project))
            {
                if (_searchService != null)
                {
                    var results = await _searchService.SearchProjectAsync(
                        project, priorityDocuments.WhereAsArray(d => d.Project == project), searchPattern, kinds, cancellationToken).ConfigureAwait(false);

                    if (results.Length > 0)
                        await onResultsFound(results.SelectAsArray(Convert)).ConfigureAwait(false);
                }

                await onProjectCompleted().ConfigureAwait(false);
            }
        }
    }

    private static INavigateToSearchResult Convert(IVSTypeScriptNavigateToSearchResult result)
        => new WrappedNavigateToSearchResult(result);

    private class WrappedNavigateToSearchResult(IVSTypeScriptNavigateToSearchResult result) : INavigateToSearchResult
    {
        private readonly IVSTypeScriptNavigateToSearchResult _result = result;

        public string AdditionalInformation => _result.AdditionalInformation;

        public string Kind => _result.Kind;

        public NavigateToMatchKind MatchKind
            => _result.MatchKind switch
            {
                VSTypeScriptNavigateToMatchKind.Exact => NavigateToMatchKind.Exact,
                VSTypeScriptNavigateToMatchKind.Prefix => NavigateToMatchKind.Prefix,
                VSTypeScriptNavigateToMatchKind.Substring => NavigateToMatchKind.Substring,
                VSTypeScriptNavigateToMatchKind.Regular => NavigateToMatchKind.Regular,
                VSTypeScriptNavigateToMatchKind.None => NavigateToMatchKind.None,
                VSTypeScriptNavigateToMatchKind.CamelCaseExact => NavigateToMatchKind.CamelCaseExact,
                VSTypeScriptNavigateToMatchKind.CamelCasePrefix => NavigateToMatchKind.CamelCasePrefix,
                VSTypeScriptNavigateToMatchKind.CamelCaseNonContiguousPrefix => NavigateToMatchKind.CamelCaseNonContiguousPrefix,
                VSTypeScriptNavigateToMatchKind.CamelCaseSubstring => NavigateToMatchKind.CamelCaseSubstring,
                VSTypeScriptNavigateToMatchKind.CamelCaseNonContiguousSubstring => NavigateToMatchKind.CamelCaseNonContiguousSubstring,
                VSTypeScriptNavigateToMatchKind.Fuzzy => NavigateToMatchKind.Fuzzy,
                _ => throw ExceptionUtilities.UnexpectedValue(_result.MatchKind),
            };

        public bool IsCaseSensitive => _result.IsCaseSensitive;

        public string Name => _result.Name;

        public ImmutableArray<TextSpan> NameMatchSpans => _result.NameMatchSpans;

        public string SecondarySort => _result.SecondarySort;

        public string Summary => _result.Summary;

        public INavigableItem NavigableItem => new VSTypeScriptNavigableItemWrapper(_result.NavigableItem);

        public ImmutableArray<PatternMatch> Matches => NavigateToSearchResultHelpers.GetMatches(this);
    }
}
