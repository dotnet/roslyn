// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities.RemoteHost;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Moq;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo
{
    public abstract class AbstractNavigateToTests
    {
        protected static ExportProvider s_exportProvider =
            MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.CreateAssemblyCatalogWithCSharpAndVisualBasic());

        protected INavigateToItemProvider _provider;
        protected NavigateToTestAggregator _aggregator;

        protected abstract TestWorkspace CreateWorkspace(string content, ExportProvider exportProvider);
        protected abstract string Language { get; }

        protected async Task TestAsync(string content, Func<TestWorkspace, Task> body)
        {
            await TestAsync(content, body, outOfProcess: true);
            await TestAsync(content, body, outOfProcess: false);
        }

        private async Task TestAsync(string content, Func<TestWorkspace, Task> body, bool outOfProcess)
        {
            using (var workspace = SetupWorkspace(content))
            {
                workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, outOfProcess)
                                                     .WithChangedOption(RemoteFeatureOptions.OutOfProcessAllowed, outOfProcess)
                                                     .WithChangedOption(RemoteFeatureOptions.NavigateToEnabled, outOfProcess);

                await body(workspace);
            }
        }

        protected TestWorkspace SetupWorkspace(XElement workspaceElement)
        {
            var workspace = TestWorkspace.Create(workspaceElement, exportProvider: s_exportProvider);
            InitializeWorkspace(workspace);
            return workspace;
        }

        protected TestWorkspace SetupWorkspace(string content)
        {
            var workspace = CreateWorkspace(content, s_exportProvider);
            InitializeWorkspace(workspace);
            return workspace;
        }

        private void InitializeWorkspace(TestWorkspace workspace)
        {
            var aggregateListener = AggregateAsynchronousOperationListener.CreateEmptyListener();

            _provider = new NavigateToItemProvider(workspace, aggregateListener);
            _aggregator = new NavigateToTestAggregator(_provider);
        }

#pragma warning disable CS0618 // MatchKind is obsolete
        protected void VerifyNavigateToResultItems(
            List<NavigateToItem> expecteditems, IEnumerable<NavigateToItem> items)
        {
            expecteditems = expecteditems.OrderBy(i => i.Name).ToList();
            items = items.OrderBy(i => i.Name).ToList();

            Assert.Equal(expecteditems.Count(), items.Count());

            for (int i = 0; i < expecteditems.Count; i++)
            {
                var expectedItem = expecteditems[i];
                var actualItem = items.ElementAt(i);
                Assert.Equal(expectedItem.Name, actualItem.Name);
                Assert.Equal(expectedItem.MatchKind, actualItem.MatchKind);
                Assert.Equal(expectedItem.Language, actualItem.Language);
                Assert.Equal(expectedItem.Kind, actualItem.Kind);
                Assert.Equal(expectedItem.IsCaseSensitive, actualItem.IsCaseSensitive);
                if (!string.IsNullOrEmpty(expectedItem.SecondarySort))
                {
                    Assert.Contains(expectedItem.SecondarySort, actualItem.SecondarySort, StringComparison.Ordinal);
                }
            }
        }

        internal void VerifyNavigateToResultItem(
            NavigateToItem result, string name, string displayMarkup, 
            MatchKind matchKind, string navigateToItemKind,
            Glyph glyph, string additionalInfo = null)
        {
            // Verify symbol information
            Assert.Equal(name, result.Name);
            Assert.Equal(matchKind, result.MatchKind);
            Assert.Equal(this.Language, result.Language);
            Assert.Equal(navigateToItemKind, result.Kind);

            MarkupTestFile.GetSpans(displayMarkup, out displayMarkup,
                out ImmutableArray<TextSpan> expectedDisplayNameSpans);

            var itemDisplay = (NavigateToItemDisplay)result.DisplayFactory.CreateItemDisplay(result);

            Assert.Equal(itemDisplay.GlyphMoniker, glyph.GetImageMoniker());

            Assert.Equal(displayMarkup, itemDisplay.Name);
            Assert.Equal<TextSpan>(
                expectedDisplayNameSpans,
                itemDisplay.GetNameMatchRuns("").Select(s => s.ToTextSpan()).ToImmutableArray());

            if (additionalInfo != null)
            {
                Assert.Equal(additionalInfo, itemDisplay.AdditionalInformation);
            }
        }

        private BitmapSource CreateIconBitmapSource()
        {
            int stride = PixelFormats.Bgr32.BitsPerPixel / 8 * 16;
            return BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgr32, null, new byte[16 * stride], stride);
        }

        // For ordering of NavigateToItems, see
        // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.language.navigateto.interfaces.navigatetoitem.aspx
        protected static int CompareNavigateToItems(NavigateToItem a, NavigateToItem b)
        {
            int result = ((int)a.MatchKind) - ((int)b.MatchKind);
            if (result != 0)
            {
                return result;
            }

            result = a.Name.CompareTo(b.Name);
            if (result != 0)
            {
                return result;
            }

            result = a.Kind.CompareTo(b.Kind);
            if (result != 0)
            {
                return result;
            }

            result = a.SecondarySort.CompareTo(b.SecondarySort);
            return result;
        }
#pragma warning restore CS0618 // MatchKind is obsolete
    }
}
