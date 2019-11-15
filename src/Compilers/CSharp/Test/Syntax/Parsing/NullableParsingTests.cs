// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NullableParsingTests : ParsingTests
    {
        public NullableParsingTests(ITestOutputHelper output) :
            base(output)
        {
        }

        [WorkItem(23272, "https://github.com/dotnet/roslyn/issues/23272")]
        [Fact]
        public void PartialAccessibilityAndNullableArray()
        {
            // Note: the typo `privat` is necessary for https://github.com/dotnet/roslyn/issues/23272.
            UsingTree(
@"class C
{
    privat C[]? F;
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "privat");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpression);
                                        {
                                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [WorkItem(24482, "https://github.com/dotnet/roslyn/issues/24482")]
        [Fact]
        public void NullableArray_Cast_01()
        {
            UsingExpression("(object[]?)null");
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [WorkItem(24482, "https://github.com/dotnet/roslyn/issues/24482")]
        [Fact]
        public void NullableArray_Cast_02()
        {
            UsingExpression("(object[]??)null",
                // (1,1): error CS1073: Unexpected token 'null'
                // (object[]??)null
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(object[]??)").WithArguments("null").WithLocation(1, 1),
                // (1,2): error CS1525: Invalid expression term 'object'
                // (object[]??)null
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "object").WithArguments("object").WithLocation(1, 2),
                // (1,9): error CS0443: Syntax error; value expected
                // (object[]??)null
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(1, 9),
                // (1,12): error CS1525: Invalid expression term ')'
                // (object[]??)null
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 12));
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CoalesceExpression);
                {
                    N(SyntaxKind.ElementAccessExpression);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.Argument);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.QuestionQuestionToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [WorkItem(24482, "https://github.com/dotnet/roslyn/issues/24482")]
        [Fact]
        public void NullableArray_Cast_03()
        {
            UsingExpression("(object[?])null",
                // (1,1): error CS1073: Unexpected token 'null'
                // (object[?])null
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(object[?])").WithArguments("null").WithLocation(1, 1),
                // (1,2): error CS1525: Invalid expression term 'object'
                // (object[?])null
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "object").WithArguments("object").WithLocation(1, 2),
                // (1,9): error CS1001: Identifier expected
                // (object[?])null
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "?").WithLocation(1, 9));
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.ObjectKeyword);
                    }
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [WorkItem(24482, "https://github.com/dotnet/roslyn/issues/24482")]
        [Fact]
        public void NullableArray_Cast_04()
        {
            UsingExpression("(object?[]?[])null");
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.ObjectKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.OmittedArraySizeExpression);
                        {
                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [WorkItem(24482, "https://github.com/dotnet/roslyn/issues/24482")]
        [Fact]
        public void NullableArray_Cast_05()
        {
            UsingExpression("(object[][]?[]?)null");
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.ObjectKeyword);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalOperator_NotNullableType()
        {
            UsingExpression("x is T ? y : z");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "z");
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalOperator_NullableType()
        {
            UsingExpression("x is T ? ? y : z");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "z");
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalOperator_NotNullableArray()
        {
            UsingExpression("x is T[] ? y : z");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "z");
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalOperator_NullableArray()
        {
            UsingExpression("x is T[] ? ? y : z");
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "z");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingOperator_NotNullableType()
        {
            UsingExpression("x as T?? y");
            N(SyntaxKind.CoalesceExpression);
            {
                N(SyntaxKind.AsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.AsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.QuestionQuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingOperator_NullableType()
        {
            UsingExpression("x as T? ?? y");
            N(SyntaxKind.CoalesceExpression);
            {
                N(SyntaxKind.AsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.AsKeyword);
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                }
                N(SyntaxKind.QuestionQuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingOperator_NullableType_Invalid()
        {
            UsingExpression("x as T??? y",
                // (1,9): error CS1525: Invalid expression term '?'
                // x as T??? y
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?").WithLocation(1, 9),
                // (1,12): error CS1003: Syntax error, ':' expected
                // x as T??? y
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":", "").WithLocation(1, 12),
                // (1,12): error CS1733: Expected expression
                // x as T??? y
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.CoalesceExpression);
                {
                    N(SyntaxKind.AsExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.AsKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    N(SyntaxKind.QuestionQuestionToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingOperator_NotNullableArray()
        {
            UsingExpression("x as T[] ?? y");
            N(SyntaxKind.CoalesceExpression);
            {
                N(SyntaxKind.AsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.AsKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.QuestionQuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingOperator_NullableArray()
        {
            UsingExpression("x as T[] ? ?? y");
            N(SyntaxKind.CoalesceExpression);
            {
                N(SyntaxKind.AsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.AsKeyword);
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                }
                N(SyntaxKind.QuestionQuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
            EOF();
        }

        [Fact]
        public void DeclarationPattern_NullableType()
        {
            UsingStatement("switch (e) { case T? t: break; }",
                // (1,25): error CS1525: Invalid expression term 'break'
                // switch (e) { case T? t: break; }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "break").WithArguments("break").WithLocation(1, 25),
                // (1,25): error CS1003: Syntax error, ':' expected
                // switch (e) { case T? t: break; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "break").WithArguments(":", "break").WithLocation(1, 25));
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void DeclarationPattern_NullableArray()
        {
            UsingStatement("switch (e) { case T[]? t: break; }",
                // (1,19): error CS8652: The feature 'type pattern' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // switch (e) { case T[]? t: break; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "T[]").WithArguments("type pattern").WithLocation(1, 19),
                // (1,22): error CS1003: Syntax error, ':' expected
                // switch (e) { case T[]? t: break; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "?").WithArguments(":", "?").WithLocation(1, 22),
                // (1,22): error CS1513: } expected
                // switch (e) { case T[]? t: break; }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "?").WithLocation(1, 22));
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.TypePattern);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "T");
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        M(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.LabeledStatement);
                    {
                        N(SyntaxKind.IdentifierToken, "t");
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.BreakStatement);
                        {
                            N(SyntaxKind.BreakKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void DeclarationPattern_ArrayOfNullableType()
        {
            UsingStatement("switch (e) { case T?[] t: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "T");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void DeclarationPattern_NullableArrayOfArray()
        {
            UsingStatement("switch (e) { case T[]?[] t: break; }");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.ArrayType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "T");
                                        }
                                        N(SyntaxKind.ArrayRankSpecifier);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.OmittedArraySizeExpression);
                                            {
                                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.QuestionToken);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void NullableArray_TypeArgument()
        {
            UsingExpression("F<A[]?, object[]?>()");
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.ObjectKeyword);
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void NullableArray_TupleType()
        {
            UsingStatement("(object[]?, A[]?) t;");
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.OmittedArraySizeExpression);
                                        {
                                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.OmittedArraySizeExpression);
                                        {
                                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "t");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void NullableArray_Using()
        {
            UsingStatement("using (A[]? a = b) { }");
            N(SyntaxKind.UsingStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void NullableArray_Query()
        {
            UsingExpression("from A[]? a in b select a");
            N(SyntaxKind.QueryExpression);
            {
                N(SyntaxKind.FromClause);
                {
                    N(SyntaxKind.FromKeyword);
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.InKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.QueryBody);
                {
                    N(SyntaxKind.SelectClause);
                    {
                        N(SyntaxKind.SelectKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableArray_ExplicitlyTypedLambda()
        {
            UsingExpression("F((object[]? a) => a)");
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.ParenthesizedLambdaExpression);
                        {
                            N(SyntaxKind.ParameterList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.ObjectKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.OmittedArraySizeExpression);
                                                {
                                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                        N(SyntaxKind.QuestionToken);
                                    }
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void NullableArray_PartialMember()
        {
            UsingTree(
@"class C
{
    partial A[]? F();
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.ArrayRankSpecifier);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.IdentifierToken, "F");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CreateNullableArray_01()
        {
            UsingExpression("new object[,][]?",
                // (1,17): error CS1733: Expected expression
                // new object[,][]?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, ':' expected
                // new object[,][]?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":", "").WithLocation(1, 17),
                // (1,17): error CS1733: Expected expression
                // new object[,][]?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 17)
                );
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.ArrayCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.ColonToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CreateNullableArray_02()
        {
            UsingExpression("new object[,][]? { 1, 2 }");
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.CollectionInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CreateNullableArray_03()
        {
            UsingExpression("new object[1,2]?[3]?[4]");
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "2");
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "3");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "4");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void CreateNullableArray_04()
        {
            UsingExpression("new object[,]?[]?[]");
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.OmittedArraySizeExpression);
                                        {
                                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.OmittedArraySizeExpression);
                                        {
                                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.OmittedArraySizeExpression);
                        {
                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void CreateNullableArray_05()
        {
            UsingExpression("new object[1,2]?[3]?[]");
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "2");
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "3");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.OmittedArraySizeExpression);
                        {
                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void CreateNullableArray_06()
        {
            UsingExpression("new object[]");
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.ObjectKeyword);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.OmittedArraySizeExpression);
                        {
                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void CreateNullableArray_07()
        {
            UsingExpression("new object[1]?[2,3]?[]");
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.ArrayRankSpecifier);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "3");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.OmittedArraySizeExpression);
                        {
                            N(SyntaxKind.OmittedArraySizeExpressionToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }
    }
}
