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
public class UseExpressionBodyForOperatorsAnalyzerTests
{
    private static async Task TestWithUseExpressionBody(string code, string fixedCode)
    {
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();
    }

    private static async Task TestWithUseBlockBody(string code, string fixedCode)
    {
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedOperators, ExpressionBodyPreference.Never } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestUseExpressionBody1()
    {
        var code = """
            class C
            {
                private static C Bar() { return new C(); }

                {|IDE0024:public static C operator {|CS0161:+|}(C c1, C c2)
                {
                    Bar();
                }|}
            }
            """;
        var fixedCode = """
            class C
            {
                private static C Bar() { return new C(); }

                public static C operator +(C c1, C c2) => Bar();
            }
            """;
        await TestWithUseExpressionBody(code, fixedCode);
    }

    [Fact]
    public async Task TestUseExpressionBody2()
    {
        var code = """
            class C
            {
                private static C Bar() { return new C(); }

                {|IDE0024:public static C operator +(C c1, C c2)
                {
                    return Bar();
                }|}
            }
            """;
        var fixedCode = """
            class C
            {
                private static C Bar() { return new C(); }

                public static C operator +(C c1, C c2) => Bar();
            }
            """;
        await TestWithUseExpressionBody(code, fixedCode);
    }

    [Fact]
    public async Task TestUseExpressionBody3()
    {
        var code = """
            using System;

            class C
            {
                {|IDE0024:public static C operator +(C c1, C c2)
                {
                    throw new NotImplementedException();
                }|}
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public static C operator +(C c1, C c2) => throw new NotImplementedException();
            }
            """;
        await TestWithUseExpressionBody(code, fixedCode);
    }

    [Fact]
    public async Task TestUseExpressionBody4()
    {
        var code = """
            using System;

            class C
            {
                {|IDE0024:public static C operator +(C c1, C c2)
                {
                    throw new NotImplementedException(); // comment
                }|}
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public static C operator +(C c1, C c2) => throw new NotImplementedException(); // comment
            }
            """;
        await TestWithUseExpressionBody(code, fixedCode);
    }

    [Fact]
    public async Task TestUseBlockBody1()
    {
        var code = """
            class C
            {
                private static C Bar() { return new C(); }

                {|IDE0024:public static C operator +(C c1, C c2) => Bar();|}
            }
            """;
        var fixedCode = """
            class C
            {
                private static C Bar() { return new C(); }

                public static C operator +(C c1, C c2)
                {
                    return Bar();
                }
            }
            """;
        await TestWithUseBlockBody(code, fixedCode);
    }

    [Fact]
    public async Task TestUseBlockBody3()
    {
        var code = """
            using System;

            class C
            {
                {|IDE0024:public static C operator +(C c1, C c2) => throw new NotImplementedException();|}
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public static C operator +(C c1, C c2)
                {
                    throw new NotImplementedException();
                }
            }
            """;
        await TestWithUseBlockBody(code, fixedCode);
    }

    [Fact]
    public async Task TestUseBlockBody4()
    {
        var code = """
            using System;

            class C
            {
                {|IDE0024:public static C operator +(C c1, C c2) => throw new NotImplementedException();|} // comment
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public static C operator +(C c1, C c2)
                {
                    throw new NotImplementedException(); // comment
                }
            }
            """;
        await TestWithUseBlockBody(code, fixedCode);
    }
}
