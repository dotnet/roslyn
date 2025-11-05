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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
public sealed class UseConditionalExpressionForReturnTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    private static readonly ParseOptions CSharp8 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
    private static readonly ParseOptions CSharp9 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseConditionalExpressionForReturnDiagnosticAnalyzer(),
            new CSharpUseConditionalExpressionForReturnCodeFixProvider());

    [Fact]
    public Task TestOnSimpleReturn()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnSimpleReturn_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnSimpleReturn_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestNotWithTwoThrows()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestNotOnSimpleReturn_Throw1_CSharp6()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestNotWithSimpleThrow()
        => TestMissingAsync(
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

    [Fact]
    public Task TestOnSimpleReturnNoBlocks()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnSimpleReturnNoBlocks_NotInBlock()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingReturnValue1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestMissingReturnValue1_Throw()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingReturnValue2()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestMissingReturnValue2_Throw()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingReturnValue3()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWithNoElseBlockButFollowingReturn()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestWithNoElseBlockButFollowingReturn_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestWithNoElseBlockButFollowingReturn_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingWithoutElse()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestMissingWithoutElse_Throw()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70750")]
    public Task TestMissingWithChecked()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
    public Task TestMissingWithCheckedInIf()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
    public Task TestMissingWithUncheckedInIf()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
    public Task TestMissingWithCheckedInTrueStatement()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
    public Task TestMissingWithUncheckedInTrueStatement()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
    public Task TestMissingWithCheckedInFalseStatement()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70748")]
    public Task TestMissingWithUncheckedInFalseStatement()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70750")]
    public Task TestMissingWithUnchecked()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70750")]
    public Task TestMissingWithUnsafe()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConversion1_CSharp8()
        => TestInRegularAndScriptAsync(
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
                    return true ? "a" : "b";
                }
            }
            """, new(parseOptions: CSharp8));

    [Fact]
    public Task TestConversion1_CSharp9()
        => TestInRegularAndScriptAsync(
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
                    return true ? "a" : "b";
                }
            }
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversion1_Throw1_CSharp8()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversion1_Throw1_CSharp9()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversion1_Throw2_CSharp8()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp8));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversion1_Throw2_CSharp9()
        => TestInRegularAndScriptAsync(
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
            """, new(parseOptions: CSharp9));

    [Fact]
    public Task TestConversion2()
        => TestInRegularAndScriptAsync(
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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    [InlineData(LanguageVersion.CSharp8, "(string)null")]
    [InlineData(LanguageVersion.CSharp9, "null")]
    public Task TestConversion2_Throw1(LanguageVersion languageVersion, string expectedFalseExpression)
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversion2_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Theory]
    [InlineData(LanguageVersion.CSharp8, "(string)null")]
    [InlineData(LanguageVersion.CSharp9, "null")]
    public Task TestConversion3(LanguageVersion languageVersion, string expectedFalseExpression)
        => TestInRegularAndScriptAsync(
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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    [InlineData(LanguageVersion.CSharp8, "(string)null")]
    [InlineData(LanguageVersion.CSharp9, "null")]
    public Task TestConversion3_Throw1(LanguageVersion languageVersion, string expectedFalseExpression)
        => TestInRegularAndScriptAsync(
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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    [InlineData(LanguageVersion.CSharp8, "(string)null")]
    [InlineData(LanguageVersion.CSharp9, "null")]
    public Task TestConversion3_Throw2(LanguageVersion languageVersion, string expectedTrue)
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestKeepTriviaAroundIf()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultiLine1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultiLine2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultiLine3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestElseIfWithBlock()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestElseIfWithBlock_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestElseIfWithBlock_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestElseIfWithoutBlock()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestRefReturns1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestRefReturns1_Throw1()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestRefReturns1_Throw2()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    public Task TestOnYieldReturn()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnYieldReturn_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnYieldReturn_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    public Task TestOnYieldReturn_IEnumerableReturnType()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    public Task TestNotOnMixedYields()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestNotOnMixedYields_Throw1()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    public Task TestNotOnMixedYields_IEnumerableReturnType()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    public Task TestNotWithNoElseBlockButFollowingYieldReturn()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestWithNoElseBlockButFollowingYieldReturn_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestNotWithNoElseBlockButFollowingYieldReturn_Throw2()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")]
    public Task TestNotWithNoElseBlockButFollowingYieldReturn_IEnumerableReturnType()
        => TestMissingAsync(
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

    [Fact]
    public Task TestReturnTrueFalse1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse1_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse1_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReturnTrueFalse2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse2_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse2_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReturnTrueFalse3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse3_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse3_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReturnTrueFalse4()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse4_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestReturnTrueFalse4_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36117")]
    public Task TestMissingWhenCrossingPreprocessorDirective()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39260")]
    public Task TestTitleWhenSimplifying()
        => TestInRegularAndScriptAsync(
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
            """, new(title: AnalyzersResources.Simplify_check));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38879")]
    public Task TesSuppressionOperator()
        => TestInRegularAndScriptAsync("""
            #nullable enable

            class Program
            {
                public static string Method(bool empty)
                {
                    [||]if (empty)
                    {
                        return string.Empty;
                    }

                    return null!;
                }
            }
            """, """
            #nullable enable
            
            class Program
            {
                public static string Method(bool empty)
                {
                    return empty ? string.Empty : null!;
                }
            }
            """);

    [Fact]
    public Task TestWithCollectionExpressions()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int[] M()
                {
                    [||]if (true)
                    {
                        return [0];
                    }
                    else
                    {
                        return [1];
                    }
                }
            }
            """,
            """
            class C
            {
                int[] M()
                {
                    return true ? [0] : [1];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60859")]
    public Task UnnecessaryWithinConditionalBranch2()
        => TestInRegularAndScriptAsync(
            """
            public class IssueClass
            {
                double ID;

                public object ConvertFieldValueForStorage(object value)
                {
                    [|if|] (value is IssueClass issue)
                    {
                        return (decimal)issue.ID;
                    }
                    else
                    {
                        return -1m;
                    }
                }
            }
            """,
            """
            public class IssueClass
            {
                double ID;
            
                public object ConvertFieldValueForStorage(object value)
                {
                    return value is IssueClass issue ? (decimal)issue.ID : -1m;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72464")]
    public Task TestMissingWithVariableCollisions()
        => TestMissingAsync(
            """
            using System;

            public class IssueClass
            {
                public object Convert(Type type, string body)
                {
                    [||]if (type == typeof(bool))
                    {
                        return bool.TryParse(body, out bool value) ? 0 : 1;
                    }
                    else
                    {
                        return int.TryParse(body, out int value) ? 2 : 3;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80640")]
    public Task TestMissingWhenBoolOperatorsAreUsed()
        => TestMissingAsync("""
            class C
            {
                bool M()
                {
                    [||]if (this)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public static bool operator true(C v) => true;

                public static bool operator false(C v) => false;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80640")]
    public Task TestWithImplicitBoolConversion()
        => TestInRegularAndScriptAsync("""
            class C
            {
                bool M()
                {
                    [|if|] (this)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                public static implicit operator bool(C v) => true;
            }
            """, """
            class C
            {
                bool M()
                {
                    return this;
                }

                public static implicit operator bool(C v) => true;
            }
            """);
}
