// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.StringCopyPaste;

public sealed class PasteUnknownSourceIntoMultiLineRawStringTests
    : StringCopyPasteCommandHandlerUnknownSourceTests
{
    [WpfFact]
    public void TestNewLineIntoMultiLineRawString1()
        => TestPasteUnknownSource(
            pasteText: "\n",
            """"
            var x = """
                [||]
                """
            """",
            "var x = \"\"\"\r\n    \n    [||]\r\n    \"\"\"",
            afterUndo:
            "var x = \"\"\"\r\n    \n[||]\r\n    \"\"\"");

    [WpfFact]
    public void TestNewLineIntoMultiLineRawString2()
        => TestPasteUnknownSource(
            pasteText: """


            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                
                [||]
                """
            """",
            afterUndo:
            """"
            var x = """
                
            [||]
                """
            """");

    [WpfFact]
    public void TestSpacesIntoMultiLineRawString1()
        => TestPasteUnknownSource(
            pasteText: """    """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                    [||]
                """
            """",
            afterUndo:
            """"
            var x = """
                [||]
                """
            """");

    [WpfFact]
    public void TestSpacesIntoMultiLineRawString2()
        => TestPasteUnknownSource(
            pasteText: """
                

            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                
                [||]
                """
            """",
            afterUndo:
            """"
            var x = """
                    
            [||]
                """
            """");

    [WpfFact]
    public void TestSingleQuoteIntoMultiLineRawString()
        => TestPasteUnknownSource(
            pasteText: """'""",
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                '[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                [||]
                """
            """");

    [WpfFact]
    public void TestDoubleQuoteIntoMultiLineRawString()
        => TestPasteUnknownSource(
            pasteText: """
            "
            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                "[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                [||]
                """
            """");

    [WpfFact]
    public void TestTripleQuoteIntoMultiLineRawString1()
        => TestPasteUnknownSource(
            pasteText: """"
            """
            """",
            """"
            var x = """
                [||]
                """
            """",
            """""
            var x = """"
                """[||]
                """"
            """"",
            afterUndo:
            """"
            var x = """
                """[||]
                """
            """");

    [WpfFact]
    public void TestTripleQuoteIntoMultiLineRawString2()
        => TestPasteUnknownSource(
            pasteText: """"
            """
            """",
            """"
            var x = """  
                [||]
                """  
            """",
            """""
            var x = """"  
                """[||]
                """"  
            """"",
            afterUndo:
            """"
            var x = """  
                """[||]
                """  
            """");

    [WpfFact]
    public void TestTripleQuoteIntoMultiLineRawString3()
        => TestPasteUnknownSource(
            pasteText: """"
            """
            """",
            """"
            var x = """  
                "[||]
                """  
            """",
            """"""
            var x = """""  
                """"[||]
                """""  
            """""",
            afterUndo:
            """""
            var x = """  
                """"[||]
                """  
            """"");

    [WpfFact]
    public void TestTripleQuoteIntoMultiLineRawString4()
        => TestPasteUnknownSource(
            pasteText: """"
            """
            """",
            """"
            var x = """  
                "[||]"  
                """  
            """",
            """""""
            var x = """"""  
                """"[||]"  
                """"""  
            """"""",
            afterUndo:
            """""
            var x = """  
                """"[||]"  
                """  
            """"");

    [WpfFact]
    public void TestTripleQuoteIntoMultiLineRawString5()
        => TestPasteUnknownSource(
            pasteText: """"
            """
            """",
            """"
            var x = """  
                [||]"
                """  
            """",
            """"""
            var x = """""  
                """[||]"
                """""  
            """""",
            afterUndo:
            """"
            var x = """  
                """[||]"
                """  
            """");

    [WpfFact]
    public void TestQuadrupleQuoteIntoMultiLineRawString()
        => TestPasteUnknownSource(
            pasteText: """""
            """"
            """"",
            """"
            var x = """
                [||]
                """
            """",
            """"""
            var x = """""
                """"[||]
                """""
            """""",
            afterUndo:
            """""
            var x = """
                """"[||]
                """
            """"");

    [WpfFact]
    public void TestComplexStringIntoMultiLineRawString()
        => TestPasteUnknownSource(
            pasteText: """  ""  """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                ""  [||]
                """
            """",
            afterUndo:
            """"
            var x = """
                  ""  [||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawString()
        => TestPasteUnknownSource(
            pasteText: """abc""",
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                abc[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                [||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine1()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def
            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                abc
                def[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                abc
            def[||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine2()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def
            """,
            """"
            var x = """
            [||]
                """
            """",
            """"
            var x = """
                abc
                def[||]
                """
            """",
            afterUndo:
            """"
            var x = """
            abc
            def[||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine3()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def
            """,
            """"
            var x = """[||]

                """
            """",
            """"
            var x = """
                abc
                def[||]

                """
            """",
            afterUndo:
            """"
            var x = """abc
            def[||]

                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine4()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def
            """,
            """"
            var x = """
                goo[||]
                """
            """",
            """"
            var x = """
                gooabc
                def[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                gooabc
            def[||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine5()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def
            """,
            """"
            var x = """
                goo[||]bar
                """
            """",
            """"
            var x = """
                gooabc
                def[||]bar
                """
            """",
            afterUndo:
            """"
            var x = """
                gooabc
            def[||]bar
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine6()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def

            """,
            """"
            var x = """
                goo[||]bar
                """
            """",
            """"
            var x = """
                gooabc
                def
                [||]bar
                """
            """",
            afterUndo:
            """"
            var x = """
                gooabc
            def
            [||]bar
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine7()
        => TestPasteUnknownSource(
            pasteText: """
            abc
                def
            ghi
            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                abc
                    def
                ghi[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                abc
                def
            ghi[||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine7_B()
        => TestPasteUnknownSource(
            pasteText: """
            abc
                def
            ghi
            """,
            """"
            var x = """
                      [||]
                      """
            """",
            """"
            var x = """
                      abc
                          def
                      ghi[||]
                      """
            """",
            afterUndo:
            """"
            var x = """
                      abc
                def
            ghi[||]
                      """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine8()
        => TestPasteUnknownSource(
            pasteText: """
            abc
                def
                ghi
            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                abc
                def
                ghi[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                [||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine9()
        => TestPasteUnknownSource(
            pasteText: """
                abc
                def
                ghi
            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                abc
                def
                ghi[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                    abc
                def
                ghi[||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine10()
        => TestPasteUnknownSource(
            pasteText: """
                    abc
                def
                ghi
            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                    abc
                def
                ghi[||]
                """
            """",
            afterUndo:
            """"
            var x = """
                        abc
                def
                ghi[||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine11()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def
            """,
            """"
            var x = """[||]{|Selection:

                |}"""
            """",
            """"
            var x = """
                abc
                def
                [||]"""
            """",
            afterUndo:
            """"
            var x = """abc
            def[||]"""
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine12()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def

            """,
            """"
            var x = """[||]{|Selection:

                |}"""
            """",
            """"
            var x = """
                abc
                def
                
                [||]"""
            """",
            afterUndo:
            """"
            var x = """abc
            def
            [||]"""
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringMultiLine13()
        => TestPasteUnknownSource(
            pasteText: """
            abc
            def
            """,
            """"
            var x = """[||]{|Selection:

             |}   """
            """",
            """"
            var x = """
                abc
                def
             [||]   """
            """",
            afterUndo:
            """"
            var x = """abc
            def[||]   """
            """");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/74661")]
    public void TestNormalTextIntoMultiLineRawStringMultiLine14()
        => TestPasteUnknownSource(
            pasteText: """
                abc
                    def
                        ghi
            """,
            """"
            var x = """
                [||]
                """
            """",
            """"
            var x = """
                abc
                    def
                        ghi[||]
                """
            """",
                        afterUndo:
            """"
            var x = """
                    abc
                    def
                        ghi[||]
                """
            """");

    [WpfFact]
    public void TestNormalTextIntoMultiLineRawStringHeader1()
        => TestPasteUnknownSource(
            pasteText: """bar""",
            """"
            var x = """[||]
                goo
                """
            """",
            """"
            var x = """
                bar[||]
                goo
                """
            """",
            afterUndo:
            """"
            var x = """bar[||]
                goo
                """
            """");

    [WpfFact]
    public void TestQuotesIntoHeader1()
        => TestPasteUnknownSource(
            pasteText: """
            ""
            """,
            """"
            var x = """[||]{|Selection:

                |}"""
            """",
            """"
            var x = """
                ""
                [||]"""
            """",
            afterUndo:
            """"""
            var x = """""[||]"""
            """""");

    [WpfFact]
    public void TestQuotesIntoHeader2()
        => TestPasteUnknownSource(
            pasteText: """"
            """
            """",
            """"
            var x = """[||]{|Selection:

                |}"""
            """",
            """""
            var x = """"
                """
                [||]""""
            """"",
            afterUndo:
            """""""
            var x = """"""[||]"""
            """"""");
}
