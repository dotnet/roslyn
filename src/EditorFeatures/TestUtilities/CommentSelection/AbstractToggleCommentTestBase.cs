// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.CommentSelection
{
    public abstract class AbstractToggleCommentTestBase
    {
        abstract internal AbstractCommentSelectionBase<ValueTuple> GetToggleCommentCommandHandler(TestWorkspace workspace);

        abstract internal TestWorkspace GetWorkspace(string markup, ExportProvider exportProvider);

        protected void ToggleComment(string markup, string expected)
        {
            ToggleCommentMultiple(markup, new string[] { expected });
        }

        protected void ToggleCommentMultiple(string markup, string[] expectedText)
        {
            using (var workspace = GetWorkspace(markup, GetExportProvider()))
            {
                var doc = workspace.Documents.First();
                SetupSelection(doc.GetTextView(), doc.SelectedSpans.Select(s => Span.FromBounds(s.Start, s.End)));

                var commandHandler = GetToggleCommentCommandHandler(workspace);
                var textView = doc.GetTextView();
                var textBuffer = doc.GetTextBuffer();

                for (var i = 0; i < expectedText.Length; i++)
                {
                    commandHandler.ExecuteCommand(textView, textBuffer, ValueTuple.Create(), TestCommandExecutionContext.Create());
                    AssertCommentResult(doc.TextBuffer, textView, expectedText[i]);
                }
            }
        }

        protected void ToggleCommentWithProjectionBuffer(string surfaceBufferMarkup, string subjectBufferMarkup, string entireExpectedMarkup)
        {
            using (var workspace = GetWorkspace(subjectBufferMarkup, GetExportProvider()))
            {
                var document = workspace.CreateProjectionBufferDocument(surfaceBufferMarkup, workspace.Documents, LanguageNames.CSharp);
                SetupSelection(document.GetTextView(), document.SelectedSpans.Select(s => Span.FromBounds(s.Start, s.End)));

                var commandHandler = GetToggleCommentCommandHandler(workspace);
                var textView = document.GetTextView();
                var originalSubjectBuffer = GetBufferForContentType(ContentTypeNames.CSharpContentType, textView);

                commandHandler.ExecuteCommand(textView, originalSubjectBuffer, ValueTuple.Create(), TestCommandExecutionContext.Create());
                AssertCommentResult(textView.TextBuffer, textView, entireExpectedMarkup);
            }
        }

        private static ExportProvider GetExportProvider()
            => ExportProviderCache
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic)
                .CreateExportProvider();

        private static ITextBuffer GetBufferForContentType(string contentTypeName, ITextView textView)
            => textView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(contentTypeName)).Single();

        private static void AssertCommentResult(ITextBuffer textBuffer, IWpfTextView textView, string expectedText)
        {
            MarkupTestFile.GetSpans(expectedText, out var actualExpectedText, out ImmutableArray<TextSpan> expectedSpans);

            Assert.Equal(actualExpectedText, textBuffer.CurrentSnapshot.GetText());

            if (!expectedSpans.IsEmpty)
            {
                AssertEx.Equal(expectedSpans, textView.Selection.SelectedSpans.Select(snapshotSpan => TextSpan.FromBounds(snapshotSpan.Start, snapshotSpan.End)));
            }
        }

        private static void SetupSelection(IWpfTextView textView, IEnumerable<Span> spans)
        {
            var snapshot = textView.TextSnapshot;
            if (spans.Count() == 1)
            {
                textView.Selection.Select(new SnapshotSpan(snapshot, spans.Single()), isReversed: false);
                textView.Caret.MoveTo(new SnapshotPoint(snapshot, spans.Single().End));
            }
            else if (spans.Count() > 1)
            {
                textView.Selection.Mode = TextSelectionMode.Box;
                textView.Selection.Select(new VirtualSnapshotPoint(snapshot, spans.First().Start),
                                          new VirtualSnapshotPoint(snapshot, spans.Last().End));
                textView.Caret.MoveTo(new SnapshotPoint(snapshot, spans.Last().End));
            }
        }
    }
}
