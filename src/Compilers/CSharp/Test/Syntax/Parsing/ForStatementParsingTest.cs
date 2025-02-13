// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing;

public sealed class ForStatementParsingTest(ITestOutputHelper output) : ParsingTests(output)
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66522")]
    public void TestCommaSeparators1()
    {
        UsingStatement("for (int i = 0, j = 0; i < 10; i++) ;");

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "j");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "10");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66522")]
    public void TestCommaSeparators2()
    {
        UsingStatement("for (int i = 0, i < 10; i++) ;",
            // (1,15): error CS1002: ; expected
            // for (int i = 0, i < 10; i++) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 15));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            M(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "10");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66522")]
    public void TestCommaSeparators3()
    {
        UsingStatement("for (int i = 0, i < 10, i++) ;",
            // (1,15): error CS1002: ; expected
            // for (int i = 0, i < 10, i++) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 15),
            // (1,23): error CS1002: ; expected
            // for (int i = 0, i < 10, i++) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 23));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            M(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "10");
                }
            }
            M(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66522")]
    public void TestCommaSeparators4()
    {
        UsingStatement("for (int i = 0, i) ;",
            // (1,15): error CS1002: ; expected
            // for (int i = 0, i) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 15),
            // (1,18): error CS1002: ; expected
            // for (int i = 0, i) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(1, 18));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            M(SyntaxKind.SemicolonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "i");
            }
            M(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66522")]
    public void TestCommaSeparators5()
    {
        UsingStatement("for (int i = 0,,) ;",
            // (1,15): error CS1002: ; expected
            // for (int i = 0,,) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 15),
            // (1,16): error CS1002: ; expected
            // for (int i = 0,,) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 16));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            M(SyntaxKind.SemicolonToken);
            M(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66522")]
    public void TestCommaSeparators6()
    {
        UsingStatement("for (int i = 0, j; i < 10; i++) ;");

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "j");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "10");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestVariableDeclaratorVersusCondition1()
    {
        UsingStatement("for (int i = 0, i++; i < 10; i++) ;",
            // (1,15): error CS1002: ; expected
            // for (int i = 0, i++; i < 10; i++) ;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(1, 15),
            // (1,28): error CS1003: Syntax error, ',' expected
            // for (int i = 0, i++; i < 10; i++) ;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 28));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
            }
            M(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "10");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithInitializers1()
    {
        UsingStatement("""
            for (int offset = 0, c1, c2; offset < length;)
            {
            }
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c1");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c2");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "length");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithInitializers2()
    {
        UsingStatement("""
            for (int offset = 0, c1 = 1, c2; offset < length;)
            {
            }
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c1");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c2");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "length");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithInitializers3()
    {
        UsingStatement("""
            for (int offset = 0, c1, c2 = 1; offset < length;)
            {
            }
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c1");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c2");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "length");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithInitializers4()
    {
        UsingStatement("""
            for (int offset = 0, c1,; offset < length;)
            {
            }
            """,
            // (1,25): error CS1001: Identifier expected
            // for (int offset = 0, c1,; offset < length;)
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 25));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c1");
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.VariableDeclarator);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "length");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithInitializers5()
    {
        UsingStatement("""
            for (int offset = 0, c1, c2,; offset < length;)
            {
            }
            """,
            // (1,29): error CS1001: Identifier expected
            // for (int offset = 0, c1, c2,; offset < length;)
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 29));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c1");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c2");
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.VariableDeclarator);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "length");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithInitializers6()
    {
        UsingStatement("""
            for (int offset = 0, c1 = ,; offset < length;)
            {
            }
            """,
            // (1,27): error CS1525: Invalid expression term ','
            // for (int offset = 0, c1 = ,; offset < length;)
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 27),
            // (1,28): error CS1001: Identifier expected
            // for (int offset = 0, c1 = ,; offset < length;)
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 28));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c1");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.VariableDeclarator);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "length");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithInitializers7()
    {
        UsingStatement("""
            for (int offset = 0, c1 = , c2; offset < length;)
            {
            }
            """,
            // (1,27): error CS1525: Invalid expression term ','
            // for (int offset = 0, c1 = , c2; offset < length;)
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 27));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c1");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "c2");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "offset");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "length");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77160")]
    public void TestMultipleDeclaratorsWithExpression1()
    {
        UsingStatement("""
            for (Console.WriteLine("Blah"); true;)
            {
            }
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Console");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "WriteLine");
                    }
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.StringLiteralExpression);
                        {
                            N(SyntaxKind.StringLiteralToken, "\"Blah\"");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.TrueLiteralExpression);
            {
                N(SyntaxKind.TrueKeyword);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }
}
