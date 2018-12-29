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
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.RemoteHost;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo
{
    [UseExportProvider]
    public abstract class AbstractNavigateToTests
    {
        protected INavigateToItemProvider _provider;
        protected NavigateToTestAggregator _aggregator;

        internal readonly static PatternMatch s_emptyExactPatternMatch = new PatternMatch(PatternMatchKind.Exact, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyPrefixPatternMatch = new PatternMatch(PatternMatchKind.Prefix, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptySubstringPatternMatch = new PatternMatch(PatternMatchKind.Substring, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseExactPatternMatch = new PatternMatch(PatternMatchKind.CamelCaseExact, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCasePrefixPatternMatch = new PatternMatch(PatternMatchKind.CamelCasePrefix, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseNonContiguousPrefixPatternMatch = new PatternMatch(PatternMatchKind.CamelCaseNonContiguousPrefix, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseSubstringPatternMatch = new PatternMatch(PatternMatchKind.CamelCaseSubstring, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseNonContiguousSubstringPatternMatch = new PatternMatch(PatternMatchKind.CamelCaseNonContiguousSubstring, true, true, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyFuzzyPatternMatch = new PatternMatch(PatternMatchKind.Fuzzy, true, true, ImmutableArray<Span>.Empty);

        internal readonly static PatternMatch s_emptyExactPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.Exact, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyPrefixPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.Prefix, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptySubstringPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.Substring, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseExactPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.CamelCaseExact, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCasePrefixPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.CamelCasePrefix, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.CamelCaseNonContiguousPrefix, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseSubstringPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.CamelCaseSubstring, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyCamelCaseNonContiguousSubstringPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.CamelCaseNonContiguousSubstring, true, false, ImmutableArray<Span>.Empty);
        internal readonly static PatternMatch s_emptyFuzzyPatternMatch_NotCaseSensitive = new PatternMatch(PatternMatchKind.Fuzzy, true, false, ImmutableArray<Span>.Empty);

        protected abstract TestWorkspace CreateWorkspace(string content, ExportProvider exportProvider);
        protected abstract string Language { get; }

        protected async Task TestAsync(string content, Func<TestWorkspace, Task> body)
        {
            await TestAsync(content, body, outOfProcess: true);
            await TestAsync(content, body, outOfProcess: false);
        }

        internal async Task TestAsync(string content, Func<TestWorkspace, Task> body, bool outOfProcess)
        {
            await TestAsync(content, body, outOfProcess, null);
            await TestAsync(content, body, outOfProcess, w => new FirstDocIsVisibleDocumentTrackingService(w));
            await TestAsync(content, body, outOfProcess, w => new FirstDocIsActiveAndVisibleDocumentTrackingService(w));
        }

        internal async Task TestAsync(
            string content, Func<TestWorkspace, Task> body, bool outOfProcess,
            Func<TestWorkspace, IDocumentTrackingService> createTrackingService)
        {
            using (var workspace = SetupWorkspace(
                content, TestExportProvider.ExportProviderWithCSharpAndVisualBasic, createTrackingService))
            {
                workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, outOfProcess)
                                                     .WithChangedOption(RemoteFeatureOptions.OutOfProcessAllowed, outOfProcess)
                                                     .WithChangedOption(RemoteFeatureOptions.NavigateToEnabled, outOfProcess);

                await body(workspace);
            }
        }

        private protected TestWorkspace SetupWorkspace(
            XElement workspaceElement, ExportProvider exportProvider,
            Func<TestWorkspace, IDocumentTrackingService> createTrackingService)
        {
            var workspace = TestWorkspace.Create(workspaceElement, exportProvider: exportProvider);
            var trackingService = createTrackingService?.Invoke(workspace);
            InitializeWorkspace(workspace, trackingService);
            return workspace;
        }

        private protected TestWorkspace SetupWorkspace(
            string content, ExportProvider exportProvider,
            Func<TestWorkspace, IDocumentTrackingService> createTrackingService)
        {
            var workspace = CreateWorkspace(content, exportProvider);
            var trackingService = createTrackingService?.Invoke(workspace);
            InitializeWorkspace(workspace, trackingService);
            return workspace;
        }

        internal void InitializeWorkspace(
            TestWorkspace workspace, IDocumentTrackingService documentTrackingService)
        {
            _provider = new NavigateToItemProvider(workspace, AsynchronousOperationListenerProvider.NullListener, documentTrackingService);
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
                Assert.True(expectedItem.PatternMatch.Kind == actualItem.PatternMatch.Kind, string.Format("pattern: {0} expected: {1} actual: {2}", expectedItem.Name, expectedItem.PatternMatch.Kind, actualItem.PatternMatch.Kind));
                Assert.True(expectedItem.PatternMatch.IsCaseSensitive == actualItem.PatternMatch.IsCaseSensitive, string.Format("pattern: {0} expected: {1} actual: {2}", expectedItem.Name, expectedItem.PatternMatch.IsCaseSensitive, actualItem.PatternMatch.IsCaseSensitive));
                Assert.Equal(expectedItem.Language, actualItem.Language);
                Assert.Equal(expectedItem.Kind, actualItem.Kind);
                if (!string.IsNullOrEmpty(expectedItem.SecondarySort))
                {
                    Assert.Contains(expectedItem.SecondarySort, actualItem.SecondarySort, StringComparison.Ordinal);
                }
            }
        }

        internal void VerifyNavigateToResultItem(
            NavigateToItem result, string name, string displayMarkup,
            PatternMatchKind matchKind, string navigateToItemKind,
            Glyph glyph, string additionalInfo = null)
        {
            // Verify symbol information
            Assert.Equal(name, result.Name);
            Assert.Equal(matchKind, result.PatternMatch.Kind);
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

        internal BitmapSource CreateIconBitmapSource()
        {
            int stride = PixelFormats.Bgr32.BitsPerPixel / 8 * 16;
            return BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgr32, null, new byte[16 * stride], stride);
        }

        // For ordering of NavigateToItems, see
        // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.language.navigateto.interfaces.navigatetoitem.aspx
        protected static int CompareNavigateToItems(NavigateToItem a, NavigateToItem b)
        {
            int result = ((int)a.PatternMatch.Kind) - ((int)b.PatternMatch.Kind);
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

#pragma warning disable CS0067

    public class FirstDocIsVisibleDocumentTrackingService : IDocumentTrackingService
    {
        private readonly Workspace _workspace;

        public FirstDocIsVisibleDocumentTrackingService(Workspace workspace)
        {
            _workspace = workspace;
        }

        public event EventHandler<DocumentId> ActiveDocumentChanged;
        public event EventHandler<EventArgs> NonRoslynBufferTextChanged;

        public DocumentId TryGetActiveDocument()
            => null;

        public ImmutableArray<DocumentId> GetVisibleDocuments()
            => ImmutableArray.Create(_workspace.CurrentSolution.Projects.First().DocumentIds.First());
    }

    public class FirstDocIsActiveAndVisibleDocumentTrackingService : IDocumentTrackingService
    {
        private readonly Workspace _workspace;

        public FirstDocIsActiveAndVisibleDocumentTrackingService(Workspace workspace)
        {
            _workspace = workspace;
        }

        public event EventHandler<DocumentId> ActiveDocumentChanged;
        public event EventHandler<EventArgs> NonRoslynBufferTextChanged;

        public DocumentId TryGetActiveDocument()
            => _workspace.CurrentSolution.Projects.First().DocumentIds.First();

        public ImmutableArray<DocumentId> GetVisibleDocuments()
            => ImmutableArray.Create(_workspace.CurrentSolution.Projects.First().DocumentIds.First());
    }
#pragma warning restore
}
