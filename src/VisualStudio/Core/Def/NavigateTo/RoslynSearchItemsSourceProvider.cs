// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Search.Data;
using Microsoft.VisualStudio.Search.UI.PreviewPanel.Models;
using Microsoft.VisualStudio.Text.PatternMatching;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    [Export(typeof(ISearchItemsSourceProvider))]
    [Name(nameof(RoslynSearchItemsSourceProvider))]
    [ProducesResultType(CodeSearchResultType.Class)]
    [ProducesResultType(CodeSearchResultType.Constant)]
    [ProducesResultType(CodeSearchResultType.Delegate)]
    [ProducesResultType(CodeSearchResultType.Enum)]
    [ProducesResultType(CodeSearchResultType.EnumItem)]
    [ProducesResultType(CodeSearchResultType.Event)]
    [ProducesResultType(CodeSearchResultType.Field)]
    [ProducesResultType(CodeSearchResultType.Interface)]
    [ProducesResultType(CodeSearchResultType.Method)]
    [ProducesResultType(CodeSearchResultType.Module)]
    [ProducesResultType(CodeSearchResultType.OtherSymbol)]
    [ProducesResultType(CodeSearchResultType.Property)]
    [ProducesResultType(CodeSearchResultType.Structure)]
    internal sealed class RoslynSearchItemsSourceProvider : ISearchItemsSourceProvider
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IThreadingContext _threadingContext;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;
        private readonly IAsynchronousOperationListener _asyncListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynSearchItemsSourceProvider(
            VisualStudioWorkspace workspace,
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor threadOperationExecutor,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _workspace = workspace;
            _threadingContext = threadingContext;
            _threadOperationExecutor = threadOperationExecutor;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.NavigateTo);
        }

        public ISearchItemsSource CreateItemsSource()
            => new RoslynSearchItemsSource(this);

        private sealed class RoslynSearchItemsSource : CodeSearchItemsSourceBase
        {
            private static readonly IImmutableSet<string> s_typeKinds = ImmutableHashSet<string>.Empty
                .Add(NavigateToItemKind.Class)
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
                try
                {
                    var searchValue = searchQuery.QueryString;
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

                    var searchCurrentDocument = false; // (callback.Options as INavigateToOptions2)?.SearchCurrentDocument ?? false;

                    var viewFactory = new RoslynViewFactory(_provider);
                    var roslynCallback = new RoslynNavigateToSearchCallback(viewFactory, searchCallback);

                    var searcher = NavigateToSearcher.Create(
                        _provider._workspace.CurrentSolution,
                        _provider._asyncListener,
                        roslynCallback,
                        searchValue,
                        kinds,
                        _provider._threadingContext.DisposalToken);

                    using var token = _provider._asyncListener.BeginAsyncOperation(nameof(PerformSearchAsync));
                    await searcher.SearchAsync(searchCurrentDocument, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
                {
                }
            }

            private sealed class RoslynViewFactory : ISearchResultViewFactory
            {
                private readonly RoslynSearchItemsSourceProvider _provider;

                public RoslynViewFactory(RoslynSearchItemsSourceProvider provider)
                {
                    _provider = provider;
                }

                public SearchResultViewBase CreateSearchResultView(SearchResult result)
                {
                    if (result is not RoslynCodeSearchResult roslynResult)
                        return null!;

                    var searchResult = roslynResult.SearchResult;
                    var patternMatch = roslynResult.PatternMatch;

                    return new RoslynSearchResultView(
                        _provider,
                        roslynResult,
                        new HighlightedText(searchResult.NavigableItem.DisplayTaggedParts.JoinText(), searchResult.NameMatchSpans.Select(s => s.ToSpan()).ToArray()),
                        new HighlightedText(searchResult.AdditionalInformation, Array.Empty<VisualStudio.Text.Span>()),
                        primaryIcon: searchResult.NavigableItem.Glyph.GetImageId());
                }

                public async Task<IReadOnlyList<SearchResultPreviewPanelBase>> GetPreviewPanelsAsync(SearchResult result, SearchResultViewBase searchResultView)
                {
                    if (result is not RoslynCodeSearchResult roslynResult)
                        return null!;

                    return new List<SearchResultPreviewPanelBase>
                    {
                        new FileContentPreviewView(
                            searchResultView.Title.Text,
                            result,
                            searchResultView.PrimaryIcon,
                            _provider._threadingContext.JoinableTaskFactory)
                    };
                }
            }
        }

        /// <summary>
        /// Represent a code editor in the Preview panel.
        /// </summary>
        private sealed class RoslynFileContentPreviewView : SearchResultPreviewPanelBase
        {
            public override UIBaseModel UserInterface { get; }

            public FileContentPreviewView(
                RoslynSearchItemsSourceProvider provider,
                SearchResult searchResult,
                string title, ImageId icon)
                : base(title, icon)
            {
                UserInterface = new CodeEditorModel(
                    nameof(RoslynFileContentPreviewView),
                    new VisualStudio.Threading.AsyncLazy<TextDocumentLocation>(() =>
                    {
                        return Task.FromResult(
                            new TextDocumentLocation(
                                new Uri(file),
                                Guid.Empty /* project ID */,
                                Span.FromBounds(symbolLocation.Start, symbolLocation.End) /* There's another constructor for Line and Column parameters, if needed */));
                    },
                    provider._threadingContext.JoinableTaskFactory),
                    isEditable: false);
            }
        }

        private sealed class RoslynSearchResultView : CodeSearchResultViewBase
        {
            private readonly RoslynSearchItemsSourceProvider _provider;
            private readonly RoslynCodeSearchResult _searchResult;

            public RoslynSearchResultView(
                RoslynSearchItemsSourceProvider provider,
                RoslynCodeSearchResult searchResult,
                HighlightedText title,
                HighlightedText? description = null,
                string? hintText = null,
                SearchResultViewFlags flags = SearchResultViewFlags.ExcludeFromMostRecentlyUsed,
                ImageId primaryIcon = default(ImageId),
                ImageId secondaryIcon = default(ImageId),
                string? groupName = null)
                : base(title, description, hintText, flags, primaryIcon, secondaryIcon, groupName)
            {
                _provider = provider;
                _searchResult = searchResult;

                var filePath = _searchResult.SearchResult.NavigableItem.Document.FilePath;
                if (filePath != null)
                    this.FileLocation = new HighlightedText(filePath, Array.Empty<VisualStudio.Text.Span>());
            }

            public override void Invoke(CancellationToken cancellationToken)
            {
                var token = _provider._asyncListener.BeginAsyncOperation(nameof(NavigateTo));
                NavigateToAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
            }

            private async Task NavigateToAsync()
            {
                var document = _searchResult.SearchResult.NavigableItem.Document;
                if (document == null)
                    return;

                var workspace = document.Project.Solution.Workspace;
                var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

                // Document tabs opened by NavigateTo are carefully created as preview or regular tabs
                // by them; trying to specifically open them in a particular kind of tab here has no
                // effect.
                //
                // In the case of a stale item, don't require that the span be in bounds of the document
                // as it exists right now.
                using var context = _provider._threadOperationExecutor.BeginExecute(
                    EditorFeaturesResources.Navigating_to_definition, EditorFeaturesResources.Navigating_to_definition, allowCancellation: true, showProgress: false);
                await navigationService.TryNavigateToSpanAsync(
                    _provider._threadingContext,
                    workspace,
                    document.Id,
                    _searchResult.SearchResult.NavigableItem.SourceSpan,
                    NavigationOptions.Default,
                    allowInvalidSpan: _searchResult.SearchResult.NavigableItem.IsStale,
                    context.UserCancellationToken).ConfigureAwait(false);
            }
        }

        private sealed class RoslynNavigateToSearchCallback : INavigateToSearchCallback
        {
            private readonly ISearchResultViewFactory _viewFactory;
            private readonly ISearchCallback _searchCallback;

            public RoslynNavigateToSearchCallback(
                ISearchResultViewFactory viewFactory,
                ISearchCallback searchCallback)
            {
                _viewFactory = viewFactory;
                _searchCallback = searchCallback;
            }

            public void Done(bool isFullyLoaded)
            {
                if (!isFullyLoaded)
                    ReportIncomplete();

                _searchCallback.ReportProgress(1, 1);
            }

            public void ReportProgress(int current, int maximum)
            {
                _searchCallback.ReportProgress(current, maximum);
            }

            public void ReportIncomplete()
            {
                // "The results may be inaccurate because the search information is still being updated."
                _searchCallback.ReportIncomplete(IncompleteReason.Parsing);
            }

            public Task AddItemAsync(Project project, INavigateToSearchResult result, CancellationToken cancellationToken)
            {
                var matchedSpans = result.NameMatchSpans.SelectAsArray(t => t.ToSpan());

                var patternMatch = new PatternMatch(
                    GetPatternMatchKind(result.MatchKind),
                    punctuationStripped: false,
                    result.IsCaseSensitive,
                    matchedSpans);

                _searchCallback.AddItem(new RoslynCodeSearchResult(
                    result,
                    patternMatch,
                    _viewFactory,
                    result.Kind,
                    result.Name,
                    result.SecondarySort,
                    new[] { patternMatch },
                    result.NavigableItem.Document?.FilePath,
                    tieBreakingSortText: null,
                    perProviderItemPriority: (int)result.MatchKind,
                    flags: SearchResultFlags.Default,
                    project.Language));

                return Task.CompletedTask;
            }

            private static PatternMatchKind GetPatternMatchKind(NavigateToMatchKind matchKind)
                => matchKind switch
                {
                    NavigateToMatchKind.Exact => PatternMatchKind.Exact,
                    NavigateToMatchKind.Prefix => PatternMatchKind.Prefix,
                    NavigateToMatchKind.Substring => PatternMatchKind.Substring,
                    NavigateToMatchKind.Regular => PatternMatchKind.Fuzzy,
                    NavigateToMatchKind.None => PatternMatchKind.Fuzzy,
                    NavigateToMatchKind.CamelCaseExact => PatternMatchKind.CamelCaseExact,
                    NavigateToMatchKind.CamelCasePrefix => PatternMatchKind.CamelCasePrefix,
                    NavigateToMatchKind.CamelCaseNonContiguousPrefix => PatternMatchKind.CamelCaseNonContiguousPrefix,
                    NavigateToMatchKind.CamelCaseSubstring => PatternMatchKind.CamelCaseSubstring,
                    NavigateToMatchKind.CamelCaseNonContiguousSubstring => PatternMatchKind.CamelCaseNonContiguousSubstring,
                    NavigateToMatchKind.Fuzzy => PatternMatchKind.Fuzzy,
                    _ => throw ExceptionUtilities.UnexpectedValue(matchKind),
                };
        }

        private sealed class RoslynCodeSearchResult : CodeSearchResult
        {
            public readonly INavigateToSearchResult SearchResult;
            public readonly PatternMatch PatternMatch;

            public RoslynCodeSearchResult(
                INavigateToSearchResult result,
                PatternMatch patternMatch,
                ISearchResultViewFactory viewFactory,
                string resultType,
                string primarySortText,
                string? secondarySortText,
                IReadOnlyCollection<PatternMatch>? patternMatches,
                string? location,
                string? tieBreakingSortText,
                float perProviderItemPriority,
                SearchResultFlags flags,
                string? language) : base(viewFactory, resultType, primarySortText, secondarySortText, patternMatches, location, tieBreakingSortText, perProviderItemPriority, flags, language)
            {
                SearchResult = result;
                PatternMatch = patternMatch;
            }
        }
    }
}
