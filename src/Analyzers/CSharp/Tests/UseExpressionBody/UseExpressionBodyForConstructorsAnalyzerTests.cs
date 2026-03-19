// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
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
public sealed class UseExpressionBodyForConstructorsAnalyzerTests
{
    private static Task TestWithUseExpressionBody(string code, string fixedCode, LanguageVersion version = LanguageVersion.CSharp8)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = version,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();

    private static Task TestWithUseBlockBody(string code, string fixedCode)
        => new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, ExpressionBodyPreference.Never } }
        }.RunAsync();

    [Fact]
    public Task TestUseExpressionBody1()
        => TestWithUseExpressionBody("""
            class C
            {
                void Bar() { }

                {|IDE0021:public C()
                {
                    Bar();
                }|}
            }
            """, """
            class C
            {
                void Bar() { }

                public C() => Bar();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody2()
        => TestWithUseExpressionBody("""
            class C
            {
                int a;

                {|IDE0021:public C()
                {
                    a = Bar();
                }|}

                int Bar() { return 0; }
            }
            """, """
            class C
            {
                int a;

                public C() => a = Bar();

                int Bar() { return 0; }
            }
            """);

    [Fact]
    public Task TestUseExpressionBody3()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0021:public C()
                {
                    throw new NotImplementedException();
                }|}
            }
            """, """
            using System;

            class C
            {
                public C() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestUseExpressionBody4()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                {|IDE0021:public C()
                {
                    throw new NotImplementedException(); // comment
                }|}
            }
            """, """
            using System;

            class C
            {
                public C() => throw new NotImplementedException(); // comment
            }
            """);

    [Fact]
    public Task TestUseBlockBody1()
        => TestWithUseBlockBody("""
            class C
            {
                {|IDE0021:public C() => Bar();|}

                void Bar() { }
            }
            """, """
            class C
            {
                public C()
                {
                    Bar();
                }

                void Bar() { }
            }
            """);

    [Fact]
    public Task TestUseBlockBody2()
        => TestWithUseBlockBody("""
            class C
            {
                int a;

                {|IDE0021:public C() => a = Bar();|}

                int Bar() { return 0; }
            }
            """, """
            class C
            {
                int a;

                public C()
                {
                    a = Bar();
                }

                int Bar() { return 0; }
            }
            """);

    [Fact]
    public Task TestUseBlockBody3()
        => TestWithUseBlockBody("""
            using System;

            class C
            {
                {|IDE0021:public C() => throw new NotImplementedException();|}
            }
            """, """
            using System;

            class C
            {
                public C()
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
                {|IDE0021:public C() => throw new NotImplementedException();|} // comment
            }
            """, """
            using System;

            class C
            {
                public C()
                {
                    throw new NotImplementedException(); // comment
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7()
        => TestWithUseExpressionBody("""
            using System;
            class C
            {
                {|IDE0021:public C() {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|}
            }
            """, """
            using System;
            class C
            {
                public C()
                {
                    throw new NotImplementedException();
                }
            }
            """, LanguageVersion.CSharp6);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7_FixAll()
        => TestWithUseExpressionBody("""
            using System;
            class C
            {
                {|IDE0021:public C() {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|}
                {|IDE0021:public C(int i) {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|}
            }
            """, """
            using System;
            class C
            {
                public C()
                {
                    throw new NotImplementedException();
                }

                public C(int i)
                {
                    throw new NotImplementedException();
                }
            }
            """, LanguageVersion.CSharp6);
}
