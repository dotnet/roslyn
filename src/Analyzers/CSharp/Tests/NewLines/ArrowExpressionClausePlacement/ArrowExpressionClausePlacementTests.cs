// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.NewLines.ArrowExpressionClausePlacement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NewLines.ArrowExpressionClausePlacement;

using Verify = CSharpCodeFixVerifier<
    ArrowExpressionClausePlacementDiagnosticAnalyzer,
    ArrowExpressionClausePlacementCodeFixProvider>;

public sealed class ArrowExpressionClausePlacementTests
{
    [Fact]
    public async Task TestNotWithOptionOff()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add() =>
                    1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithSingleLineMethod()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add() => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithSingleLineProperty()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithSingleLineLocalFunction()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public void Main()
                {
                    int Add() => 1 + 2;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithLambda()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public void Main()
                {
                    Goo(() =>
                        1 + 2);
                }

                public void Goo(System.Func<int> action) { }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestMethodCase()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add() [|=>|]
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add()
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotPropertyAccessor1()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add
                {
                    get =>
                        1 + 2;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestProperty()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add [|=>|]
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalFunction()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                void Main()
                {
                    int Add() [|=>|]
                        1 + 2;
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Main()
                {
                    int Add()
                        => 1 + 2;
                }
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInDeclaration()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add(int{|CS1001:)|} =>
                    1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsInExpression()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add() =>
                    1 + {|CS1525:;|}
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithDiagnosticsAtEnd()
    {
        await new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add() =>
                    1 + 2{|CS1002:|}
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
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
                public int Add() =>
            #if true
                    1 + 2;
            #endif
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
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
            #if true
                public int Add() =>
            #endif
                    1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
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
                public int Add() [|=>|]
            #region section
                    1 + 2;
            #endregion
            }
            """,
            FixedCode = """
            class C
            {
                public int Add()
            #region section
                    => 1 + 2;
            #endregion
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
            #region section
                public int Add() [|=>|]
            #endregion
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
            #region section
                public int Add()
            #endregion
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                public int Add() [|=>|]
            #nullable enable
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add()
            #nullable enable
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                public int Add() [|=>|] 
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add()
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                public int Add() [|=>|] // comment
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add() // comment
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                public int Add() /* comment */ [|=>|]
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add() /* comment */
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                public int Add() /* comment */ [|=>|] 
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add() /* comment */
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                public int Add() /* comment1 */ [|=>|] /* comment2 */
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public int Add() /* comment1 */ /* comment2 */
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
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
                }

                public int Add() [|=>|]
                    1 + 2;
            }
            """,
            FixedCode = """
            class C
            {
                public C(int{|CS1001:)|}
                {
                }

                public int Add()
                    => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.FalseWithSuggestionEnforcement } }
        }.RunAsync();
    }
}
