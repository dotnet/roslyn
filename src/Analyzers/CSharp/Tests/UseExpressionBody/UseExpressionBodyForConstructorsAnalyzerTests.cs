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
public class UseExpressionBodyForConstructorsAnalyzerTests
{
    private static async Task TestWithUseExpressionBody(string code, string fixedCode, LanguageVersion version = LanguageVersion.CSharp8)
    {
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = version,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, ExpressionBodyPreference.WhenPossible } }
        }.RunAsync();
    }

    private static async Task TestWithUseBlockBody(string code, string fixedCode)
    {
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, ExpressionBodyPreference.Never } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestUseExpressionBody1()
    {
        var code = """
            class C
            {
                void Bar() { }

                {|IDE0021:public C()
                {
                    Bar();
                }|}
            }
            """;
        var fixedCode = """
            class C
            {
                void Bar() { }

                public C() => Bar();
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
                int a;

                {|IDE0021:public C()
                {
                    a = Bar();
                }|}

                int Bar() { return 0; }
            }
            """;
        var fixedCode = """
            class C
            {
                int a;

                public C() => a = Bar();

                int Bar() { return 0; }
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
                {|IDE0021:public C()
                {
                    throw new NotImplementedException();
                }|}
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public C() => throw new NotImplementedException();
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
                {|IDE0021:public C()
                {
                    throw new NotImplementedException(); // comment
                }|}
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public C() => throw new NotImplementedException(); // comment
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
                {|IDE0021:public C() => Bar();|}

                void Bar() { }
            }
            """;
        var fixedCode = """
            class C
            {
                public C()
                {
                    Bar();
                }

                void Bar() { }
            }
            """;
        await TestWithUseBlockBody(code, fixedCode);
    }

    [Fact]
    public async Task TestUseBlockBody2()
    {
        var code = """
            class C
            {
                int a;

                {|IDE0021:public C() => a = Bar();|}

                int Bar() { return 0; }
            }
            """;
        var fixedCode = """
            class C
            {
                int a;

                public C()
                {
                    a = Bar();
                }

                int Bar() { return 0; }
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
                {|IDE0021:public C() => throw new NotImplementedException();|}
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public C()
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
                {|IDE0021:public C() => throw new NotImplementedException();|} // comment
            }
            """;
        var fixedCode = """
            using System;

            class C
            {
                public C()
                {
                    throw new NotImplementedException(); // comment
                }
            }
            """;
        await TestWithUseBlockBody(code, fixedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7()
    {
        var code = """
            using System;
            class C
            {
                {|IDE0021:public C() {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|}
            }
            """;
        var fixedCode = """
            using System;
            class C
            {
                public C()
                {
                    throw new NotImplementedException();
                }
            }
            """;
        await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7_FixAll()
    {
        var code = """
            using System;
            class C
            {
                {|IDE0021:public C() {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|}
                {|IDE0021:public C(int i) {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|}
            }
            """;
        var fixedCode = """
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
            """;
        await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
    }
}
