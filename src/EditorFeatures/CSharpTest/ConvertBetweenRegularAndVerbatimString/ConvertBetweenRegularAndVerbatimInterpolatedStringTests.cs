// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertBetweenRegularAndVerbatimString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertBetweenRegularAndVerbatimString
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertBetweenRegularAndVerbatimString)]
    public class ConvertBetweenRegularAndVerbatimInterpolatedStringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ConvertBetweenRegularAndVerbatimInterpolatedStringCodeRefactoringProvider();

        [Fact]
        public async Task EmptyRegularString()
        {
            await TestMissingAsync("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]";
                    }
                }
                """);
        }

        [Fact]
        public async Task RegularStringWithMissingCloseQuote()
        {
            await TestMissingAsync("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||];
                    }
                }
                """);
        }

        [Fact]
        public async Task VerbatimStringWithMissingCloseQuote()
        {
            await TestMissingAsync("""
                class Test
                {
                    void Method()
                    {
                        var v = $@"[||];
                    }
                }
                """);
        }

        [Fact]
        public async Task EmptyVerbatimString()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $@"[||]";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $"";
                    }
                }
                """);
        }

        [Fact]
        public async Task TestLeadingAndTrailingTrivia()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v =
                            // leading
                            $@"[||]" /* trailing */;
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v =
                            // leading
                            $"" /* trailing */;
                    }
                }
                """);
        }

        [Fact]
        public async Task RegularStringWithBasicText()
        {
            await TestMissingAsync("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]a";
                    }
                }
                """);
        }

        [Fact]
        public async Task VerbatimStringWithBasicText()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $@"[||]a";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $"a";
                    }
                }
                """);
        }

        [Fact]
        public async Task RegularStringWithUnicodeEscape()
        {
            await TestMissingAsync("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]\u0001";
                    }
                }
                """);
        }

        [Fact]
        public async Task RegularStringWithEscapedNewLine()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]a\r\nb";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $@"a
                b";
                    }
                }
                """);
        }

        [Fact]
        public async Task VerbatimStringWithNewLine()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $@"[||]a
                b";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $"a\r\nb";
                    }
                }
                """);
        }

        [Fact]
        public async Task RegularStringWithEscapedNull()
        {
            await TestMissingAsync("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]a\0b";
                    }
                }
                """);
        }

        [Fact]
        public async Task RegularStringWithEscapedQuote()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]a\"b";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $@"a""b";
                    }
                }
                """);
        }

        [Fact]
        public async Task VerbatimStringWithEscapedQuote()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $@"[||]a""b";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $"a\"b";
                    }
                }
                """);
        }

        [Fact]
        public async Task RegularStringWithEscapedQuoteAndMultipleParts()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]{1}\"{2}";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $@"{1}""{2}";
                    }
                }
                """);
        }

        [Fact]
        public async Task VerbatimStringWithEscapedQuoteAndMultipleParts()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $@"[||]{1}""{2}";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $"{1}\"{2}";
                    }
                }
                """);
        }

        [Fact]
        public async Task EscapedCurlyBracesInRegularString()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $"[||]a\r\n{{1}}";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $@"a
                {{1}}";
                    }
                }
                """);
        }

        [Fact]
        public async Task EscapedCurlyBracesInVerbatimString()
        {
            await TestInRegularAndScript1Async("""
                class Test
                {
                    void Method()
                    {
                        var v = $@"[||]a
                {{1}}";
                    }
                }
                """,
                """
                class Test
                {
                    void Method()
                    {
                        var v = $"a\r\n{{1}}";
                    }
                }
                """);
        }
    }
}
