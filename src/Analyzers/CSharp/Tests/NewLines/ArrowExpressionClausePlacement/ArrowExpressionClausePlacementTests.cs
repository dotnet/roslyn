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
    public Task TestNotWithOptionOff()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithSingleLineMethod()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add() => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNotWithSingleLineProperty()
        => new Verify.Test
        {
            TestCode = """
            class C
            {
                public int Add => 1 + 2;
            }
            """,
            Options = { { CSharpCodeStyleOptions.AllowBlankLineAfterTokenInArrowExpressionClause, CodeStyleOption2.TrueWithSilentEnforcement } }
        }.RunAsync();

    [Fact]
    public Task TestNotWithSingleLineLocalFunction()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithLambda()
        => new Verify.Test
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

    [Fact]
    public Task TestMethodCase()
        => new Verify.Test
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

    [Fact]
    public Task TestNotPropertyAccessor1()
        => new Verify.Test
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

    [Fact]
    public Task TestProperty()
        => new Verify.Test
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

    [Fact]
    public Task TestLocalFunction()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithDiagnosticsInDeclaration()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithDiagnosticsInExpression()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithDiagnosticsAtEnd()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithFirstExprWithPPTrivia1()
        => new Verify.Test
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

    [Fact]
    public Task TestNotWithFirstExprWithPPTrivia2()
        => new Verify.Test
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

    [Fact]
    public Task TestWithRegion1()
        => new Verify.Test
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

    [Fact]
    public Task TestWithRegion2()
        => new Verify.Test
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

    [Fact]
    public Task TestWithNullableDirective1()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia1()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia2()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia3()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia4()
        => new Verify.Test
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

    [Fact]
    public Task TestTrivia5()
        => new Verify.Test
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

    [Fact]
    public Task TestWithDiagnosticsElsewhere()
        => new Verify.Test
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
