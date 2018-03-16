using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitStringLiteral
{
    public class SplitStringLiteralCommandHandlerTests
    {
        private void TestWorker(
            string inputMarkup, string expectedOutputMarkup, Action callback)
        {
            using (var workspace = TestWorkspace.CreateCSharp(inputMarkup))
            {
                var document = workspace.Documents.Single();
                var view = document.GetTextView();

                var originalSnapshot = view.TextBuffer.CurrentSnapshot;
                var originalSelection = document.SelectedSpans.Single();
                view.SetSelection(originalSelection.ToSnapshotSpan(originalSnapshot));

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
                    Assert.Equal(expectedSpans.Single().Start, view.Caret.Position.BufferPosition.Position);

                    // Ensure that after undo we go back to where we were to begin with.
                    var history = undoHistoryRegistry.GetHistory(document.TextBuffer);
                    history.Undo(count: 1);

                    var currentSnapshot = document.TextBuffer.CurrentSnapshot;
                    Assert.Equal(originalSnapshot.GetText(), currentSnapshot.GetText());
                    Assert.Equal(originalSelection.Start, view.Caret.Position.BufferPosition.Position);
                }
            }
        }

        private void TestHandled(string inputMarkup, string expectedOutputMarkup)
        {
            TestWorker(
                inputMarkup, expectedOutputMarkup,
                callback: () =>
                {
                    Assert.True(false, "Should not reach here.");
                });
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
}");
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
}");
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote2()
        {
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
}");
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote3()
        {
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
}");
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote4()
        {
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
}");
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote5()
        {
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
}");
        }

        [WorkItem(20258, "https://github.com/dotnet/roslyn/issues/20258")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public void TestBeforeEndQuote6()
        {
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
}");
        }
    }
}
