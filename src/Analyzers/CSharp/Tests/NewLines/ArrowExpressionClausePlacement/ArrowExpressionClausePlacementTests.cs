// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ArrowExpressionClausePlacement;
using Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ArrowExpressionClausePlacement;

using Verify = CSharpCodeFixVerifier<
    ArrowExpressionClausePlacementDiagnosticAnalyzer,
    ArrowExpressionClausePlacementCodeFixProvider>;

public class ArrowExpressionClausePlacementTests
{
    [Fact]
    public async Task TestNotWithOptionOff()
    {
        var code =
            """
            class C
            {
                public int Add() =>
                    1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithSingleLineMethod()
    {
        var code =
            """
            class C
            {
                public int Add() => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithSingleLineProperty()
    {
        var code =
            """
            class C
            {
                public int Add => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithSingleLineLocalFunction()
    {
        var code =
            """
            class C
            {
                public void Main()
                {
                    int Add() => 1 + 2;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithLambda()
    {
        var code =
            """
            class C
            {
                public void Main()
                {
                    Goo(() =>
                        1 + 2);
                }

                public void Goo(System.Func<int> action) { }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestMethodCase()
    {
        var code =
            """
            class C
            {
                public int Add() [|=>|]
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add()
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotPropertyAccessor1()
    {
        var code =
            """
            class C
            {
                public int Add
                {
                    get =>
                        1 + 2;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestProperty()
    {
        var code =
            """
            class C
            {
                public int Add [|=>|]
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalFunction()
    {
        var code =
            """
            class C
            {
                void Main()
                {
                    int Add() [|=>|]
                        1 + 2;
                }
            }
            """;

        var fixedCode =
            """
            class C
            {
                void Main()
                {
                    int Add()
                        => 1 + 2;
                }
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInDeclaration()
    {
        var code =
            """
            class C
            {
                public int Add(int{|CS1001:)|} =>
                    1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInExpression()
    {
        var code =
            """
            class C
            {
                public int Add() =>
                    1 + {|CS1525:;|}
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsAtEnd()
    {
        var code =
            """
            class C
            {
                public int Add() =>
                    1 + 2{|CS1002:|}
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia1()
    {
        var code =
            """
            class C
            {
                public int Add() =>
            #if true
                    1 + 2;
            #endif
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithFirstExprWithPPTrivia2()
    {
        var code =
            """
            class C
            {
            #if true
                public int Add() =>
            #endif
                    1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = code,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegion1()
    {
        var code =
            """
            class C
            {
                public int Add() [|=>|]
            #region section
                    1 + 2;
            #endregion
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add()
            #region section
                    => 1 + 2;
            #endregion
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithRegion2()
    {
        var code =
            """
            class C
            {
            #region section
                public int Add() [|=>|]
            #endregion
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
            #region section
                public int Add()
            #endregion
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithNullableDirective1()
    {
        var code =
            """
            class C
            {
                public int Add() [|=>|]
            #nullable enable
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add()
            #nullable enable
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia1()
    {
        var code =
            """
            class C
            {
                public int Add() [|=>|] 
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add()
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia2()
    {
        var code =
            """
            class C
            {
                public int Add() [|=>|] // comment
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add() // comment
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia3()
    {
        var code =
            """
            class C
            {
                public int Add() /* comment */ [|=>|]
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add() /* comment */
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia4()
    {
        var code =
            """
            class C
            {
                public int Add() /* comment */ [|=>|] 
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add() /* comment */
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia5()
    {
        var code =
            """
            class C
            {
                public int Add() /* comment1 */ [|=>|] /* comment2 */
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public int Add() /* comment1 */ /* comment2 */
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                }

                public int Add() [|=>|]
                    1 + 2;
            }
            """;

        var fixedCode =
            """
            class C
            {
                public C(int{|CS1001:)|}
                {
                }

                public int Add()
                    => 1 + 2;
            }
            """;

        await new Verify.Test
        {
            TestCode = code,
            FixedCode = fixedCode,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }
}
