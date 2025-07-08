// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConditionalExpressionPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ConditionalExpressionPlacement;

using Verify = CSharpCodeFixVerifier<
    ConditionalExpressionPlacementDiagnosticAnalyzer,
    ConditionalExpressionPlacementCodeFixProvider>;

public sealed class ConditionalExpressionPlacementTests
{
    [Fact]
    public async Task TestNotWithOptionOff()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        0 :
                        1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestBaseCase()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|]
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInCondition()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true || {|CS1525:?|}
                        0:
                        1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInTrue()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        0 + {|CS1525::|}
                        1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInFalse()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        0 :
                        1 +{|CS1525:;|}
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithMissingColon()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        0{|CS1003:{|CS1525:;|}|}
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithQuestionNotAtEndOfLine()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ? 1 :
                        1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithColonNotAtEndOfLine()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        1 : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia1()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
            #if true
                        1 :
                        1;
            #endif
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia2()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        1 :
            #if true
                        1;
            #endif
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia3()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
            #if true
                    var v = true ?
            #endif
                        1 :
                        1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia4()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
            #if true
                    var v = true ?
                        1 :
            #endif
                        1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia5()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        1 :
            #if true
                        1;
            #endif
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegion1()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|]
            #region true section
                        0 :
            #endregion
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
            #region true section
                        ? 0
            #endregion
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegion2()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|]
                        0 :
            #region true section
                        1;
            #endregion
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
                        ? 0
            #region true section
                        : 1;
            #endregion
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNullableDirective1()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|]
            #nullable enable
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
            #nullable enable
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNullableDirective2()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|]
                        0 :
            #nullable enable
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
                        ? 0
            #nullable enable
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNested1()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|]
                        true [|?|]
                            0 :
                            1 :
                        2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
                        ? true
                            ? 0
                            : 1
                        : 2;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNested2()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|]
                        true ?
                            0 : 1 :
                        2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
                        ? true ?
                            0 : 1
                        : 2;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNested3()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        true [|?|]
                            0 :
                            1 : 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true ?
                        true
                            ? 0
                            : 1 : 2;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|] 
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia2()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true [|?|] // comment
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true // comment
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia3()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true /*comment*/ [|?|]
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true /*comment*/
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia4()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true /*comment*/ [|?|] 
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true /*comment*/
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia5()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C()
                {
                    var v = true /*comment1*/ [|?|] /*comment2*/
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C()
                {
                    var v = true /*comment1*/ /*comment2*/
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithDiagnosticsElsewhere()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public C(int{|CS1001:)|}
                {
                    var v = true [|?|]
                        0 :
                        1;
                }
            }
            """,
            FixedCode = """
            class C
            {
                public C(int{|CS1001:)|}
                {
                    var v = true
                        ? 0
                        : 1;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }
}
