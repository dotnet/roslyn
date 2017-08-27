// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal abstract class AbstractNavigateToItemDisplay : INavigateToItemDisplay2
    {
        protected readonly INavigateToSearchResult SearchResult;
        private ReadOnlyCollection<DescriptionItem> _descriptionItems;

        protected AbstractNavigateToItemDisplay(INavigateToSearchResult searchResult)
        {
            SearchResult = searchResult;
        }

        public string AdditionalInformation => SearchResult.AdditionalInformation;

        public string Description => null;

        public ReadOnlyCollection<DescriptionItem> DescriptionItems
        {
            get
            {
                if (_descriptionItems == null)
                {
                    _descriptionItems = CreateDescriptionItems();
                }

                return _descriptionItems;
            }
        }

        private ReadOnlyCollection<DescriptionItem> CreateDescriptionItems()
        {
            var document = SearchResult.NavigableItem.Document;
            if (document == null)
            {
                return new List<DescriptionItem>().AsReadOnly();
            }

            var sourceText = document.GetTextAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

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
                                new[] { new DescriptionRun((sourceText.Lines.IndexOf(SearchResult.NavigableItem.SourceSpan.Start) + 1).ToString()) }))
                    };

            var summary = SearchResult.Summary;
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

        public abstract Icon Glyph { get; }

        public string Name => SearchResult.NavigableItem.DisplayTaggedParts.JoinText();

        public void NavigateTo()
        {
            var document = SearchResult.NavigableItem.Document;
            if (document == null)
            {
                return;
            }

            var workspace = document.Project.Solution.Workspace;
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

            // Document tabs opened by NavigateTo are carefully created as preview or regular
            // tabs by them; trying to specifically open them in a particular kind of tab here
            // has no effect.
            navigationService.TryNavigateToSpan(workspace, document.Id, SearchResult.NavigableItem.SourceSpan);
        }

        public int GetProvisionalViewingStatus()
        {
            var document = SearchResult.NavigableItem.Document;
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
            var document = SearchResult.NavigableItem.Document;
            if (document == null)
            {
                return;
            }

            var workspace = document.Project.Solution.Workspace;
            var previewService = workspace.Services.GetService<INavigateToPreviewService>();

            previewService.PreviewItem(this);
        }

        public IReadOnlyList<Span> GetNameMatchRuns(string searchValue)
            => SearchResult.NameMatchSpans.NullToEmpty().SelectAsArray(ts => ts.ToSpan());
    }
}
