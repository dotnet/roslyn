// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

using VerifyCS = CSharpCodeFixVerifier<
    UseExpressionBodyDiagnosticAnalyzer,
    UseExpressionBodyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForLocalFunctionsAnalyzerTests
{
    private static Task TestMissingWithUseExpressionBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();

    private static Task TestWithUseExpressionBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();

    private static Task TestWithUseExpressionBodyWhenOnSingleLine(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.WhenOnSingleLine } }
        }.RunAsync();

    private static Task TestWithUseBlockBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode, ReferenceAssemblies? referenceAssemblies = null)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.Never } },
            ReferenceAssemblies = referenceAssemblies ?? ReferenceAssemblies.Default,
        }.RunAsync();

    [Fact]
    public Task TestUseExpressionBody1()
        => TestWithUseExpressionBody("""
            class C
            {
                void Test() { }

                void Goo()
                {
                    {|IDE0061:void Bar()
                    {
                        Test();
                    }|}
                }
            }
            """, """
            class C
            {
                void Test() { }

                void Goo()
                {
                    void Bar() => Test();
                }
            }
            """);

    [Fact]
    public Task TestUseExpressionBody2()
        => TestWithUseExpressionBody("""
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    {|IDE0061:int Bar()
                    {
                        return Test();
                    }|}
                }
            }
            """, """
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    int Bar() => Test();
                }
            }
            """);

    [Fact]
    public Task TestUseExpressionBody3()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                void Goo()
                {
                    {|IDE0061:int Bar()
                    {
                        throw new NotImplementedException();
                    }|}
                }
            }
            """, """
            using System;

            class C
            {
                void Goo()
                {
                    int Bar() => throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestUseExpressionBody4()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                void Goo()
                {
                    {|IDE0061:int Bar()
                    {
                        throw new NotImplementedException(); // comment
                    }|}
                }
            }
            """, """
            using System;

            class C
            {
                void Goo()
                {
                    int Bar() => throw new NotImplementedException(); // comment
                }
            }
            """);

    [Fact]
    public async Task TestUseExpressionBodyWhenOnSingleLineMissing()
    {
        var code = """
            class C
            {
                void Goo()
                {
                    int Bar()
                    {
                        return 1 +
                            2 +
                            3;
                    }
                }
            }
            """;
        await TestWithUseExpressionBodyWhenOnSingleLine(code, code);
    }

    [Fact]
    public Task TestUseExpressionBodyWhenOnSingleLine()
        => TestWithUseExpressionBodyWhenOnSingleLine("""
            class C
            {
                void Goo()
                {
                    {|IDE0061:int Bar()
                    {
                        return 1 + 2 + 3;
                    }|}
                }
            }
            """, """
            class C
            {
                void Goo()
                {
                    int Bar() => 1 + 2 + 3;
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody1()
        => TestWithUseBlockBody("""
            class C
            {
                void Test() { }

                void Goo()
                {
                    {|IDE0061:void Bar() => Test();|}
                }
            }
            """, """
            class C
            {
                void Test() { }

                void Goo()
                {
                    void Bar()
                    {
                        Test();
                    }
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody2()
        => TestWithUseBlockBody("""
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    {|IDE0061:int Bar() => Test();|}
                }
            }
            """, """
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    int Bar()
                    {
                        return Test();
                    }
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody3()
        => TestWithUseBlockBody("""
            using System;

            class C
            {
                void Goo()
                {
                    {|IDE0061:int Bar() => throw new NotImplementedException();|}
                }
            }
            """, """
            using System;

            class C
            {
                void Goo()
                {
                    int Bar()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody4()
        => TestWithUseBlockBody("""
            using System;

            class C
            {
                void Goo()
                {
                    {|IDE0061:int Bar() => throw new NotImplementedException();|} // comment
                }
            }
            """, """
            using System;

            class C
            {
                void Goo()
                {
                    int Bar()
                    {
                        throw new NotImplementedException(); // comment
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments1()
        => TestWithUseExpressionBody("""
            class C
            {
                void Test() { }

                void Goo()
                {
                    {|IDE0061:void Bar()
                    {
                        // Comment
                        Test();
                    }|}
                }
            }
            """, """
            class C
            {
                void Test() { }

                void Goo()
                {
                    void Bar() =>
                        // Comment
                        Test();
                }
            }
            """);

    [Fact]
    public Task TestComments2()
        => TestWithUseExpressionBody("""
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    {|IDE0061:int Bar()
                    {
                        // Comment
                        return Test();
                    }|}
                }
            }
            """, """
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    int Bar() =>
                        // Comment
                        Test();
                }
            }
            """);

    [Fact]
    public Task TestComments3()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                Exception Test() { return new Exception(); }

                void Goo()
                {
                    {|IDE0061:void Bar()
                    {
                        // Comment
                        throw Test();
                    }|}
                }
            }
            """, """
            using System;

            class C
            {
                Exception Test() { return new Exception(); }

                void Goo()
                {
                    void Bar() =>
                        // Comment
                        throw Test();
                }
            }
            """);

    [Fact]
    public Task TestComments4()
        => TestWithUseExpressionBody("""
            class C
            {
                void Test() { }

                void Goo()
                {
                    {|IDE0061:void Bar()
                    {
                        Test(); // Comment
                    }|}
                }
            }
            """, """
            class C
            {
                void Test() { }

                void Goo()
                {
                    void Bar() => Test(); // Comment
                }
            }
            """);

    [Fact]
    public Task TestComments5()
        => TestWithUseExpressionBody("""
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    {|IDE0061:int Bar()
                    {
                        return Test(); // Comment
                    }|}
                }
            }
            """, """
            class C
            {
                int Test() { return 0; }

                void Goo()
                {
                    int Bar() => Test(); // Comment
                }
            }
            """);

    [Fact]
    public Task TestComments6()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                Exception Test() { return new Exception(); }

                void Goo()
                {
                    {|IDE0061:void Bar()
                    {
                        throw Test(); // Comment
                    }|}
                }
            }
            """, """
            using System;

            class C
            {
                Exception Test() { return new Exception(); }

                void Goo()
                {
                    void Bar() => throw Test(); // Comment
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80400")]
    public Task TestDirectives1()
        => TestMissingWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
                    void Bar()
                    {
            #if DEBUG
                        Console.WriteLine();
            #endif
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80400")]
    public Task TestDirectives2()
        => TestMissingWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
                    void Bar()
                    {
            #if DEBUG
                        Console.WriteLine();
            #elif RELEASE
                        Console.WriteLine();
            #endif
                    }
                }
            }
            """);

    [Fact]
    public Task TestDirectives3()
        => TestWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
                    {|IDE0061:void Bar()
                    {
            #if DEBUG
                        Console.WriteLine(0);
            #else
                        Console.WriteLine(1);
            #endif
                    }|}
                }
            }
            """, """
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
                    void Bar() =>
            #if DEBUG
                        Console.WriteLine(0);
            #else
                        Console.WriteLine(1);
            #endif

                }
            }
            """);

    [Fact]
    public Task TestDirectives4()
        => TestWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
                    {|IDE0061:void Bar()
                    {
            #if DEBUG
                        Console.WriteLine(0);
            #elif RELEASE
                        Console.WriteLine(1);
            #else
                        Console.WriteLine(2);
            #endif
                    }|}
                }
            }
            """, """
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
                    void Bar() =>
            #if DEBUG
                        Console.WriteLine(0);
            #elif RELEASE
                        Console.WriteLine(1);
            #else
                        Console.WriteLine(2);
            #endif

                }
            }
            """);

    [Fact]
    public Task TestUseBlockBodyAsync1()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                async Task Goo()
                {
                    {|IDE0061:async Task Bar() => await Test();|}
                }

                Task Test() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async Task Goo()
                {
                    async Task Bar()
                    {
                        await Test();
                    }
                }

                Task Test() { return Task.CompletedTask; }
            }
            """);

    [Fact]
    public Task TestUseBlockBodyAsync2()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                async void Goo()
                {
                    {|IDE0061:async void Bar() => await Test();|}
                }

                Task Test() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async void Goo()
                {
                    async void Bar()
                    {
                        await Test();
                    }
                }

                Task Test() { return Task.CompletedTask; }
            }
            """);

    [Fact]
    public Task TestUseBlockBodyAsync3()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                void Goo() 
                {
                    {|IDE0061:async ValueTask Test() => await Bar();|}
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                void Goo() 
                {
                    async ValueTask Test()
                    {
                        await Bar();
                    }
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """, ReferenceAssemblies.NetStandard.NetStandard21);

    [Fact]
    public Task TestUseBlockBodyAsync4()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    {|IDE0061:Task<int> Test() => Bar();|}
                }

                Task<int> Bar() { return Task.FromResult(0); }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Task<int> Test()
                    {
                        return Bar();
                    }
                }

                Task<int> Bar() { return Task.FromResult(0); }
            }
            """);

    [Fact]
    public Task TestUseBlockBodyAsync5()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    {|IDE0061:Task Test() => Bar();|}
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                void Goo()
                {
                    Task Test()
                    {
                        return Bar();
                    }
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """);

    [Fact]
    public Task TestUseBlockBodyNestedLocalFunction()
        => TestWithUseBlockBody("""
            class C
            {
                void NestedTest() { }

                void Goo()
                {
                    void Bar()
                    {
                        {|IDE0061:void Test() => NestedTest();|}
                    }
                }
            }
            """, """
            class C
            {
                void NestedTest() { }

                void Goo()
                {
                    void Bar()
                    {
                        void Test()
                        {
                            NestedTest();
                        }
                    }
                }
            }
            """);

    [Fact]
    public Task TestUseExpressionBodyNestedLocalFunction()
        => TestWithUseExpressionBody("""
            class C
            {
                void NestedTest() { }

                void Goo()
                {
                    void Bar()
                    {
                        {|IDE0061:void Test()
                        {
                            NestedTest();
                        }|}
                    }
                }
            }
            """, """
            class C
            {
                void NestedTest() { }

                void Goo()
                {
                    void Bar()
                    {
                        void Test() => NestedTest();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57570")]
    public Task TestUseExpressionBodyTopLevelStatment()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
                Sources =
                {
                    """
                    {|IDE0061:int Bar(int x)
                    {
                        return x;
                    }|}
                    """
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    int Bar(int x) => x;
                    """
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.WhenPossible } },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57570")]
    public Task TestUseBlockBodyTopLevelStatment()
        => new VerifyCS.Test
        {
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
                Sources =
                {
                    """
                    {|IDE0061:int Bar(int x) => x;|}
                    """
                },
            },
            FixedState =
            {
                Sources =
                {
                    """
                    int Bar(int x) { return x; }
                    """
                },
            },
            LanguageVersion = LanguageVersion.CSharp9,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedLocalFunctions, ExpressionBodyPreference.Never } },
        }.RunAsync();
}
