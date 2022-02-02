﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.CSharp.RawStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RawStringLiteral
{
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

            public static RawStringLiteralTestState CreateTestState(string markup)
                => new(GetWorkspaceXml(markup));

            public static XElement GetWorkspaceXml(string markup)
                => XElement.Parse(string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferences=""true"">
        <Document>{0}</Document>
    </Project>
</Workspace>", markup));

            internal void AssertCodeIs(string expectedCode)
            {
                MarkupTestFile.GetPositionAndSpans(expectedCode, out var massaged, out int? caretPosition, out var spans);
                Assert.Equal(massaged, TextView.TextSnapshot.GetText());
                Assert.Equal(caretPosition!.Value, TextView.Caret.Position.BufferPosition.Position);

                var virtualSpaces = spans.SingleOrDefault(kvp => kvp.Key.StartsWith("VirtualSpaces#"));
                if (virtualSpaces.Key != null)
                {
                    var virtualOffset = int.Parse(virtualSpaces.Key.Substring("VirtualSpaces-".Length));
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
@"var v = """"""$$""""""");

            testState.SendReturn(handled: true);
            testState.AssertCodeIs(
@"var v = """"""
$${|VirtualSpaces-4:|}
    """"""");
        }

        [WpfFact]
        public void TestReturnInSixQuotesWithSemicolonAfter()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""$$"""""";");

            testState.SendReturn(handled: true);
            testState.AssertCodeIs(
@"var v = """"""
$${|VirtualSpaces-4:|}
    """""";");
        }

        [WpfFact]
        public void TestReturnInSixQuotesNotAtMiddle()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""""$$""""");

            testState.SendReturn(handled: false);
            testState.AssertCodeIs(
@"var v = """"""""$$""""");
        }

        [WpfFact]
        public void TestReturnInSixQuotes_Interpolated()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""""$$""""""");

            testState.SendReturn(handled: true);
            testState.AssertCodeIs(
@"var v = $""""""
$${|VirtualSpaces-4:|}
    """"""");
        }

        [WpfFact]
        public void TestReturnInSixQuotesWithSemicolonAfter_Interpolated()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""""$$"""""";");

            testState.SendReturn(handled: true);
            testState.AssertCodeIs(
@"var v = $""""""
$${|VirtualSpaces-4:|}
    """""";");
        }

        [WpfFact]
        public void TestReturnInSixQuotesNotAtMiddle_Interpolated()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""""""$$""""");

            testState.SendReturn(handled: false);
            testState.AssertCodeIs(
@"var v = $""""""""$$""""");
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
@"var v = """"""$$""""""");
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
@"var v = $""""""$$""""""");
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
@"var v = ""$$""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = """"$$""");
        }

        [WpfFact]
        public void TestDoNotGrowEmptyInsideFourQuotes()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"$$""""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = """"""$$""""");
        }

        [WpfFact]
        public void TestDoGrowEmptyInsideSixQuotesInMiddle()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""$$""""""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = """"""""$$""""""""");
        }

        [WpfFact]
        public void TestDoGrowEmptyInsideSixQuotesInInterpolatedRaw()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""""$$""""""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = $""""""""$$""""""""");
        }

        [WpfFact]
        public void TestDoNotGrowEmptyInsideSixQuotesWhenNotInMiddle1()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""$$""""""""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = $""""""$$""""""""");
        }

        #endregion

        #region grow delimiters

        [WpfFact]
        public void TestGrowDelimetersWhenEndExists_SingleLine()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""$$ """"""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = """"""""$$ """"""""");
        }

        [WpfFact]
        public void TestGrowDelimetersWhenEndExists_MultiLine()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = """"""$$

    """"""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = """"""""$$

    """"""""");
        }

        [WpfFact]
        public void TestGrowDelimetersWhenEndExists_Interpolated()
        {
            using var testState = RawStringLiteralTestState.CreateTestState(
@"var v = $""""""$$

    """"""");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = $""""""""$$

    """"""""");
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
@"var v = """"""$$

    """"");

            testState.SendTypeChar('"');
            testState.AssertCodeIs(
@"var v = """"""""$$

    """"");
        }

        #endregion
    }
}
