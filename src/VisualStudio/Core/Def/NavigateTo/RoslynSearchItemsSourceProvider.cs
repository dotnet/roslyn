// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    /// <summary>
    /// Roslyn implementation of the <see cref="ISearchItemsSourceProvider"/>.  This is the entry-point from VS to
    /// support the 'all in one search provider' UI (which supercedes the previous 'go to' UI).
    /// </summary>
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
    internal sealed partial class RoslynSearchItemsSourceProvider : ISearchItemsSourceProvider
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IThreadingContext _threadingContext;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly RoslynSearchResultViewFactory _viewFactory;

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

            _viewFactory = new RoslynSearchResultViewFactory(this);
        }

        public ISearchItemsSource CreateItemsSource()
            => new RoslynSearchItemsSource(this);

        private sealed class RoslynCodeSearchResult : CodeSearchResult
        {
            public readonly INavigateToSearchResult SearchResult;

            public RoslynCodeSearchResult(
                RoslynSearchItemsSourceProvider provider,
                INavigateToSearchResult searchResult,
                string resultType,
                string primarySortText,
                string secondarySortText,
                IReadOnlyCollection<PatternMatch> patternMatches,
                string? location,
                float perProviderItemPriority,
                string language)
                : base(
                      provider._viewFactory,
                      resultType,
                      primarySortText,
                      secondarySortText,
                      patternMatches,
                      location,
                      perProviderItemPriority: perProviderItemPriority,
                      language: language)
            {
                SearchResult = searchResult;
            }
        }

        private sealed class RoslynSearchResultViewFactory : ISearchResultViewFactory
        {
            private readonly RoslynSearchItemsSourceProvider _provider;

            public RoslynSearchResultViewFactory(RoslynSearchItemsSourceProvider provider)
            {
                _provider = provider;
            }

            public SearchResultViewBase CreateSearchResultView(SearchResult result)
            {
                if (result is not RoslynCodeSearchResult roslynResult)
                    return null!;

                var searchResult = roslynResult.SearchResult;

                return new RoslynSearchResultView(
                    _provider,
                    roslynResult,
                    new HighlightedText(searchResult.NavigableItem.DisplayTaggedParts.JoinText(), searchResult.NameMatchSpans.Select(m => m.ToSpan()).ToArray()),
                    new HighlightedText(searchResult.AdditionalInformation, Array.Empty<VisualStudio.Text.Span>()),
                    primaryIcon: searchResult.NavigableItem.Glyph.GetImageId());
            }

            public Task<IReadOnlyList<SearchResultPreviewPanelBase>> GetPreviewPanelsAsync(SearchResult result, SearchResultViewBase searchResultView)
                => Task.FromResult(GetPreviewPanels(result, searchResultView) ?? Array.Empty<SearchResultPreviewPanelBase>());

            private IReadOnlyList<SearchResultPreviewPanelBase>? GetPreviewPanels(SearchResult result, SearchResultViewBase searchResultView)
            {
                if (result is not RoslynCodeSearchResult roslynResult)
                    return null;

                var document = roslynResult.SearchResult.NavigableItem.Document;
                var filePath = document.FilePath;
                if (filePath is null)
                    return null;

                if (!Uri.TryCreate(filePath, UriKind.RelativeOrAbsolute, out var uri))
                    return null;

                var projectGuid = _provider._workspace.GetProjectGuid(document.Project.Id);
                if (projectGuid == Guid.Empty)
                    return null;

                return new List<SearchResultPreviewPanelBase>
                {
                    new RoslynSearchResultPreviewPanel(
                        _provider,
                        roslynResult,
                        uri,
                        projectGuid,
                        searchResultView.Title.Text,
                        searchResultView.PrimaryIcon)
                };
            }
        }

        /// <summary>
        /// Represent a code editor in the Preview panel.
        /// </summary>
        private sealed class RoslynSearchResultPreviewPanel : SearchResultPreviewPanelBase
        {
            public override UIBaseModel UserInterface { get; }

            public RoslynSearchResultPreviewPanel(
                RoslynSearchItemsSourceProvider provider,
                RoslynCodeSearchResult searchResult,
                Uri uri,
                Guid projectGuid,
                string title,
                ImageId icon)
                : base(title, icon)
            {
                UserInterface = new CodeEditorModel(
                    nameof(RoslynSearchResultPreviewPanel),
                    new VisualStudio.Threading.AsyncLazy<TextDocumentLocation>(() =>
                        Task.FromResult(new TextDocumentLocation(
                            uri,
                            projectGuid,
                            searchResult.SearchResult.NavigableItem.SourceSpan.ToSpan())),
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
    }
}
