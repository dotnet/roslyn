// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
    public partial class UseConditionalExpressionForReturnTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        private static readonly ParseOptions CSharp8 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
        private static readonly ParseOptions CSharp9 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

        public UseConditionalExpressionForReturnTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseConditionalExpressionForReturnDiagnosticAnalyzer(),
                new CSharpUseConditionalExpressionForReturnCodeFixProvider());

        [Fact]
        public async Task TestOnSimpleReturn()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnSimpleReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true ? throw new System.Exception() : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnSimpleReturn_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return 0;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true ? 0 : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestNotWithTwoThrows()
        {
            await TestMissingAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestNotOnSimpleReturn_Throw1_CSharp6()
        {
            await TestMissingAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """, parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestNotWithSimpleThrow()
        {
            await TestMissingAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            throw;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnSimpleReturnNoBlocks()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                            return 0;
                        else
                            return 1;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnSimpleReturnNoBlocks_NotInBlock()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        if (true)
                            [||]if (true)
                                return 0;
                            else
                                return 1;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        if (true)
                            return true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingReturnValue1()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return 0;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestMissingReturnValue1_Throw()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingReturnValue2()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestMissingReturnValue2_Throw()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingReturnValue3()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestWithNoElseBlockButFollowingReturn()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        [||]if (true)
                        {
                            return 0;
                        }

                        return 1;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        return true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestWithNoElseBlockButFollowingReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }

                        return 1;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        return true ? throw new System.Exception() : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestWithNoElseBlockButFollowingReturn_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        [||]if (true)
                        {
                            return 0;
                        }

                        throw new System.Exception();
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        return true ? 0 : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutElse()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return 0;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestMissingWithoutElse_Throw()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70750")]
        public async Task TestMissingWithChecked()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (x < 0)
                        {
                            throw new System.Exception();
                        }
                        checked
                        {
                            return x - y;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
        public async Task TestMissingWithCheckedInIf()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (checked(x == y))
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        return checked(x == y) ? 0 : 1;
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
        public async Task TestMissingWithUncheckedInIf()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (unchecked(x == y))
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        return unchecked(x == y) ? 0 : 1;
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
        public async Task TestMissingWithCheckedInTrueStatement()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (x == y)
                        {
                            return checked(x - y);
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        return x == y ? checked(x - y) : 1;
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
        public async Task TestMissingWithUncheckedInTrueStatement()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (x == y)
                        {
                            return unchecked(x - y);
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        return x == y ? unchecked(x - y) : 1;
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
        public async Task TestMissingWithCheckedInFalseStatement()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (x == y)
                        {
                            return 1;
                        }
                        else
                        {
                            return checked(x - y);
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        return x == y ? 1 : checked(x - y);
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
        public async Task TestMissingWithUncheckedInFalseStatement()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (x == y)
                        {
                            return 1;
                        }
                        else
                        {
                            return unchecked(x - y);
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        return x == y ? 1 : unchecked(x - y);
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70750")]
        public async Task TestMissingWithUnchecked()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (x < 0)
                        {
                            throw new System.Exception();
                        }
                        unchecked
                        {
                            return x - y;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70750")]
        public async Task TestMissingWithUnsafe()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        int x = 0;
                        int y = 0;
                        [||]if (x < 0)
                        {
                            throw new System.Exception();
                        }
                        unsafe
                        {
                            return x - y;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestConversion1_CSharp8()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    object M()
                    {
                        [||]if (true)
                        {
                            return "a";
                        }
                        else
                        {
                            return "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    object M()
                    {
                        return true ? "a" : (object)"b";
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact]
        public async Task TestConversion1_CSharp9()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    object M()
                    {
                        [||]if (true)
                        {
                            return "a";
                        }
                        else
                        {
                            return "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    object M()
                    {
                        return true ? "a" : (object)"b";
                    }
                }
                """, parseOptions: CSharp9);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversion1_Throw1_CSharp8()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    object M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    object M()
                    {
                        return true ? throw new System.Exception() : (object)"b";
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversion1_Throw1_CSharp9()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    object M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    object M()
                    {
                        return true ? throw new System.Exception() : (object)"b";
                    }
                }
                """, parseOptions: CSharp9);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversion1_Throw2_CSharp8()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    object M()
                    {
                        [||]if (true)
                        {
                            return "a";
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    object M()
                    {
                        return true ? (object)"a" : throw new System.Exception();
                    }
                }
                """, parseOptions: CSharp8);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversion1_Throw2_CSharp9()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    object M()
                    {
                        [||]if (true)
                        {
                            return "a";
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    object M()
                    {
                        return true ? (object)"a" : throw new System.Exception();
                    }
                }
                """, parseOptions: CSharp9);
        }

        [Fact]
        public async Task TestConversion2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    string M()
                    {
                        [||]if (true)
                        {
                            return "a";
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    string M()
                    {
                        return true ? "a" : null;
                    }
                }
                """);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        [InlineData(LanguageVersion.CSharp8, "(string)null")]
        [InlineData(LanguageVersion.CSharp9, "null")]
        public async Task TestConversion2_Throw1(LanguageVersion languageVersion, string expectedFalseExpression)
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    string M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    string M()
                    {
                        return true ? throw new System.Exception() : 
                """ + expectedFalseExpression + """
                ;
                    }
                }
                """, parameters: new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversion2_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    string M()
                    {
                        [||]if (true)
                        {
                            return "a";
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    string M()
                    {
                        return true ? "a" : throw new System.Exception();
                    }
                }
                """);
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp8, "(string)null")]
        [InlineData(LanguageVersion.CSharp9, "null")]
        public async Task TestConversion3(LanguageVersion languageVersion, string expectedFalseExpression)
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    string M()
                    {
                        [||]if (true)
                        {
                            return null;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    string M()
                    {
                        return true ? null : 
                """ + expectedFalseExpression + """
                ;
                    }
                }
                """, parameters: new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        [InlineData(LanguageVersion.CSharp8, "(string)null")]
        [InlineData(LanguageVersion.CSharp9, "null")]
        public async Task TestConversion3_Throw1(LanguageVersion languageVersion, string expectedFalseExpression)
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    string M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    string M()
                    {
                        return true ? throw new System.Exception() : 
                """ + expectedFalseExpression + """
                ;
                    }
                }
                """, parameters: new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        [InlineData(LanguageVersion.CSharp8, "(string)null")]
        [InlineData(LanguageVersion.CSharp9, "null")]
        public async Task TestConversion3_Throw2(LanguageVersion languageVersion, string expectedTrue)
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    string M()
                    {
                        [||]if (true)
                        {
                            return null;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    string M()
                    {
                        return true ? 
                """ + expectedTrue + """
                 : throw new System.Exception();
                    }
                }
                """, parameters: new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion)));
        }

        [Fact]
        public async Task TestKeepTriviaAroundIf()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        // leading
                        [||]if (true)
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        } // trailing
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        // leading
                        return true ? 0 : 1; // trailing
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        {|FixAllInDocument:if|} (true)
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }

                        if (true)
                        {
                            return 2;
                        }

                        return 3;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true ? 0 : 1;

                        return true ? 2 : 3;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMultiLine1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return Foo(
                                1, 2, 3);
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true
                            ? Foo(
                                1, 2, 3)
                            : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMultiLine2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return 0;
                        }
                        else
                        {
                            return Foo(
                                1, 2, 3);
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true
                            ? 0
                            : Foo(
                                1, 2, 3);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMultiLine3()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            return Foo(
                                1, 2, 3);
                        }
                        else
                        {
                            return Foo(
                                4, 5, 6);
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        return true
                            ? Foo(
                                1, 2, 3)
                            : Foo(
                                4, 5, 6);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestElseIfWithBlock()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        if (true)
                        {
                        }
                        else [||]if (false)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            return false ? 1 : 0;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestElseIfWithBlock_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        if (true)
                        {
                        }
                        else [||]if (false)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            return false ? throw new System.Exception() : 0;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestElseIfWithBlock_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        if (true)
                        {
                        }
                        else [||]if (false)
                        {
                            return 1;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            return false ? 1 : throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestElseIfWithoutBlock()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        if (true) return 2;
                        else [||]if (false) return 1;
                        else return 0;
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        if (true) return 2;
                        else return false ? 1 : 0;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestRefReturns1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    ref int M(ref int i, ref int j)
                    {
                        [||]if (true)
                        {
                            return ref i;
                        }
                        else
                        {
                            return ref j;
                        }
                    }
                }
                """,
                """
                class C
                {
                    ref int M(ref int i, ref int j)
                    {
                        return ref true ? ref i : ref j;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestRefReturns1_Throw1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    ref int M(ref int i, ref int j)
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return ref j;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestRefReturns1_Throw2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    ref int M(ref int i, ref int j)
                    {
                        [||]if (true)
                        {
                            return ref i;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        public async Task TestOnYieldReturn()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            yield return 0;
                        }
                        else
                        {
                            yield return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        yield return true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnYieldReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            yield return 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        yield return true ? throw new System.Exception() : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnYieldReturn_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            yield return 0;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    int M()
                    {
                        yield return true ? 0 : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        public async Task TestOnYieldReturn_IEnumerableReturnType()
        {
            await TestInRegularAndScript1Async(
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M()
                    {
                        [||]if (true)
                        {
                            yield return 0;
                        }
                        else
                        {
                            yield return 1;
                        }
                    }
                }
                """,
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M()
                    {
                        yield return true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        public async Task TestNotOnMixedYields()
        {
            await TestMissingAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            yield break;
                        }
                        else
                        {
                            yield return 1;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestNotOnMixedYields_Throw1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    int M()
                    {
                        [||]if (true)
                        {
                            yield break;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        public async Task TestNotOnMixedYields_IEnumerableReturnType()
        {
            await TestMissingAsync(
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M()
                    {
                        [||]if (true)
                        {
                            yield break;
                        }
                        else
                        {
                            yield return 1;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        public async Task TestNotWithNoElseBlockButFollowingYieldReturn()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        [||]if (true)
                        {
                            yield return 0;
                        }

                        yield return 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestWithNoElseBlockButFollowingYieldReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        [||]if (true)
                        {
                            throw new System.Exception();
                        }

                        yield return 1;
                    }
                }
                """,

                """
                class C
                {
                    void M()
                    {
                        yield return true ? throw new System.Exception() : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestNotWithNoElseBlockButFollowingYieldReturn_Throw2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        [||]if (true)
                        {
                            yield return 0;
                        }

                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
        public async Task TestNotWithNoElseBlockButFollowingYieldReturn_IEnumerableReturnType()
        {
            await TestMissingAsync(
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> M()
                    {
                        [||]if (true)
                        {
                            yield return 0;
                        }

                        yield return 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestReturnTrueFalse1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a == 0;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse1_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a == 0 ? throw new System.Exception() : false;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse1_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            return true;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a == 0 ? true : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestReturnTrueFalse2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a != 0;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse2_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a == 0 ? throw new System.Exception() : true;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse2_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            return false;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a == 0 ? false : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestReturnTrueFalse3()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            return false;
                        }

                        return true;
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a != 0;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse3_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            throw new System.Exception();
                        }

                        return true;
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a == 0 ? throw new System.Exception() : true;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse3_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    bool M(int a)
                    {
                        [||]if (a == 0)
                        {
                            return false;
                        }

                        throw new System.Exception();
                    }
                }
                """,
                """
                class C
                {
                    bool M(int a)
                    {
                        return a == 0 ? false : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestReturnTrueFalse4()
        {
            await TestInRegularAndScript1Async(
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<bool> M(int a)
                    {
                        [||]if (a == 0)
                        {
                            yield return false;
                        }
                        else
                        {
                            yield return true;
                        }
                    }
                }
                """,
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<bool> M(int a)
                    {
                        yield return a != 0;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse4_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<bool> M(int a)
                    {
                        [||]if (a == 0)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            yield return true;
                        }
                    }
                }
                """,
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<bool> M(int a)
                    {
                        yield return a == 0 ? throw new System.Exception() : true;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestReturnTrueFalse4_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<bool> M(int a)
                    {
                        [||]if (a == 0)
                        {
                            yield return false;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<bool> M(int a)
                    {
                        yield return a == 0 ? false : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36117")]
        public async Task TestMissingWhenCrossingPreprocessorDirective()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    int M()
                    {
                        bool check = true;
                #if true
                        [||]if (check)
                            return 3;
                #endif
                        return 2;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39260")]
        public async Task TestTitleWhenSimplifying()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string node1, string node2)
                    {
                        [|if|] (AreSimilarCore(node1, node2))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    private bool AreSimilarCore(string node1, string node2)
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
                """
                class C
                {
                    void M(string node1, string node2)
                    {
                        return AreSimilarCore(node1, node2);
                    }

                    private bool AreSimilarCore(string node1, string node2)
                    {
                        throw new NotImplementedException();
                    }
                }
                """, title: AnalyzersResources.Simplify_check);
        }
    }
}
