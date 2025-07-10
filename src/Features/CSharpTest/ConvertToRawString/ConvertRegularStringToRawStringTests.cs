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
public sealed class ConvertRegularStringToRawStringTests
{
    private static Task VerifyRefactoringAsync(string testCode, string fixedCode, int index = 0, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp11,
            CodeActionIndex = index,
            TestState =
            {
                OutputKind = outputKind,
            },
        }.RunAsync();

    [Fact]
    public async Task TestNotInDirective()
    {
        var code = """
            #line 1 [||]"goo.cs"
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnEmptyString()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]"";
                }
            }
            """;

        await VerifyRefactoringAsync(code, code);
    }

    [Fact]
    public async Task TestNotOnEmptyVerbatimString()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]@"";
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
                    var v = [||]"\uD800";
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
                    var v = [||]"\uDC00";
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
                    var v = [||]"\uD83D\uDC69";
                }
            }
            """,
            """"
            public class C
            {
                void M()
                {
                    var v = """👩""";
                }
            }
            """");

    [Fact]
    public async Task TestNotOnNullChar()
    {
        var code = """
            public class C
            {
                void M()
                {
                    var v = [||]"\u0000";
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
                    var v = [||]"\u007F";
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
                    var v = [||]"a";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """a""";
                }
            }
            """");

    [Fact]
    public Task TestVerbatimSimpleString()
        => VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@"a";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """a""";
                }
            }
            """");

    [Fact]
    public Task TestSimpleStringTopLevel()
        => VerifyRefactoringAsync("""
            var v = [||]"a";
            """, """"
            var v = """a""";
            """", outputKind: OutputKind.ConsoleApplication);

    [Fact]
    public Task TestStringWithQuoteInMiddle()
        => VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]"goo\"bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """goo"bar""";
                }
            }
            """");

    [Fact]
    public Task TestVerbatimStringWithQuoteInMiddle()
        => VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@"goo""bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """goo"bar""";
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
                    var v = [||]"\"goobar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """
                        "goobar
                        """;
                }
            }
            """");

    [Fact]
    public Task TestVerbatimStringWithQuoteAtStart()
        => VerifyRefactoringAsync(""""
            public class C
            {
                void M()
                {
                    var v = [||]@"""goobar";
                }
            }
            """", """"
            public class C
            {
                void M()
                {
                    var v = """
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
                    var v = [||]"goobar\"";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """
                        goobar"
                        """;
                }
            }
            """");

    [Fact]
    public Task TestVerbatimStringWithQuoteAtEnd()
        => VerifyRefactoringAsync(""""
            public class C
            {
                void M()
                {
                    var v = [||]@"goobar""";
                }
            }
            """", """"
            public class C
            {
                void M()
                {
                    var v = """
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
                    var v = [||]"goo\r\nbar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """
                        goo
                        bar
                        """;
                }
            }
            """");

    [Fact]
    public Task TestVerbatimStringWithNewLine()
        => VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@"goo
            bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """
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
                    var v = [||]"\r\ngoobar\r\n";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """

                        goobar

                        """;
                }
            }
            """");

    [Fact]
    public Task TestVerbatimStringWithNewLineAtStartAndEnd()
        => VerifyRefactoringAsync("""
            public class C
            {
                void M()
                {
                    var v = [||]@"
            goobar
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """

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
                    var v = [||]@"
            goobar
            ";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """
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
                    var v = [||]"goo\r\nbar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """
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
                    var v = [||]@"
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
                    var v = """
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
            var v = [||]"goo\r\nbar";
            """, """"
            var v = """
                goo
                bar
                """;
            """", outputKind: OutputKind.ConsoleApplication);

    [Fact]
    public Task TestWithoutLeadingWhitespaceTopLevel()
        => VerifyRefactoringAsync("""
            var v = [||]@"
            from x in y
            where x > 0
            select x";
            """, """"
            var v = """
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
                    var v = [||]@"goo
            bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v = """
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
                            [||]"goo\r\nbar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                            """
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
                            [||]@"goo
            bar";
                }
            }
            """, """"
            public class C
            {
                void M()
                {
                    var v =
                            """
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
                    var v = [||]@"
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
                    var v = """
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
                    var v = [||]@"
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
                    var v = """
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
                    var v = [||]@"
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
                    var v = """
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
                    var v = [||]@"
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
                    var v = """
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
                    var v = [||]@"
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
                    var v = """
                        from x in y

                        where x > 0

                        select x
                        """;
                }
            }
            """", index: 1);
}
