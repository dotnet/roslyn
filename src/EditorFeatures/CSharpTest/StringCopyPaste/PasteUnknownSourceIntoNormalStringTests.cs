// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public class PasteUnknownSourceIntoNormalStringTests
        : StringCopyPasteCommandHandlerUnknownSourceTests
    {
        [WpfFact]
        public void TestNewLineIntoNormalString1()
        {
            TestPasteUnknownSource(
                pasteText: "\n",
                @"var x = ""[||]""",
                @"var x = ""\n[||]""",
                afterUndo: "var x = \"\n[||]\"");
        }

        [WpfFact]
        public void TestNewLineIntoNormalString2()
        {
            TestPasteUnknownSource(
                pasteText: "\r\n",
                @"var x = ""[||]""",
                @"var x = ""\r\n[||]""",
                afterUndo: "var x = \"\r\n[||]\"");
        }

        [WpfFact]
        public void TestTabIntoNormalString1()
        {
            TestPasteUnknownSource(
                pasteText: "\t",
                @"var x = ""[||]""",
                @"var x = ""\t[||]""",
                afterUndo: "var x = \"\t[||]\"");
        }

        [WpfFact]
        public void TestBackslashTIntoNormalString1()
        {
            TestPasteUnknownSource(
                pasteText: @"\t",
                @"var x = ""[||]""",
                @"var x = ""\t[||]""",
                afterUndo: @"var x = ""[||]""");
        }

        [WpfFact]
        public void TestSingleQuoteIntoNormalString()
        {
            TestPasteUnknownSource(
                pasteText: "'",
                @"var x = ""[||]""",
                @"var x = ""'[||]""",
                afterUndo: "var x = \"[||]\"");
        }

        [WpfFact]
        public void TestDoubleQuoteIntoNormalString()
        {
            TestPasteUnknownSource(
                pasteText: "\"",
                @"var x = ""[||]""",
                @"var x = ""\""[||]""",
                afterUndo: "var x = \"\"[||]\"");
        }

        [WpfFact]
        public void TestComplexStringIntoNormalString()
        {
            TestPasteUnknownSource(
                pasteText: "\t\"\"\t",
                @"var x = ""[||]""",
                @"var x = ""\t\""\""\t[||]""",
                afterUndo: "var x = \"\t\"\"\t[||]\"");
        }

        [WpfFact]
        public void TestNormalTextIntoNormalString()
        {
            TestPasteUnknownSource(
                pasteText: "abc",
                @"var x = ""[||]""",
                @"var x = ""abc[||]""",
                afterUndo: @"var x = ""[||]""");
        }
    }
}
