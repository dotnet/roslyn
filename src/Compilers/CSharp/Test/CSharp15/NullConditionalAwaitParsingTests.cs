// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class NullConditionalAwaitParsingTests : ParsingTests
{
    public NullConditionalAwaitParsingTests(ITestOutputHelper output) : base(output) { }

    // In async code `await? X` is a null-conditional-await expression, while in non-async code `await` stays an
    // identifier and the `?` is handled by ordinary expression parsing.
    private static string InAsync(string bodyStatement) => $$"""
        async void M()
        {
            {{bodyStatement}}
        }
        """;

    private static string InNonAsync(string bodyStatement) => $$"""
        void M()
        {
            {{bodyStatement}}
        }
        """;

    #region Full shape (one representative test)

    [Fact]
    public void FullShape_ClassAsyncMethodWithDiscardAssignment()
    {
        UsingTree("""
            class C
            {
                async void M()
                {
                    _ = await? x;
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "_");
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    #endregion

    #region Top-level local function contexts

    [Fact]
    public void AwaitQuestion_TopLevelAsyncLocalFunction()
    {
        UsingTree(InAsync("await? x;"));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.AwaitExpression);
                            {
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    [Fact]
    public void AwaitQuestion_TopLevelNonAsyncLocalFunction_IsNotAwaitExpression()
    {
        // In non-async code, `await?` is not a null-conditional await. The `await` is an
        // identifier and the `?` starts a ternary; without a `:` the parser reports the
        // standard ternary-missing-colon diagnostics and no AwaitExpression is built.
        UsingTree(
            InNonAsync("_ = await? x;"),
            // (3,17): error CS1003: Syntax error, ':' expected
            //     _ = await? x;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(3, 17),
            // (3,17): error CS1525: Invalid expression term ';'
            //     _ = await? x;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "_");
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.ConditionalExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    M(SyntaxKind.ColonToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    [Fact]
    public void AwaitQuestionColonY_TopLevelAsyncLocalFunction_EatsQuestionThenFailsOnColon()
    {
        // Async counterpart to the non-async ternary test below: `_ = await ? x : y;` in
        // an async method. Here `await` is the keyword and `?` is eaten as the
        // null-conditional-await marker, so the expression is `await? x` and `: y;` is left
        // over. Recovery: the parser injects a missing `;` after `x`, injects a missing
        // `}` to close the local function, then treats `: y;` as an errant `:` plus a new
        // top-level `y;` statement.
        UsingTree(
            InAsync("_ = await ? x : y;"),
            // (3,19): error CS1002: ; expected
            //     _ = await ? x : y;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(3, 19),
            // (3,19): error CS1513: } expected
            //     _ = await ? x : y;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 19),
            // (3,19): error CS1022: Type or namespace definition, or end-of-file expected
            //     _ = await ? x : y;
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(3, 19),
            // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "_");
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    [Fact]
    public void AwaitQuestionColonY_TopLevelNonAsyncLocalFunction_IsTernary()
    {
        // `await ? x : y` in non-async code is a ternary conditional expression with
        // `await` as the identifier condition.
        UsingTree(InNonAsync("_ = await ? x : y;"));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "_");
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.ConditionalExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    #endregion

    #region Lambda contexts

    [Fact]
    public void AwaitQuestion_InAsyncLambda()
    {
        // `() => await? x` inside an `async` lambda: the lambda body is an expression in
        // async context, so `await? x` is an AwaitExpression with QuestionToken.
        UsingExpression("async () => await? x");

        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.AsyncKeyword);
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
        }
    }

    [Fact]
    public void AwaitQuestion_InNonAsyncLambda_IsNotAwaitExpression()
    {
        // In a non-async lambda, `await` is an identifier and `?` starts a ternary. With a
        // complete ternary `await ? x : y` this is a ConditionalExpression in the lambda body.
        UsingExpression("() => await ? x : y");

        N(SyntaxKind.ParenthesizedLambdaExpression);
        {
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "await");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
        }
    }

    #endregion

    #region Language version

    [Fact]
    public void LastNonPreviewLanguageVersion_ParsesWithoutDiagnostics()
    {
        // The parser does not gate `await?` on language version; feature-availability is
        // checked in binding (a later phase). On the last non-preview language version the
        // tree parses cleanly with no diagnostics and the same shape as on Preview.
        UsingTree(InAsync("await? x;"), TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp14));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.AwaitExpression);
                            {
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    #endregion

    #region Whitespace / trivia between 'await' and '?'

    // All ten variants produce the same tree shape (trivia differs, tokens identical).
    [Theory]
    [InlineData("await?x")]
    [InlineData("await ?x")]
    [InlineData("await? x")]
    [InlineData("await ? x")]
    [InlineData("await  ?  x")]
    [InlineData("await\t?\tx")]
    [InlineData("await\r\n?\r\nx")]
    [InlineData("await/*c*/?/*c*/x")]
    [InlineData("await/**/?x")]
    [InlineData("await //line-comment\n?x")]
    public void TriviaBetweenAwaitAndQuestion(string expression)
    {
        UsingTree(InAsync($"{expression};"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region Async context: every contextual-identifier operand

    // A shape-uniform theory over every contextual-identifier operand. `await <id>` and
    // `await? <id>` both produce an AwaitExpression with IdentifierName operand. The same
    // identifier is also exercised as the operand in a non-async method to confirm that the
    // existing (no-`?`) form still parses as an AwaitExpression, and that the `?` form
    // there does not.

    [Theory]
    [InlineData("x")]
    [InlineData("field")]
    [InlineData("value")]
    [InlineData("var")]
    [InlineData("dynamic")]
    [InlineData("async")]
    [InlineData("yield")]
    [InlineData("record")]
    [InlineData("file")]
    [InlineData("global")]
    [InlineData("notnull")]
    [InlineData("unmanaged")]
    public void AsyncContext_AwaitQuestion_IdentifierOperand(string operand)
    {
        UsingTree(InAsync($"await? {operand};"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, operand);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Theory]
    [InlineData("x")]
    [InlineData("field")]
    [InlineData("value")]
    [InlineData("var")]
    [InlineData("dynamic")]
    [InlineData("async")]
    [InlineData("yield")]
    [InlineData("record")]
    [InlineData("file")]
    [InlineData("global")]
    [InlineData("notnull")]
    [InlineData("unmanaged")]
    public void AsyncContext_AwaitNoQuestion_IdentifierOperand(string operand)
    {
        UsingTree(InAsync($"await {operand};"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, operand);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Theory]
    [InlineData("x")]
    [InlineData("field")]
    [InlineData("value")]
    [InlineData("var")]
    [InlineData("dynamic")]
    [InlineData("async")]
    [InlineData("yield")]
    [InlineData("record")]
    [InlineData("file")]
    [InlineData("global")]
    [InlineData("notnull")]
    [InlineData("unmanaged")]
    public void SyncContext_AwaitQuestion_IdentifierOperand_IsTernary(string operand)
    {
        // `_ = await? <id>;` in non-async: `await` is identifier, `?` starts ternary,
        // `<id>` is the true-branch, `;` causes missing-`:` diagnostics.
        UsingTree(
            InNonAsync($"_ = await? {operand};"),
            // (3,15 + len(operand)): errors about missing `:` and missing whenFalse
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(3, 15 + operand.Length + 1),
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 15 + operand.Length + 1));

        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, operand);
                    }
                    M(SyntaxKind.ColonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Theory]
    [InlineData("x")]
    [InlineData("field")]
    [InlineData("value")]
    [InlineData("var")]
    [InlineData("dynamic")]
    [InlineData("async")]
    [InlineData("yield")]
    [InlineData("record")]
    [InlineData("file")]
    [InlineData("global")]
    [InlineData("notnull")]
    [InlineData("unmanaged")]
    public void SyncContext_AwaitNoQuestion_IdentifierOperand(string operand)
    {
        // `_ = await <id>;` in non-async: operand starts with an identifier (not `with`), so
        // the parser still produces an AwaitExpression (binding will later diagnose
        // "await requires an async function").
        UsingTree(InNonAsync($"_ = await {operand};"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, operand);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region Async context: every keyword / literal operand start

    // Each of these operands exercises a different first-token case of the parser's
    // await-expression recognition
    // (keyword or literal) and produces a distinct operand sub-tree, so each gets its own
    // fact. The facts collectively confirm that every kind listed in the switch is
    // accepted as an operand of the `await?` form.

    [Fact]
    public void AsyncContext_AwaitQuestion_NewKeyword()
    {
        UsingTree(InAsync("await? new C();"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_ThisKeyword()
    {
        UsingTree(InAsync("await? this;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ThisExpression);
                {
                    N(SyntaxKind.ThisKeyword);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_BaseKeyword()
    {
        UsingTree(InAsync("await? base;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.BaseExpression);
                {
                    N(SyntaxKind.BaseKeyword);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_DelegateKeyword()
    {
        UsingTree(InAsync("await? delegate { };"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.AnonymousMethodExpression);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_TypeOfKeyword()
    {
        UsingTree(InAsync("await? typeof(int);"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.TypeOfExpression);
                {
                    N(SyntaxKind.TypeOfKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_CheckedKeyword()
    {
        UsingTree(InAsync("await? checked(0);"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.CheckedExpression);
                {
                    N(SyntaxKind.CheckedKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_UncheckedKeyword()
    {
        UsingTree(InAsync("await? unchecked(0);"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.UncheckedExpression);
                {
                    N(SyntaxKind.UncheckedKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_DefaultKeyword_Literal()
    {
        UsingTree(InAsync("await? default;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_DefaultKeyword_Expression()
    {
        UsingTree(InAsync("await? default(int);"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.DefaultExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_TrueKeyword()
    {
        UsingTree(InAsync("await? true;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.TrueLiteralExpression);
                {
                    N(SyntaxKind.TrueKeyword);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_FalseKeyword()
    {
        UsingTree(InAsync("await? false;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.FalseLiteralExpression);
                {
                    N(SyntaxKind.FalseKeyword);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_NullKeyword()
    {
        UsingTree(InAsync("await? null;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Theory]
    [InlineData("42")]
    [InlineData("42.5")]
    public void AsyncContext_AwaitQuestion_NumericLiteralOperand(string operand)
    {
        UsingTree(InAsync($"await? {operand};"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, operand);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_CharacterLiteral()
    {
        UsingTree(InAsync("await? 'c';"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.CharacterLiteralExpression);
                {
                    N(SyntaxKind.CharacterLiteralToken, "'c'");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_StringLiteral()
    {
        UsingTree(InAsync("await? \"hello\";"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.StringLiteralExpression);
                {
                    N(SyntaxKind.StringLiteralToken, "\"hello\"");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_VerbatimStringLiteral()
    {
        UsingTree(InAsync("await? @\"verbatim\";"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.StringLiteralExpression);
                {
                    N(SyntaxKind.StringLiteralToken, "@\"verbatim\"");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_Utf8StringLiteral()
    {
        UsingTree(InAsync("await? \"u8-scalar\"u8;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.Utf8StringLiteralExpression);
                {
                    N(SyntaxKind.Utf8StringLiteralToken, "\"u8-scalar\"u8");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_SingleLineRawStringLiteral()
    {
        UsingTree(InAsync("await? \"\"\"raw\"\"\";"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.StringLiteralExpression);
                {
                    N(SyntaxKind.SingleLineRawStringLiteralToken, "\"\"\"raw\"\"\"");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_Utf8SingleLineRawStringLiteral()
    {
        UsingTree(InAsync("await? \"\"\"raw\"\"\"u8;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.Utf8StringLiteralExpression);
                {
                    N(SyntaxKind.Utf8SingleLineRawStringLiteralToken, "\"\"\"raw\"\"\"u8");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_MultiLineRawStringLiteral()
    {
        UsingTree(InAsync("await? \"\"\"\n raw-ml\n \"\"\";"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.StringLiteralExpression);
                {
                    N(SyntaxKind.MultiLineRawStringLiteralToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_Utf8MultiLineRawStringLiteral()
    {
        UsingTree(InAsync("await? \"\"\"\n raw-ml\n \"\"\"u8;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.Utf8StringLiteralExpression);
                {
                    N(SyntaxKind.Utf8MultiLineRawStringLiteralToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AsyncContext_AwaitQuestion_InterpolatedString()
    {
        UsingTree(InAsync("await? $\"interp{1}\";"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InterpolatedStringExpression);
                {
                    N(SyntaxKind.InterpolatedStringStartToken);
                    N(SyntaxKind.InterpolatedStringText);
                    {
                        N(SyntaxKind.InterpolatedStringTextToken, "interp");
                    }
                    N(SyntaxKind.Interpolation);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.InterpolatedStringEndToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region Non-async `with` and non-switch follow-tokens

    [Fact]
    public void SyncContext_AwaitWithIdentifier_IsIdentifier()
    {
        // `with` is the explicit exception among identifier operands in non-async context:
        // `await with { }` parses as a `with`-expression on the identifier `await`, not as
        // an await expression, because `with` can legally follow the identifier `await`.
        UsingTree(InNonAsync("_ = await with { };"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.WithExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void SyncContext_Await_Invocation_IsIdentifier()
    {
        // In non-async context, `await(x)` parses as an invocation on the identifier
        // `await`, not as an await expression.
        UsingTree(InNonAsync("_ = await(x);"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void SyncContext_Await_ElementAccess_IsIdentifier()
    {
        UsingTree(InNonAsync("_ = await[0];"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void SyncContext_Await_MemberAccess_IsIdentifier()
    {
        UsingTree(InNonAsync("_ = await.x;"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void SyncContext_Await_BinaryAdd_IsIdentifier()
    {
        UsingTree(InNonAsync("_ = await + x;"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.AddExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void SyncContext_Await_PostfixIncrement_IsIdentifier()
    {
        UsingTree(InNonAsync("_ = await++;"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.PostIncrementExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.PlusPlusToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region Escaped `await`

    [Fact]
    public void EscapedAwait_PlainIdentifier()
    {
        // `@await` defeats the contextual-keyword interpretation of `await`. It must not
        // be treated as a null-conditional await or as the `await` operator in any context.
        UsingTree(InNonAsync("_ = @await;"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@await");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void EscapedAwait_InTernary()
    {
        UsingTree(InNonAsync("_ = @await ? x : y;"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "@await");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void EscapedAwait_ConditionalMemberAccess()
    {
        UsingTree(InNonAsync("_ = @await?.Member;"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "@await");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Member");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void EscapedAwait_InAsyncMethod_StillIdentifier()
    {
        // Even inside an async method, `@await` is an identifier, not the await keyword.
        UsingTree(InAsync("_ = @await;"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@await");
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region field / value contextual-keyword operands

    [Fact]
    public void FieldKeyword_InPropertyGetter_IsNotAwaitExpression_InNonAsync()
    {
        // A property accessor without `async` is a non-async context. `await? field` is not
        // a null-conditional await; it's a ternary with `await` as identifier, with errors
        // for the missing `:` and whenFalse.
        UsingTree(
            """
            class C
            {
                int P { get { _ = await? field; return 0; } }
            }
            """,
            // (3,35): error CS1003: Syntax error, ':' expected
            //     int P { get { _ = await? field; return 0; } }
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(3, 35),
            // (3,35): error CS1525: Invalid expression term ';'
            //     int P { get { _ = await? field; return 0; } }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 35));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.ExpressionStatement);
                                {
                                    N(SyntaxKind.SimpleAssignmentExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "_");
                                        }
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.ConditionalExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "await");
                                            }
                                            N(SyntaxKind.QuestionToken);
                                            N(SyntaxKind.FieldExpression);
                                            {
                                                N(SyntaxKind.FieldKeyword);
                                            }
                                            M(SyntaxKind.ColonToken);
                                            M(SyntaxKind.IdentifierName);
                                            {
                                                M(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.ReturnStatement);
                                {
                                    N(SyntaxKind.ReturnKeyword);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    #endregion

    #region Precedence / disambiguation (async context)

    // These use UsingExpression for non-`?` forms (which parse in a default non-async
    // context but still produce AwaitExpressions since the operand starts with a token
    // that's unambiguous after `await`), and UsingExpressionInAsync for `?` forms (which
    // require an async context to be recognized as null-conditional awaits).

    [Fact]
    public void Precedence_AwaitVsTernary_NoQuestion()
    {
        // `await a ? b : c` binds as `(await a) ? b : c`. `a` is a non-`with` identifier,
        // so `await a` is an AwaitExpression and the following `?` is consumed as the start
        // of a ternary at the outer level (await has unary precedence, tighter than
        // conditional).
        UsingExpression("await a ? b : c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "c");
            }
        }
    }

    [Fact]
    public void Precedence_AwaitQuestion_ThenTernary()
    {
        UsingExpressionInAsync("await? a ? b : c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "c");
            }
        }
    }

    [Fact]
    public void Precedence_AwaitQuestion_BinaryPlus()
    {
        // `await? x + y` binds as `(await? x) + y` because await has unary precedence.
        UsingExpressionInAsync("await? x + y");

        N(SyntaxKind.AddExpression);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
            N(SyntaxKind.PlusToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "y");
            }
        }
    }

    [Fact]
    public void Precedence_AwaitQuestion_MemberAccess()
    {
        // `await? x.Foo()` binds as `await? (x.Foo())`: the operand is a primary expression
        // that consumes member access and invocation before the unary await completes.
        UsingExpressionInAsync("await? x.Foo()");

        N(SyntaxKind.AwaitExpression);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Foo");
                    }
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
        }
    }

    [Fact]
    public void Precedence_UnaryNotOnAwaitQuestion()
    {
        UsingExpressionInAsync("!await? x");

        N(SyntaxKind.LogicalNotExpression);
        {
            N(SyntaxKind.ExclamationToken);
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
        }
    }

    [Fact]
    public void Nested_AwaitQuestionOfAwaitQuestion()
    {
        UsingExpressionInAsync("await? await? x");

        N(SyntaxKind.AwaitExpression);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
        }
    }

    [Fact]
    public void Nested_AwaitQuestionOfAwait()
    {
        UsingExpressionInAsync("await? await x");

        N(SyntaxKind.AwaitExpression);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
        }
    }

    [Fact]
    public void IsExpressionInTernary_WithAwaitQuestion()
    {
        UsingExpressionInAsync("x is int ? await? y : z");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "z");
            }
        }
    }

    #endregion

    #region Error recovery

    [Fact]
    public void MissingOperand_InAsync()
    {
        // In async context `await?` is unconditionally parsed as an AwaitExpression, so a
        // missing operand produces a missing-identifier node and the standard
        // ERR_InvalidExprTerm diagnostic.
        UsingTree(
            InAsync("_ = await?;"),
            // (3,15): error CS1525: Invalid expression term ';'
            //     _ = await?;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 15));

        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.QuestionToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void DoubleQuestion_InAsync()
    {
        // `await? ?x` in async: the first `?` is eaten as the null-conditional marker and
        // the operand parse on the second `?` fails (a `?` can't start a primary), leaving
        // a missing identifier. The second `?` is then consumed at the outer level as the
        // start of a ternary whose condition is the (AwaitExpression{missing-operand}),
        // whose whenTrue is `x`, and whose `:`/whenFalse are missing.
        UsingTree(
            InAsync("_ = await? ?x;"),
            // (3,16): error CS1525: Invalid expression term '?'
            //     _ = await? ?x;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(3, 16),
            // (3,18): error CS1003: Syntax error, ':' expected
            //     _ = await? ?x;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(3, 18),
            // (3,18): error CS1525: Invalid expression term ';'
            //     _ = await? ?x;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 18));

        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.QuestionToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    M(SyntaxKind.ColonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region await? does not steal the await-foreach / await-using statement forms

    // `await foreach` and `await using` are separate grammar productions driven by
    // statement-level lookahead at PeekToken(1). Users who have recently learned about
    // `await?` may naturally write `await? foreach` or `await? using` — which isn't a
    // legal form because the `?` would only be meaningful on a value expression, not on
    // a statement. The parser recovers by emitting a single ERR_UnexpectedToken on `?`
    // and parsing the rest as a normal `await foreach` / `await using` statement, with
    // the `?` attached as trailing skipped syntax on the `await` keyword.

    [Fact]
    public void AwaitQuestion_ForEach_InAsync_RecoversAsAwaitForEach()
    {
        UsingTree(
            InAsync("await? foreach (var x in xs) { }"),
            // (3,10): error CS1525: Invalid expression term '?'
            //         await? foreach (var x in xs) { }
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "?").WithArguments("?").WithLocation(3, 10));

        WalkTopLevelAsyncLocalFunctionPreamble();

        // Whole statement parsed as a plain `await foreach`; the `?` is attached as
        // trailing skipped syntax on the `await` keyword (visible as the diagnostic
        // above) and does not appear in the tree shape.
        N(SyntaxKind.ForEachStatement);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.ForEachKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "var");
            }
            N(SyntaxKind.IdentifierToken, "x");
            N(SyntaxKind.InKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "xs");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitQuestion_Using_Parenthesized_InAsync_RecoversAsAwaitUsing()
    {
        UsingTree(
            InAsync("await? using (d) { }"),
            // (3,10): error CS1525: Invalid expression term '?'
            //         await? using (d) { }
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "?").WithArguments("?").WithLocation(3, 10));

        WalkTopLevelAsyncLocalFunctionPreamble();

        // Whole statement parsed as a plain `await using (d) { }`.
        N(SyntaxKind.UsingStatement);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.UsingKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitQuestion_UsingDeclaration_InAsync_RecoversAsAwaitUsingDeclaration()
    {
        // `await? using var d = ...` — the declaration-style `await using`. Same recovery:
        // single ERR_UnexpectedToken on `?`, parsed as `await using var d = ...`.
        UsingTree(
            InAsync("await? using var d = x;"),
            // (3,10): error CS1525: Invalid expression term '?'
            //         await? using var d = x;
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "?").WithArguments("?").WithLocation(3, 10));

        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.LocalDeclarationStatement);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.UsingKeyword);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "var");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitForEach_NoQuestion_StillParsesAsAwaitForEach()
    {
        // Regression: unchanged `await foreach` still picks the ForEachStatement form with
        // an AwaitKeyword modifier.
        UsingTree(InAsync("await foreach (var x in xs) { }"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ForEachStatement);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.ForEachKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "var");
            }
            N(SyntaxKind.IdentifierToken, "x");
            N(SyntaxKind.InKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "xs");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitUsing_NoQuestion_StillParsesAsAwaitUsing()
    {
        UsingTree(InAsync("await using (d) { }"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.UsingStatement);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.UsingKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region Other async contexts

    [Fact]
    public void AwaitQuestion_ExpressionBodiedAsyncMethod()
    {
        UsingTree("""
            async void M() => _ = await? t;
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.AwaitExpression);
                            {
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "t");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
    }

    [Fact]
    public void AwaitQuestion_ParenthesizedOperand_InAsync()
    {
        // In async context `await? (t)` parses as an AwaitExpression whose operand is the
        // parenthesized expression `(t)`.
        UsingTree(InAsync("await? (t);"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "t");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    #endregion

    #region await?. and await?[ ] — conditional-access spellings that could look like the new form

    // `await?.x` and `await?[0]` are syntactically ambiguous between two interpretations:
    //  (a) `(await)?.x` / `(await)?[0]` — null-conditional access on the identifier `await`.
    //  (b) `await? (.x)` / `await? ([0])` — the new null-conditional await on an operand
    //      starting with `.` or `[`.
    //
    // The parser resolves this by context: in non-async, `await` stays an identifier and
    // the outer `?.`/`?[` is ordinary null-conditional access. In async, the `?` is eaten
    // as the null-conditional-await marker; the remaining `.x` or `[0]` is then parsed as
    // the operand (where it produces a syntax error for missing identifier).

    [Fact]
    public void AwaitQuestionDotX_InNonAsync_IsConditionalMemberAccessOnAwaitIdentifier()
    {
        UsingTree(InNonAsync("_ = await?.x;"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitQuestionBracket_InNonAsync_IsConditionalElementAccessOnAwaitIdentifier()
    {
        UsingTree(InNonAsync("_ = await?[0];"));
        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ElementBindingExpression);
                    {
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitQuestionDotX_InAsync_EatsQuestionAndParsesDotAsErrorRecovery()
    {
        // In async, the `?` is eaten as the null-conditional-await marker and the operand
        // parse starts on `.x`. `.` cannot start a primary expression, so the parser emits
        // ERR_InvalidExprTerm and produces a missing-identifier operand with `.x` attached
        // as a postfix member access. The whole expression is an AwaitExpression with
        // QuestionToken; it is NOT a conditional access on `await`.
        UsingTree(
            InAsync("_ = await?.x;"),
            // (3,15): error CS1525: Invalid expression term '.'
            //     _ = await?.x;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments(".").WithLocation(3, 15));

        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitQuestion_OnConditionalAccessInvocation_InNonAsync()
    {
        // Non-async counterpart to the motivating async spec case. Here `await?` is not a
        // null-conditional await: `await` stays an identifier and `?` starts a ternary.
        // The whenTrue branch parses the following `x?.Y()` as a conditional-access
        // invocation (on `x`, not on `await`). Without a `:` the ternary emits the standard
        // missing-colon diagnostics.
        UsingTree(
            InNonAsync("_ = await? x?.Y();"),
            // (3,22): error CS1003: Syntax error, ':' expected
            //     _ = await? x?.Y();
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(3, 22),
            // (3,22): error CS1525: Invalid expression term ';'
            //     _ = await? x?.Y();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(3, 22));

        WalkTopLevelNonAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.MemberBindingExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Y");
                                }
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                    M(SyntaxKind.ColonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitQuestion_OnConditionalAccessInvocation_InAsync()
    {
        // The motivating case from the spec: `await? x?.Y();`. The outer `await?` is the
        // new null-conditional await; its operand is a conditional-access invocation on
        // `x`. The two `?` tokens serve different grammars and nest cleanly.
        UsingTree(InAsync("await? x?.Y();"));
        WalkTopLevelAsyncLocalFunctionPreamble();
        N(SyntaxKind.ExpressionStatement);
        {
            N(SyntaxKind.AwaitExpression);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Y");
                            }
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        WalkTopLevelLocalFunctionPostamble();
    }

    [Fact]
    public void AwaitQuestionBracket_InAsync_EatsQuestionAndParsesBracketAsCollectionExpression()
    {
        // In async, the `?` is eaten as the null-conditional-await marker and `[0]` parses
        // as a collection expression operand. There is no conditional element access here.
        UsingExpressionInAsync("await?[0]");

        N(SyntaxKind.AwaitExpression);
        {
            N(SyntaxKind.AwaitKeyword);
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
    }

    #endregion

    #region helpers

    // Walks the outer scaffolding of `async void M() { ... }` up to (but not including) the
    // block body. Use with WalkTopLevelLocalFunctionPostamble to close out the walk.
    private void WalkTopLevelAsyncLocalFunctionPreamble()
    {
        N(SyntaxKind.CompilationUnit);
        N(SyntaxKind.GlobalStatement);
        N(SyntaxKind.LocalFunctionStatement);
        N(SyntaxKind.AsyncKeyword);
        N(SyntaxKind.PredefinedType);
        N(SyntaxKind.VoidKeyword);
        N(SyntaxKind.IdentifierToken, "M");
        N(SyntaxKind.ParameterList);
        N(SyntaxKind.OpenParenToken);
        N(SyntaxKind.CloseParenToken);
        N(SyntaxKind.Block);
        N(SyntaxKind.OpenBraceToken);
    }

    // Walks the outer scaffolding of `void M() { ... }`.
    private void WalkTopLevelNonAsyncLocalFunctionPreamble()
    {
        N(SyntaxKind.CompilationUnit);
        N(SyntaxKind.GlobalStatement);
        N(SyntaxKind.LocalFunctionStatement);
        N(SyntaxKind.PredefinedType);
        N(SyntaxKind.VoidKeyword);
        N(SyntaxKind.IdentifierToken, "M");
        N(SyntaxKind.ParameterList);
        N(SyntaxKind.OpenParenToken);
        N(SyntaxKind.CloseParenToken);
        N(SyntaxKind.Block);
        N(SyntaxKind.OpenBraceToken);
    }

    // Closes the outer scaffolding (CloseBraceToken of the block and EndOfFileToken).
    private void WalkTopLevelLocalFunctionPostamble()
    {
        N(SyntaxKind.CloseBraceToken);
        N(SyntaxKind.EndOfFileToken);
    }

    // Parses the given expression text inside a top-level async local function so the
    // parse happens in an async context, then walks just the expression subtree via
    // UsingNode. This is the async counterpart to UsingExpression (which parses in a
    // default non-async context).
    private void UsingExpressionInAsync(string expressionText, params DiagnosticDescription[] expectedErrors)
    {
        var source = InAsync($"_ = {expressionText};");
        var tree = SyntaxFactory.ParseSyntaxTree(source);
        Assert.Equal(source, tree.GetRoot().ToFullString());

        var assignment = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().Single();
        var expression = (CSharpSyntaxNode)assignment.Right;

        Assert.Equal(expressionText, expression.ToFullString());
        expression.GetDiagnostics().Verify(expectedErrors);
        UsingNode(expression);
    }

    #endregion
}
