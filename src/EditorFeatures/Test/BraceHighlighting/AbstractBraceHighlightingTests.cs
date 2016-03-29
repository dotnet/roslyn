// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.BraceHighlighting
{
    public abstract class AbstractBraceHighlightingTests
    {
        protected async Task TestBraceHighlightingAsync(string markup)
        {
            using (var workspace = await CreateWorkspaceAsync(markup))
            {
                WpfTestCase.RequireWpfFact($"{nameof(AbstractBraceHighlightingTests)}.{nameof(TestBraceHighlightingAsync)} creates asynchronous taggers");

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
                await provider.ProduceTagsAsync_ForTestingPurposesOnly(context);

                var expectedHighlights = testDocument.SelectedSpans.Select(ts => ts.ToSpan()).OrderBy(s => s.Start).ToList();
                var actualHighlights = context.tagSpans.Select(ts => ts.Span.Span).OrderBy(s => s.Start).ToList();

                Assert.Equal(expectedHighlights, actualHighlights);
            }
        }

        protected abstract Task<TestWorkspace> CreateWorkspaceAsync(string markup);
    }
}
