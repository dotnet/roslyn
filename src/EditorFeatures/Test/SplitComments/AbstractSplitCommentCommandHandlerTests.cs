// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SplitComment
{
    public class AbstractSplitCommentCommandHandlerTests
    {
        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issure. This bug does not represent a product
        /// failure.
        /// </summary>
        private static void TestWorker(
            string inputMarkup,
            string? expectedOutputMarkup,
            Action callback,
            bool verifyUndo = true,
            IndentStyle indentStyle = IndentStyle.Smart)
        {
            using var workspace = TestWorkspace.CreateCSharp(inputMarkup);
            var workspaceOptions = workspace.Options.WithChangedOption(new OptionKey(SmartIndent, LanguageNames.CSharp), indentStyle);
            var workspaceChanges = workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspaceOptions));
            Assert.True(workspaceChanges);

            var document = workspace.Documents.Single();
            var view = document.GetTextView();

            var originalSnapshot = view.TextBuffer.CurrentSnapshot;
            var originalSelections = document.SelectedSpans;

            var snapshotSpans = new List<SnapshotSpan>();
            foreach (var selection in originalSelections)
                snapshotSpans.Add(selection.ToSnapshotSpan(originalSnapshot));

            view.SetMultiSelection(snapshotSpans);

            var undoHistoryRegistry = workspace.GetService<ITextUndoHistoryRegistry>();
            var commandHandler = workspace.ExportProvider.GetCommandHandler<SplitCommentCommandHandler>(nameof(SplitCommentCommandHandler));
            if (!commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(view, view.TextBuffer), TestCommandExecutionContext.Create()))
            {
                callback();
            }

            if (expectedOutputMarkup != null)
            {
                MarkupTestFile.GetSpans(expectedOutputMarkup,
                    out var expectedOutput, out ImmutableArray<TextSpan> expectedSpans);

                Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.AsText().ToString());

                if (verifyUndo)
                {
                    // Ensure that after undo we go back to where we were to begin with.
                    var history = undoHistoryRegistry.GetHistory(document.GetTextBuffer());
                    history.Undo(count: originalSelections.Count);

                    var currentSnapshot = document.GetTextBuffer().CurrentSnapshot;
                    Assert.Equal(originalSnapshot.GetText(), currentSnapshot.GetText());
                }
            }
        }

        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issue. This bug does not represent a product
        /// failure.
        /// </summary>
        protected static void TestHandled(
            string inputMarkup, string expectedOutputMarkup,
            bool verifyUndo = true, IndentStyle indentStyle = IndentStyle.Smart)
        {
            TestWorker(
                inputMarkup, expectedOutputMarkup,
                callback: () =>
                {
                    Assert.True(false, "Should not reach here.");
                },
                verifyUndo, indentStyle);
        }

        protected static void TestNotHandled(string inputMarkup)
        {
            var notHandled = false;
            TestWorker(
                inputMarkup, null,
                callback: () =>
                {
                    notHandled = true;
                });

            Assert.True(notHandled);
        }
    }
}
