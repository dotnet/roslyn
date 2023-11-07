// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceMatching;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.BraceHighlighting
{
    [UseExportProvider]
    public abstract class AbstractBraceHighlightingTests
    {
        protected async Task TestBraceHighlightingAsync(
            string markup, ParseOptions options = null, bool swapAnglesWithBrackets = false)
        {
            MarkupTestFile.GetPositionAndSpans(markup,
                out var text, out var cursorPosition, out var expectedSpans);

            // needed because markup test file can't support [|[|] to indicate selecting
            // just an open bracket.
            if (swapAnglesWithBrackets)
            {
                text = text.Replace("<", "[").Replace(">", "]");
            }

            using (var workspace = CreateWorkspace(text, options))
            {
                WpfTestRunner.RequireWpfFact($"{nameof(AbstractBraceHighlightingTests)}.{nameof(TestBraceHighlightingAsync)} creates asynchronous taggers");

                var provider = new BraceHighlightingViewTaggerProvider(
                    workspace.GetService<IThreadingContext>(),
                    GetBraceMatchingService(workspace),
                    workspace.GetService<IGlobalOptionService>(),
                    visibilityTracker: null,
                    AsynchronousOperationListenerProvider.NullProvider);

                var testDocument = workspace.Documents.First();
                var buffer = testDocument.GetTextBuffer();
                var document = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault();
                var context = new TaggerContext<BraceHighlightTag>(
                    document, buffer.CurrentSnapshot,
                    new SnapshotPoint(buffer.CurrentSnapshot, cursorPosition));
                await provider.GetTestAccessor().ProduceTagsAsync(context);

                var expectedHighlights = expectedSpans.Select(ts => ts.ToSpan()).OrderBy(s => s.Start).ToList();
                var actualHighlights = context.TagSpans.Select(ts => ts.Span.Span).OrderBy(s => s.Start).ToList();

                Assert.Equal(expectedHighlights, actualHighlights);
            }
        }

        internal virtual IBraceMatchingService GetBraceMatchingService(TestWorkspace workspace)
            => workspace.GetService<IBraceMatchingService>();

        protected abstract TestWorkspace CreateWorkspace(string markup, ParseOptions options);
    }
}
