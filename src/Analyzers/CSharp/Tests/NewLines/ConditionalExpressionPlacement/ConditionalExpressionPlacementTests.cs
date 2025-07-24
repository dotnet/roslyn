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
    public Task TestNotWithOptionOff()
        => new Verify.Test
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

    [Fact]
    public Task TestBaseCase()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithDiagnosticsInCondition()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithDiagnosticsInTrue()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithDiagnosticsInFalse()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithMissingColon()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithQuestionNotAtEndOfLine()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithColonNotAtEndOfLine()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithFirstExprWithPPTrivia1()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithFirstExprWithPPTrivia2()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithFirstExprWithPPTrivia3()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithFirstExprWithPPTrivia4()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithFirstExprWithPPTrivia5()
        => new Verify.Test
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

    [Fact]
    public Task TestWithRegion1()
        => new Verify.Test
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

    [Fact]
    public Task TestWithRegion2()
        => new Verify.Test
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

    [Fact]
    public Task TestWithNullableDirective1()
        => new Verify.Test
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

    [Fact]
    public Task TestWithNullableDirective2()
        => new Verify.Test
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

    [Fact]
    public Task TestNested1()
        => new Verify.Test
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

    [Fact]
    public Task TestNested2()
        => new Verify.Test
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

    [Fact]
    public Task TestNested3()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia1()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia2()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia3()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia4()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia5()
        => new Verify.Test
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

    [Fact]
    public Task TestWithDiagnosticsElsewhere()
        => new Verify.Test
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
