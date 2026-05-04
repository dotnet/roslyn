// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseConditionalExpressionForAssignmentDiagnosticAnalyzer,
    CSharpUseConditionalExpressionForAssignmentCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
public sealed partial class UseConditionalExpressionForAssignmentTests
{
    private static async Task TestMissingAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        LanguageVersion languageVersion = LanguageVersion.CSharp8,
        OptionsCollection? options = null)
    {
        var test = new VerifyCS.Test
        {
            TestCode = testCode,
            LanguageVersion = languageVersion,
            Options = { options },
        };

        await test.RunAsync();
    }

    private static Task TestInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        LanguageVersion languageVersion = LanguageVersion.CSharp8,
        OptionsCollection? options = null,
        string? equivalenceKey = null)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = languageVersion,
            CodeActionEquivalenceKey = equivalenceKey,
            Options = { options },
        }.RunAsync();

    private static readonly OptionsCollection PreferImplicitTypeAlways = new(LanguageNames.CSharp)
    {
        { CSharpCodeStyleOptions.VarWhenTypeIsApparent, CodeStyleOption2.TrueWithSilentEnforcement },
        { CSharpCodeStyleOptions.VarElsewhere, CodeStyleOption2.TrueWithSilentEnforcement },
        { CSharpCodeStyleOptions.VarForBuiltInTypes, CodeStyleOption2.TrueWithSilentEnforcement },
    };

    [Fact]
    public Task TestOnSimpleAssignment()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnSimpleAssignment_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnSimpleAssignment_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestNotWithTwoThrows()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestNotOnSimpleAssignment_Throw1_CSharp6()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestWithSimpleThrow()
        => TestMissingAsync(
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

    [Fact]
    public Task TestOnSimpleAssignmentNoBlocks()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnSimpleAssignmentNoBlocks_NotInBlock()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotOnSimpleAssignmentToDifferentTargets()
        => TestMissingAsync(
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

    [Fact]
    public Task TestOnAssignmentToUndefinedField()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnAssignmentToUndefinedField_Throw()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnNonUniformTargetSyntax()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnAssignmentToDefinedField()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnAssignmentToAboveLocalNoInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnAssignmentToAboveLocalNoInitializer_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestOnAssignmentToAboveLocalNoInitializer_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnAssignmentToAboveLocalLiteralInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnAssignmentToAboveLocalDefaultLiteralInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnAssignmentToAboveLocalDefaultExpressionInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDoNotMergeAssignmentToAboveLocalWithComplexInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDoNotMergeAssignmentToAboveLocalIfIntermediaryStatement()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDoNotMergeAssignmentToAboveIfLocalUsedInIfCondition()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDoNotMergeAssignmentToAboveIfMultiDecl()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUseImplicitTypeForIntrinsicTypes()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUseImplicitTypeWhereApparent()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUseImplicitTypeWherePossible()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingWithoutElse()
        => TestMissingAsync(
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

    [Fact]
    public Task TestMissingWithoutElseWithStatementAfterwards()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestMissingWithoutElseWithThrowStatementAfterwards()
        => TestMissingAsync(
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

    [Fact]
    public Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw1_CSharp8()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw1_CSharp9()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConversionWithUseVarForAll_CastInsertedToKeepTypeSame_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestConversionWithUseVarForAll_CanUseVarBecauseConditionalTypeMatches_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConversionWithUseVarForAll_CanUseVarButRequiresCastOfConditionalBranch_CSharp8()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestConversionWithUseVarForAll_CanUseVarButRequiresCastOfConditionalBranch_CSharp9()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestKeepTriviaAroundIf()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultiLine1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultiLine2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultiLine3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestElseIfWithBlock()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestElseIfWithBlock_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestElseIfWithBlock_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestElseIfWithoutBlock()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestRefAssignment1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestRefAssignment1_Throw1()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestRefAssignment1_Throw2()
        => TestMissingAsync(
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

    [Fact]
    public Task TestTrueFalse1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestTrueFalse_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestTrueFalse_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTrueFalse2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestFalseTrue_Throw1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")]
    public Task TestFalseTrue_Throw2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58898")]
    public Task TestRemoveRedundantCast()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33284")]
    public Task TestConditionalWithLambdas()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39260")]
    public Task TestTitleWhenSimplifying()
        => TestInRegularAndScriptAsync(
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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/67649")]
    [InlineData("int", "int")]
    [InlineData("string", "string")]
    [InlineData("string", "object")]
    [InlineData("object", "string")]
    [InlineData("int", "long")]
    [InlineData("long", "int")]
    public Task TestForDiscardsWithMatchingOrConvertibleExpressionTypes(string originalFirstType, string originalSecondType)
        => TestInRegularAndScriptAsync($$"""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67649")]
    public Task TestMissingForDiscardsWithDifferentTypes()
        => TestMissingAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67649")]
    public Task TestMissingForDiscardsWithBothImplicitConversions()
        => TestMissingAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68578")]
    public Task TestMissingWhenAssignmentReferencesPatternVariable()
        => TestMissingAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68578")]
    public Task TestMissingWhenAssignmentReferencesOutVariable()
        => TestMissingAsync("""
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71403")]
    public Task TestGlobalStatements()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                using System;

                object? x = null;
                object? y = null;
                object? z;

                [|if|] (x != null)
                {
                    z = x;
                }
                else
                {
                    z = y;
                }

                Console.WriteLine($"{x}{y}{z}");
                """,
            FixedCode = """
                #nullable enable

                using System;

                object? x = null;
                object? y = null;
                object? z = x != null ? x : y;
            
                Console.WriteLine($"{x}{y}{z}");
                """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = {
                OutputKind = OutputKind.ConsoleApplication,
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58897")]
    public Task TestCommentsOnElse()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(bool containsHighBits)
                {
                    int write;

                    [|if|] (containsHighBits)
                    {
                        write = 0;
                    }
                    // Comment on else
                    else
                    {
                        write = 1;
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
                    int write = containsHighBits
                        ? 0
                        // Comment on else
                        : 1;
                }
            }
            """, LanguageVersion.CSharp9);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60859")]
    public Task UnnecessaryWithinConditionalBranch2()
        => TestInRegularAndScriptAsync(
            """
            public class IssueClass
            {
                double ID;

                public void ConvertFieldValueForStorage(object value)
                {
                    object o;
                    [|if|] (value is IssueClass issue)
                    {
                        o = (decimal)issue.ID;
                    }
                    else
                    {
                        o = -1m;
                    }
                }
            }
            """,
            """
            public class IssueClass
            {
                double ID;
            
                public void ConvertFieldValueForStorage(object value)
                {
                    object o = value is IssueClass issue ? (decimal)issue.ID : -1m;
                }
            }
            """, LanguageVersion.CSharp13);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck1()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    if (test != null && test.Field == null)
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck1_B()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    if (null != test && test.Field == null)
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck2()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    if (test is not null && test.Field is null)
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """, LanguageVersion.CSharp9);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck3()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    if (test is { })
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck4()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    if (test is { } x)
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck5()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    if (test is Test)
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck6()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    if (test is Test t)
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75200")]
    public Task TestNullCheck7()
        => TestMissingAsync("""
            using System;
            public class Program
            {
                public void N(object[] parent, int i, object value)
                {
                    if (parent is { })
                    {
                        parent[i] = value;
                    }
                    else throw new Exception();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63441")]
    public Task TestNullCheck_Positive1()
        => TestInRegularAndScriptAsync("""
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    [|if|] (test.Field == null)
                    {
                        test.Field = string.Empty;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
                public class Test
                {
                    public string Field;
                }
            }
            """, """
            using System;
            public class Program
            {
                public static void TestMethod(Test test)
                {
                    test.Field = test.Field == null ? string.Empty : throw new InvalidOperationException();
                }
                public class Test
                {
                    public string Field;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72464")]
    public Task TestMissingWithVariableCollisions()
        => TestMissingAsync(
            """
            using System;

            public class IssueClass
            {
                public void Convert(Type type, string body)
                {
                    object o;
                    if (type == typeof(bool))
                    {
                         o = bool.TryParse(body, out bool value) ? 0 : 1;
                    }
                    else
                    {
                        o = int.TryParse(body, out int value) ? 2 : 3;
                    }
                }
            }
            """);

    [Fact]
    public Task TestOnNullConditionalAssignment1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                int i;

                void M(C c)
                {
                    [|if|] (true)
                    {
                        c?.i = 0;
                    }
                    else
                    {
                        c?.i = 1;
                    }
                }
            }
            """,
            """
            class C
            {
                int i;

                void M(C c)
                {
                    c?.i = true ? 0 : 1;
                }
            }
            """,
            languageVersion: LanguageVersion.CSharp14);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80640")]
    public Task TestWhenBooleanOperatorsAreUsed()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M(int i)
                {
                    [|if|] (this)
                    {
                        i = 1;
                    }
                    else
                    {
                        i = 0;
                    }
                }

                public static bool operator true(C v) => true;

                public static bool operator false(C v) => false;
            }
            """, """
            class C
            {
                void M(int i)
                {
                    i = this ? 1 : 0;
                }

                public static bool operator true(C v) => true;

                public static bool operator false(C v) => false;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80640")]
    public Task TestWithImplicitBoolConversion()
        => TestInRegularAndScriptAsync("""
            class C
            {
                void M(int i)
                {
                    [|if|] (this)
                    {
                        i = 1;
                    }
                    else
                    {
                        i = 0;
                    }
                }

                public static implicit operator bool(C v) => true;
            }
            """, """
            class C
            {
                void M(int i)
                {
                    i = this ? 1 : 0;
                }

                public static implicit operator bool(C v) => true;
            }
            """);
}
