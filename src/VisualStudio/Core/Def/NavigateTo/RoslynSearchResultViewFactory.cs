// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Search.Data;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal sealed partial class RoslynSearchItemsSourceProvider
{
    /// <summary>
    /// Implementation of the <see cref="ISearchResultViewFactory"/>.  Responsible for actually producing both the
    /// item presented in the search results list, and the async preview for that item.
    /// </summary>
    private sealed class RoslynSearchResultViewFactory(RoslynSearchItemsSourceProvider provider) : ISearchResultViewFactory
    {
        private readonly RoslynSearchItemsSourceProvider _provider = provider;

        public SearchResultViewBase CreateSearchResultView(SearchResult result)
        {
            if (result is not RoslynCodeSearchResult roslynResult)
                return null!;

            var searchResult = roslynResult.SearchResult;

            return new RoslynSearchResultView(
                _provider,
                searchResult,
                new HighlightedText(
                    searchResult.NavigableItem.DisplayTaggedParts.JoinText(),
                    [.. searchResult.NameMatchSpans.NullToEmpty().Select(m => m.ToSpan())]),
                new HighlightedText(
                    searchResult.AdditionalInformation,
                    []),
                primaryIcon: searchResult.NavigableItem.Glyph.GetImageId());
        }

        public Task<IReadOnlyList<SearchResultPreviewPanelBase>> GetPreviewPanelsAsync(SearchResult result, SearchResultViewBase searchResultView)
            => Task.FromResult(GetPreviewPanels(result, searchResultView) ?? []);

        private IReadOnlyList<SearchResultPreviewPanelBase>? GetPreviewPanels(SearchResult result, SearchResultViewBase searchResultView)
        {
            if (result is not RoslynCodeSearchResult roslynResult)
                return null;

            // Try to map from the document to navigate to, to the project-guid and URI for that document.  If we
            // fail, don't show any preview for this item.

            var document = roslynResult.SearchResult.NavigableItem.Document;

            // RoslynNavigateToSearchCallback will have placed the file-path for this document in the .Location property
            // of this search result.  This will either be the true location of a real file in the workspace.  Or a
            // temporary dummy file placed on disk for source-generated documents.  This dummy file will have been made
            // in coordination with the SourceGeneratedFileManager.  That way when the editor tries to open that file,
            // SourceGeneratedFileManager will intercept, fetch the actual contents, and pressent those in the text
            // buffer that the user sees.
            var filePath = roslynResult.Location;
            if (filePath is null)
                return null;

            Uri? absoluteUri;
            try
            {
                absoluteUri = ProtocolConversions.CreateAbsoluteUri(filePath);
            }
            catch (UriFormatException)
            {
                // Unable to create an absolute URI for this path
                return null;
            }

            var projectGuid = _provider._workspace.GetProjectGuid(document.Project.Id);
            if (projectGuid == Guid.Empty)
                return null;

            return [new RoslynSearchResultPreviewPanel(
                _provider,
                // Editor APIs require a parseable System.Uri instance
                absoluteUri,
                projectGuid,
                roslynResult.SearchResult.NavigableItem.SourceSpan.ToSpan(),
                searchResultView.Title.Text,
                searchResultView.PrimaryIcon)];
        }
    }
}
