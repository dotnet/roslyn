// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste;

public class PasteUnknownSourceIntoSingleLineInterpolatedRawStringTests
    : StringCopyPasteCommandHandlerUnknownSourceTests
{
    [WpfFact]
    public void TestNewLineIntoSingleLineRawString1_A()
    {
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
            pasteText: """{0}""",
""""
var x = $""" [||] """
"""",
""""
var x = $""" {0}[||] """
"""",
            afterUndo:
""""
var x = $""" [||] """
"""");
    }

    [WpfFact]
    public void TestOpenCloseBraceIntoSingleLineRawString1()
    {
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
        TestPasteUnknownSource(
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
}
