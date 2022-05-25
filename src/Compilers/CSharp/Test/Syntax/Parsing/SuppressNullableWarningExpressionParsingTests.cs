// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public class SuppressNullableWarningExpressionParsingTests : ParsingTests
    {
        public SuppressNullableWarningExpressionParsingTests(ITestOutputHelper output) :
            base(output)
        {
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        [Fact]
        public void Null()
        {
            var source =
@"class C
{
    object F = null!;
}";
            var tree = UsingTree(source);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            N(SyntaxKind.ObjectKeyword);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.SuppressNullableWarningExpression);
                                    {
                                        N(SyntaxKind.NullLiteralExpression);
                                        {
                                            N(SyntaxKind.NullKeyword);
                                        }
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                }
                            }
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
        public void Expression()
        {
            UsingNode(
                "o = o!",
                TestOptions.Regular8);
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.SuppressNullableWarningExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ExclamationToken);
                }
            }

            UsingNode(
                "o = o!!",
                TestOptions.Regular8);
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.SuppressNullableWarningExpression);
                {
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                    N(SyntaxKind.ExclamationToken);
                }
            }

            UsingNode(
                "o = !o!",
                TestOptions.Regular8);
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.LogicalNotExpression);
                {
                    N(SyntaxKind.ExclamationToken);
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                }
            }
        }

        [Fact]
        public void NotEquals()
        {
            UsingNode(
                "o = o!!=null",
                TestOptions.Regular8);
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.NotEqualsExpression);
                {
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                    N(SyntaxKind.ExclamationEqualsToken);
                    N(SyntaxKind.NullLiteralExpression);
                    {
                        N(SyntaxKind.NullKeyword);
                    }
                }
            }
        }

        // `x!==y` is treated as `x != =y` rather than `x! == y`.
        // The scanner could handle this case by looking ahead
        // but the scanner typically consumes tokens eagerly.
        // For comparison, `x as T???y` is treated as `x as T ?? ? y`
        // rather than `x as T? ?? y`.
        [Fact]
        public void TestEquals()
        {
            UsingNode(
                "o = o!==null",
                TestOptions.Regular8,
                // (1,8): error CS1525: Invalid expression term '='
                // o = o!==null
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(1, 8));
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "o");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.NotEqualsExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "o");
                        }
                        N(SyntaxKind.ExclamationEqualsToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.NullLiteralExpression);
                    {
                        N(SyntaxKind.NullKeyword);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalAccess_01()
        {
            UsingNode(
                "o!?.ToString()",
                TestOptions.Regular8);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.SuppressNullableWarningExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ExclamationToken);
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
            }
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_02()
        {
            UsingNode("x?.y!.z.ToString()");

            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.MemberBindingExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "z");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ToString");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_03()
        {
            UsingNode("x?.y!?.z.ToString()");

            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.MemberBindingExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "z");
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ToString");
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
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_04()
        {
            UsingNode("x?.y?!.z.ToString()", options: null,
                // (1,7): error CS1525: Invalid expression term '.'
                // x?.y?!.z.ToString()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".", isSuppressed: false).WithArguments(".").WithLocation(1, 7),
                // (1,20): error CS1003: Syntax error, ':' expected
                // x?.y?!.z.ToString()
                Diagnostic(ErrorCode.ERR_SyntaxError, "", isSuppressed: false).WithArguments(":").WithLocation(1, 20),
                // (1,20): error CS1733: Expected expression
                // x?.y?!.z.ToString()
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "", isSuppressed: false).WithLocation(1, 20));

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.LogicalNotExpression);
                {
                    N(SyntaxKind.ExclamationToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "z");
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ToString");
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
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_05()
        {
            UsingNode("x?.y?![0].ToString()", options: null,
                // (1,7): error CS1525: Invalid expression term '['
                // x?.y?![0].ToString()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "[", isSuppressed: false).WithArguments("[").WithLocation(1, 7),
                // (1,21): error CS1003: Syntax error, ':' expected
                // x?.y?![0].ToString()
                Diagnostic(ErrorCode.ERR_SyntaxError, "", isSuppressed: false).WithArguments(":").WithLocation(1, 21),
                // (1,21): error CS1733: Expected expression
                // x?.y?![0].ToString()
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "", isSuppressed: false).WithLocation(1, 21));

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.LogicalNotExpression);
                {
                    N(SyntaxKind.ExclamationToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ToString");
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
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_06()
        {
            UsingNode("x?.y?!().ToString()", options: null,
                // (1,8): error CS1525: Invalid expression term ')'
                // x?.y?!().ToString()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")", isSuppressed: false).WithArguments(")").WithLocation(1, 8),
                // (1,20): error CS1003: Syntax error, ':' expected
                // x?.y?!().ToString()
                Diagnostic(ErrorCode.ERR_SyntaxError, "", isSuppressed: false).WithArguments(":").WithLocation(1, 20),
                // (1,20): error CS1733: Expected expression
                // x?.y?!().ToString()
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "", isSuppressed: false).WithLocation(1, 20));

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.LogicalNotExpression);
                {
                    N(SyntaxKind.ExclamationToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.ParenthesizedExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ToString");
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
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_07()
        {
            UsingNode("x?.y!?!.ToString()", options: null,
                // (1,8): error CS1525: Invalid expression term '.'
                // x?.y!?!.ToString()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".", isSuppressed: false).WithArguments(".").WithLocation(1, 8),
                // (1,19): error CS1003: Syntax error, ':' expected
                // x?.y!?!.ToString()
                Diagnostic(ErrorCode.ERR_SyntaxError, "", isSuppressed: false).WithArguments(":").WithLocation(1, 19),
                // (1,19): error CS1733: Expected expression
                // x?.y!?!.ToString()
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "", isSuppressed: false).WithLocation(1, 19));

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.LogicalNotExpression);
                {
                    N(SyntaxKind.ExclamationToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "ToString");
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
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_ElementAccess()
        {
            UsingNode("x?.y![1].z.ToString()");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.SuppressNullableWarningExpression);
                                {
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
                                    }
                                    N(SyntaxKind.ExclamationToken);
                                }
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "z");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ToString");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_SuppressInvocation()
        {
            UsingNode("x?.y!(0)");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.MemberBindingExpression);
                        {
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_PostfixSuppression()
        {
            UsingNode("x?.y!");
            N(SyntaxKind.SuppressNullableWarningExpression);
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.MemberBindingExpression);
                    {
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.ExclamationToken);
            }
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_Suppression_LangVersion()
        {
            UsingNode("x?.y!.z", options: TestOptions.Regular7_3,
                // (1,3): error CS8370: Feature 'nullable reference types' is not available in C# 7.3. Please use language version 8.0 or greater.
                // x?.y!.z
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, ".y!", isSuppressed: false).WithArguments("nullable reference types", "8.0").WithLocation(1, 3));
            verify();

            UsingNode("x?.y!.z", options: TestOptions.Regular8);
            verify();

            void verify()
            {
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.SuppressNullableWarningExpression);
                        {
                            N(SyntaxKind.MemberBindingExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.ExclamationToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "z");
                        }
                    }
                }
                EOF();
            }
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_Repeated_01()
        {
            UsingNode("x?.y!!.z");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.SuppressNullableWarningExpression);
                        {
                            N(SyntaxKind.MemberBindingExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.ExclamationToken);
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "z");
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_Repeated_02()
        {
            UsingNode("x?.y!!!!.z");
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.SuppressNullableWarningExpression);
                    {
                        N(SyntaxKind.SuppressNullableWarningExpression);
                        {
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.SuppressNullableWarningExpression);
                                {
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
                                    }
                                    N(SyntaxKind.ExclamationToken);
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.ExclamationToken);
                        }
                        N(SyntaxKind.ExclamationToken);
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "z");
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem(47712, "https://github.com/dotnet/roslyn/pull/47712")]
        public void ConditionalAccess_Repeated_03()
        {
            UsingNode("x?.y.z!!");
            N(SyntaxKind.SuppressNullableWarningExpression);
            {
                N(SyntaxKind.SuppressNullableWarningExpression);
                {
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.MemberBindingExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "z");
                            }
                        }
                    }
                    N(SyntaxKind.ExclamationToken);
                }
                N(SyntaxKind.ExclamationToken);
            }
            EOF();
        }
    }
}
