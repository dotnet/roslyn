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
public sealed class ConvertInterpolatedStringToRawStringTests
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
    public Task TestOnCombinedSurrogate()
        => VerifyRefactoringAsync(
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

    [Fact]
    public Task TestOnSplitSurrogate()
        => VerifyRefactoringAsync(
            """
            public class C
            {
                void M()
                {
                    var v = [||]$"\uD83D{0}\uDC69";
                }
            }
            """);

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
    public Task TestSimpleString()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestBraces1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestBraces1_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestBraces2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestBraces2_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestBraces3()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestBraces3_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestBraces4_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_SingleLine1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_SingleLine2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_SingleLine3()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_SingleLine4()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_SingleLine5()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_SingleLine6()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_SingleLine7()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine1_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine1_B()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine1_C()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine1_D()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine1_E()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine1_F()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine3()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine4()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine4_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine5()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine5_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine6()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine6_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine7()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine8()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestInterpolation_MultiLine9()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimSimpleString1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimSimpleString2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestSimpleStringTopLevel()
        => VerifyRefactoringAsync("""
            var v = [||]$"a";
            """, """"
            var v = $"""a""";
            """", outputKind: OutputKind.ConsoleApplication);

    [Fact]
    public Task TestStringWithQuoteInMiddle()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithQuoteInMiddle1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithQuoteInMiddle2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestStringWithQuoteAtStart()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithQuoteAtStart1()
        => VerifyRefactoringAsync(""""
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

    [Fact]
    public Task TestVerbatimStringWithQuoteAtStart2()
        => VerifyRefactoringAsync(""""
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

    [Fact]
    public Task TestStringWithQuoteAtEnd()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithQuoteAtEnd1()
        => VerifyRefactoringAsync(""""
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

    [Fact]
    public Task TestVerbatimStringWithQuoteAtEnd2()
        => VerifyRefactoringAsync(""""
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

    [Fact]
    public Task TestStringWithNewLine()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithNewLine1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithNewLine2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestStringWithNewLineAtStartAndEnd()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithNewLineAtStartAndEnd1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimStringWithNewLineAtStartAndEnd2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestNoIndentVerbatimStringWithNewLineAtStartAndEnd()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestIndentedString()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestIndentedStringTopLevel()
        => VerifyRefactoringAsync("""
            var v = [||]$"goo\r\nbar";
            """, """"
            var v = $"""
                goo
                bar
                """;
            """", outputKind: OutputKind.ConsoleApplication);

    [Fact]
    public Task TestWithoutLeadingWhitespaceTopLevel()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimIndentedString()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestIndentedStringOnOwnLine()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestVerbatimIndentedStringOnOwnLine()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace3()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace4()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace5()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace6()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace7()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace7_A()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace7_B()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithoutLeadingWhitespace8()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString1()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString2()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString3()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString4()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString5()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString6()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString7()
        => VerifyRefactoringAsync("""
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

    [Fact]
    public Task TestWithNestedVerbatimString8()
        => VerifyRefactoringAsync("""
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
