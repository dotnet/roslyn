// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
                    N(SyntaxKind.IdentifierToken);
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.NotEqualsExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.ExclamationEqualsToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NullLiteralExpression);
                        {
                            N(SyntaxKind.NullKeyword);
                        }
                    }
                }
            }
        }

        [Fact]
        public void ConditionalAccess()
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
    }
}
