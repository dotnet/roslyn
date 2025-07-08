// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

using VerifyCS = CSharpCodeFixVerifier<
    UseExpressionBodyDiagnosticAnalyzer,
    UseExpressionBodyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForConversionOperatorsAnalyzerTests
{
    private static Task TestWithUseExpressionBody(string code, string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();

    private static Task TestWithUseBlockBody(string code, string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, ExpressionBodyPreference.Never } }
        }.RunAsync();

    [Fact]
    public async Task TestUseExpressionBody1()
    {
        await TestWithUseExpressionBody("""
            class C
            {
                static int Bar() { return 0; }

                {|IDE0023:public static implicit operator {|CS0161:C|}(int i)
                {
                    Bar();
                }|}
            }
            """, """
            class C
            {
                static int Bar() { return 0; }

                public static implicit operator C(int i) => Bar();
            }
            """);
    }

    [Fact]
    public async Task TestUseExpressionBody2()
    {
        await TestWithUseExpressionBody("""
            class C
            {
                static int Bar() { return 0; }

                {|IDE0023:public static implicit operator C(int i)
                {
                    return Bar();
                }|}
            }
            """, """
            class C
            {
                static int Bar() { return 0; }

                public static implicit operator C(int i) => Bar();
            }
            """);
    }

    [Fact]
    public async Task TestUseExpressionBody3()
    {
        await TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0023:public static implicit operator C(int i)
                {
                    throw new NotImplementedException();
                }|}
            }
            """, """
            using System;

            class C
            {
                public static implicit operator C(int i) => throw new NotImplementedException();
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
                {|IDE0023:public static implicit operator C(int i)
                {
                    throw new NotImplementedException(); // comment
                }|}
            }
            """, """
            using System;

            class C
            {
                public static implicit operator C(int i) => throw new NotImplementedException(); // comment
            }
            """);
    }

    [Fact]
    public async Task TestUseBlockBody1()
    {
        await TestWithUseBlockBody("""
            class C
            {
                static int Bar() { return 0; }

                {|IDE0023:public static implicit operator C(int i) => Bar();|}
            }
            """, """
            class C
            {
                static int Bar() { return 0; }

                public static implicit operator C(int i)
                {
                    return Bar();
                }
            }
            """);
    }

    [Fact]
    public async Task TestUseBlockBody3()
    {
        await TestWithUseBlockBody("""
            using System;

            class C
            {
                {|IDE0023:public static implicit operator C(int i) => throw new NotImplementedException();|}
            }
            """, """
            using System;

            class C
            {
                public static implicit operator C(int i)
                {
                    throw new NotImplementedException();
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
                {|IDE0023:public static implicit operator C(int i) => throw new NotImplementedException();|} // comment
            }
            """, """
            using System;

            class C
            {
                public static implicit operator C(int i)
                {
                    throw new NotImplementedException(); // comment
                }
            }
            """);
    }
}
