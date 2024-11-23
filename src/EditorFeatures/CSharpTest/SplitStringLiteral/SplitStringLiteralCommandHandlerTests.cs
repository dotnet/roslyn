// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;
using IndentStyle = Microsoft.CodeAnalysis.Formatting.FormattingOptions2.IndentStyle;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitStringLiteral;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.SplitStringLiteral)]
public class SplitStringLiteralCommandHandlerTests
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
        IndentStyle indentStyle = IndentStyle.Smart,
        bool useTabs = false,
        string? endOfLine = null)
    {
        var workspaceXml = $"""
            <Workspace>
                <Project Language="C#">
                    <Document Normalize="{endOfLine is null}">{(endOfLine is null ? inputMarkup : inputMarkup.ReplaceLineEndings(endOfLine))}</Document>
                </Project>
            </Workspace>
            """;

        using var workspace = EditorTestWorkspace.Create(workspaceXml);

        if (useTabs && expectedOutputMarkup != null)
        {
            Assert.Contains("\t", expectedOutputMarkup);
        }

        var editorOptionsFactory = workspace.GetService<IEditorOptionsFactoryService>();

        var document = workspace.Documents.Single();
        var view = document.GetTextView();
        var textBuffer = view.TextBuffer;
        var options = editorOptionsFactory.GetOptions(textBuffer);

        options.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !useTabs);
        options.SetOptionValue(DefaultOptions.TabSizeOptionId, 4);
        options.SetOptionValue(DefaultOptions.IndentStyleId, indentStyle.ToEditorIndentStyle());
        if (endOfLine != null)
            options.SetOptionValue(DefaultOptions.NewLineCharacterOptionId, endOfLine);

        // Remove once https://github.com/dotnet/roslyn/issues/62204 is fixed:
        workspace.GlobalOptions.SetGlobalOption(IndentationOptionsStorage.SmartIndent, document.Project.Language, indentStyle);

        var originalSnapshot = textBuffer.CurrentSnapshot;
        var originalSelections = document.SelectedSpans;

        // primary caret will be the last one:
        view.SetMultiSelection(originalSelections.Select(selection => selection.ToSnapshotSpan(originalSnapshot)));

        // only validate when there is no selected text since the splitter is disabled in that case:
        if (originalSelections.All(selection => selection.IsEmpty))
        {
            Assert.Equal(originalSelections.Last().Start, view.Caret.Position.BufferPosition.Position);
        }

        var undoHistoryRegistry = workspace.GetService<ITextUndoHistoryRegistry>();
        var commandHandler = workspace.ExportProvider.GetCommandHandler<SplitStringLiteralCommandHandler>(nameof(SplitStringLiteralCommandHandler));

        if (!commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(view, textBuffer), TestCommandExecutionContext.Create()))
        {
            callback();
        }

        if (expectedOutputMarkup != null)
        {
            MarkupTestFile.GetSpans(expectedOutputMarkup, out var expectedOutput, out var expectedSpans);

            Assert.Equal(expectedOutput, textBuffer.CurrentSnapshot.AsText().ToString());
            Assert.Equal(expectedSpans.Last().Start, view.Caret.Position.BufferPosition.Position);

            if (verifyUndo)
            {
                // Ensure that after undo we go back to where we were to begin with.
                var history = undoHistoryRegistry.GetHistory(document.GetTextBuffer());
                history.Undo(count: originalSelections.Count);

                var currentSnapshot = document.GetTextBuffer().CurrentSnapshot;
                Assert.Equal(originalSnapshot.GetText(), currentSnapshot.GetText());
                Assert.Equal(originalSelections.Last().Start, view.Caret.Position.BufferPosition.Position);
            }
        }
    }

    /// <summary>
    /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
    /// Most tests will continue to verifyUndo, but select tests will skip it due to
    /// this known test infrastructure issure. This bug does not represent a product
    /// failure.
    /// </summary>
    private static void TestHandled(
        string inputMarkup,
        string expectedOutputMarkup,
        bool verifyUndo = true,
        IndentStyle indentStyle = IndentStyle.Smart,
        bool useTabs = false,
        string? endOfLine = null)
    {
        TestWorker(
            inputMarkup, expectedOutputMarkup,
            callback: () =>
            {
                Assert.True(false, "Should not reach here.");
            },
            verifyUndo, indentStyle, useTabs, endOfLine);
    }

    private static void TestNotHandled(string inputMarkup)
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

    [WpfFact]
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

    [WpfFact]
    public void TestMissingBeforeUtf8String()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = [||]""""u8;
    }
}");
    }

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
    public void TestMissingAfterUtf8String_1()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"[||]u8;
    }
}");
    }

    [WpfFact]
    public void TestMissingAfterUtf8String_2()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"u8[||];
    }
}");
    }

    [WpfFact]
    public void TestMissingAfterUtf8String_3()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"u8[||]
    }
}");
    }

    [WpfFact]
    public void TestMissingAfterUtf8String_4()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = $""""u8 [||]
    }
}");
    }

    [WpfFact]
    public void TestMissingAfterUtf8String_5()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"u[||]8;
    }
}");
    }

    [WpfFact]
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

    [WpfFact]
    public void TestMissingInUtf8VerbatimString()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = @""a[||]b""u8;
    }
}");
    }

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/41322")]
    public void TestInEmptyString_LF()
    {
        // Do not verifyUndo because of https://github.com/dotnet/roslyn/issues/28033
        // When that issue is fixed, we can reenable verifyUndo
        TestHandled(
"class C\n{\n    void M()\n    {\n        var v = \"[||]\";\n    }\n}",
"class C\n{\n    void M()\n    {\n        var v = \"\" +\n            \"[||]\";\n    }\n}",
        verifyUndo: false,
        endOfLine: "\n");
    }

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/41322")]
    public void TestInEmptyInterpolatedString_LF()
    {
        TestHandled(
"class C\n{\n    void M()\n    {\n        var v = $\"[||]\";\n    }\n}",
"class C\n{\n    void M()\n    {\n        var v = $\"\" +\n            $\"[||]\";\n    }\n}",
endOfLine: "\n");
    }

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
    public void TestUtf8String_1()
    {
        TestHandled(
@"class C
{
    void M()
    {
        var v = ""now is [||]the time""u8;
    }
}",
@"class C
{
    void M()
    {
        var v = ""now is ""u8 +
            ""[||]the time""u8;
    }
}");
    }

    [WpfFact]
    public void TestUtf8String_2()
    {
        TestHandled(
@"class C
{
    void M()
    {
        var v = ""now is [||]the time""U8;
    }
}",
@"class C
{
    void M()
    {
        var v = ""now is ""U8 +
            ""[||]the time""U8;
    }
}");
    }

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/20258")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/20258")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/20258")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/20258")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/20258")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/20258")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39040")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39040")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/39040")]
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

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/40277")]
    public void TestInStringWithKeepTabsEnabled1()
    {
        TestHandled(
@"class C
{
	void M()
	{
		var s = ""Hello [||]world"";
	}
}",
@"class C
{
	void M()
	{
		var s = ""Hello "" +
			""[||]world"";
	}
}",
        useTabs: true);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/40277")]
    public void TestInStringWithKeepTabsEnabled2()
    {
        TestHandled(
@"class C
{
	void M()
	{
		var s = ""Hello "" +
			""there [||]world"";
	}
}",
@"class C
{
	void M()
	{
		var s = ""Hello "" +
			""there "" +
			""[||]world"";
	}
}",
        useTabs: true);
    }

    [WpfFact]
    public void TestMissingInRawStringLiteral()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"""Hello[||]there
world
"""""";
    }
}");
    }

    [WpfFact]
    public void TestMissingInRawStringLiteralInterpolation()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = $""""""Hello[||]there
world
"""""";
    }
}");
    }

    [WpfFact]
    public void TestMissingInRawStringLiteralInterpolation_MultiBrace()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = ${|#0:|}$""""""Hello[||]there
world
"""""";
    }
}");
    }

    [WpfFact]
    public void TestMissingInRawUtf8StringLiteral()
    {
        TestNotHandled(
@"class C
{
    void M()
    {
        var v = """"""Hello[||]there
world
""""""u8;
    }
}");
    }
}
