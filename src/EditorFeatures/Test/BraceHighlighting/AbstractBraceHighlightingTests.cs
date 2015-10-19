﻿using System.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.BraceHighlighting
{
    public abstract class AbstractBraceHighlightingTests
    {
        protected void TestBraceHighlighting(string markup)
        {
            using (var workspace = CreateWorkspace(markup))
            {
                var provider = new BraceHighlightingViewTaggerProvider(
                    workspace.GetService<IBraceMatchingService>(),
                    workspace.GetService<IForegroundNotificationService>(),
                    AggregateAsynchronousOperationListener.EmptyListeners);

                var testDocument = workspace.Documents.First();
                var buffer = testDocument.TextBuffer;
                var document = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().FirstOrDefault();
                var context = new TaggerContext<BraceHighlightTag>(
                    document, buffer.CurrentSnapshot,
                    new SnapshotPoint(buffer.CurrentSnapshot, testDocument.CursorPosition.Value));
                provider.ProduceTagsAsync_ForTestingPurposesOnly(context).Wait();

                var expectedHighlights = testDocument.SelectedSpans.Select(ts => ts.ToSpan()).OrderBy(s => s.Start).ToList();
                var actualHighlights = context.tagSpans.Select(ts => ts.Span.Span).OrderBy(s => s.Start).ToList();

                Assert.Equal(expectedHighlights, actualHighlights);
            }
        }

        protected abstract TestWorkspace CreateWorkspace(string markup);
    }
}
