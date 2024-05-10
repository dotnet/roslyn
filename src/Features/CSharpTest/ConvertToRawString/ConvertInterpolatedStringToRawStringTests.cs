// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertToRawString;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRawString;

using VerifyCS = CSharpCodeRefactoringVerifier<
    ConvertStringToRawStringCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.CodeActionsConvertToRawString)]
public class ConvertInterpolatedStringToRawStringTests
{
    private static async Task VerifyRefactoringAsync(string testCode, string? fixedCode = null, int index = 0, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        fixedCode ??= testCode;

        await new VerifyCS.Test
        {
            MarkupOptions = Testing.MarkupOptions.TreatPositionIndicatorsAsCode,
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp11,
            CodeActionIndex = index,
            TestState =
            {
                OutputKind = outputKind,
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOnEmptyString()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnEmptyVerbatimString1()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]@$"";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnEmptyVerbatimString2()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$@"";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnHighSurrogateChar()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"\uD800";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnHighSurrogateChar1()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"\uD800{0}";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnHighSurrogateChar2()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"{0}\uD800";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnLowSurrogateChar1()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"\uDC00";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestOnCombinedSurrogate()
    {
        await VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]$"\uD83D\uDC69";
                }
            }
            """,
            """"
            public class C
            {
                void M()
                {
                    var v = $"""👩""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestOnSplitSurrogate()
    {
        await VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]$"\uD83D{0}\uDC69";
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotOnNullChar()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"\u0000";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnNullChar1()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"\u0000{0}";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnNullChar2()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"{0}\u0000";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnControlCharacter()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"\u007F";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnControlCharacter1()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"\u007F{0}";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnControlCharacter2()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]$"{0}\u007F";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestSimpleString()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"a";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""a""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestBraces1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{{";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $$"""{""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestBraces1_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"}}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $$"""}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestBraces2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{{a{{";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $$"""{a{""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestBraces2_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{{a}}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $$"""{a}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestBraces3()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{{{{";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $$$"""{{""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestBraces3_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"}}}}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $$$"""}}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestBraces4_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{{}}}}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $$$"""{}}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_SingleLine1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"a{0}b";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""a{0}b""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_SingleLine2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{0}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""{0}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_SingleLine3()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{0:dddd}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""{0:dddd}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_SingleLine4()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{0:\u0041}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""{0:A}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_SingleLine5()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{0}ab{1}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""{0}ab{1}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_SingleLine6()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"{0}\"{1}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""{0}"{1}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_SingleLine7()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0}""{1}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""{0}"{1}""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0 +
                        1}""{1 +
                        2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 +
                            1}"{1 +
                            2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine1_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0 +
            1}""{1 +
            2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 +
                        1}"{1 +
                        2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine1_B()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"A{0 +
                        1}""{1 +
                        2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        A{0 +
                            1}"{1 +
                            2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine1_C()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0 +
                1}""{1 +
                2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 +
                        1}"{1 +
                        2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine1_D()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0 +
                    1}""{1 +
                    2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 +
                        1}"{1 +
                        2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine1_E()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0 +
                        1}""{1 +
                        2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 +
                            1}"{1 +
                            2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine1_F()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0 +
                            1}""{1 +
                            2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 +
                                1}"{1 +
                                2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{
                        0 + 1}""{
                        1 + 2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {
                            0 + 1}"{
                            1 + 2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine3()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        {0 + 1}""
                        {1 + 2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""

                                    {0 + 1}"
                                    {1 + 2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine4()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        {0 + 1}""
                        {1 + 2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestInterpolation_MultiLine4_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        {0 + 1}""
                        {1 + 2}
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestInterpolation_MultiLine5()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
            {0 + 1}""
            {1 + 2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestInterpolation_MultiLine5_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
            {0 + 1}""
            {1 + 2}
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestInterpolation_MultiLine6()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v =
            [||]$@"
            {0 + 1}""
            {1 + 2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                        $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestInterpolation_MultiLine6_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v =
            [||]$@"
            {0 + 1}""
            {1 + 2}
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                        $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestInterpolation_MultiLine7()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v =
            [||]$@"{
                0 + 1
            }""
            {
                1 + 2
            }";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                        $"""
                        {
                            0 + 1
                        }"
                        {
                            1 + 2
                        }
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine8()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v =
            [||]$@"{0 + 1}""
            {1 + 2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                        $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestInterpolation_MultiLine9()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"{0 + 1}""
            {1 + 2}";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        {0 + 1}"
                        {1 + 2}
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimSimpleString1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@$"a";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""a""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimSimpleString2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"a";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""a""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestSimpleStringTopLevel()
    {
        await VerifyRefactoringAsync("""
            var v = [||]$"a";
            """, """"
            var v = $"""a""";
            """", outputKind: OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task TestStringWithQuoteInMiddle()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"goo\"bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""goo"bar""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithQuoteInMiddle1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@$"goo""bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""goo"bar""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithQuoteInMiddle2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"goo""bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""goo"bar""";
                }
            }
            """");
    }

    [Fact]
    public async Task TestStringWithQuoteAtStart()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"\"goobar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        "goobar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithQuoteAtStart1()
    {
        await VerifyRefactoringAsync(""""
            public class C
            {
                void M()
                {
                    var v = [||]@$"""goobar";
                }
            }
            """", """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        "goobar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithQuoteAtStart2()
    {
        await VerifyRefactoringAsync(""""
            public class C
            {
                void M()
                {
                    var v = [||]$@"""goobar";
                }
            }
            """", """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        "goobar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestStringWithQuoteAtEnd()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"goobar\"";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goobar"
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithQuoteAtEnd1()
    {
        await VerifyRefactoringAsync(""""
            public class C
            {
                void M()
                {
                    var v = [||]$@"goobar""";
                }
            }
            """", """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goobar"
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithQuoteAtEnd2()
    {
        await VerifyRefactoringAsync(""""
            public class C
            {
                void M()
                {
                    var v = [||]@$"goobar""";
                }
            }
            """", """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goobar"
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestStringWithNewLine()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"goo\r\nbar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goo
                        bar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithNewLine1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"goo
            bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goo
                        bar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithNewLine2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@$"goo
            bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goo
                        bar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestStringWithNewLineAtStartAndEnd()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"\r\ngoobar\r\n";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""

                        goobar

                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithNewLineAtStartAndEnd1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
            goobar
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""

                        goobar

                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimStringWithNewLineAtStartAndEnd2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@$"
            goobar
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""

                        goobar

                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestNoIndentVerbatimStringWithNewLineAtStartAndEnd()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@$"
            goobar
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goobar
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestIndentedString()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$"goo\r\nbar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goo
                        bar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@$"
            from x in y
            where x > 0
            select x";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestIndentedStringTopLevel()
    {
        await VerifyRefactoringAsync("""
            var v = [||]$"goo\r\nbar";
            """, """"
            var v = $"""
                goo
                bar
                """;
            """", outputKind: OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespaceTopLevel()
    {
        await VerifyRefactoringAsync("""
            var v = [||]@$"
            from x in y
            where x > 0
            select x";
            """, """"
            var v = $"""
                from x in y
                where x > 0
                select x
                """;
            """", index: 1, outputKind: OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task TestVerbatimIndentedString()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@$"goo
            bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        goo
                        bar
                        """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestIndentedStringOnOwnLine()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v =
                            [||]$"goo\r\nbar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                            $"""
                            goo
                            bar
                            """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestVerbatimIndentedStringOnOwnLine()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v =
                            [||]@$"goo
            bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                            $"""
                            goo
                            bar
                            """;
                }
            }
            """");
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y
                        where x > 0
                        select x";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace3()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y
                        where x > 0
                        select x
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace4()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y
                            where x > 0
                            select x
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                            where x > 0
                            select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace5()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                            from x in y
                        where x > 0
                        select x
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                            from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace6()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y

                        where x > 0

                        select x
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y

                        where x > 0

                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace7()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"from x in y
                        where x > 0
                        select x
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace7_A()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"from x in y
                        where x > 0
                        select x";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace7_B()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"from x in y
                        where x > 0
                        select x  
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithoutLeadingWhitespace8()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"from x in y
                        where x > 0
                        select x";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString1()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
                                This needs stay indented
                            "
                        }
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
                                This needs stay indented
                            "
                        }
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString2()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
            This should not prevent dedentation
                            "
                        }
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
            This should not prevent dedentation
                            "
                        }
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString3()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
                                    
                                The whitespace above/below me needs to stay
                                    
                            "
                        }
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
                                    
                                The whitespace above/below me needs to stay
                                    
                            "
                        }
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString4()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
                    
                The whitespace above/below me needs to stay
                    
                            "
                        }
                        ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
                            @"
                    
                The whitespace above/below me needs to stay
                    
                            "
                        }
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString5()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
            from x in y
            where x > 0
            select x
            {
                @"
                    This needs to stay put
                "
            }
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
                @"
                    This needs to stay put
                "
                        }
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString6()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
            from x in y
            where x > 0
            select x
            {
            @"
                This needs to stay put
            "
            }
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
            @"
                This needs to stay put
            "
                        }
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString7()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
            from x in y
            where x > 0
            select x
            {
            @"This can move"
            }
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
                        @"This can move"
                        }
                        """;
                }
            }
            """", index: 1);
    }

    [Fact]
    public async Task TestWithNestedVerbatimString8()
    {
        await VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]$@"
            from x in y
            where x > 0
            select x
            {
                @"This can move"
            }
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = $"""
                        from x in y
                        where x > 0
                        select x
                        {
                            @"This can move"
                        }
                        """;
                }
            }
            """", index: 1);
    }
}
