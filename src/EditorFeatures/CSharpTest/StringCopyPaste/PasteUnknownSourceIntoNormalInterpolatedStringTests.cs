// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public class PasteUnknownSourceIntoNormalInterpolatedStringTests
        : StringCopyPasteCommandHandlerUnknownSourceTests
    {
        #region Paste from external source into normal interpolated string no hole

        [WpfFact]
        public void TestNewLineIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: "\n",
                """
                var x = $"[||]"
                """,
                """
                var x = $"\n[||]"
                """,
                afterUndo: "var x = $\"\n[||]\"");
        }

        [WpfFact]
        public void TestNewLineIntoNormalInterpolatedString2()
        {
            TestPasteUnknownSource(
                pasteText: """


                """,
                """
                var x = $"[||]"
                """,
                """
                var x = $"\r\n[||]"
                """,
                afterUndo: """
                var x = $"
                [||]"
                """);
        }

        [WpfFact]
        public void TestTabIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: "\t",
                """
                var x = $"[||]"
                """,
                """
                var x = $"\t[||]"
                """,
                afterUndo: "var x = $\"\t[||]\"");
        }

        [WpfFact]
        public void TestSingleQuoteIntoNormalInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: """'""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"'[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        [WpfFact]
        public void TestDoubleQuoteIntoNormalInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: """
                "
                """,
                """
                var x = $"[||]"
                """,
                """
                var x = $"\"[||]"
                """,
                afterUndo: """
                var x = $""[||]"
                """);
        }

        [WpfFact]
        public void TestComplexStringIntoNormalInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: "\t\"\"\t",
                """
                var x = $"[||]"
                """,
                """
                var x = $"\t\"\"\t[||]"
                """,
                afterUndo: "var x = $\"\t\"\"\t[||]\"");
        }

        [WpfFact]
        public void TestNormalTextIntoNormalInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: """abc""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"abc[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        [WpfFact]
        public void TestOpenCurlyIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """{""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"{{[||]"
                """,
                afterUndo: """
                var x = $"{[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """{{""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"{{[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesAndContentIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """{{0""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"{{0[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        [WpfFact]
        public void TestCloseCurlyIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """}""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"}}[||]"
                """,
                afterUndo: """
                var x = $"}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """}}""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"}}[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesAndContentIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """}}0""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"}}0[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        [WpfFact]
        public void TestCurlyWithContentIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """x{0}y""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"x{0}y[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        [WpfFact]
        public void TestCurliesWithContentIntoNormalInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """x{{0}}y""",
                """
                var x = $"[||]"
                """,
                """
                var x = $"x{{0}}y[||]"
                """,
                afterUndo: """
                var x = $"[||]"
                """);
        }

        #endregion

        #region Paste from external source into normal interpolated string before hole

        [WpfFact]
        public void TestNewLineIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\n",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"\n[||]{0}"
                """,
                afterUndo: "var x = $\"\n[||]{0}\"");
        }

        [WpfFact]
        public void TestNewLineIntoNormalInterpolatedStringBeforeHole2()
        {
            TestPasteUnknownSource(
                pasteText: """


                """,
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"\r\n[||]{0}"
                """,
                afterUndo: """
                var x = $"
                [||]{0}"
                """);
        }

        [WpfFact]
        public void TestTabIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\t",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"\t[||]{0}"
                """,
                afterUndo: "var x = $\"\t[||]{0}\"");
        }

        [WpfFact]
        public void TestSingleQuoteIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: """'""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"'[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestDoubleQuoteIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: """
                "
                """,
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"\"[||]{0}"
                """,
                afterUndo: """
                var x = $""[||]{0}"
                """);
        }

        [WpfFact]
        public void TestComplexStringIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: "\t\"\"\t",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"\t\"\"\t[||]{0}"
                """,
                afterUndo: "var x = $\"\t\"\"\t[||]{0}\"");
        }

        [WpfFact]
        public void TestNormalTextIntoNormalInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: """abc""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"abc[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestOpenCurlyIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"{{[||]{0}"
                """,
                afterUndo: """
                var x = $"{[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"{{[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesAndContentIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{0""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"{{0[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestCloseCurlyIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"}}[||]{0}"
                """,
                afterUndo: """
                var x = $"}[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"}}[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesAndContentIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}0""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"}}0[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestCurlyWithContentIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{0}y""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"x{0}y[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestCurliesWithContentIntoNormalInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{{0}}y""",
                """
                var x = $"[||]{0}"
                """,
                """
                var x = $"x{{0}}y[||]{0}"
                """,
                afterUndo: """
                var x = $"[||]{0}"
                """);
        }

        #endregion

        #region Paste from external source into normal interpolated string after hole

        [WpfFact]
        public void TestNewLineIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\n",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}\n[||]"
                """,
                afterUndo: "var x = $\"{0}\n[||]\"");
        }

        [WpfFact]
        public void TestNewLineIntoNormalInterpolatedStringAfterHole2()
        {
            TestPasteUnknownSource(
                pasteText: """


                """,
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}\r\n[||]"
                """,
                afterUndo: """
                var x = $"{0}
                [||]"
                """);
        }

        [WpfFact]
        public void TestTabIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\t",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}\t[||]"
                """,
                afterUndo: "var x = $\"{0}\t[||]\"");
        }

        [WpfFact]
        public void TestSingleQuoteIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: """'""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}'[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestDoubleQuoteIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: """
                "
                """,
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}\"[||]"
                """,
                afterUndo: """
                var x = $"{0}"[||]"
                """);
        }

        [WpfFact]
        public void TestComplexStringIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: "\t\"\"\t",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}\t\"\"\t[||]"
                """,
                afterUndo: "var x = $\"{0}\t\"\"\t[||]\"");
        }

        [WpfFact]
        public void TestNormalTextIntoNormalInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: """abc""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}abc[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestOpenCurlyIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}{{[||]"
                """,
                afterUndo: """
                var x = $"{0}{[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}{{[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesAndContentIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{0""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}{{0[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestCloseCurlyIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}}}[||]"
                """,
                afterUndo: """
                var x = $"{0}}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}}}[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesAndContentIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}0""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}}}0[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestCurlyWithContentIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{0}y""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}x{0}y[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestCurliesWithContentIntoNormalInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{{0}}y""",
                """
                var x = $"{0}[||]"
                """,
                """
                var x = $"{0}x{{0}}y[||]"
                """,
                afterUndo: """
                var x = $"{0}[||]"
                """);
        }

        #endregion
    }
}
