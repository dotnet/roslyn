// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Threading;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal partial class NavigateToItemProvider
    {
        private class NavigateToItemDisplay : INavigateToItemDisplay2
        {
            private readonly INavigateToSearchResult _searchResult;
            private readonly NavigateToIconFactory _iconFactory;

            private Icon _glyph;
            private ReadOnlyCollection<DescriptionItem> _descriptionItems;

            public NavigateToItemDisplay(INavigateToSearchResult searchResult, NavigateToIconFactory iconFactory)
            {
                _searchResult = searchResult;
                _iconFactory = iconFactory;
            }

            public string AdditionalInformation
            {
                get
                {
                    return _searchResult.AdditionalInformation;
                }
            }

            public string Description
            {
                get
                {
                    return null;
                }
            }

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
                var document = _searchResult.NavigableItem.Document;
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
                                new[] { new DescriptionRun((sourceText.Lines.IndexOf(_searchResult.NavigableItem.SourceSpan.Start) + 1).ToString()) }))
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

            public Icon Glyph
            {
                get
                {
                    if (_glyph == null)
                    {
                        _glyph = _iconFactory.GetIcon(_searchResult.NavigableItem.Glyph);
                    }

                    return _glyph;
                }
            }

            public string Name
            {
                get
                {
                    return _searchResult.NavigableItem.DisplayString;
                }
            }

            public void NavigateTo()
            {
                var document = _searchResult.NavigableItem.Document;
                var workspace = document.Project.Solution.Workspace;
                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

                // Document tabs opened by NavigateTo are carefully created as preview or regular
                // tabs by them; trying to specifically open them in a particular kind of tab here
                // has no effect.
                navigationService.TryNavigateToSpan(workspace, document.Id, _searchResult.NavigableItem.SourceSpan);
            }

            public int GetProvisionalViewingStatus()
            {
                var document = _searchResult.NavigableItem.Document;
                var workspace = document.Project.Solution.Workspace;
                var previewService = workspace.Services.GetService<INavigateToPreviewService>();

                return previewService.GetProvisionalViewingStatus(document);
            }

            public void PreviewItem()
            {
                var document = _searchResult.NavigableItem.Document;
                var workspace = document.Project.Solution.Workspace;
                var previewService = workspace.Services.GetService<INavigateToPreviewService>();

                previewService.PreviewItem(this);
            }
        }
    }
}
