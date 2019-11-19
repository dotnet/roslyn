// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitStringLiteral
{
    [UseExportProvider]
    public class SplitStringLiteralCommandHandlerTests
    {
        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issure. This bug does not represent a product
        /// failure.
        /// </summary>
        private void TestWorker(
            string inputMarkup,
            string expectedOutputMarkup,
            Action callback,
            bool verifyUndo = true,
            IndentStyle indentStyle = IndentStyle.Smart)
        {
            using var workspace = TestWorkspace.CreateCSharp(inputMarkup);
            workspace.Options = workspace.Options.WithChangedOption(SmartIndent, LanguageNames.CSharp, indentStyle);

            var document = workspace.Documents.Single();
            var view = document.GetTextView();

            var originalSnapshot = view.TextBuffer.CurrentSnapshot;
            var originalSelections = document.SelectedSpans;

            var snapshotSpans = new List<SnapshotSpan>();
            foreach (var selection in originalSelections)
            {
                snapshotSpans.Add(selection.ToSnapshotSpan(originalSnapshot));
            }
            view.SetMultiSelection(snapshotSpans);

            var undoHistoryRegistry = workspace.GetService<ITextUndoHistoryRegistry>();
            var commandHandler = new SplitStringLiteralCommandHandler(
                undoHistoryRegistry,
                workspace.GetService<IEditorOperationsFactoryService>());

            if (!commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(view, view.TextBuffer), TestCommandExecutionContext.Create()))
            {
                callback();
            }

            if (expectedOutputMarkup != null)
            {
                MarkupTestFile.GetSpans(expectedOutputMarkup,
                    out var expectedOutput, out ImmutableArray<TextSpan> expectedSpans);

                Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.AsText().ToString());
                Assert.Equal(expectedSpans.First().Start, view.Caret.Position.BufferPosition.Position);

                if (verifyUndo)
                {
                    // Ensure that after undo we go back to where we were to begin with.
                    var history = undoHistoryRegistry.GetHistory(document.GetTextBuffer());
                    history.Undo(count: originalSelections.Count);

                    var currentSnapshot = document.GetTextBuffer().CurrentSnapshot;
                    Assert.Equal(originalSnapshot.GetText(), currentSnapshot.GetText());
                    Assert.Equal(originalSelections.First().Start, view.Caret.Position.BufferPosition.Position);
                }
            }
        }

        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issure. This bug does not represent a product
        /// failure.
        /// </summary>
        private void TestHandled(
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

        private void TestNotHandled(string inputMarkup)
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingBeforeString()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = [||]"""";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingBeforeInterpolatedString()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = [||]$"""";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterString_1()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"[||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterString_2()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = """" [||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterString_3()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterString_4()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = """" [||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterInterpolatedString_1()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = $""""[||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterInterpolatedString_2()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = $"""" [||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterInterpolatedString_3()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = $""""[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingAfterInterpolatedString_4()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = $"""" [||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingInVerbatimString()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = @""a[||]b"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingInInterpolatedVerbatimString()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = $@""a[||]b"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInEmptyString()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class C
{
    void M()
    {
        var v = ""[||]"";
    }
}",
@"class C
{
    void M()
    {
        var v = """" +
            ""[||]"";
    }
}",
            verifyUndo: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInEmptyString_BlockIndent()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class C
{
    void M()
    {
        var v = ""[||]"";
    }
}",
@"class C
{
    void M()
    {
        var v = """" +
        ""[||]"";
    }
}",
            verifyUndo: false,
            IndentStyle.Block);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInEmptyString_NoneIndent()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class C
{
    void M()
    {
        var v = ""[||]"";
    }
}",
@"class C
{
    void M()
    {
        var v = """" +
""[||]"";
    }
}",
            verifyUndo: false,
            IndentStyle.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInEmptyInterpolatedString()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = $""[||]"";
    }
}",
@"class C
{
    void M()
    {
        var v = $"""" +
            $""[||]"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInEmptyInterpolatedString_BlockIndent()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = $""[||]"";
    }
}",
@"class C
{
    void M()
    {
        var v = $"""" +
        $""[||]"";
    }
}", indentStyle: IndentStyle.Block);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInEmptyInterpolatedString_NoneIndent()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = $""[||]"";
    }
}",
@"class C
{
    void M()
    {
        var v = $"""" +
$""[||]"";
    }
}", indentStyle: IndentStyle.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestSimpleString1()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = ""now is [||]the time"";
    }
}",
@"class C
{
    void M()
    {
        var v = ""now is "" +
            ""[||]the time"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInterpolatedString1()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = $""now is [||]the { 1 + 2 } time for { 3 + 4 } all good men"";
    }
}",
@"class C
{
    void M()
    {
        var v = $""now is "" +
            $""[||]the { 1 + 2 } time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInterpolatedString2()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = $""now is the [||]{ 1 + 2 } time for { 3 + 4 } all good men"";
    }
}",
@"class C
{
    void M()
    {
        var v = $""now is the "" +
            $""[||]{ 1 + 2 } time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestInterpolatedString3()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = $""now is the { 1 + 2 }[||] time for { 3 + 4 } all good men"";
    }
}",
@"class C
{
    void M()
    {
        var v = $""now is the { 1 + 2 }"" +
            $""[||] time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingInInterpolation1()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = $""now is the {[||] 1 + 2 } time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMissingInInterpolation2()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = $""now is the { 1 + 2 [||]} time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestSelection()
        {
            TestNotHandled(
@"class C
{
    void M()
    {
        var v = ""now is [|the|] time"";
    }
}");
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote1()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}[||]"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3"";
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""[||]"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3"";
    }
}",
            verifyUndo: false);
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote2()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}[||]"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3"";
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""[||]"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3"";
    }
}",
            verifyUndo: false);
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote3()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}[||]"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3"";
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}"" +
            $""[||]"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3"";
    }
}",
            verifyUndo: false);
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote4()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1[||]"" +
            ""string2"" +
            ""string3"";
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""[||]"" +
            ""string2"" +
            ""string3"";
    }
}",
            verifyUndo: false);
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote5()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2[||]"" +
            ""string3"";
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2"" +
            ""[||]"" +
            ""string3"";
    }
}",
            verifyUndo: false);
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote6()
        {
            // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
            // When that issue is fixed, we can reenable verifyUndo
            TestHandled(
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3[||]"";
    }
}",
@"class Program
{
    static void Main(string[] args)
    {
        var str = $""somestring { args[0]}"" +
            $""{args[1]}"" +
            $""{args[2]}"";

        var str2 = ""string1"" +
            ""string2"" +
            ""string3"" +
            ""[||]"";
    }
}",
            verifyUndo: false);
        }

        [WorkItem(39040, "https://github.com/dotnet/roslyn/issues/39040")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMultiCaretSingleLine()
        {
            TestHandled(
@"class C
{
    void M()
    {
        var v = ""now is [||]the ti[||]me"";
    }
}",
@"class C
{
    void M()
    {
        var v = ""now is "" +
            ""[||]the ti"" +
            ""[||]me"";
    }
}");
        }

        [WorkItem(39040, "https://github.com/dotnet/roslyn/issues/39040")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMultiCaretMultiLines()
        {
            TestHandled(
@"class C
{
    string s = ""hello w[||]orld"";

    void M()
    {
        var v = ""now is [||]the ti[||]me"";
    }
}",
@"class C
{
    string s = ""hello w"" +
        ""[||]orld"";

    void M()
    {
        var v = ""now is "" +
            ""[||]the ti"" +
            ""[||]me"";
    }
}");
        }

        [WorkItem(39040, "https://github.com/dotnet/roslyn/issues/39040")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestMultiCaretInterpolatedString()
        {
            TestHandled(
@"class C
{
    string s = ""hello w[||]orld"";

    void M()
    {
        var location = ""world"";
        var s = $""H[||]ello {location}!"";
    }
}",
@"class C
{
    string s = ""hello w"" +
        ""[||]orld"";

    void M()
    {
        var location = ""world"";
        var s = $""H"" +
            $""[||]ello {location}!"";
    }
}");
        }
    }
}
