// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.KeywordHighlighting
{
    [UseExportProvider]
    public abstract class AbstractKeywordHighlighterTests
    {
        private static readonly Dictionary<Type, IExportProviderFactory> _specificHighlighterExportProviderFactories = new Dictionary<Type, IExportProviderFactory>();
        private ExportProvider _exportProvider;

        protected ExportProvider ExportProvider
        {
            get
            {
                return _exportProvider ??= GetExportProvider(this);

                static ExportProvider GetExportProvider(AbstractKeywordHighlighterTests self)
                {
                    IExportProviderFactory factory;
                    lock (_specificHighlighterExportProviderFactories)
                    {
                        factory = _specificHighlighterExportProviderFactories.GetOrAdd(
                            self.GetType(),
                            type => ExportProviderCache.GetOrCreateExportProviderFactory(self.GetExportCatalog()));
                    }

                    return factory.CreateExportProvider();
                }
            }
        }

        internal abstract Type GetHighlighterType();
        protected abstract IEnumerable<ParseOptions> GetOptions();
        protected abstract TestWorkspace CreateWorkspaceFromFile(string code, ParseOptions options);

        protected virtual ComposableCatalog GetExportCatalog()
        {
            var catalogWithoutCompletion = TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithoutPartsOfType(typeof(IHighlighter));
            var catalog = catalogWithoutCompletion.WithPart(GetHighlighterType());
            return catalog;
        }

        protected async Task TestAsync(
            string code)
        {
            foreach (var option in GetOptions())
            {
                await TestAsync(code, option);
            }
        }

        private async Task TestAsync(string markup, ParseOptions options)
        {
            using var workspace = CreateWorkspaceFromFile(markup, options);
            var testDocument = workspace.Documents.Single();
            var expectedHighlightSpans = testDocument.SelectedSpans.ToList();
            expectedHighlightSpans.Sort();

            var cursorSpan = testDocument.AnnotatedSpans["Cursor"].Single();
            var textSnapshot = testDocument.GetTextBuffer().CurrentSnapshot;
            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);

            var service = Assert.IsType<HighlightingService>(workspace.ExportProvider.GetExportedValue<IHighlightingService>());

            var root = await document.GetSyntaxRootAsync();

            // Check that every point within the span (inclusive) produces the expected set of
            // results.
            for (var i = 0; i <= cursorSpan.Length; i++)
            {
                var position = cursorSpan.Start + i;
                var highlightSpans = new List<TextSpan>();
                service.AddHighlights(root, position, highlightSpans, CancellationToken.None);

                CheckSpans(root.SyntaxTree, expectedHighlightSpans, highlightSpans);
            }
        }

        private static void CheckSpans(SyntaxTree tree, IList<TextSpan> expectedHighlightSpans, List<TextSpan> highlightSpans)
        {
            for (var j = 0; j < Math.Max(highlightSpans.Count, expectedHighlightSpans.Count); j++)
            {
                if (j >= expectedHighlightSpans.Count)
                {
                    var actualLineSpan = tree.GetLineSpan(highlightSpans[j]).Span;
                    var actualText = tree.GetText().ToString(highlightSpans[j]);
                    Assert.False(true, $"Unexpected highlight at {actualLineSpan}: '{actualText}'");
                }
                else if (j >= highlightSpans.Count)
                {
                    var expectedLineSpan = tree.GetLineSpan(expectedHighlightSpans[j]).Span;
                    var expectedText = tree.GetText().ToString(expectedHighlightSpans[j]);
                    Assert.False(true, $"Missing highlight at {expectedLineSpan}: '{expectedText}'");
                }

                var expected = expectedHighlightSpans[j];
                var actual = highlightSpans[j];

                if (actual != expected)
                    Assert.Equal(tree.GetLineSpan(expected).Span, tree.GetLineSpan(actual).Span);
            }
        }
    }
}
