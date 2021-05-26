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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(INavigateToSearchService), InternalLanguageNames.TypeScript), Shared]
    internal class VSTypeScriptNavigateToSearchService : INavigateToSearchService
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

        public async Task<NavigateToSearchLocation> SearchDocumentAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds,
            Func<INavigateToSearchResult, Task> onResultFound,
            bool isFullyLoaded, CancellationToken cancellationToken)
        {
            if (_searchService != null)
            {
                var results = await _searchService.SearchDocumentAsync(document, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
                foreach (var result in results)
                    await onResultFound(Convert(result)).ConfigureAwait(false);
            }

            return NavigateToSearchLocation.Latest;
        }

        public async Task<NavigateToSearchLocation> SearchProjectAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern,
            IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound,
            bool isFullyLoaded, CancellationToken cancellationToken)
        {
            if (_searchService != null)
            {
                var results = await _searchService.SearchProjectAsync(project, priorityDocuments, searchPattern, kinds, cancellationToken).ConfigureAwait(false);
                foreach (var result in results)
                    await onResultFound(Convert(result)).ConfigureAwait(false);
            }

            return NavigateToSearchLocation.Latest;
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

            public INavigableItem? NavigableItem => _result.NavigableItem == null ? null : new WrappedNavigableItem(_result.NavigableItem);
        }

        private class WrappedNavigableItem : INavigableItem
        {
            private readonly IVSTypeScriptNavigableItem _navigableItem;

            public WrappedNavigableItem(IVSTypeScriptNavigableItem navigableItem)
            {
                _navigableItem = navigableItem;
            }

            public Glyph Glyph => _navigableItem.Glyph;

            public ImmutableArray<TaggedText> DisplayTaggedParts => _navigableItem.DisplayTaggedParts;

            public bool DisplayFileLocation => _navigableItem.DisplayFileLocation;

            public bool IsImplicitlyDeclared => _navigableItem.IsImplicitlyDeclared;

            public Document Document => _navigableItem.Document;

            public TextSpan SourceSpan => _navigableItem.SourceSpan;

            public bool IsStale => false;

            public ImmutableArray<INavigableItem> ChildItems
                => _navigableItem.ChildItems.IsDefault
                    ? default
                    : _navigableItem.ChildItems.SelectAsArray(i => (INavigableItem)new WrappedNavigableItem(i));
        }
    }
}
