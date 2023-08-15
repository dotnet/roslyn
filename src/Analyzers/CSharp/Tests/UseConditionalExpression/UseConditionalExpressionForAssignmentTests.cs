// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer,
        CSharpUseConditionalExpressionForAssignmentCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
    public partial class UseConditionalExpressionForAssignmentTests
    {
        private static async Task TestMissingAsync(
            string testCode,
            LanguageVersion languageVersion = LanguageVersion.CSharp8,
            OptionsCollection? options = null)
        {
            var test = new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = languageVersion,
                Options = { options },
            };

            await test.RunAsync();
        }

        private static async Task TestInRegularAndScript1Async(
            string testCode,
            string fixedCode,
            LanguageVersion languageVersion = LanguageVersion.CSharp8,
            OptionsCollection? options = null,
            string? equivalenceKey = null)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = languageVersion,
                CodeActionEquivalenceKey = equivalenceKey,
                Options = { options },
            }.RunAsync();
        }

        private static readonly OptionsCollection PreferImplicitTypeAlways = new(LanguageNames.CSharp)
        {
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.TrueWithSilentEnforcement },
            { CSharpCodeStyleOptions.VarElsewhere, CodeStyleOption2.TrueWithSilentEnforcement },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOption2.TrueWithSilentEnforcement },
        };

        [Fact]
        public async Task TestOnSimpleAssignment()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnSimpleAssignment_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true ? throw new System.Exception() : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnSimpleAssignment_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true ? 0 : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestNotWithTwoThrows()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestNotOnSimpleAssignment_Throw1_CSharp6()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """, LanguageVersion.CSharp6);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestWithSimpleThrow()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                            {|CS0156:throw|};
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnSimpleAssignmentNoBlocks()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                            i = 0;
                        else
                            i = 1;
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnSimpleAssignmentNoBlocks_NotInBlock()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                            [|if|] (true)
                                i = 0;
                            else
                                i = 1;
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                            i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNotOnSimpleAssignmentToDifferentTargets()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int i, int j)
                    {
                        if (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            j = 1;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnAssignmentToUndefinedField()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        [|if|] (true)
                        {
                            this.{|CS1061:i|} = 0;
                        }
                        else
                        {
                            this.{|CS1061:i|} = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        this.{|CS1061:i|} = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnAssignmentToUndefinedField_Throw()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        [|if|] (true)
                        {
                            this.{|CS1061:i|} = 0;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        this.{|CS1061:i|} = true ? 0 : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnNonUniformTargetSyntax()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    private int i;

                    void M()
                    {
                        [|if|] (true)
                        {
                            this.i = 0;
                        }
                        else
                        {
                            this . i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    private int i;

                    void M()
                    {
                        this.i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnAssignmentToDefinedField()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    int i;

                    void M()
                    {
                        [|if|] (true)
                        {
                            this.i = 0;
                        }
                        else
                        {
                            this.i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    int i;

                    void M()
                    {
                        this.i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnAssignmentToAboveLocalNoInitializer()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnAssignmentToAboveLocalNoInitializer_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? 0 : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestOnAssignmentToAboveLocalNoInitializer_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i;
                        [|if|] (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? throw new System.Exception() : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnAssignmentToAboveLocalLiteralInitializer()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = 0;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestOnAssignmentToAboveLocalDefaultLiteralInitializer()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = default;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? 0 : 1;
                    }
                }
                """, LanguageVersion.Latest);
        }

        [Fact]
        public async Task TestOnAssignmentToAboveLocalDefaultExpressionInitializer()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = default(int);
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestDoNotMergeAssignmentToAboveLocalWithComplexInitializer()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = Foo();
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }

                    int Foo() => 0;
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = Foo();
                        i = true ? 0 : 1;
                    }

                    int Foo() => 0;
                }
                """);
        }

        [Fact]
        public async Task TestDoNotMergeAssignmentToAboveLocalIfIntermediaryStatement()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void M()
                    {
                        int i = 0;
                        Console.WriteLine();
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void M()
                    {
                        int i = 0;
                        Console.WriteLine();
                        i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestDoNotMergeAssignmentToAboveIfLocalUsedInIfCondition()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = 0;
                        [|if|] (Bar(i))
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }

                    bool Bar(int i) => true;
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = 0;
                        i = Bar(i) ? 0 : 1;
                    }

                    bool Bar(int i) => true;
                }
                """);
        }

        [Fact]
        public async Task TestDoNotMergeAssignmentToAboveIfMultiDecl()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = 0, j = 0;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = 0, j = 0;
                        i = true ? 0 : 1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestUseImplicitTypeForIntrinsicTypes()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = 0;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var i = true ? 0 : 1;
                    }
                }
                """, options: new OptionsCollection(LanguageNames.CSharp) { { CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOption2.TrueWithSilentEnforcement } });
        }

        [Fact]
        public async Task TestUseImplicitTypeWhereApparent()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = 0;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? 0 : 1;
                    }
                }
                """, options: new OptionsCollection(LanguageNames.CSharp) { { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.TrueWithSilentEnforcement } });
        }

        [Fact]
        public async Task TestUseImplicitTypeWherePossible()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int i = 0;
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = true ? 0 : 1;
                    }
                }
                """, options: new OptionsCollection(LanguageNames.CSharp) { { CSharpCodeStyleOptions.VarElsewhere, CodeStyleOption2.TrueWithSilentEnforcement } });
        }

        [Fact]
        public async Task TestMissingWithoutElse()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                            i = 0;
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMissingWithoutElseWithStatementAfterwards()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                            i = 0;
                        }

                        i = 1;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestMissingWithoutElseWithThrowStatementAfterwards()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                            i = 0;
                        }

                        throw new System.Exception();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        // cast will be necessary, otherwise 'var' would get the type 'string'.
                        object o;
                        [|if|] (true)
                        {
                            o = "a";
                        }
                        else
                        {
                            o = "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        // cast will be necessary, otherwise 'var' would get the type 'string'.
                        var o = true ? "a" : (object)"b";
                    }
                }
                """, options: PreferImplicitTypeAlways);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw1_CSharp8()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        object o;
                        [|if|] (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            o = "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var o = true ? throw new System.Exception() : (object)"b";
                    }
                }
                """, LanguageVersion.CSharp8, PreferImplicitTypeAlways);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw1_CSharp9()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        object o;
                        [|if|] (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            o = "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var o = true ? throw new System.Exception() : (object)"b";
                    }
                }
                """, LanguageVersion.CSharp9, options: PreferImplicitTypeAlways);
        }

        [Fact]
        public async Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        object o;
                        [|if|] (true)
                        {
                            o = "a";
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var o = true ? (object)"a" : throw new System.Exception();
                    }
                }
                """, options: PreferImplicitTypeAlways);
        }

        [Fact]
        public async Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        string s;
                        [|if|] (true)
                        {
                            s = "a";
                        }
                        else
                        {
                            s = null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s = true ? "a" : null;
                    }
                }
                """, options: PreferImplicitTypeAlways);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        string s;
                        [|if|] (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            s = null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s = true ? throw new System.Exception() : (string)null;
                    }
                }
                """, options: PreferImplicitTypeAlways);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        string s;
                        [|if|] (true)
                        {
                            s = "a";
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s = true ? "a" : throw new System.Exception();
                    }
                }
                """, options: PreferImplicitTypeAlways);
        }

        [Fact]
        public async Task TestConversionWithUseVarForAll_CanUseVarButRequiresCastOfConditionalBranch_CSharp8()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        string s;
                        [|if|] (true)
                        {
                            s = null;
                        }
                        else
                        {
                            s = null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s = true ? null : (string)null;
                    }
                }
                """, LanguageVersion.CSharp8, PreferImplicitTypeAlways);
        }

        [Fact]
        public async Task TestConversionWithUseVarForAll_CanUseVarButRequiresCastOfConditionalBranch_CSharp9()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        string s;
                        [|if|] (true)
                        {
                            s = null;
                        }
                        else
                        {
                            s = null;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s = true ? null : (string)null;
                    }
                }
                """, LanguageVersion.CSharp9, options: PreferImplicitTypeAlways);
        }

        [Fact]
        public async Task TestKeepTriviaAroundIf()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        // leading
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        } // trailing
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        // leading
                        i = true ? 0 : 1; // trailing
                    }
                }
                """);
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = 1;
                        }

                        string s;
                        [|if|] (true)
                        {
                            s = "a";
                        }
                        else
                        {
                            s = "b";
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true ? 0 : 1;

                        string s = true ? "a" : "b";
                    }
                }
                """);
        }

        [Fact]
        public async Task TestMultiLine1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                        {
                            i = Foo(
                                1, 2, 3);
                        }
                        else
                        {
                            i = 1;
                        }
                    }

                    int Foo(int x, int y, int z) => 0;
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true
                            ? Foo(
                                1, 2, 3)
                            : 1;
                    }

                    int Foo(int x, int y, int z) => 0;
                }
                """);
        }

        [Fact]
        public async Task TestMultiLine2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                        {
                            i = 0;
                        }
                        else
                        {
                            i = Foo(
                                1, 2, 3);
                        }
                    }

                    int Foo(int x, int y, int z) => 0;
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true
                            ? 0
                            : Foo(
                                1, 2, 3);
                    }

                    int Foo(int x, int y, int z) => 0;
                }
                """);
        }

        [Fact]
        public async Task TestMultiLine3()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        [|if|] (true)
                        {
                            i = Foo(
                                1, 2, 3);
                        }
                        else
                        {
                            i = Foo(
                                4, 5, 6);
                        }
                    }

                    int Foo(int x, int y, int z) => 0;
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        i = true
                            ? Foo(
                                1, 2, 3)
                            : Foo(
                                4, 5, 6);
                    }

                    int Foo(int x, int y, int z) => 0;
                }
                """);
        }

        [Fact]
        public async Task TestElseIfWithBlock()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                        }
                        else [|if|] (false)
                        {
                            i = 1;
                        }
                        else
                        {
                            i = 0;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            i = false ? 1 : 0;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestElseIfWithBlock_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                        }
                        else [|if|] (false)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            i = 0;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            i = false ? throw new System.Exception() : 0;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestElseIfWithBlock_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                        }
                        else [|if|] (false)
                        {
                            i = 1;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(int i)
                    {
                        if (true)
                        {
                        }
                        else
                        {
                            i = false ? 1 : throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestElseIfWithoutBlock()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                class C
                {
                    void M(int i)
                    {
                        if (true) i = 2;
                        else [|if|] (false) i = 1;
                        else i = 0;
                    }
                }
                """,
                FixedCode = """
                class C
                {
                    void M(int i)
                    {
                        if (true) i = 2;
                        else i = false ? 1 : 0;
                    }
                }
                """,
                CodeFixTestBehaviors = Testing.CodeFixTestBehaviors.FixOne,
                FixedState =
                {
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(6,9): hidden IDE0045: 'if' statement can be simplified
                        VerifyCS.Diagnostic().WithSpan(5, 9, 5, 11).WithSpan(5, 9, 6, 32),
                    }
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestRefAssignment1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(ref int i, ref int j)
                    {
                        ref int x = ref i;
                        [|if|] (true)
                        {
                            x = ref i;
                        }
                        else
                        {
                            x = ref j;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(ref int i, ref int j)
                    {
                        ref int x = ref i;
                        x = ref true ? ref i : ref j;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestRefAssignment1_Throw1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(ref int i, ref int j)
                    {
                        ref int x = ref i;
                        if (true)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            x = ref j;
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestRefAssignment1_Throw2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(ref int i, ref int j)
                    {
                        ref int x = ref i;
                        if (true)
                        {
                            x = ref i;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestTrueFalse1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        [|if|] (j == 0)
                        {
                            i = true;
                        }
                        else
                        {
                            i = false;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        i = j == 0;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestTrueFalse_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        [|if|] (j == 0)
                        {
                            i = true;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        i = j == 0 ? true : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestTrueFalse_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        [|if|] (j == 0)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            i = false;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        i = j == 0 ? throw new System.Exception() : false;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestTrueFalse2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        [|if|] (j == 0)
                        {
                            i = false;
                        }
                        else
                        {
                            i = true;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        i = j != 0;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestFalseTrue_Throw1()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        [|if|] (j == 0)
                        {
                            throw new System.Exception();
                        }
                        else
                        {
                            i = true;
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        i = j == 0 ? throw new System.Exception() : true;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
        public async Task TestFalseTrue_Throw2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        [|if|] (j == 0)
                        {
                            i = false;
                        }
                        else
                        {
                            throw new System.Exception();
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M(bool i, int j)
                    {
                        i = j == 0 ? false : throw new System.Exception();
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
        public async Task TestRemoveRedundantCast()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void M()
                    {
                        Guid? id;

                        [|if|] (true)
                        {
                            id = Guid.NewGuid();
                        }
                        else
                        {
                            id = Guid.Empty;
                        }
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void M()
                    {
                        var id = true ? Guid.NewGuid() : (Guid?)Guid.Empty;
                    }
                }
                """, options: PreferImplicitTypeAlways);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33284")]
        public async Task TestConditionalWithLambdas()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void M(bool containsHighBits)
                    {
                        Action<char> write;

                        [|if|] (containsHighBits)
                        {
                            write = (char character) => Console.WriteLine(1);
                        }
                        else
                        {
                            write = (char character) => Console.WriteLine(2);
                        }
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void M(bool containsHighBits)
                    {
                        Action<char> write = containsHighBits ? ((char character) => Console.WriteLine(1)) : ((char character) => Console.WriteLine(2));
                    }
                }
                """, LanguageVersion.CSharp9);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39260")]
        public async Task TestTitleWhenSimplifying()
        {
            await TestInRegularAndScript1Async(
                """
                using System;

                class C
                {
                    void M(string node1, string node2)
                    {
                        bool b;
                        [|if|] (AreSimilarCore(node1, node2))
                        {
                            b = true;
                        }
                        else
                        {
                            b = false;
                        }
                    }

                    private bool AreSimilarCore(string node1, string node2)
                    {
                        throw new NotImplementedException();
                    }
                }
                """,
                """
                using System;

                class C
                {
                    void M(string node1, string node2)
                    {
                        bool b = AreSimilarCore(node1, node2);
                    }

                    private bool AreSimilarCore(string node1, string node2)
                    {
                        throw new NotImplementedException();
                    }
                }
                """, LanguageVersion.CSharp9, equivalenceKey: nameof(AnalyzersResources.Simplify_check));
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67649")]
        [InlineData("int", "int")]
        [InlineData("string", "string")]
        [InlineData("string", "object")]
        [InlineData("object", "string")]
        [InlineData("int", "long")]
        [InlineData("long", "int")]
        public async Task TestForDiscardsWithMatchingOrConvertibleExpressionTypes(string originalFirstType, string originalSecondType)
        {
            await TestInRegularAndScript1Async($$"""
                class MyClass
                {
                    void M(bool flag)
                    {
                        [|if|] (flag)
                        {
                            _ = A();
                        }
                        else
                        {
                            _ = B();
                        }
                    }

                    {{originalFirstType}} A() => default;
                    {{originalSecondType}} B() => default;
                }
                """, $$"""
                class MyClass
                {
                    void M(bool flag)
                    {
                        _ = flag ? A() : B();
                    }
                
                    {{originalFirstType}} A() => default;
                    {{originalSecondType}} B() => default;
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67649")]
        public async Task TestMissingForDiscardsWithDifferentTypes()
        {
            await TestMissingAsync("""
                class MyClass
                {
                    void M(bool flag)
                    {
                        if (flag)
                        {
                            _ = A();
                        }
                        else
                        {
                            _ = B();
                        }
                    }
                
                    int A() => default;
                    string B() => default;
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67649")]
        public async Task TestMissingForDiscardsWithBothImplicitConversions()
        {
            await TestMissingAsync("""
                class MyClass
                {
                    void M(bool flag)
                    {
                        if (flag)
                        {
                            _ = GetC();
                        }
                        else
                        {
                            _ = GetString();
                        }
                    }

                    C GetC() => new C();
                    string GetString() => "";
                }

                class C
                {
                    public static implicit operator C(string c) => new C();
                    public static implicit operator string(C c) => "";
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68578")]
        public async Task TestMissingWhenAssignmentReferencesPatternVariable()
        {
            await TestMissingAsync("""
                using System;

                public class Class1
                {
                    public int i;
                }

                public class Program
                {
                    public static void Test(object obj)
                    {
                        if (obj is Class1 c)
                        {
                            c.i = 1;
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68578")]
        public async Task TestMissingWhenAssignmentReferencesOutVariable()
        {
            await TestMissingAsync("""
                using System;

                public class Class1
                {
                    public int i;
                }

                public class Program
                {
                    public static void Test(object obj)
                    {
                        if (TryGetValue(out var c))
                        {
                            c.i = 1;
                        }
                        else    
                        {
                            throw new Exception();
                        }
                    }

                    private static bool TryGetValue(out Class1 c) => throw new NotImplementedException();
                }
                """);
        }
    }
}
