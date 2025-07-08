// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
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
public sealed class UseExpressionBodyForPropertiesAnalyzerTests
{
    private static async Task TestWithUseExpressionBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        LanguageVersion version = LanguageVersion.CSharp8)
    {
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = version,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never },
            },
            MarkupOptions = MarkupOptions.None,
        }.RunAsync();
    }

    private static async Task TestWithUseBlockBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode)
    {
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never },
            },
            MarkupOptions = MarkupOptions.None,
        }.RunAsync();
    }

    [Fact]
    public async Task TestUseExpressionBody1()
    {
        await TestWithUseExpressionBody("""
            class C
            {
                int Bar() { return 0; }

                {|IDE0025:int Goo
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

                int Goo => Bar();
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

                int Goo
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
    public async Task TestMissingWithAttribute()
    {
        var code = """
            using System;

            class AAttribute : Attribute {}

            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    [A]
                    get
                    {
                        return Bar();
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

                int Goo
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
                {|IDE0025:int Goo
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
                int Goo => throw new NotImplementedException();
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
                {|IDE0025:int Goo
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
                int Goo => throw new NotImplementedException(); // comment
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

                {|IDE0025:int Goo => Bar();|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int Goo
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
    public async Task TestUseBlockBodyForAccessorEventWhenAccessorWantExpression1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int Bar() { return 0; }

                {|IDE0025:int Goo => Bar();|}
            }
            """,
            FixedCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get => Bar();
                }
            }
            """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible },
            },
            MarkupOptions = MarkupOptions.None,
            NumberOfFixAllIterations = 2,
            NumberOfIncrementalIterations = 2,
        }.RunAsync();
    }

    [Fact]
    public async Task TestUseBlockBody3()
    {
        await TestWithUseBlockBody("""
            using System;

            class C
            {
                {|IDE0025:int Goo => throw new NotImplementedException();|}
            }
            """, """
            using System;

            class C
            {
                int Goo
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
                {|IDE0025:int Goo => throw new NotImplementedException();|} // comment
            }
            """, """
            using System;

            class C
            {
                int Goo
                {
                    get
                    {
                        throw new NotImplementedException(); // comment
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16386")]
    public async Task TestUseExpressionBodyKeepTrailingTrivia()
    {
        await TestWithUseExpressionBody("""
            class C
            {
                private string _prop = "HELLO THERE!";
                {|IDE0025:public string Prop { get { return _prop; } }|}

                public string OtherThing => "Pickles";
            }
            """, """
            class C
            {
                private string _prop = "HELLO THERE!";
                public string Prop => _prop;

                public string OtherThing => "Pickles";
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public async Task TestDirectivesInBlockBody1()
    {
        await TestWithUseExpressionBody("""
            class C
            {
                int Bar() { return 0; }
                int Baz() { return 0; }

                {|IDE0025:int Goo
                {
                    get
                    {
            #if true
                        return Bar();
            #else
                        return Baz();
            #endif
                    }
                }|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }
                int Baz() { return 0; }

                int Goo =>
            #if true
                        Bar();
            #else
                        return Baz();
            #endif

            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public async Task TestDirectivesInBlockBody2()
    {
        await TestWithUseExpressionBody("""
            class C
            {
                int Bar() { return 0; }
                int Baz() { return 0; }

                {|IDE0025:int Goo
                {
                    get
                    {
            #if false
                        return Bar();
            #else
                        return Baz();
            #endif
                    }
                }|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }
                int Baz() { return 0; }

                int Goo =>
            #if false
                        return Bar();
            #else
                        Baz();
            #endif

            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public async Task TestMissingWithDirectivesInExpressionBody1()
    {
        var code = """
            class C
            {
                int Bar() { return 0; }
                int Baz() { return 0; }

                int Goo =>
            #if true
                        Bar();
            #else
                        Baz();
            #endif
            }
            """;
        await TestWithUseBlockBody(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
    public async Task TestMissingWithDirectivesInExpressionBody2()
    {
        var code = """
            class C
            {
                int Bar() { return 0; }
                int Baz() { return 0; }

                int Goo =>
            #if false
                        Bar();
            #else
                        Baz();
            #endif
            }
            """;
        await TestWithUseBlockBody(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19193")]
    public async Task TestMoveTriviaFromExpressionToReturnStatement()
    {
        await TestWithUseBlockBody("""
            class C
            {
                {|IDE0022:int Goo(int i) =>
                    //comment
                    i * i;|}
            }
            """, """
            class C
            {
                int Goo(int i)
                {
                    //comment
                    return i * i;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfHasThrowExpressionPriorToCSharp7()
    {
        await TestWithUseExpressionBody("""
            using System;
            class C
            {
                {|IDE0025:int Goo => {|CS8059:throw|} new NotImplementedException();|}
            }
            """, """
            using System;
            class C
            {
                int Goo
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """, LanguageVersion.CSharp6);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfHasThrowExpressionPriorToCSharp7_FixAll()
    {
        await TestWithUseExpressionBody("""
            using System;
            class C
            {
                {|IDE0025:int Goo => {|CS8059:throw|} new NotImplementedException();|}
                {|IDE0025:int Bar => {|CS8059:throw|} new NotImplementedException();|}
            }
            """, """
            using System;
            class C
            {
                int Goo
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }

                int Bar
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """, LanguageVersion.CSharp6);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50181")]
    public async Task TestUseExpressionBodyPreserveComments()
    {
        await TestWithUseExpressionBody("""
            public class C
            {
                {|IDE0025:public long Length                   //N
                {
                    // N = N1 + N2
                    get { return 1 + 2; }
                }|}
            }
            """, """
            public class C
            {
                public long Length                   //N
                                                     // N = N1 + N2
                    => 1 + 2;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77473")]
    public async Task TestMissingWithInitializer1()
    {
        var code = """
            class C
            {
                object Goo
                {
                    get
                    {
                        return field;
                    }
                } = new();
            }
            """;
        await TestWithUseExpressionBody(code, code, LanguageVersionExtensions.CSharpNext);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77473")]
    public async Task TestMissingWithInitializer2()
    {
        var code = """
            class C
            {
                object Goo
                {
                    get
                    {
                        return field ??= new();
                    }
                } = new();
            }
            """;
        await TestWithUseExpressionBody(code, code, LanguageVersionExtensions.CSharpNext);
    }
}
