// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;

[UseExportProvider]
public abstract partial class AbstractNavigateToTests
{
    protected static readonly TestComposition DefaultComposition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestWorkspaceNavigateToSearchHostService));
    protected static readonly TestComposition FirstVisibleComposition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestWorkspaceNavigateToSearchHostService), typeof(FirstDocIsVisibleDocumentTrackingService.Factory));
    protected static readonly TestComposition FirstActiveAndVisibleComposition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestWorkspaceNavigateToSearchHostService), typeof(FirstDocumentIsActiveAndVisibleDocumentTrackingService.Factory));

    protected INavigateToItemProvider _provider;
    protected NavigateToTestAggregator _aggregator;

    internal static readonly PatternMatch s_emptyExactPatternMatch = new(PatternMatchKind.Exact, true, true, []);
    internal static readonly PatternMatch s_emptyPrefixPatternMatch = new(PatternMatchKind.Prefix, true, true, []);
    internal static readonly PatternMatch s_emptySubstringPatternMatch = new(PatternMatchKind.Substring, true, true, []);
    internal static readonly PatternMatch s_emptyCamelCaseExactPatternMatch = new(PatternMatchKind.CamelCaseExact, true, true, []);
    internal static readonly PatternMatch s_emptyCamelCasePrefixPatternMatch = new(PatternMatchKind.CamelCasePrefix, true, true, []);
    internal static readonly PatternMatch s_emptyCamelCaseNonContiguousPrefixPatternMatch = new(PatternMatchKind.CamelCaseNonContiguousPrefix, true, true, []);
    internal static readonly PatternMatch s_emptyCamelCaseSubstringPatternMatch = new(PatternMatchKind.CamelCaseSubstring, true, true, []);
    internal static readonly PatternMatch s_emptyCamelCaseNonContiguousSubstringPatternMatch = new(PatternMatchKind.CamelCaseNonContiguousSubstring, true, true, []);
    internal static readonly PatternMatch s_emptyFuzzyPatternMatch = new(PatternMatchKind.Fuzzy, true, true, []);

    internal static readonly PatternMatch s_emptyExactPatternMatch_NotCaseSensitive = new(PatternMatchKind.Exact, true, false, []);
    internal static readonly PatternMatch s_emptyPrefixPatternMatch_NotCaseSensitive = new(PatternMatchKind.Prefix, true, false, []);
    internal static readonly PatternMatch s_emptySubstringPatternMatch_NotCaseSensitive = new(PatternMatchKind.Substring, true, false, []);
    internal static readonly PatternMatch s_emptyCamelCaseExactPatternMatch_NotCaseSensitive = new(PatternMatchKind.CamelCaseExact, true, false, []);
    internal static readonly PatternMatch s_emptyCamelCasePrefixPatternMatch_NotCaseSensitive = new(PatternMatchKind.CamelCasePrefix, true, false, []);
    internal static readonly PatternMatch s_emptyCamelCaseNonContiguousPrefixPatternMatch_NotCaseSensitive = new(PatternMatchKind.CamelCaseNonContiguousPrefix, true, false, []);
    internal static readonly PatternMatch s_emptyCamelCaseSubstringPatternMatch_NotCaseSensitive = new(PatternMatchKind.CamelCaseSubstring, true, false, []);
    internal static readonly PatternMatch s_emptyCamelCaseNonContiguousSubstringPatternMatch_NotCaseSensitive = new(PatternMatchKind.CamelCaseNonContiguousSubstring, true, false, []);
    internal static readonly PatternMatch s_emptyFuzzyPatternMatch_NotCaseSensitive = new(PatternMatchKind.Fuzzy, true, false, []);

    protected abstract EditorTestWorkspace CreateWorkspace(string content, TestComposition composition);
    protected abstract string Language { get; }

    public enum Composition
    {
        Default,
        FirstVisible,
        FirstActiveAndVisible,
    }

    private protected static NavigateToItemProvider CreateProvider(EditorTestWorkspace workspace)
        => new(
            workspace,
            workspace.GetService<IThreadingContext>(),
            workspace.GetService<IUIThreadOperationExecutor>(),
            AsynchronousOperationListenerProvider.NullListener);

    protected async Task TestAsync(TestHost testHost, Composition composition, string content, Func<EditorTestWorkspace, Task> body)
    {
        var testComposition = composition switch
        {
            Composition.Default => DefaultComposition,
            Composition.FirstVisible => FirstVisibleComposition,
            Composition.FirstActiveAndVisible => FirstActiveAndVisibleComposition,
            _ => throw ExceptionUtilities.UnexpectedValue(composition),
        };

        await TestAsync(content, body, testHost, testComposition);
    }

    protected async Task TestAsync(TestHost testHost, Composition composition, XElement content, Func<EditorTestWorkspace, Task> body)
    {
        var testComposition = composition switch
        {
            Composition.Default => DefaultComposition,
            Composition.FirstVisible => FirstVisibleComposition,
            Composition.FirstActiveAndVisible => FirstActiveAndVisibleComposition,
            _ => throw ExceptionUtilities.UnexpectedValue(composition),
        };

        await TestAsync(content, body, testHost, testComposition);
    }

    private async Task TestAsync(
        string content, Func<EditorTestWorkspace, Task> body, TestHost testHost,
        TestComposition composition)
    {
        using var workspace = CreateWorkspace(content, testHost, composition);
        await body(workspace);
    }

    protected async Task TestAsync(
        XElement content, Func<EditorTestWorkspace, Task> body, TestHost testHost,
        TestComposition composition)
    {
        using var workspace = CreateWorkspace(content, testHost, composition);
        await body(workspace);
    }

    private protected EditorTestWorkspace CreateWorkspace(
        XElement workspaceElement,
        TestHost testHost,
        TestComposition composition)
    {
        composition = composition.WithTestHostParts(testHost);

        var workspace = EditorTestWorkspace.Create(workspaceElement, composition: composition);
        InitializeWorkspace(workspace);
        return workspace;
    }

    private protected EditorTestWorkspace CreateWorkspace(
        string content,
        TestHost testHost,
        TestComposition composition)
    {
        composition = composition.WithTestHostParts(testHost);

        var workspace = CreateWorkspace(content, composition);
        InitializeWorkspace(workspace);
        return workspace;
    }

    internal void InitializeWorkspace(EditorTestWorkspace workspace)
    {
        _provider = new NavigateToItemProvider(
            workspace,
            workspace.GetService<IThreadingContext>(),
            workspace.GetService<IUIThreadOperationExecutor>(),
            AsynchronousOperationListenerProvider.NullListener);
        _aggregator = new NavigateToTestAggregator(_provider);
    }

    protected static void VerifyNavigateToResultItems(
        List<NavigateToItem> expecteditems, IEnumerable<NavigateToItem> items)
    {
        expecteditems = [.. expecteditems.OrderBy(i => i.Name)];
        items = items.OrderBy(i => i.Name).ToList();

        Assert.Equal(expecteditems.Count, items.Count());

        for (var i = 0; i < expecteditems.Count; i++)
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

        MarkupTestFile.GetSpans(displayMarkup, out displayMarkup, out var expectedDisplayNameSpans);

        var itemDisplay = (NavigateToItemDisplay)result.DisplayFactory.CreateItemDisplay(result);

        Assert.Equal(itemDisplay.GlyphMoniker, glyph.GetImageMoniker());

        Assert.Equal(displayMarkup, itemDisplay.Name);
        Assert.Equal<TextSpan>(
            expectedDisplayNameSpans,
            itemDisplay.GetNameMatchRuns("").SelectAsArray(s => s.ToTextSpan()));

        if (additionalInfo != null)
        {
            Assert.Equal(additionalInfo, itemDisplay.AdditionalInformation);
        }
    }

    internal static BitmapSource CreateIconBitmapSource()
    {
        var stride = PixelFormats.Bgr32.BitsPerPixel / 8 * 16;
        return BitmapSource.Create(16, 16, 96, 96, PixelFormats.Bgr32, null, new byte[16 * stride], stride);
    }

    // For ordering of NavigateToItems, see
    // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.language.navigateto.interfaces.navigatetoitem.aspx
    protected static int CompareNavigateToItems(NavigateToItem a, NavigateToItem b)
        => ComparerWithState.CompareTo(a, b, s_comparisonComponents);

    private static readonly ImmutableArray<Func<NavigateToItem, IComparable>> s_comparisonComponents =
        [item => (int)item.PatternMatch.Kind, item => item.Name, item => item.Kind, item => item.SecondarySort];

    private sealed class FirstDocIsVisibleDocumentTrackingService : IDocumentTrackingService
    {
        private readonly Workspace _workspace;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        private FirstDocIsVisibleDocumentTrackingService(Workspace workspace)
            => _workspace = workspace;

        public event EventHandler<DocumentId> ActiveDocumentChanged { add { } remove { } }

        public DocumentId TryGetActiveDocument()
            => null;

        public ImmutableArray<DocumentId> GetVisibleDocuments()
            => [_workspace.CurrentSolution.Projects.First().DocumentIds.First()];

        [ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService), ServiceLayer.Test), Shared, PartNotDiscoverable]
        public sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new FirstDocIsVisibleDocumentTrackingService(workspaceServices.Workspace);
        }
    }

    [ExportWorkspaceService(typeof(IWorkspaceNavigateToSearcherHostService))]
    [PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    protected sealed class TestWorkspaceNavigateToSearchHostService() : IWorkspaceNavigateToSearcherHostService
    {
        public async ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
            => true;
    }
}
