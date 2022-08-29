// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public class PasteUnknownSourceIntoVerbatimInterpolatedStringTests
        : StringCopyPasteCommandHandlerUnknownSourceTests
    {
        #region Paste from external source into verbatim interpolated string no hole

        [WpfFact]
        public void TestNewLineIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: "\n",
                """
                var x = $@"[||]"
                """,
                "var x = $@\"\n[||]\"",
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestNewLineIntoVerbatimInterpolatedString2()
        {
            TestPasteUnknownSource(
                pasteText: """


                """,
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"
                [||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestTabIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: "\t",
                """
                var x = $@"[||]"
                """,
                "var x = $@\"\t[||]\"",
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestSingleQuoteIntoVerbatimInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: """'""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"'[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestDoubleQuoteIntoVerbatimInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: """
                "
                """,
                """
                var x = $@"[||]"
                """,
                """"
                var x = $@"""[||]"
                """",
                afterUndo: """
                var x = $@""[||]"
                """);
        }

        [WpfFact]
        public void TestComplexStringIntoVerbatimInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: "\t\"\"\t",
                """
                var x = $@"[||]"
                """,
                "var x = $@\"\t\"\"\t[||]\"",
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestVerbatimTextIntoVerbatimInterpolatedString()
        {
            TestPasteUnknownSource(
                pasteText: """abc""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"abc[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestOpenCurlyIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """{""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"{{[||]"
                """,
                afterUndo: """
                var x = $@"{[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """{{""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"{{[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesAndContentIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """{{0""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"{{0[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestCloseCurlyIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """}""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"}}[||]"
                """,
                afterUndo: """
                var x = $@"}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """}}""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"}}[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesAndContentIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """}}0""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"}}0[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestCurlyWithContentIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """x{0}y""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"x{0}y[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        [WpfFact]
        public void TestCurliesWithContentIntoVerbatimInterpolatedString1()
        {
            TestPasteUnknownSource(
                pasteText: """x{{0}}y""",
                """
                var x = $@"[||]"
                """,
                """
                var x = $@"x{{0}}y[||]"
                """,
                afterUndo: """
                var x = $@"[||]"
                """);
        }

        #endregion

        #region Paste from external source into verbatim interpolated string before hole

        [WpfFact]
        public void TestNewLineIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\n",
                """
                var x = $@"[||]{0}"
                """,
                "var x = $@\"\n[||]{0}\"",
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestNewLineIntoVerbatimInterpolatedStringBeforeHole2()
        {
            TestPasteUnknownSource(
                pasteText: """


                """,
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"
                [||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTabIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\t",
                """
                var x = $@"[||]{0}"
                """,
                "var x = $@\"\t[||]{0}\"",
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestSingleQuoteIntoVerbatimInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: """'""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"'[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestDoubleQuoteIntoVerbatimInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: """
                "
                """,
                """
                var x = $@"[||]{0}"
                """,
                """"
                var x = $@"""[||]{0}"
                """",
                afterUndo: """
                var x = $@""[||]{0}"
                """);
        }

        [WpfFact]
        public void TestComplexStringIntoVerbatimInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: "\t\"\"\t",
                """
                var x = $@"[||]{0}"
                """,
                "var x = $@\"\t\"\"\t[||]{0}\"",
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestVerbatimTextIntoVerbatimInterpolatedStringBeforeHole()
        {
            TestPasteUnknownSource(
                pasteText: """abc""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"abc[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestOpenCurlyIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"{{[||]{0}"
                """,
                afterUndo: """
                var x = $@"{[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"{{[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesAndContentIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{0""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"{{0[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestCloseCurlyIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"}}[||]{0}"
                """,
                afterUndo: """
                var x = $@"}[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"}}[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesAndContentIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}0""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"}}0[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestCurlyWithContentIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{0}y""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"x{0}y[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        [WpfFact]
        public void TestCurliesWithContentIntoVerbatimInterpolatedStringBeforeHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{{0}}y""",
                """
                var x = $@"[||]{0}"
                """,
                """
                var x = $@"x{{0}}y[||]{0}"
                """,
                afterUndo: """
                var x = $@"[||]{0}"
                """);
        }

        #endregion

        #region Paste from external source into verbatim interpolated string after hole

        [WpfFact]
        public void TestNewLineIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\n",
                """
                var x = $@"{0}[||]"
                """,
                "var x = $@\"{0}\n[||]\"",
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestNewLineIntoVerbatimInterpolatedStringAfterHole2()
        {
            TestPasteUnknownSource(
                pasteText: """


                """,
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}
                [||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestTabIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: "\t",
                """
                var x = $@"{0}[||]"
                """,
                "var x = $@\"{0}\t[||]\"",
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestSingleQuoteIntoVerbatimInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: """'""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}'[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestDoubleQuoteIntoVerbatimInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: """
                "
                """,
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}""[||]"
                """,
                afterUndo: """
                var x = $@"{0}"[||]"
                """);
        }

        [WpfFact]
        public void TestComplexStringIntoVerbatimInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: "\t\"\"\t",
                """
                var x = $@"{0}[||]"
                """,
                "var x = $@\"{0}\t\"\"\t[||]\"",
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestVerbatimTextIntoVerbatimInterpolatedStringAfterHole()
        {
            TestPasteUnknownSource(
                pasteText: """abc""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}abc[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestOpenCurlyIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}{{[||]"
                """,
                afterUndo: """
                var x = $@"{0}{[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}{{[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoOpenCurliesAndContentIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """{{0""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}{{0[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestCloseCurlyIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}}}[||]"
                """,
                afterUndo: """
                var x = $@"{0}}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}}}[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestTwoCloseCurliesAndContentIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """}}0""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}}}0[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestCurlyWithContentIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{0}y""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}x{0}y[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        [WpfFact]
        public void TestCurliesWithContentIntoVerbatimInterpolatedStringAfterHole1()
        {
            TestPasteUnknownSource(
                pasteText: """x{{0}}y""",
                """
                var x = $@"{0}[||]"
                """,
                """
                var x = $@"{0}x{{0}}y[||]"
                """,
                afterUndo: """
                var x = $@"{0}[||]"
                """);
        }

        #endregion
    }
}
