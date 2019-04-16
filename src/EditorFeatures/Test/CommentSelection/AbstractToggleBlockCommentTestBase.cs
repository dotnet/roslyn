﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CommentSelection
{
    public abstract class AbstractToggleBlockCommentTestBase
    {
        abstract internal ToggleBlockCommentCommandHandler GetToggleBlockCommentCommandHandler(TestWorkspace workspace);

        protected void ToggleBlockComment(string markup, string expected)
        {
            ToggleBlockCommentMultiple(markup, new string[] { expected });
        }

        protected void ToggleBlockCommentMultiple(string markup, string[] expectedText)
        {
            var exportProvider = ExportProviderCache
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(MockToggleBlockCommentExperimentationService)))
                .CreateExportProvider();

            using (var workspace = TestWorkspace.CreateCSharp(markup, exportProvider: exportProvider))
            {

                var doc = workspace.Documents.First();
                SetupSelection(doc.GetTextView(), doc.SelectedSpans.Select(s => Span.FromBounds(s.Start, s.End)));

                var commandHandler = GetToggleBlockCommentCommandHandler(workspace);
                var textView = doc.GetTextView();
                var textBuffer = doc.GetTextBuffer();

                for (var i = 0; i < expectedText.Length; i++)
                {
                    commandHandler.ExecuteCommand(textView, textBuffer, ValueTuple.Create(), TestCommandExecutionContext.Create());
                    AssertCommentResult(doc.TextBuffer, textView, expectedText[i]);
                }
            }
        }

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

        [ExportWorkspaceService(typeof(IExperimentationService), WorkspaceKind.Test), Shared]
        private class MockToggleBlockCommentExperimentationService : IExperimentationService
        {
            public bool IsExperimentEnabled(ExperimentName experimentName)
            {
                return WellKnownExperimentNames.ToggleBlockComment.Equals(experimentName);
            }
        }
    }
}
