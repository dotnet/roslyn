// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SplitComment
{
    public abstract class AbstractSplitCommentCommandHandlerTests
    {
        protected abstract EditorTestWorkspace CreateWorkspace(string markup);

        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issue. This bug does not represent a product
        /// failure.
        /// </summary>
        private void TestWorker(
            string inputMarkup,
            string? expectedOutputMarkup,
            Action callback,
            bool enabled,
            bool useTabs)
        {
            if (useTabs)
            {
                // Make sure the tests seem well formed (i.e. no one accidentally replaced the tabs in them with spaces.
                Assert.True(inputMarkup.Contains("\t"));
                if (expectedOutputMarkup != null)
                    Assert.True(expectedOutputMarkup.Contains("\t"));
            }

            using var workspace = CreateWorkspace(inputMarkup);

            var globalOptions = workspace.GlobalOptions;
            var language = workspace.Projects.Single().Language;

            globalOptions.SetGlobalOption(SplitCommentOptionsStorage.Enabled, language, enabled);
            globalOptions.SetGlobalOption(FormattingOptions2.UseTabs, language, useTabs);

            var document = workspace.Documents.Single();
            var view = document.GetTextView();

            var originalSnapshot = view.TextBuffer.CurrentSnapshot;
            var originalSelections = document.SelectedSpans;

            var snapshotSpans = new List<SnapshotSpan>();
            foreach (var selection in originalSelections)
                snapshotSpans.Add(new SnapshotSpan(originalSnapshot, new Span(selection.Start, selection.Length)));

            view.SetMultiSelection(snapshotSpans);

            var undoHistoryRegistry = workspace.GetService<ITextUndoHistoryRegistry>();
            var commandHandler = workspace.ExportProvider.GetCommandHandler<SplitCommentCommandHandler>(nameof(SplitCommentCommandHandler));
            if (!commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(view, view.TextBuffer), TestCommandExecutionContext.Create()))
            {
                callback();
            }

            if (expectedOutputMarkup != null)
            {
                MarkupTestFile.GetSpans(expectedOutputMarkup, out var expectedOutput, out var expectedSpans);

                Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.AsText().ToString());

                // Ensure that after undo we go back to where we were to begin with.
                var history = undoHistoryRegistry.GetHistory(document.GetTextBuffer());
                history.Undo(count: originalSelections.Count);

                var currentSnapshot = document.GetTextBuffer().CurrentSnapshot;
                Assert.Equal(originalSnapshot.GetText(), currentSnapshot.GetText());
            }
        }

        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issue. This bug does not represent a product
        /// failure.
        /// </summary>
        protected void TestHandled(string inputMarkup, string expectedOutputMarkup, bool enabled = true, bool useTabs = false)
        {
            TestWorker(
                inputMarkup, expectedOutputMarkup,
                callback: () =>
                {
                    Assert.True(false, "Should not reach here.");
                }, enabled, useTabs);
        }

        protected void TestNotHandled(string inputMarkup, bool enabled = true, bool useTabs = false)
        {
            var notHandled = false;
            TestWorker(
                inputMarkup, null,
                callback: () =>
                {
                    notHandled = true;
                }, enabled, useTabs);

            Assert.True(notHandled);
        }
    }
}
