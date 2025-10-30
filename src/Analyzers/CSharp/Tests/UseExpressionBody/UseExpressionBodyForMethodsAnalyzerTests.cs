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
public sealed class UseExpressionBodyForMethodsAnalyzerTests
{
    private static Task TestMissingWithUseExpressionBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        LanguageVersion version = LanguageVersion.CSharp8)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = version,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();

    private static Task TestWithUseExpressionBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        LanguageVersion version = LanguageVersion.CSharp8)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = version,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();

    private static Task TestWithUseBlockBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        ReferenceAssemblies? referenceAssemblies = null)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, ExpressionBodyPreference.Never } },
            ReferenceAssemblies = referenceAssemblies ?? ReferenceAssemblies.Default,
        }.RunAsync();

    [Fact]
    public void TestOptionEditorConfig1()
    {
        var option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("true", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.WhenPossible, option.Value);
        Assert.Equal(NotificationOption2.Silent, option.Notification);

        option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("false", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.Never, option.Value);
        Assert.Equal(NotificationOption2.Silent, option.Notification);

        option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_on_single_line", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.WhenOnSingleLine, option.Value);
        Assert.Equal(NotificationOption2.Silent, option.Notification);

        option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("true:blah", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.Never, option.Value);
        Assert.Equal(NotificationOption2.Silent, option.Notification);

        option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_blah:error", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.Never, option.Value);
        Assert.Equal(NotificationOption2.Silent, option.Notification);

        option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("false:error", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.Never, option.Value);
        Assert.Equal(NotificationOption2.Error.WithIsExplicitlySpecified(true), option.Notification);

        option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("true:warning", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.WhenPossible, option.Value);
        Assert.Equal(NotificationOption2.Warning.WithIsExplicitlySpecified(true), option.Notification);

        option = CSharpCodeStyleOptions.ParseExpressionBodyPreference("when_on_single_line:suggestion", CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        Assert.Equal(ExpressionBodyPreference.WhenOnSingleLine, option.Value);
        Assert.Equal(NotificationOption2.Suggestion.WithIsExplicitlySpecified(true), option.Notification);
    }

    [Fact]
    public Task TestUseExpressionBody1()
        => TestWithUseExpressionBody("""
            class C
            {
                void Bar() => Bar();

                {|IDE0022:void Goo()
                {
                    Bar();
                }|}
            }
            """, """
            class C
            {
                void Bar() => Bar();

                void Goo() => Bar();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody2()
        => TestWithUseExpressionBody("""
            class C
            {
                int Bar() => 0;

                {|IDE0022:int Goo()
                {
                    return Bar();
                }|}
            }
            """, """
            class C
            {
                int Bar() => 0;

                int Goo() => Bar();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody3()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0022:int Goo()
                {
                    throw new NotImplementedException();
                }|}
            }
            """, """
            using System;

            class C
            {
                int Goo() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody4()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0022:int Goo()
                {
                    throw new NotImplementedException(); // comment
                }|}
            }
            """, """
            using System;

            class C
            {
                int Goo() => throw new NotImplementedException(); // comment
            }
            """);

    [Fact]
    public Task TestUseBlockBody1()
        => TestWithUseBlockBody("""
            class C
            {
                void Bar() { }

                {|IDE0022:void Goo() => Bar();|}
            }
            """, """
            class C
            {
                void Bar() { }

                void Goo()
                {
                    Bar();
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody2()
        => TestWithUseBlockBody("""
            class C
            {
                int Bar() { return 0; }

                {|IDE0022:int Goo() => Bar();|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int Goo()
                {
                    return Bar();
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody3()
        => TestWithUseBlockBody("""
            using System;

            class C
            {
                {|IDE0022:int Goo() => throw new NotImplementedException();|}
            }
            """, """
            using System;

            class C
            {
                int Goo()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody4()
        => TestWithUseBlockBody("""
            using System;

            class C
            {
                {|IDE0022:int Goo() => throw new NotImplementedException();|} // comment
            }
            """, """
            using System;

            class C
            {
                int Goo()
                {
                    throw new NotImplementedException(); // comment
                }
            }
            """);

    [Fact]
    public Task TestComments1()
        => TestWithUseExpressionBody("""
            class C
            {
                void Bar() => Bar();

                {|IDE0022:void Goo()
                {
                    // Comment
                    Bar();
                }|}
            }
            """, """
            class C
            {
                void Bar() => Bar();

                void Goo() =>
                    // Comment
                    Bar();
            }
            """);

    [Fact]
    public Task TestComments2()
        => TestWithUseExpressionBody("""
            class C
            {
                int Bar() => 0;

                {|IDE0022:int Goo()
                {
                    // Comment
                    return Bar();
                }|}
            }
            """, """
            class C
            {
                int Bar() => 0;

                int Goo() =>
                    // Comment
                    Bar();
            }
            """);

    [Fact]
    public Task TestComments3()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                Exception Bar() => new Exception();

                {|IDE0022:void Goo()
                {
                    // Comment
                    throw Bar();
                }|}
            }
            """, """
            using System;

            class C
            {
                Exception Bar() => new Exception();

                void Goo() =>
                    // Comment
                    throw Bar();
            }
            """);

    [Fact]
    public Task TestComments4()
        => TestWithUseExpressionBody("""
            class C
            {
                void Bar() => Bar();

                {|IDE0022:void Goo()
                {
                    Bar(); // Comment
                }|}
            }
            """, """
            class C
            {
                void Bar() => Bar();

                void Goo() => Bar(); // Comment
            }
            """);

    [Fact]
    public Task TestComments5()
        => TestWithUseExpressionBody("""
            class C
            {
                int Bar() => 0;

                {|IDE0022:int Goo()
                {
                    return Bar(); // Comment
                }|}
            }
            """, """
            class C
            {
                int Bar() => 0;

                int Goo() => Bar(); // Comment
            }
            """);

    [Fact]
    public Task TestComments6()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                Exception Bar() => new Exception();

                {|IDE0022:void Goo()
                {
                    throw Bar(); // Comment
                }|}
            }
            """, """
            using System;

            class C
            {
                Exception Bar() => new Exception();

                void Goo() => throw Bar(); // Comment
            }
            """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80400")]
    public Task TestDirectives1()
        => TestMissingWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
            #if DEBUG
                    Console.WriteLine();
            #endif
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    public Task TestDirectives2()
        => TestWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                {|IDE0022:void Method()
                {
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    Console.WriteLine(1);
            #endif
                }|}
            }
            """, """
            #define DEBUG
            using System;

            class Program
            {
                void Method() =>
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    Console.WriteLine(1);
            #endif

            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69783")]
    public async Task TestDirectives3()
    {
        var code = """
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    Console.WriteLine(1);
                    Console.WriteLine(2);
            #endif
                }
            }
            """;
        await TestWithUseExpressionBody(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    public Task TestDirectives4()
        => TestWithUseExpressionBody("""
            #define RELEASE
            using System;

            class Program
            {
                {|IDE0022:void Method()
                {
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    Console.WriteLine(1);
            #endif
                }|}
            }
            """, """
            #define RELEASE
            using System;

            class Program
            {
                void Method() =>
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    Console.WriteLine(1);
            #endif

            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    public Task TestDirectives5()
        => TestWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                {|IDE0022:void Method()
                {
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    throw new System.NotImplementedException();
            #endif
                }|}
            }
            """, """
            #define DEBUG
            using System;

            class Program
            {
                void Method() =>
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    throw new System.NotImplementedException();
            #endif

            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    public Task TestDirectives6()
        => TestWithUseExpressionBody("""
            #define RELEASE
            using System;

            class Program
            {
                {|IDE0022:void Method()
                {
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    throw new System.NotImplementedException();
            #endif
                }|}
            }
            """, """
            #define RELEASE
            using System;

            class Program
            {
                void Method() =>
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    throw new System.NotImplementedException();
            #endif

            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69783")]
    public async Task TestDirectives7()
    {
        var code = """
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
            #if DEBUG
            #endif
                    Console.WriteLine(0);
                }
            }
            """;
        await TestWithUseExpressionBody(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69783")]
    public async Task TestDirectives8()
    {
        var code = """
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
                    Console.WriteLine(0);
            #if DEBUG
            #endif
                }
            }
            """;
        await TestWithUseExpressionBody(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69783")]
    public async Task TestDirectives9()
    {
        var code = """
            #define DEBUG
            using System;

            class Program
            {
                void Method()
                {
            #if DEBUG
                    Console.WriteLine(0);
            #else
                    Console.WriteLine(1);
            #endif

            #if DEBUG
            #endif
                }
            }
            """;
        await TestWithUseExpressionBody(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    public Task TestDirectives10()
        => TestWithUseExpressionBody("""
            #define DEBUG
            using System;

            class Program
            {
                {|IDE0022:void Method()
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
            """, """
            #define DEBUG
            using System;

            class Program
            {
                void Method() =>
            #if DEBUG
                    Console.WriteLine(0);
            #elif RELEASE
                    Console.WriteLine(1);
            #else
                    Console.WriteLine(2);
            #endif

            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    public Task TestDirectives11()
        => TestWithUseExpressionBody("""
            #define RELEASE
            using System;

            class Program
            {
                {|IDE0022:void Method()
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
            """, """
            #define RELEASE
            using System;

            class Program
            {
                void Method() =>
            #if DEBUG
                    Console.WriteLine(0);
            #elif RELEASE
                    Console.WriteLine(1);
            #else
                    Console.WriteLine(2);
            #endif

            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17120")]
    public Task TestDirectives12()
        => TestWithUseExpressionBody("""
            #define OTHER
            using System;

            class Program
            {
                {|IDE0022:void Method()
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
            """, """
            #define OTHER
            using System;

            class Program
            {
                void Method() =>
            #if DEBUG
                    Console.WriteLine(0);
            #elif RELEASE
                    Console.WriteLine(1);
            #else
                    Console.WriteLine(2);
            #endif

            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp6()
        => TestWithUseExpressionBody("""
            using System;
            class C
            {
                {|IDE0022:void M() {|CS8026:=>|} {|CS8026:throw|} new NotImplementedException();|}
            }
            """, """
            using System;
            class C
            {
                void M()
                {
                    throw new NotImplementedException();
                }
            }
            """, LanguageVersion.CSharp5);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20352")]
    public async Task TestDoNotOfferToConvertToBlockIfExpressionBodyPreferredIfCSharp6()
    {
        var code = """
            using System;
            class C
            {
                int M() => 0;
            }
            """;
        await TestWithUseExpressionBody(code, code, LanguageVersion.CSharp6);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20352")]
    public Task TestOfferToConvertToExpressionIfCSharp6()
        => TestWithUseExpressionBody("""
            using System;
            class C
            {
                {|IDE0022:int M() { return 0; }|}
            }
            """, """
            using System;
            class C
            {
                int M() => 0;
            }
            """, LanguageVersion.CSharp6);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20352")]
    public async Task TestDoNotOfferToConvertToExpressionInCSharp6IfThrowExpression()
    {
        var code = """
            using System;
            class C
            {
                // throw expressions not supported in C# 6.
                void M() { throw new Exception(); }
            }
            """;
        await TestWithUseExpressionBody(code, code, LanguageVersion.CSharp6);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp6_FixAll()
        => TestWithUseExpressionBody("""
            using System;
            class C
            {
                {|IDE0022:void M() => {|CS8059:throw|} new NotImplementedException();|}
                {|IDE0022:void M(int i) => {|CS8059:throw|} new NotImplementedException();|}
                int M(bool b) => 0;
            }
            """, """
            using System;
            class C
            {
                void M()
                {
                    throw new NotImplementedException();
                }

                void M(int i)
                {
                    throw new NotImplementedException();
                }

                int M(bool b) => 0;
            }
            """, LanguageVersion.CSharp6);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25202")]
    public Task TestUseBlockBodyAsync1()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                {|IDE0022:async Task Goo() => await Bar();|}

                Task Bar() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async Task Goo()
                {
                    await Bar();
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25202")]
    public Task TestUseBlockBodyAsync2()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                {|IDE0022:async void Goo() => await Bar();|}

                Task Bar() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async void Goo()
                {
                    await Bar();
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25202")]
    public Task TestUseBlockBodyAsync3()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                {|IDE0022:async void Goo() => await Bar();|}

                Task Bar() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async void Goo()
                {
                    await Bar();
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25202")]
    public Task TestUseBlockBodyAsync4()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                {|IDE0022:async ValueTask Goo() => await Bar();|}

                Task Bar() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async ValueTask Goo()
                {
                    await Bar();
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """, ReferenceAssemblies.NetStandard.NetStandard21);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25202")]
    public Task TestUseBlockBodyAsync5()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                {|IDE0022:async Task<int> Goo() => await Bar();|}

                Task<int> Bar() { return Task.FromResult(0); }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async Task<int> Goo()
                {
                    return await Bar();
                }

                Task<int> Bar() { return Task.FromResult(0); }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25202")]
    public Task TestUseBlockBodyAsync6()
        => TestWithUseBlockBody("""
            using System.Threading.Tasks;

            class C
            {
                {|IDE0022:Task Goo() => Bar();|}

                Task Bar() { return Task.CompletedTask; }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                Task Goo()
                {
                    return Bar();
                }

                Task Bar() { return Task.CompletedTask; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53532")]
    public Task TestUseBlockBodyTrivia1()
        => TestWithUseBlockBody("""
            using System;
            class C
            {
                {|IDE0022:void M()
                    // Test
                    => Console.WriteLine();|}
            }
            """, """
            using System;
            class C
            {
                void M()
                {
                    // Test
                    Console.WriteLine();
                }
            }
            """);
}
