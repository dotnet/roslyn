// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste
{
    public class PasteKnownSourceIntoSingleLineRawInterpolatedStringTests : StringCopyPasteCommandHandlerKnownSourceTests
    {
        #region Normal Copy/Paste tests

        // Tests where we actually set up a document to copy code from.

        [WpfFact]
        public void TestPasteSimpleNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "{|Copy:goo|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""goo[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "{|Copy:goo|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" goo[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteOpenBraceNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "{|Copy:{|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $$"""{[||] """;
"""",
""""
var dest =
    $"""{[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteOpenBraceNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "{|Copy:{|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $$""" {[||]""";
"""",
""""
var dest =
    $""" {[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteOpenCloseBraceNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "{|Copy:{}|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $$"""{}[||] """;
"""",
""""
var dest =
    $"""{}[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteOpenCloseBraceNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "{|Copy:{}|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $$""" {}[||]""";
"""",
""""
var dest =
    $""" {}[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteLooksLikeInterpolationNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "{|Copy:{0}|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $$"""{0}[||] """;
"""",
""""
var dest =
    $"""{0}[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteLooksLikeInterpolationNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "{|Copy:{0}|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $$""" {0}[||]""";
"""",
""""
var dest =
    $""" {0}[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSimpleSubstringNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "g{|Copy:o|}o";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""o[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleSubstringNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "g{|Copy:o|}o";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" o[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPastePartiallySelectedEscapeNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "\{|Copy:n|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""n[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPastePartiallySelectedEscapeNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "\{|Copy:n|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" n[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteFullySelectedEscapeNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "{|Copy:\n|}";""",
""""
var dest =
    $"""[||] """;
"""",
"var dest =\r\n    $\"\"\"\r\n    \n    [||] \r\n    \"\"\";",
""""
var dest =
    $"""\n[||] """;
"""");
        }

        [WpfFact(Skip = "Investigating")]
        public void TestPasteFullySelectedEscapeNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "{|Copy:\n|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""
     
    
    [||]""";
"""",
""""
var dest =
    $""" \n[||]""";
"""");
        }

        [WpfFact]
        public void TestPastePartiallySelectedQuoteNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "\{|Copy:"|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""
    "[||] 
    """;
"""",
"""""
var dest =
    $""""[||] """;
""""");
        }

        [WpfFact]
        public void TestPastePartiallySelectedQuoteNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "\{|Copy:"|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""
     "
    [||]""";
"""",
""""
var dest =
    $""" "[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteFullySelectedQuoteNormalLiteralContent()
        {
            TestCopyPaste(
"""var v = "{|Copy:\"|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""
    "[||] 
    """;
"""",
""""
var dest =
    $"""\"[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteFullySelectedQuoteNormalLiteralContent2()
        {
            TestCopyPaste(
"""var v = "{|Copy:\"|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""
     "
    [||]""";
"""",
""""
var dest =
    $""" \"[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSimpleVerbatimLiteralContent()
        {
            TestCopyPaste(
"""var v = @"{|Copy:goo|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""goo[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleVerbatimLiteralContent2()
        {
            TestCopyPaste(
"""var v = @"{|Copy:goo|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" goo[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSimpleSubstringVerbatimLiteralContent()
        {
            TestCopyPaste(
"""var v = @"g{|Copy:o|}o";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""o[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleSubstringVerbatimLiteralContent2()
        {
            TestCopyPaste(
"""var v = @"g{|Copy:o|}o";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" o[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSelectedVerbatimNewLineLiteralContent()
        {
            TestCopyPaste(
"""
var v = @"{|Copy:
|}";
""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""
    
    [||] 
    """;
"""",
""""
var dest =
    $"""
[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSelectedVerbatimNewLineLiteralContent2()
        {
            TestCopyPaste(
"""
var v = @"{|Copy:
|}";
""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""
     
    
    [||]""";
"""",
""""
var dest =
    $""" 
[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteFullySelectedEscapeVerbatimLiteralContent()
        {
            TestCopyPaste(
"""var v = @"{|Copy:""|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""
    "[||] 
    """;
"""",
""""""
var dest =
    $"""""[||] """;
"""""");
        }

        [WpfFact]
        public void TestPasteFullySelectedEscapeVerbatimLiteralContent2()
        {
            TestCopyPaste(
"""var v = @"{|Copy:""|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""
     "
    [||]""";
"""",
""""
var dest =
    $""" ""[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawSingleLineLiteralContent()
        {
            TestCopyPaste(
""""var v = """{|Copy:goo|}""";"""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""goo[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawSingleLineLiteralContent2()
        {
            TestCopyPaste(
""""var v = """{|Copy:goo|}""";"""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" goo[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteQuotesRawSingleLineLiteralContent()
        {
            TestCopyPaste(
""""var v = """{|Copy: "" |}""";"""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $""" "" [||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteQuotesRawSingleLineLiteralContent2()
        {
            TestCopyPaste(
""""var v = """{|Copy: "" |}""";"""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""  "" [||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawMultiLineLiteralContent1()
        {
            TestCopyPaste(
""""
var v = """
    {|Copy:goo|}
    """;
"""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""goo[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawMultiLineLiteralContent1B()
        {
            TestCopyPaste(
""""
var v = """
    {|Copy:goo|}
    """;
"""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" goo[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawMultiLineLiteralContent2()
        {
            TestCopyPaste(
""""
var v = """
    {|Copy:goo
    bar|}
    """;
"""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""
    goo
    bar[||] 
    """;
"""",
""""
var dest =
    $"""goo
    bar[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawMultiLineLiteralContent2B()
        {
            TestCopyPaste(
""""
var v = """
    {|Copy:goo
    bar|}
    """;
"""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""
     goo
    bar
    [||]""";
"""",
""""
var dest =
    $""" goo
    bar[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawMultiLineLiteralContent3()
        {
            TestCopyPaste(
""""
var v = """
{|Copy:    goo
    bar|}
    """;
"""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""
    goo
    bar[||] 
    """;
"""",
""""
var dest =
    $"""    goo
    bar[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteSimpleRawMultiLineLiteralContent3B()
        {
            TestCopyPaste(
""""
var v = """
{|Copy:    goo
    bar|}
    """;
"""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $"""
     goo
    bar
    [||]""";
"""",
""""
var dest =
    $"""     goo
    bar[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromInterpolatedStringLiteralContent1()
        {
            TestCopyPaste(
"""var v = $"{|Copy:{0:X}|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""{0:X}[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromInterpolatedStringLiteralContent1B()
        {
            TestCopyPaste(
"""var v = $"{|Copy:{0:X}|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" {0:X}[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromInterpolatedStringLiteralContent2()
        {
            TestCopyPaste(
"""var v = $"{|Copy:{0:\"X\"}|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""{0:"X"}[||] """;
"""",
""""
var dest =
    $"""{0:\"X\"}[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromInterpolatedStringLiteralContent2B()
        {
            TestCopyPaste(
"""var v = $"{|Copy:{0:\"X\"}|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" {0:"X"}[||]""";
"""",
""""
var dest =
    $""" {0:\"X\"}[||]""";
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromVerbatimInterpolatedStringLiteralContent1()
        {
            TestCopyPaste(
"""var v = $@"{|Copy:{0:X}|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""{0:X}[||] """;
"""",
""""
var dest =
    $"""[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromVerbatimInterpolatedStringLiteralContent1B()
        {
            TestCopyPaste(
"""var v = $@"{|Copy:{0:X}|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" {0:X}[||]""";
"""",
""""
var dest =
    $""" [||]""";
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromVerbatimInterpolatedStringLiteralContent2()
        {
            TestCopyPaste(
"""var v = $@"{|Copy:{0:""X""}|}";""",
""""
var dest =
    $"""[||] """;
"""",
""""
var dest =
    $"""{0:"X"}[||] """;
"""",
""""
var dest =
    $"""{0:""X""}[||] """;
"""");
        }

        [WpfFact]
        public void TestPasteInterpolationWithFormatClauseFromVerbatimInterpolatedStringLiteralContent2B()
        {
            TestCopyPaste(
"""var v = $@"{|Copy:{0:""X""}|}";""",
""""
var dest =
    $""" [||]""";
"""",
""""
var dest =
    $""" {0:"X"}[||]""";
"""",
""""
var dest =
    $""" {0:""X""}[||]""";
"""");
        }

        #endregion

        #region Known Source tests

        // Tests where we place things directly on the clipboard (avoiding the need to do the actual copy).
        // This allows a port of the tests in PasteUnknownSourceIntoSingleLineInterpolatedRawStringTests.cs

        [WpfFact]
        public void TestNewLineIntoSingleLineRawString1_A()
        {
            TestPasteKnownSource(
                pasteText: "\n",
""""
var x = $"""[||] """
"""",
"var x = $\"\"\"\r\n    \n    [||] \r\n    \"\"\"",
                afterUndo:
"var x = $\"\"\"\n[||] \"\"\"");
        }

        [WpfFact]
        public void TestNewLineIntoSingleLineRawString1_B()
        {
            TestPasteKnownSource(
                pasteText: "\n",
""""
var x = $""" [||]"""
"""",
"var x = $\"\"\"\r\n     \n    \r\n    [||]\"\"\"",
                afterUndo:
"var x = $\"\"\" \n[||]\"\"\"");
        }

        [WpfFact]
        public void TestNewLineIntoSingleLineRawString2_A()
        {
            TestPasteKnownSource(
                pasteText: """


                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
    
    [||] 
    """
"""",
                afterUndo:
""""
var x = $"""
[||] """
"""");
        }

        [WpfFact]
        public void TestNewLineIntoSingleLineRawString2_B()
        {
            TestPasteKnownSource(
                pasteText: """


                """,
""""
var x = $""" [||]"""
"""",
""""
var x = $"""
     
    
    [||]"""
"""",
                afterUndo:
""""
var x = $""" 
[||]"""
"""");
        }

        [WpfFact]
        public void TestSpacesIntoSingleLineRawString1_A()
        {
            TestPasteKnownSource(
                pasteText: """    """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""    [||] """
"""",
                afterUndo:
""""
var x = $"""[||] """
"""");
        }

        [WpfFact]
        public void TestSpacesIntoSingleLineRawString1_B()
        {
            TestPasteKnownSource(
                pasteText: """    """,
""""
var x = $""" [||]"""
"""",
""""
var x = $"""     [||]"""
"""",
                afterUndo:
""""
var x = $""" [||]"""
"""");
        }

        [WpfFact]
        public void TestSpacesIntoSingleLineRawString2()
        {
            TestPasteKnownSource(
                pasteText: """
                    

                """,
""""
var x = $"""
    [||]
    """
"""",
""""
var x = $"""
        
    [||]
    """
"""",
                afterUndo:
""""
var x = $"""
        
[||]
    """
"""");
        }

        [WpfFact]
        public void TestSingleQuoteIntoSingleLineRawString_A()
        {
            TestPasteKnownSource(
                pasteText: """'""",
""""
var x = $"""[||] """
"""",
""""
var x = $"""'[||] """
"""",
                afterUndo:
""""
var x = $"""[||] """
"""");
        }

        [WpfFact]
        public void TestSingleQuoteIntoSingleLineRawString_B()
        {
            TestPasteKnownSource(
                pasteText: """'""",
""""
var x = $""" [||]"""
"""",
""""
var x = $""" '[||]"""
"""",
                afterUndo:
""""
var x = $""" [||]"""
"""");
        }

        [WpfFact]
        public void TestDoubleQuoteIntoSingleLineRawString_A()
        {
            TestPasteKnownSource(
                pasteText: """
                "
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
    "[||] 
    """
"""",
                afterUndo:
"""""
var x = $""""[||] """
""""");
        }

        [WpfFact]
        public void TestDoubleQuoteIntoSingleLineRawString_B()
        {
            TestPasteKnownSource(
                pasteText: """
                "
                """,
""""
var x = $""" [||]"""
"""",
""""
var x = $"""
     "
    [||]"""
"""",
                afterUndo:
""""
var x = $""" "[||]"""
"""");
        }

        [WpfFact]
        public void TestTripleQuoteIntoSingleLineRawString1_A()
        {
            TestPasteKnownSource(
                pasteText: """"
                """
                """",
""""
var x = $"""[||] """
"""",
"""""
var x = $""""
    """[||] 
    """"
""""",
                afterUndo:
"""""""
var x = $""""""[||] """
""""""");
        }

        [WpfFact]
        public void TestTripleQuoteIntoSingleLineRawString1_B()
        {
            TestPasteKnownSource(
                pasteText: """"
                """
                """",
""""
var x = $""" [||]"""
"""",
"""""
var x = $""""
     """
    [||]""""
""""",
                afterUndo:
""""
var x = $""" """[||]"""
"""");
        }

        [WpfFact]
        public void TestTripleQuoteIntoSingleLineRawString3()
        {
            TestPasteKnownSource(
                pasteText: """"
                """
                """",
""""
var x = $""" "[||] """
"""",
""""""
var x = $""""" """"[||] """""
"""""",
                afterUndo:
"""""
var x = $""" """"[||] """
""""");
        }

        [WpfFact]
        public void TestTripleQuoteIntoSingleLineRawString4()
        {
            TestPasteKnownSource(
                pasteText: """"
                """
                """",
""""
var x = $""" "[||]" """
"""",
"""""""
var x = $"""""" """"[||]" """"""
""""""",
                afterUndo:
"""""
var x = $""" """"[||]" """
""""");
        }

        [WpfFact]
        public void TestTripleQuoteIntoSingleLineRawString5()
        {
            TestPasteKnownSource(
                pasteText: """"
                """
                """",
""""
var x = $""" [||]" """
"""",
""""""
var x = $""""" """[||]" """""
"""""",
                afterUndo:
""""
var x = $""" """[||]" """
"""");
        }

        [WpfFact]
        public void TestQuadrupleQuoteIntoSingleLineRawString()
        {
            TestPasteKnownSource(
                pasteText: """""
                """"
                """"",
""""
var x = $"""
    [||]
    """
"""",
""""""
var x = $"""""
    """"[||]
    """""
"""""",
                afterUndo:
"""""
var x = $"""
    """"[||]
    """
""""");
        }

        [WpfFact]
        public void TestOpenCurlyIntoSingleLineRawString_A()
        {
            TestPasteKnownSource(
                pasteText: """{""",
""""
var x = $"""[||] """
"""",
""""
var x = $$"""{[||] """
"""",
                afterUndo:
""""
var x = $"""{[||] """
"""");
        }

        [WpfFact]
        public void TestOpenCurlyIntoSingleLineRawString_B()
        {
            TestPasteKnownSource(
                pasteText: """{""",
""""
var x = $""" [||]"""
"""",
""""
var x = $$""" {[||]"""
"""",
                afterUndo:
""""
var x = $""" {[||]"""
"""");
        }

        [WpfFact]
        public void TestOpenQuoteAndTripleOpenBraceIntoSingleLineRawString1()
        {
            TestPasteKnownSource(
                pasteText: """
                "{{{
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $$$$"""
    "{{{[||] 
    """
"""",
                afterUndo:
"""""
var x = $""""{{{[||] """
""""");
        }

        [WpfFact]
        public void TestTripleOpenQuoteAndTripleOpenBraceIntoSingleLineRawString1()
        {
            TestPasteKnownSource(
                pasteText: """"
                """{{{
                """",
""""
var x = $"""[||] """
"""",
"""""
var x = $$$$""""
    """{{{[||] 
    """"
""""",
                afterUndo:
"""""""
var x = $""""""{{{[||] """
""""""");
        }

        [WpfFact]
        public void TestTripleOpenQuoteAndTripleOpenBraceIntoSingleLineRawString2()
        {
            TestPasteKnownSource(
                pasteText: """" """{{{"""",
""""
var x = $"""[||] """
"""",
"""""
var x = $$$$"""" """{{{[||] """"
""""",
                afterUndo:
""""
var x = $""" """{{{[||] """
"""");
        }

        [WpfFact]
        public void TestTripleOpenBraceIntoSingleLineRawString1_A()
        {
            TestPasteKnownSource(
                pasteText: """{{{""",
""""
var x = $"""[||] """
"""",
""""
var x = $$$$"""{{{[||] """
"""",
                afterUndo:
""""
var x = $"""{{{[||] """
"""");
        }

        [WpfFact]
        public void TestTripleOpenBraceIntoSingleLineRawString1_B()
        {
            TestPasteKnownSource(
                pasteText: """{{{""",
""""
var x = $""" [||]"""
"""",
""""
var x = $$$$""" {{{[||]"""
"""",
                afterUndo:
""""
var x = $""" {{{[||]"""
"""");
        }

        [WpfFact]
        public void TestTripleOpenBraceIntoSingleLineRawString3()
        {
            TestPasteKnownSource(
                pasteText: """{{{""",
""""
var x = $""" "[||] """
"""",
""""
var x = $$$$""" "{{{[||] """
"""",
                afterUndo:
""""
var x = $""" "{{{[||] """
"""");
        }

        [WpfFact]
        public void TestTripleOpenBraceIntoSingleLineRawString4()
        {
            TestPasteKnownSource(
                pasteText: """{{{""",
""""
var x = $""" "[||]" """
"""",
""""
var x = $$$$""" "{{{[||]" """
"""",
                afterUndo:
""""
var x = $""" "{{{[||]" """
"""");
        }

        [WpfFact]
        public void TestTripleOpenBraceIntoSingleLineRawString5()
        {
            TestPasteKnownSource(
                pasteText: """{{{""",
""""
var x = $""" [||]" """
"""",
""""
var x = $$$$""" {{{[||]" """
"""",
                afterUndo:
""""
var x = $""" {{{[||]" """
"""");
        }

        [WpfFact]
        public void TestInterpolationIntoSingleLineRawString1()
        {
            TestPasteKnownSource(
                pasteText: """{0}""",
""""
var x = $""" [||] """
"""",
""""
var x = $$""" {0}[||] """
"""",
                afterUndo:
""""
var x = $""" {0}[||] """
"""");
        }

        [WpfFact]
        public void TestOpenCloseBraceIntoSingleLineRawString1()
        {
            TestPasteKnownSource(
                pasteText: """{}""",
""""
var x = $""" [||] """
"""",
""""
var x = $$""" {}[||] """
"""",
                afterUndo:
""""
var x = $""" {}[||] """
"""");
        }

        [WpfFact]
        public void TestOpenCloseBraceIntoSingleLineRawString2()
        {
            TestPasteKnownSource(
                pasteText: """{}""",
""""
var x = $$""" [||] """
"""",
""""
var x = $$""" {}[||] """
"""",
                afterUndo:
""""
var x = $$""" [||] """
"""");
        }

        [WpfFact]
        public void TestOpenCloseBraceIntoSingleLineRawString3()
        {
            TestPasteKnownSource(
                pasteText: """{{}""",
""""
var x = $$""" [||] """
"""",
""""
var x = $$$""" {{}[||] """
"""",
                afterUndo:
""""
var x = $$""" {{}[||] """
"""");
        }

        [WpfFact]
        public void TestOpenCloseBraceIntoSingleLineRawString4()
        {
            TestPasteKnownSource(
                pasteText: """{}}""",
""""
var x = $$""" [||] """
"""",
""""
var x = $$$""" {}}[||] """
"""",
                afterUndo:
""""
var x = $$""" {}}[||] """
"""");
        }

        [WpfFact]
        public void TestOpenCloseBraceIntoSingleLineRawString5()
        {
            TestPasteKnownSource(
                pasteText: """{{}}""",
""""
var x = $$""" [||] """
"""",
""""
var x = $$$""" {{}}[||] """
"""",
                afterUndo:
""""
var x = $$""" {{}}[||] """
"""");
        }

        [WpfFact]
        public void TestComplexStringIntoSingleLineRawString()
        {
            TestPasteKnownSource(
                pasteText: """  ""  """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""  ""  [||] """
"""",
                afterUndo:
""""
var x = $"""[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawString_A()
        {
            TestPasteKnownSource(
                pasteText: """abc""",
""""
var x = $"""[||] """
"""",
""""
var x = $"""abc[||] """
"""",
                afterUndo:
""""
var x = $"""[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawString_B()
        {
            TestPasteKnownSource(
                pasteText: """abc""",
""""
var x = $""" [||]"""
"""",
""""
var x = $""" abc[||]"""
"""",
                afterUndo:
""""
var x = $""" [||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine1_A()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
    abc
    def[||] 
    """
"""",
                afterUndo:
""""
var x = $"""abc
def[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine1_B()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $""" [||]"""
"""",
""""
var x = $"""
     abc
    def
    [||]"""
"""",
                afterUndo:
""""
var x = $""" abc
def[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine4()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $"""goo[||]"""
"""",
""""
var x = $"""
    gooabc
    def
    [||]"""
"""",
                afterUndo:
""""
var x = $"""gooabc
def[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine5()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $"""goo[||]bar"""
"""",
""""
var x = $"""
    gooabc
    def[||]bar
    """
"""",
                afterUndo:
""""
var x = $"""gooabc
def[||]bar"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine6()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def

                """,
""""
var x = $"""goo[||]bar"""
"""",
""""
var x = $"""
    gooabc
    def
    [||]bar
    """
"""",
                afterUndo:
""""
var x = $"""gooabc
def
[||]bar"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine7_A()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                    def
                ghi
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
    abc
        def
    ghi[||] 
    """
"""",
                afterUndo:
""""
var x = $"""abc
    def
ghi[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine7_B()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                    def
                ghi
                """,
""""
var x = $""" [||]"""
"""",
""""
var x = $"""
     abc
        def
    ghi
    [||]"""
"""",
                afterUndo:
""""
var x = $""" abc
    def
ghi[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine8_A()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                    def
                    ghi
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
    abc
        def
        ghi[||] 
    """
"""",
                afterUndo:
""""
var x = $"""abc
    def
    ghi[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine8_B()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                    def
                    ghi
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
    abc
        def
        ghi[||] 
    """
"""",
                afterUndo:
""""
var x = $"""abc
    def
    ghi[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine9_A()
        {
            TestPasteKnownSource(
                pasteText: """
                    abc
                    def
                    ghi
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
        abc
        def
        ghi[||] 
    """
"""",
                afterUndo:
""""
var x = $"""    abc
    def
    ghi[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine9_B()
        {
            TestPasteKnownSource(
                pasteText: """
                    abc
                    def
                    ghi
                """,
""""
var x = $""" [||]"""
"""",
""""
var x = $"""
         abc
        def
        ghi
    [||]"""
"""",
                afterUndo:
""""
var x = $"""     abc
    def
    ghi[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine10_A()
        {
            TestPasteKnownSource(
                pasteText: """
                        abc
                    def
                    ghi
                """,
""""
var x = $"""[||] """
"""",
""""
var x = $"""
            abc
        def
        ghi[||] 
    """
"""",
                afterUndo:
""""
var x = $"""        abc
    def
    ghi[||] """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine10_B()
        {
            TestPasteKnownSource(
                pasteText: """
                        abc
                    def
                    ghi
                """,
""""
var x = $""" [||]"""
"""",
""""
var x = $"""
             abc
        def
        ghi
    [||]"""
"""",
                afterUndo:
""""
var x = $"""         abc
    def
    ghi[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine11_A()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $"""[||]{|Selection:    |}"""
"""",
""""
var x = $"""
    abc
    def
    [||]"""
"""",
                afterUndo:
""""
var x = $"""abc
def[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine11_B()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $"""  [||]{|Selection:    |}  """
"""",
""""
var x = $"""
      abc
    def[||]  
    """
"""",
                afterUndo:
""""
var x = $"""  abc
def[||]  """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine12_A()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def

                """,
""""
var x = $"""[||]{|Selection:    |}"""
"""",
""""
var x = $"""
    abc
    def
    
    [||]"""
"""",
                afterUndo:
""""
var x = $"""abc
def
[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine12_B()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def

                """,
""""
var x = $"""  [||]{|Selection:    |}  """
"""",
""""
var x = $"""
      abc
    def
    [||]  
    """
"""",
                afterUndo:
""""
var x = $"""  abc
def
[||]  """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine13_A()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $"""[||]{|Selection:    |}  """
"""",
""""
var x = $"""
    abc
    def[||]  
    """
"""",
                afterUndo:
""""
var x = $"""abc
def[||]  """
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringSingleLine13_B()
        {
            TestPasteKnownSource(
                pasteText: """
                abc
                def
                """,
""""
var x = $"""  [||]{|Selection:    |}"""
"""",
""""
var x = $"""
      abc
    def
    [||]"""
"""",
                afterUndo:
""""
var x = $"""  abc
def[||]"""
"""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringHeader1_A()
        {
            TestPasteKnownSource(
                pasteText: """
                "bar
                """,
""""
var x = $"""[||]goo"""
"""",
""""
var x = $"""
    "bar[||]goo
    """
"""",
                afterUndo:
"""""
var x = $""""bar[||]goo"""
""""");
        }

        [WpfFact]
        public void TestNormalTextIntoSingleLineRawStringHeader1_B()
        {
            TestPasteKnownSource(
                pasteText: """
                bar"
                """,
""""
var x = $"""goo[||]"""
"""",
""""
var x = $"""
    goobar"
    [||]"""
"""",
                afterUndo:
""""
var x = $"""goobar"[||]"""
"""");
        }

        [WpfFact]
        public void TestQuotesIntoHeader1()
        {
            TestPasteKnownSource(
                pasteText: """
                ""
                """,
""""
var x = $"""[||]{|Selection:    |}"""
"""",
""""
var x = $"""
    ""
    [||]"""
"""",
                afterUndo:
""""""
var x = $"""""[||]"""
"""""");
        }

        [WpfFact]
        public void TestQuotesIntoHeader2()
        {
            TestPasteKnownSource(
                pasteText: """"
                """
                """",
""""
var x = $"""[||]{|Selection:    |}"""
"""",
"""""
var x = $""""
    """
    [||]""""
""""",
                afterUndo:
"""""""
var x = $""""""[||]"""
""""""");
        }

        #endregion
    }
}
