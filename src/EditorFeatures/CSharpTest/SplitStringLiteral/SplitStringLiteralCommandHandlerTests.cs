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
        private async Task TestWorkerAsync(
            string inputMarkup, string expectedOutputMarkup, Action callback)
        {
            using (var workspace = await TestWorkspace.CreateCSharpAsync(inputMarkup))
            {
                var document = workspace.Documents.Single();
                var view = document.GetTextView();

                var snapshot = view.TextBuffer.CurrentSnapshot;
                view.SetSelection(document.SelectedSpans.Single().ToSnapshotSpan(snapshot));

                var commandHandler = new SplitStringLiteralCommandHandler();
                commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(view, view.TextBuffer), callback);

                if (expectedOutputMarkup != null)
                {
                    string expectedOutput;
                    IList<TextSpan> expectedSpans;
                    MarkupTestFile.GetSpans(expectedOutputMarkup, out expectedOutput, out expectedSpans);

                    Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.AsText().ToString());
                    Assert.Equal(expectedSpans.Single().Start, view.Caret.Position.BufferPosition.Position);
                }
            }
        }

        private Task TestHandledAsync(string inputMarkup, string expectedOutputMarkup)
        {
            return TestWorkerAsync(
                inputMarkup, expectedOutputMarkup,
                callback: () =>
                {
                    Assert.True(false, "Should not reach here.");
                });
        }

        private async Task TestNotHandledAsync(string inputMarkup)
        {
            var notHandled = false;
            await TestWorkerAsync(
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
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = [||]"""";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingBeforeInterpolatedString()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = [||]$"""";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterString()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = """"[||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingAfterInterpolatedString()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = $""""[||];
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingInVerbatimString()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = @""a[||]b"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingInInterpolatedVerbatimString()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = $@""a[||]b"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestInEmptyString()
        {
            await TestHandledAsync(
@"class C {
    void M() {
        var v = ""[||]"";
    }
}",
@"class C {
    void M() {
        var v = """" +
            ""[||]"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestInEmptyInterpolatedString()
        {
            await TestHandledAsync(
@"class C {
    void M() {
        var v = $""[||]"";
    }
}",
@"class C {
    void M() {
        var v = $"""" +
            $""[||]"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestSimpleString1()
        {
            await TestHandledAsync(
@"class C {
    void M() {
        var v = ""now is [||]the time"";
    }
}",
@"class C {
    void M() {
        var v = ""now is "" +
            ""[||]the time"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestInterpolatedString1()
        {
            await TestHandledAsync(
@"class C {
    void M() {
        var v = $""now is [||]the { 1 + 2 } time for { 3 + 4 } all good men"";
    }
}",
@"class C {
    void M() {
        var v = $""now is "" +
            $""[||]the { 1 + 2 } time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestInterpolatedString2()
        {
            await TestHandledAsync(
@"class C {
    void M() {
        var v = $""now is the [||]{ 1 + 2 } time for { 3 + 4 } all good men"";
    }
}",
@"class C {
    void M() {
        var v = $""now is the "" +
            $""[||]{ 1 + 2 } time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestInterpolatedString3()
        {
            await TestHandledAsync(
@"class C {
    void M() {
        var v = $""now is the { 1 + 2 }[||] time for { 3 + 4 } all good men"";
    }
}",
@"class C {
    void M() {
        var v = $""now is the { 1 + 2 }"" +
            $""[||] time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingInInterpolation1()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = $""now is the {[||] 1 + 2 } time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestMissingInInterpolation2()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = $""now is the { 1 + 2 [||]} time for { 3 + 4 } all good men"";
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
        public async Task TestSelection()
        {
            await TestNotHandledAsync(
@"class C {
    void M() {
        var v = ""now is [|the|] time"";
    }
}");
        }
    }
}