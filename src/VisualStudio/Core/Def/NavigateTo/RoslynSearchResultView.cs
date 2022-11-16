// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Search.Data;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal sealed partial class RoslynSearchItemsSourceProvider
    {
        private sealed class RoslynSearchResultView : CodeSearchResultViewBase
        {
            private readonly RoslynSearchItemsSourceProvider _provider;
            private readonly RoslynCodeSearchResult _searchResult;

            public RoslynSearchResultView(
                RoslynSearchItemsSourceProvider provider,
                RoslynCodeSearchResult searchResult,
                HighlightedText title,
                ImageId primaryIcon)
                : base(title, primaryIcon: primaryIcon)
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
