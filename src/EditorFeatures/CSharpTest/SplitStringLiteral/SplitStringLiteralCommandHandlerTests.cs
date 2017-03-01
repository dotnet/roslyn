using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using System.Collections.Generic;

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

                var snapshot = view.TextBuffer.CurrentSnapshot;
                view.SetSelection(document.SelectedSpans.Single().ToSnapshotSpan(snapshot));

                var commandHandler = new SplitStringLiteralCommandHandler();
                commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(view, view.TextBuffer), callback);

                if (expectedOutputMarkup != null)
                {
                    MarkupTestFile.GetSpans(expectedOutputMarkup, out var expectedOutput, out IList<TextSpan> expectedSpans);

                    Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.AsText().ToString());
                    Assert.Equal(expectedSpans.Single().Start, view.Caret.Position.BufferPosition.Position);
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
        public async Task TestMissingBeforeString()
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
        public async Task TestMissingBeforeInterpolatedString()
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
        public async Task TestMissingAfterString_1()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = """"[||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterString_2()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = """" [||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterString_3()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = """"[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterString_4()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = """" [||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterInterpolatedString_1()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = $""""[||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterInterpolatedString_2()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = $"""" [||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterInterpolatedString_3()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = $""""[||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterInterpolatedString_4()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = $"""" [||]
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingInVerbatimString()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = @""a[||]b"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingInInterpolatedVerbatimString()
        {
            await TestNotHandledAsync(
@"class C
{
    void M()
    {
        var v = $@""a[||]b"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestInEmptyString()
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
        public async Task TestInEmptyInterpolatedString()
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
        public async Task TestSimpleString1()
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
        public async Task TestInterpolatedString1()
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
        public async Task TestInterpolatedString2()
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
        public async Task TestInterpolatedString3()
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
        public async Task TestMissingInInterpolation1()
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
        public async Task TestMissingInInterpolation2()
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
        public async Task TestSelection()
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
    }
}