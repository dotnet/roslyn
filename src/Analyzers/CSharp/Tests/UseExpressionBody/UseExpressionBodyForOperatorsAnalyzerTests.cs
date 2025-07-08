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
public sealed class UseExpressionBodyForOperatorsAnalyzerTests
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
    public Task TestUseExpressionBody1()
        => TestWithUseExpressionBody("""
            class C
            {
                private static C Bar() { return new C(); }

                {|IDE0024:public static C operator {|CS0161:+|}(C c1, C c2)
                {
                    Bar();
                }|}
            }
            """, """
            class C
            {
                private static C Bar() { return new C(); }

                public static C operator +(C c1, C c2) => Bar();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody2()
        => TestWithUseExpressionBody("""
            class C
            {
                private static C Bar() { return new C(); }

                {|IDE0024:public static C operator +(C c1, C c2)
                {
                    return Bar();
                }|}
            }
            """, """
            class C
            {
                private static C Bar() { return new C(); }

                public static C operator +(C c1, C c2) => Bar();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody3()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0024:public static C operator +(C c1, C c2)
                {
                    throw new NotImplementedException();
                }|}
            }
            """, """
            using System;

            class C
            {
                public static C operator +(C c1, C c2) => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody4()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0024:public static C operator +(C c1, C c2)
                {
                    throw new NotImplementedException(); // comment
                }|}
            }
            """, """
            using System;

            class C
            {
                public static C operator +(C c1, C c2) => throw new NotImplementedException(); // comment
            }
            """);

    [Fact]
    public Task TestUseBlockBody1()
        => TestWithUseBlockBody("""
            class C
            {
                private static C Bar() { return new C(); }

                {|IDE0024:public static C operator +(C c1, C c2) => Bar();|}
            }
            """, """
            class C
            {
                private static C Bar() { return new C(); }

                public static C operator +(C c1, C c2)
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
                {|IDE0024:public static C operator +(C c1, C c2) => throw new NotImplementedException();|}
            }
            """, """
            using System;

            class C
            {
                public static C operator +(C c1, C c2)
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
                {|IDE0024:public static C operator +(C c1, C c2) => throw new NotImplementedException();|} // comment
            }
            """, """
            using System;

            class C
            {
                public static C operator +(C c1, C c2)
                {
                    throw new NotImplementedException(); // comment
                }
            }
            """);
}
