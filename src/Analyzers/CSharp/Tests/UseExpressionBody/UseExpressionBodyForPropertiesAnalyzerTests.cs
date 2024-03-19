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
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody
{
    using VerifyCS = CSharpCodeFixVerifier<
        UseExpressionBodyDiagnosticAnalyzer,
        UseExpressionBodyCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
    public class UseExpressionBodyForPropertiesAnalyzerTests
    {
        private static async Task TestWithUseExpressionBody(string code, string fixedCode, LanguageVersion version = LanguageVersion.CSharp8)
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

        private static async Task TestWithUseBlockBody(string code, string fixedCode)
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
            var code = """
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
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo => Bar();
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
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
            var code = """
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
                """;
            var fixedCode = """
                using System;

                class C
                {
                    int Goo => throw new NotImplementedException();
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
                    {|IDE0025:int Goo
                    {
                        get
                        {
                            throw new NotImplementedException(); // comment
                        }
                    }|}
                }
                """;
            var fixedCode = """
                using System;

                class C
                {
                    int Goo => throw new NotImplementedException(); // comment
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
                    int Bar() { return 0; }

                    {|IDE0025:int Goo => Bar();|}
                }
                """;
            var fixedCode = """
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
                """;
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20363")]
        public async Task TestUseBlockBodyForAccessorEventWhenAccessorWantExpression1()
        {
            var code = """
                class C
                {
                    int Bar() { return 0; }

                    {|IDE0025:int Goo => Bar();|}
                }
                """;
            var fixedCode = """
                class C
                {
                    int Bar() { return 0; }

                    int Goo
                    {
                        get => Bar();
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = fixedCode,
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
            var code = """
                using System;

                class C
                {
                    {|IDE0025:int Goo => throw new NotImplementedException();|}
                }
                """;
            var fixedCode = """
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
                    {|IDE0025:int Goo => throw new NotImplementedException();|} // comment
                }
                """;
            var fixedCode = """
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
                """;
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16386")]
        public async Task TestUseExpressionBodyKeepTrailingTrivia()
        {
            var code = """
                class C
                {
                    private string _prop = "HELLO THERE!";
                    {|IDE0025:public string Prop { get { return _prop; } }|}

                    public string OtherThing => "Pickles";
                }
                """;
            var fixedCode = """
                class C
                {
                    private string _prop = "HELLO THERE!";
                    public string Prop => _prop;

                    public string OtherThing => "Pickles";
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
        public async Task TestDirectivesInBlockBody1()
        {
            var code = """
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
                """;
            var fixedCode = """
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
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19235")]
        public async Task TestDirectivesInBlockBody2()
        {
            var code = """
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
                """;
            var fixedCode = """
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
                """;
            await TestWithUseExpressionBody(code, fixedCode);
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
            // TODO: This test is unrelated to properties. It should be moved to UseExpressionBodyForMethodsAnalyzerTests.
            var code = """
                class C
                {
                    {|IDE0022:int Goo(int i) =>
                        //comment
                        i * i;|}
                }
                """;
            var fixedCode = """
                class C
                {
                    int Goo(int i)
                    {
                        //comment
                        return i * i;
                    }
                }
                """;
            await TestWithUseBlockBody(code, fixedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfHasThrowExpressionPriorToCSharp7()
        {
            var code = """
                using System;
                class C
                {
                    {|IDE0025:int Goo => {|CS8059:throw|} new NotImplementedException();|}
                }
                """;
            var fixedCode = """
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
                """;
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
        public async Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfHasThrowExpressionPriorToCSharp7_FixAll()
        {
            var code = """
                using System;
                class C
                {
                    {|IDE0025:int Goo => {|CS8059:throw|} new NotImplementedException();|}
                    {|IDE0025:int Bar => {|CS8059:throw|} new NotImplementedException();|}
                }
                """;
            var fixedCode = """
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
                """;
            await TestWithUseExpressionBody(code, fixedCode, LanguageVersion.CSharp6);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50181")]
        public async Task TestUseExpressionBodyPreserveComments()
        {
            var code = """
                public class C
                {
                    {|IDE0025:public long Length                   //N
                    {
                        // N = N1 + N2
                        get { return 1 + 2; }
                    }|}
                }
                """;
            var fixedCode = """
                public class C
                {
                    public long Length                   //N
                                                         // N = N1 + N2
                        => 1 + 2;
                }
                """;
            await TestWithUseExpressionBody(code, fixedCode);
        }
    }
}
