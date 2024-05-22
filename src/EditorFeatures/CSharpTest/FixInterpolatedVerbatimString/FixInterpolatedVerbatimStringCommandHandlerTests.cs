// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.FixInterpolatedVerbatimString;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.FixInterpolatedVerbatimString
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
    public class FixInterpolatedVerbatimStringCommandHandlerTests
    {
        private static EditorTestWorkspace CreateTestWorkspace(string inputMarkup)
        {
            var workspace = EditorTestWorkspace.CreateCSharp(inputMarkup);
            var document = workspace.Documents.Single();
            var view = document.GetTextView();
            view.SetSelection(document.SelectedSpans.Single().ToSnapshotSpan(view.TextBuffer.CurrentSnapshot));
            return workspace;
        }

        private static (string quoteCharSnapshotText, int quoteCharCaretPosition) TypeQuoteChar(EditorTestWorkspace workspace)
        {
            var view = workspace.Documents.Single().GetTextView();
            var commandHandler = workspace.ExportProvider.GetCommandHandler<FixInterpolatedVerbatimStringCommandHandler>(nameof(FixInterpolatedVerbatimStringCommandHandler));

            string quoteCharSnapshotText = null;
            int quoteCharCaretPosition = default;

            commandHandler.ExecuteCommand(new TypeCharCommandArgs(view, view.TextBuffer, '"'),
                () =>
                {
                    var editorOperations = workspace.GetService<IEditorOperationsFactoryService>().GetEditorOperations(view);
                    editorOperations.InsertText("""
                        "
                        """);

                    quoteCharSnapshotText = view.TextBuffer.CurrentSnapshot.GetText();
                    quoteCharCaretPosition = view.Caret.Position.BufferPosition.Position;

                }, TestCommandExecutionContext.Create());

            return (quoteCharSnapshotText, quoteCharCaretPosition);
        }

        private static void TestHandled(string inputMarkup, string expectedOutputMarkup)
        {
            using var workspace = CreateTestWorkspace(inputMarkup);
            var (quoteCharSnapshotText, quoteCharCaretPosition) = TypeQuoteChar(workspace);
            var view = workspace.Documents.Single().GetTextView();

            MarkupTestFile.GetSpans(expectedOutputMarkup, out var expectedOutput, out var expectedSpans);

            Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.GetText());
            Assert.Equal(expectedSpans.Single().Start, view.Caret.Position.BufferPosition.Position);

            var history = workspace.GetService<ITextUndoHistoryRegistry>().GetHistory(view.TextBuffer);
            history.Undo(count: 1);

            // Ensure that after undo, the ordering fix is undone but the quote remains inserted
            Assert.Equal(quoteCharSnapshotText, view.TextBuffer.CurrentSnapshot.GetText());
            Assert.Equal(quoteCharCaretPosition, view.Caret.Position.BufferPosition.Position);
        }

        private static void TestNotHandled(string inputMarkup)
        {
            using var workspace = CreateTestWorkspace(inputMarkup);
            var originalView = workspace.Documents.Single().GetTextView();
            var originalSnapshotText = originalView.TextBuffer.CurrentSnapshot.GetText();
            var originalCaretPosition = originalView.Caret.Position.BufferPosition.Position;

            var (quoteCharSnapshotText, quoteCharCaretPosition) = TypeQuoteChar(workspace);
            var view = workspace.Documents.Single().GetTextView();

            Assert.Equal(quoteCharSnapshotText, view.TextBuffer.CurrentSnapshot.GetText());
            Assert.Equal(quoteCharCaretPosition, view.Caret.Position.BufferPosition.Position);

            var history = workspace.GetService<ITextUndoHistoryRegistry>().GetHistory(view.TextBuffer);
            history.Undo(count: 1);

            // Ensure that after undo, the quote is removed because the command made no changes
            Assert.Equal(originalSnapshotText, view.TextBuffer.CurrentSnapshot.GetText());
            Assert.Equal(originalCaretPosition, view.Caret.Position.BufferPosition.Position);
        }

        [WpfFact]
        public void TestAfterAtSignDollarSign()
        {
            TestHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = @$[||]
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var v = $@"[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingAfterDollarSignAtSign()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = $@[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingAfterAtSign()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = @[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingAfterDollarSign()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = $[||]
                    }
                }
                """);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public void TestMissingInEmptyFileAfterAtSignDollarSign()
            => TestHandled(@"@$[||]", @"$@""[||]");

        [WpfFact]
        public void TestMissingInEmptyFileAfterDollarSign()
            => TestNotHandled(@"$[||]");

        [WpfFact]
        public void TestMissingInEmptyFile()
            => TestNotHandled(@"[||]");

        [WpfFact]
        public void TestAfterAtSignDollarSignEndOfFile()
        {
            TestHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = @$[||]
                """,
                """
                class C
                {
                    void M()
                    {
                        var v = $@"[||]
                """);
        }

        [WpfFact]
        public void TestMissingInClassDeclaration()
        {
            TestNotHandled(
                """
                class C
                {
                    @$[||]
                }
                """);
        }

        [WpfFact]
        public void TestMissingInComment1()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = // @$[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingInComment2()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = /* @$[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingInString()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = "@$[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingInVerbatimString()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = @"@$[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingInInterpolatedString()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = $"@$[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingInInterpolatedVerbatimString1()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = $@"@$[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestMissingInInterpolatedVerbatimString2()
        {
            TestNotHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = @$"@$[||]
                    }
                }
                """);
        }

        [WpfFact]
        public void TestTrivia()
        {
            TestHandled(
                """
                class C
                {
                    void M()
                    {
                        var v = // a
                                /* b */ @$[||] // c
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var v = // a
                                /* b */ $@"[||] // c
                    }
                }
                """);
        }
    }
}
