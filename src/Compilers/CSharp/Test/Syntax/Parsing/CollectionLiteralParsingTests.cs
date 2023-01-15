// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionLiteralParsingTests : ParsingTests
    {
        public CollectionLiteralParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ExpressionDotAccess()
        {
            UsingTree("_ = [A, B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
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
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess()
        {
            UsingTree("[A, B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionNullSafeAccess()
        {
            UsingTree("_ = [A, B]?.C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
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
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelNullSafeAccess()
        {
            UsingTree("[A, B]?.C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalAccessExpression);
                        {
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.MemberBindingExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionPointerAccess()
        {
            UsingTree("_ = [A, B]->C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
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
                                N(SyntaxKind.PointerMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.MinusGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelPointerAccess()
        {
            UsingTree("[A, B]->C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.PointerMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.MinusGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AttributeOnTopLevelDotAccessStatement()
        {
            UsingTree("[A] [B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AttemptToImmediatelyIndexInTopLevelStatement()
        {
            UsingTree(
                """["A", "B"][0].C();""",
                // (1,2): error CS1001: Identifier expected
                // ["A", "B"][0].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"""A""").WithLocation(1, 2),
                // (1,2): error CS1001: Identifier expected
                // ["A", "B"][0].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"""A""").WithLocation(1, 2),
                // (1,7): error CS1001: Identifier expected
                // ["A", "B"][0].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"""B""").WithLocation(1, 7),
                // (1,7): error CS1001: Identifier expected
                // ["A", "B"][0].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"""B""").WithLocation(1, 7));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.Attribute);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            M(SyntaxKind.Attribute);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
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
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AlwaysParsedAsAttributeInsideNamespace()
        {
            UsingTree("""
                namespace A;
                [B].C();
                """,
                // (2,3): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // [B].C();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "]").WithLocation(2, 3),
                // (2,4): error CS1022: Type or namespace definition, or end-of-file expected
                // [B].C();
                Diagnostic(ErrorCode.ERR_EOFExpected, ".").WithLocation(2, 4));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FileScopedNamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.SemicolonToken);
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionIs()
        {
            UsingTree("_ = [A, B] is [A, B];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.ListPattern);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionWith()
        {
            UsingTree("_ = [A, B] with { };");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
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
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionSwitch()
        {
            UsingTree("_ = [A, B] switch { _ => M() };");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SwitchExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchExpressionArm);
                                {
                                    N(SyntaxKind.DiscardPattern);
                                    {
                                        N(SyntaxKind.UnderscoreToken);
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "M");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelSwitch()
        {
            UsingTree(
                "[A, B] switch { _ => M() };",
                // (1,15): error CS1525: Invalid expression term '{'
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 15),
                // (1,15): error CS8515: Parentheses are required around the switch governing expression.
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_SwitchGoverningExpressionRequiresParens, "{").WithLocation(1, 15),
                // (1,17): error CS1513: } expected
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "_").WithLocation(1, 17),
                // (1,26): error CS1002: ; expected
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 26),
                // (1,26): error CS1022: Type or namespace definition, or end-of-file expected
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 26));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.SwitchStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.SwitchKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleLambdaExpression);
                        {
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "M");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void EmptyCollection()
        {
            UsingTree("_ = [];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CollectionOfEmptyCollection()
        {
            UsingTree("_ = [[]];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryOfEmptyCollections()
        {
            UsingTree("_ = [[]: []];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.DictionaryElement);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionarySyntaxMissingKeyAndValue()
        {
            UsingTree(
                "_ = [:];",
                // (1,6): error CS1003: Syntax error, ',' expected
                // _ = [:];
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 6));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
