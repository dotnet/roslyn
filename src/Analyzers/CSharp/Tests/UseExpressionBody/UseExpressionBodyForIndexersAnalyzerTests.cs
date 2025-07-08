// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

using VerifyCS = CSharpCodeFixVerifier<
    UseExpressionBodyDiagnosticAnalyzer,
    UseExpressionBodyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForIndexersAnalyzerTests
{
    private static Task TestWithUseExpressionBody(string code, string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenPossible },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never },
            }
        }.RunAsync();

    private static Task TestWithUseBlockBody(string code, string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never },
            }
        }.RunAsync();

    [Fact]
    public async Task TestUseExpressionBody1()
    {
        await TestWithUseExpressionBody("""
            class C
            {
                int Bar() { return 0; }

                {|IDE0026:int this[int i]
                {
                    get
                    {
                        return Bar();
                    }
                }|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int this[int i] => Bar();
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithSetter()
    {
        var code = """
            class C
            {
                int Bar() { return 0; }

                int this[int i]
                {
                    get
                    {
                        return Bar();
                    }

                    set
                    {
                    }
                }
            }
            """;
        await TestWithUseExpressionBody(code, code);
    }

    [Fact]
    public async Task TestMissingOnSetter1()
    {
        var code = """
            class C
            {
                void Bar() { }

                int this[int i]
                {
                    set
                    {
                        Bar();
                    }
                }
            }
            """;
        await TestWithUseExpressionBody(code, code);
    }

    [Fact]
    public async Task TestUseExpressionBody3()
    {
        await TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0026:int this[int i]
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }|}
            }
            """, """
            using System;

            class C
            {
                int this[int i] => throw new NotImplementedException();
            }
            """);
    }

    [Fact]
    public async Task TestUseExpressionBody4()
    {
        await TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0026:int this[int i]
                {
                    get
                    {
                        throw new NotImplementedException(); // comment
                    }
                }|}
            }
            """, """
            using System;

            class C
            {
                int this[int i] => throw new NotImplementedException(); // comment
            }
            """);
    }

    [Fact]
    public async Task TestUseBlockBody1()
    {
        await TestWithUseBlockBody("""
            class C
            {
                int Bar() { return 0; }

                {|IDE0026:int this[int i] => Bar();|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int this[int i]
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20363")]
    public Task TestUseBlockBodyForAccessorEventWhenAccessorWantExpression1()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int Bar() { return 0; }

                {|IDE0026:int this[int i] => Bar();|}
            }
            """,
            FixedCode = """
            class C
            {
                int Bar() { return 0; }

                int this[int i]
                {
                    get => Bar();
                }
            }
            """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible },
            },
            NumberOfFixAllIterations = 2,
            NumberOfIncrementalIterations = 2,
        }.RunAsync();

    [Fact]
    public async Task TestUseBlockBody3()
    {
        await TestWithUseBlockBody("""
            using System;

            class C
            {
                {|IDE0026:int this[int i] => throw new NotImplementedException();|}
            }
            """, """
            using System;

            class C
            {
                int this[int i]
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestUseBlockBody4()
    {
        await TestWithUseBlockBody("""
            using System;

            class C
            {
                {|IDE0026:int this[int i] => throw new NotImplementedException();|} // comment
            }
            """, """
            using System;

            class C
            {
                int this[int i]
                {
                    get
                    {
                        throw new NotImplementedException(); // comment
                    }
                }
            }
            """);
    }
}
