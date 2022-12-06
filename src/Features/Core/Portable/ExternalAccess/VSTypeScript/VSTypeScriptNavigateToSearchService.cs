// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(INavigateToSearchService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptNavigateToSearchService : INavigateToSearchService
    {
        private readonly IVSTypeScriptNavigateToSearchService? _searchService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptNavigateToSearchService(
            [Import(AllowDefault = true)] IVSTypeScriptNavigateToSearchService? searchService)
        {
            _searchService = searchService;
        }

        public IImmutableSet<string> KindsProvided => _searchService?.KindsProvided ?? ImmutableHashSet<string>.Empty;

        public bool CanFilter => _searchService?.CanFilter ?? false;

        public async IAsyncEnumerable<INavigateToSearchResult> SearchDocumentAsync(
            Document document,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_searchService != null)
            {
                var results = await _searchService.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
                foreach (var result in results)
                    yield return Convert(result);
            }
        }

        public async IAsyncEnumerable<INavigateToSearchResult> SearchProjectAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (_searchService != null)
            {
                var results = await _searchService.SearchProjectAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
                foreach (var result in results)
                    yield return Convert(result);
            }
        }

        public IAsyncEnumerable<INavigateToSearchResult> SearchCachedDocumentsAsync(
            Project project,
            ImmutableArray<Document> priorityDocuments,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            // we don't support searching cached documents.
            return AsyncEnumerable<INavigateToSearchResult>.Empty;
        }

        public IAsyncEnumerable<INavigateToSearchResult> SearchGeneratedDocumentsAsync(
            Project project,
            string searchPattern,
            IImmutableSet<string> kinds,
            Document? activeDocument,
            CancellationToken cancellationToken)
        {
            // we don't support searching generated documents.
            return AsyncEnumerable<INavigateToSearchResult>.Empty;
        }

        private static INavigateToSearchResult Convert(IVSTypeScriptNavigateToSearchResult result)
            => new WrappedNavigateToSearchResult(result);

        private class WrappedNavigateToSearchResult : INavigateToSearchResult
        {
            private readonly IVSTypeScriptNavigateToSearchResult _result;

            public WrappedNavigateToSearchResult(IVSTypeScriptNavigateToSearchResult result)
            {
                _result = result;
            }

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

            public INavigableItem? NavigableItem => _result.NavigableItem == null ? null : new VSTypeScriptNavigableItemWrapper(_result.NavigableItem);
        }
    }
}
