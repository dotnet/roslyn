// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConditionalExpressionPlacement;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ConditionalExpressionPlacement;

using Verify = CSharpCodeFixVerifier<
    ConditionalExpressionPlacementDiagnosticAnalyzer,
    ConditionalExpressionPlacementCodeFixProvider>;

public class ConditionalExpressionPlacementTests
{
    [Fact]
    public async Task TestNotWithOptionOff()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true ?
                        0 :
                        1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestBaseCase()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true [|?|]
                        0 :
                        1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C()
                {
                    var v = true
                        ? 0
                        : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInCondition()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true || {|CS1525:?|}
                        0:
                        1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInTrue()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true ?
                        0 + {|CS1525::|}
                        1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInFalse()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true ?
                        0 :
                        1 +{|CS1525:;|}
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithMissingColon()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true ?
                        0{|CS1003:{|CS1525:;|}|}
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithQuestionNotAtEndOfLine()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true ? 1 :
                        1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithColonNotAtEndOfLine()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true ?
                        1 : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia1()
    {
        var code =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia2()
    {
        var code =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia3()
    {
        var code =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia4()
    {
        var code =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia5()
    {
        var code =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegion1()
    {
        var code =
            """
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
            """;

        var fixedCode =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegion2()
    {
        var code =
            """
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
            """;

        var fixedCode =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNullableDirective1()
    {
        var code =
            """
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
            """;

        var fixedCode =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNullableDirective2()
    {
        var code =
            """
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
            """;

        var fixedCode =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNested1()
    {
        var code =
            """
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
            """;

        var fixedCode =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNested2()
    {
        var code =
            """
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
            """;

        var fixedCode =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNested3()
    {
        var code =
            """
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
            """;

        var fixedCode =
            """
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
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia1()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true [|?|] 
                        0 :
                        1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C()
                {
                    var v = true
                        ? 0
                        : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia2()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true [|?|] // comment
                        0 :
                        1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C()
                {
                    var v = true // comment
                        ? 0
                        : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia3()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true /*comment*/ [|?|]
                        0 :
                        1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C()
                {
                    var v = true /*comment*/
                        ? 0
                        : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia4()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true /*comment*/ [|?|] 
                        0 :
                        1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C()
                {
                    var v = true /*comment*/
                        ? 0
                        : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia5()
    {
        var code =
            """
            class C
            {
                public C()
                {
                    var v = true /*comment1*/ [|?|] /*comment2*/
                        0 :
                        1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C()
                {
                    var v = true /*comment1*/ /*comment2*/
                        ? 0
                        : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithDiagnosticsElsewhere()
    {
        var code =
            """
            class C
            {
                public C(int{|CS1001:)|}
                {
                    var v = true [|?|]
                        0 :
                        1;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C(int{|CS1001:)|}
                {
                    var v = true
                        ? 0
                        : 1;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInConditionalExpression, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }
}
