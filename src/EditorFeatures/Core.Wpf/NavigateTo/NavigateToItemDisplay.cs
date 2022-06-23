// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal sealed class NavigateToItemDisplay : INavigateToItemDisplay3
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly INavigateToSearchResult _searchResult;
        private ReadOnlyCollection<DescriptionItem> _descriptionItems;

        public NavigateToItemDisplay(
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor threadOperationExecutor,
            IAsynchronousOperationListener asyncListener,
            INavigateToSearchResult searchResult)
        {
            _threadingContext = threadingContext;
            _threadOperationExecutor = threadOperationExecutor;
            _asyncListener = asyncListener;
            _searchResult = searchResult;
        }

        public string AdditionalInformation => _searchResult.AdditionalInformation;

        public string Description => null;

        public ReadOnlyCollection<DescriptionItem> DescriptionItems
        {
            get
            {
                _descriptionItems ??= CreateDescriptionItems();
                return _descriptionItems;
            }
        }

        private ReadOnlyCollection<DescriptionItem> CreateDescriptionItems()
        {
            var document = _searchResult.NavigableItem.Document;
            if (document == null)
            {
                return new List<DescriptionItem>().AsReadOnly();
            }

            var sourceText = document.GetTextSynchronously(CancellationToken.None);
            var span = NavigateToUtilities.GetBoundedSpan(_searchResult.NavigableItem, sourceText);

            var items = new List<DescriptionItem>
                    {
                        new DescriptionItem(
                            new ReadOnlyCollection<DescriptionRun>(
                                new[] { new DescriptionRun("Project:", bold: true) }),
                            new ReadOnlyCollection<DescriptionRun>(
                                new[] { new DescriptionRun(document.Project.Name) })),
                        new DescriptionItem(
                            new ReadOnlyCollection<DescriptionRun>(
                                new[] { new DescriptionRun("File:", bold: true) }),
                            new ReadOnlyCollection<DescriptionRun>(
                                new[] { new DescriptionRun(document.FilePath ?? document.Name) })),
                        new DescriptionItem(
                            new ReadOnlyCollection<DescriptionRun>(
                                new[] { new DescriptionRun("Line:", bold: true) }),
                            new ReadOnlyCollection<DescriptionRun>(
                                new[] { new DescriptionRun((sourceText.Lines.IndexOf(span.Start) + 1).ToString()) }))
                    };

            var summary = _searchResult.Summary;
            if (!string.IsNullOrWhiteSpace(summary))
            {
                items.Add(
                    new DescriptionItem(
                        new ReadOnlyCollection<DescriptionRun>(
                            new[] { new DescriptionRun("Summary:", bold: true) }),
                        new ReadOnlyCollection<DescriptionRun>(
                            new[] { new DescriptionRun(summary) })));
            }

            return items.AsReadOnly();
        }

        public Icon Glyph => null;

        public string Name => _searchResult.NavigableItem.DisplayTaggedParts.JoinText();

        public void NavigateTo()
        {
            var token = _asyncListener.BeginAsyncOperation(nameof(NavigateTo));
            NavigateToAsync().ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
        }

        private async Task NavigateToAsync()
        {
            var document = _searchResult.NavigableItem.Document;
            if (document == null)
                return;

            var workspace = document.Project.Solution.Workspace;
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

            // Document tabs opened by NavigateTo are carefully created as preview or regular tabs
            // by them; trying to specifically open them in a particular kind of tab here has no
            // effect.
            //
            // In the case of a stale item, don't require that the span be in bounds of the document
            // as it exists right now.
            using var context = _threadOperationExecutor.BeginExecute(
                EditorFeaturesResources.Navigating_to_definition, EditorFeaturesResources.Navigating_to_definition, allowCancellation: true, showProgress: false);
            await navigationService.TryNavigateToSpanAsync(
                _threadingContext,
                workspace,
                document.Id,
                _searchResult.NavigableItem.SourceSpan,
                NavigationOptions.Default,
                allowInvalidSpan: _searchResult.NavigableItem.IsStale,
                context.UserCancellationToken).ConfigureAwait(false);
        }

        public int GetProvisionalViewingStatus()
        {
            var document = _searchResult.NavigableItem.Document;
            if (document == null)
            {
                return 0;
            }

            var workspace = document.Project.Solution.Workspace;
            var previewService = workspace.Services.GetService<INavigateToPreviewService>();

            return previewService.GetProvisionalViewingStatus(document);
        }

        public void PreviewItem()
        {
            var document = _searchResult.NavigableItem.Document;
            if (document == null)
            {
                return;
            }

            var workspace = document.Project.Solution.Workspace;
            var previewService = workspace.Services.GetService<INavigateToPreviewService>();

            previewService.PreviewItem(this);
        }

        public ImageMoniker GlyphMoniker => _searchResult.NavigableItem.Glyph.GetImageMoniker();

        public IReadOnlyList<Span> GetNameMatchRuns(string searchValue)
            => _searchResult.NameMatchSpans.NullToEmpty().SelectAsArray(ts => ts.ToSpan());

        public IReadOnlyList<Span> GetAdditionalInformationMatchRuns(string searchValue)
            => SpecializedCollections.EmptyReadOnlyList<Span>();
    }
}
