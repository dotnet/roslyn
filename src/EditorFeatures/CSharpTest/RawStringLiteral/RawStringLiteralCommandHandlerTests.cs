// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RawStringLiteral;

[UseExportProvider]
public class RawStringLiteralCommandHandlerTests
{
    internal sealed class RawStringLiteralTestState : AbstractCommandHandlerTestState
    {
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeaturesWpf.AddParts(
            typeof(RawStringLiteralCommandHandler));

        private readonly RawStringLiteralCommandHandler _commandHandler;

        public RawStringLiteralTestState(XElement workspaceElement)
            : base(workspaceElement, s_composition)
        {
            _commandHandler = (RawStringLiteralCommandHandler)GetExportedValues<ICommandHandler>().
                Single(c => c is RawStringLiteralCommandHandler);
        }

        public static RawStringLiteralTestState CreateTestState(string markup, bool withSpansOnly = false)
            => new(GetWorkspaceXml(markup, withSpansOnly));

        public static XElement GetWorkspaceXml(string markup, bool withSpansOnly)
        {
            var spansOnlyMarkup = withSpansOnly ? """Markup="SpansOnly" """ : "";
            return XElement.Parse($"""
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document {spansOnlyMarkup}>{markup}</Document>
    </Project>
</Workspace>
""");
        }

        internal void AssertCodeIs(string expectedCode, bool withSpansOnly = false)
        {
            if (withSpansOnly)
                expectedCode = expectedCode.Replace("$", "\uD7FF");

            MarkupTestFile.GetPositionAndSpans(expectedCode, out var massaged, out int? caretPosition, out var spans);

            if (withSpansOnly)
            {
                Assert.Null(caretPosition);
                massaged = massaged.Replace("\uD7FF", "$");
            }

            Assert.Equal(massaged, TextView.TextSnapshot.GetText());

            if (!withSpansOnly)
                Assert.Equal(caretPosition!.Value, TextView.Caret.Position.BufferPosition.Position);

            var virtualSpaces = spans.SingleOrDefault(kvp => kvp.Key.StartsWith("VirtualSpaces#"));
            if (virtualSpaces.Key != null)
            {
                var virtualOffset = int.Parse(virtualSpaces.Key["VirtualSpaces-".Length..]);
                Assert.True(TextView.Caret.InVirtualSpace);
                Assert.Equal(virtualOffset, TextView.Caret.Position.VirtualBufferPosition.VirtualSpaces);
            }
        }

        public void SendTypeChar(char ch)
            => SendTypeChar(ch, _commandHandler.ExecuteCommand, () => EditorOperations.InsertText(ch.ToString()));

        public void SendReturn(bool handled)
            => SendReturn(_commandHandler.ExecuteCommand, () =>
            {
                Assert.False(handled, "Return key should have been handled");
            });
    }

    #region enter tests

    [WpfFact]
    public void TestReturnInSixQuotes()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = """$$"""
            """");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = """
            $${|VirtualSpaces-4:|}
                """
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesWithSemicolonAfter()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""$$"""""";");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = """
            $${|VirtualSpaces-4:|}
                """;
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesNotAtMiddle()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """""
            var v = """"$$""
            """"");

        testState.SendReturn(handled: false);
        testState.AssertCodeIs(
            """""
            var v = """"$$""
            """"");
    }

    [WpfFact]
    public void TestReturnInSixQuotes_Interpolated()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = $"""$$"""
            """");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = $"""
            $${|VirtualSpaces-4:|}
                """
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesMoreQuotesLaterOn()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = """$$""";
            Console.WriteLine("Goo");
            """");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = """
            $${|VirtualSpaces-4:|}
                """;
            Console.WriteLine("Goo");
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesAsArgument1()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = WriteLine("""$$"""
            """");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = WriteLine("""
            $${|VirtualSpaces-4:|}
                """
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesAsArgument2()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = WriteLine(""""""$$"""""")");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = WriteLine("""
            $${|VirtualSpaces-4:|}
                """)
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesAsArgument3()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = WriteLine(""""""$$"""""");");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = WriteLine("""
            $${|VirtualSpaces-4:|}
                """);
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesAsArgument4()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = WriteLine(
                """$$"""
            """");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = WriteLine(
                """
            $${|VirtualSpaces-4:|}
                """
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesAsArgument5()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = WriteLine(
                """$$""")
            """");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = WriteLine(
                """
            $${|VirtualSpaces-4:|}
                """)
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesAsArgument6()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = WriteLine(
                """$$""");
            """");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = WriteLine(
                """
            $${|VirtualSpaces-4:|}
                """);
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesWithSemicolonAfter_Interpolated()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""""$$"""""";");

        testState.SendReturn(handled: true);
        testState.AssertCodeIs(
            """"
            var v = $"""
            $${|VirtualSpaces-4:|}
                """;
            """");
    }

    [WpfFact]
    public void TestReturnInSixQuotesNotAtMiddle_Interpolated()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """""
            var v = $""""$$""
            """"");

        testState.SendReturn(handled: false);
        testState.AssertCodeIs(
            """""
            var v = $""""$$""
            """"");
    }

    [WpfFact]
    public void TestReturnEndOfFile()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""$$");

        testState.SendReturn(handled: false);
    }

    [WpfFact]
    public void TestReturnInEmptyFile()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"$$");

        testState.SendReturn(handled: false);
    }

    #endregion

    #region generate initial empty raw string

    [WpfFact]
    public void TestGenerateAtEndOfFile()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"$$");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """"
            var v = """$$"""
            """");
    }

    [WpfFact]
    public void TestGenerateWithSemicolonAfter()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"$$;");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
@"var v = """"""$$"""""";");
    }

    [WpfFact]
    public void TestGenerateWithInterpolatedString()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""$$");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """"
            var v = $"""$$"""
            """");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66538")]
    public void TestGenerateWithInterpolatedString_TwoDollarSigns()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
"""var v = $$""[||]""", withSpansOnly: true);

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
""""
var v = $$"""[||]"""
"""", withSpansOnly: true);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66538")]
    public void TestGenerateWithInterpolatedString_TwoDollarSigns_FourthDoubleQuote()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
""""var v = $$"""[||]""";"""", withSpansOnly: true);

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
"""""var v = $$""""[||]"""";""""", withSpansOnly: true);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/66538")]
    public void TestGenerateWithInterpolatedString_ThreeDollarSigns()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
"""var v = $$$""[||]""", withSpansOnly: true);

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
""""
var v = $$$"""[||]"""
"""", withSpansOnly: true);
    }

    [WpfFact]
    public void TestNoGenerateWithVerbatimString()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = @""""$$");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
@"var v = @""""""$$");
    }

    [WpfFact]
    public void TestNoGenerateWithVerbatimInterpolatedString1()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = @$""""$$");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
@"var v = @$""""""$$");
    }

    [WpfFact]
    public void TestNoGenerateWithVerbatimInterpolatedString2()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $@""""$$");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
@"var v = $@""""""$$");
    }

    #endregion

    #region grow empty raw string

    [WpfFact]
    public void TestDoNotGrowEmptyInsideSimpleString()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """
            var v = "$$"
            """);

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """
            var v = ""$$"
            """);
    }

    [WpfFact]
    public void TestDoNotGrowEmptyInsideFourQuotes()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """
            var v = ""$$""
            """);

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """"
            var v = """$$""
            """");
    }

    [WpfFact]
    public void TestDoGrowEmptyInsideSixQuotesInMiddle()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = """$$"""
            """");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """""
            var v = """"$$""""
            """"");
    }

    [WpfFact]
    public void TestDoGrowEmptyInsideSixQuotesInInterpolatedRaw()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = $"""$$"""
            """");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """""
            var v = $""""$$""""
            """"");
    }

    [WpfFact]
    public void TestDoNotGrowEmptyInsideSixQuotesWhenNotInMiddle1()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """""
            var v = $""$$""""
            """"");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """""
            var v = $"""$$""""
            """"");
    }

    #endregion

    #region grow delimiters

    [WpfFact]
    public void TestGrowDelimetersWhenEndExists_SingleLine()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = """$$ """
            """");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """""
            var v = """"$$ """"
            """"");
    }

    [WpfFact]
    public void TestGrowDelimetersWhenEndExists_MultiLine()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = """$$

                """
            """");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """""
            var v = """"$$

                """"
            """"");
    }

    [WpfFact]
    public void TestGrowDelimetersWhenEndExists_Interpolated()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = $"""$$

                """
            """");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """""
            var v = $""""$$

                """"
            """"");
    }

    [WpfFact]
    public void TestDoNotGrowDelimetersWhenEndNotThere()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""$$");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
@"var v = """"""""$$");
    }

    [WpfFact]
    public void TestDoNotGrowDelimetersWhenEndTooShort()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
            """"
            var v = """$$

                ""
            """");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """""
            var v = """"$$

                ""
            """"");
    }

    #endregion

    [WpfFact]
    public void TestTypeQuoteEmptyFile()
    {
        using var testState = RawStringLiteralTestState.CreateTestState(
@"$$");

        testState.SendTypeChar('"');
        testState.AssertCodeIs(
            """
            "$$
            """);
    }
}
