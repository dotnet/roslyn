// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.FixInterpolatedVerbatimString;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
    public class FixInterpolatedVerbatimStringCommandHandlerTests
    {
        private static readonly char[] s_allChars = { '@', '$', '"' };

        private static TestWorkspace CreateTestWorkspace(string inputMarkup)
        {
            var workspace = TestWorkspace.CreateCSharp(inputMarkup);
            var document = workspace.Documents.Single();
            var view = document.GetTextView();
            view.SetSelection(document.SelectedSpans.Single().ToSnapshotSpan(view.TextBuffer.CurrentSnapshot));
            return workspace;
        }

        private static (string insertedCharSnapshotText, int insertedCharCaretPosition) TypeChar(TestWorkspace workspace, char ch)
        {
            var view = workspace.Documents.Single().GetTextView();
            var commandHandler = new FixInterpolatedVerbatimStringCommandHandler(
                workspace.GetService<ITextUndoHistoryRegistry>(),
                workspace.GetService<IEditorOperationsFactoryService>());

            string insertedCharSnapshotText = default;
            int insertedCharCaretPosition = default;

            commandHandler.ExecuteCommand(new TypeCharCommandArgs(view, view.TextBuffer, ch),
                () =>
                {
                    var editorOperations = workspace.GetService<IEditorOperationsFactoryService>().GetEditorOperations(view);
                    editorOperations.InsertText(ch.ToString());

                    // We want to get the snapshot after the character was inserted, but before the command is executed
                    insertedCharSnapshotText = view.TextBuffer.CurrentSnapshot.GetText();
                    insertedCharCaretPosition = view.Caret.Position.BufferPosition.Position;

                }, TestCommandExecutionContext.Create());

            return (insertedCharSnapshotText, insertedCharCaretPosition);
        }

        private static void TestHandled(char insertedChar, string inputMarkup, string expectedOutputMarkup)
        {
            using (var workspace = CreateTestWorkspace(inputMarkup))
            {
                var (insertedCharSnapshotText, insertedCharCaretPosition) = TypeChar(workspace, insertedChar);
                var view = workspace.Documents.Single().GetTextView();

                MarkupTestFile.GetSpans(expectedOutputMarkup,
                    out var expectedOutput, out ImmutableArray<TextSpan> expectedSpans);

                Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.GetText());
                Assert.Equal(expectedSpans.Single().Start, view.Caret.Position.BufferPosition.Position);
 
                var history = workspace.GetService<ITextUndoHistoryRegistry>().GetHistory(view.TextBuffer);
                history.Undo(count: 1);

                // Ensure that after undo, the ordering fix is undone but the typed character remains inserted
                Assert.Equal(insertedCharSnapshotText, view.TextBuffer.CurrentSnapshot.GetText());
                Assert.Equal(insertedCharCaretPosition, view.Caret.Position.BufferPosition.Position);
            }
        }

        private static void TestNotHandled(char insertedChar, string inputMarkup)
        {
            using (var workspace = CreateTestWorkspace(inputMarkup))
            {
                var originalView = workspace.Documents.Single().GetTextView();
                var originalSnapshotText = originalView.TextBuffer.CurrentSnapshot.GetText();
                var originalCaretPosition = originalView.Caret.Position.BufferPosition.Position;

                var (insertedCharSnapshotText, insertedCharCaretPosition) = TypeChar(workspace, insertedChar);
                var view = workspace.Documents.Single().GetTextView();

                Assert.Equal(insertedCharSnapshotText, view.TextBuffer.CurrentSnapshot.GetText());
                Assert.Equal(insertedCharCaretPosition, view.Caret.Position.BufferPosition.Position);

                var history = workspace.GetService<ITextUndoHistoryRegistry>().GetHistory(view.TextBuffer);
                history.Undo(count: 1);

                // Ensure that after undo, the typed character is removed because the command made no changes
                Assert.Equal(originalSnapshotText, view.TextBuffer.CurrentSnapshot.GetText());
                Assert.Equal(originalCaretPosition, view.Caret.Position.BufferPosition.Position);
            }
        }

        private static void TestHandledWithSingle(char ch, string inputMarkup, string expectedOutputMarkup)
        {
            TestHandled(ch, inputMarkup, expectedOutputMarkup);

            foreach (var other in s_allChars)
            {
                if (other != ch)
                {
                    TestNotHandled(other, inputMarkup);
                }
            }
        }

        private static void TestNotHandledWithAny(string inputMarkup)
        {
            foreach (var ch in s_allChars)
            {
                TestNotHandled(ch, inputMarkup);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestAfterAtSignDollarSign()
        {
            TestHandledWithSingle('"',
@"class C
{
    void M()
    {
        var v = @$[||]
    }
}",
@"class C
{
    void M()
    {
        var v = $@""[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestBetweenAtSignQuoteSign()
        {
            TestHandledWithSingle('$',
@"class C
{
    void M()
    {
        var v = @[||]""
    }
}",
@"class C
{
    void M()
    {
        var v = $@[||]""
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestBeforeDollarSignQuoteSign()
        {
            TestHandledWithSingle('@',
@"class C
{
    void M()
    {
        var v = [||]$""
    }
}",
@"class C
{
    void M()
    {
        var v = $@[||]""
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingAfterDollarSignAtSign()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = $@[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingAfterAtSign()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = @[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingAfterDollarSign()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = $[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingAfterQuoteSign()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = ""[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInEmptyFileAfterAtSignDollarSign()
        {
            TestNotHandledWithAny(@"@$[||]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInEmptyFileAfterAtSign()
        {
            TestNotHandledWithAny(@"@[||]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInEmptyFileAfterDollarSign()
        {
            TestNotHandledWithAny( @"$[||]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInEmptyFileAfterQuoteSign()
        {
            TestNotHandledWithAny(@"$[||]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInEmptyFile()
        {
            TestNotHandledWithAny(@"[||]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestAfterAtSignDollarSignEndOfFile()
        {
            TestHandledWithSingle('"',
@"class C
{
    void M()
    {
        var v = @$[||]",
@"class C
{
    void M()
    {
        var v = $@""[||]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInClassDeclaration()
        {
            TestNotHandledWithAny(
@"class C
{
    @$[||]
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInComment1()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = // @$[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInComment2()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = /* @$[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInString()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = ""@$[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInVerbatimString()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = @""@$[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInInterpolatedString()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = $""@$[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInInterpolatedVerbatimString1()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = $@""@$[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestMissingInInterpolatedVerbatimString2()
        {
            TestNotHandledWithAny(
@"class C
{
    void M()
    {
        var v = @$""@$[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.FixInterpolatedVerbatimString)]
        public void TestTrivia()
        {
            TestHandledWithSingle('"',
@"class C
{
    void M()
    {
        var v = // a
                /* b */ @$[||] // c
    }
}",
@"class C
{
    void M()
    {
        var v = // a
                /* b */ $@""[||] // c
    }
}");
        }
    }
}
