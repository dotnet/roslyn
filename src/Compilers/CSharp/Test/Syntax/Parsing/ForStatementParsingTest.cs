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

    [Fact]
    public void TestVariousExpressions_AnonymousFunction()
    {
        UsingStatement("""
            for (delegate() {};delegate() {};delegate() {});
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.AnonymousMethodExpression);
            {
                N(SyntaxKind.DelegateKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.AnonymousMethodExpression);
            {
                N(SyntaxKind.DelegateKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.AnonymousMethodExpression);
            {
                N(SyntaxKind.DelegateKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
    public void TestVariousExpressions_AnonymousObjectCreation()
    {
        UsingStatement("""
            for (new();new();new());
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
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
    public void TestVariousExpressions_ArrayCreation()
    {
        UsingStatement("""
            for (new int[] { };new int[] { };new int[] { });
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
    public void TestVariousExpressions_Assignment1()
    {
        UsingStatement("""
            for (a=1;a=1;a=1);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
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
    public void TestVariousExpressions_Assignment2()
    {
        UsingStatement("""
            for (a+=1;a+=1;a+=1);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.AddAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusEqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.AddAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusEqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.AddAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusEqualsToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
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
    public void TestVariousExpressions_Cast()
    {
        UsingStatement("""
            for ((int)0;(int)0;(int)0);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
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
    public void TestVariousExpressions_Checked()
    {
        UsingStatement("""
            for (checked(0);checked(0);checked(0));
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
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
            N(SyntaxKind.SemicolonToken);
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
            N(SyntaxKind.SemicolonToken);
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestVariousExpressions_Collection()
    {
        UsingStatement("""
            for ([];[];[]);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
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
    public void TestVariousExpressions_ConditionalAccess()
    {
        UsingStatement("""
            for (a?.b;a?.b;a?.b);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.MemberBindingExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.MemberBindingExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.MemberBindingExpression);
                {
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
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
    public void TestVariousExpressions_DefaultExpression1()
    {
        UsingStatement("""
            for (default(int);default(int);default(int));
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
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
            N(SyntaxKind.SemicolonToken);
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
            N(SyntaxKind.SemicolonToken);
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestVariousExpressions_DefaultExpression2()
    {
        UsingStatement("""
            for (default;default;default);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.DefaultLiteralExpression);
            {
                N(SyntaxKind.DefaultKeyword);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.DefaultLiteralExpression);
            {
                N(SyntaxKind.DefaultKeyword);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.DefaultLiteralExpression);
            {
                N(SyntaxKind.DefaultKeyword);
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
    public void TestVariousExpressions_ElementAccess()
    {
        UsingStatement("""
            for (a[0];a[0];a[0]);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
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
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
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
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestVariousExpressions_ImplicitArrayCreation()
    {
        UsingStatement("""
            for (new[]{};new[]{};new[]{});
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ImplicitArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ImplicitArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ImplicitArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
    public void TestVariousExpressions_InterpolatedString()
    {
        UsingStatement("""
            for ($"";$"";$"");
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.InterpolatedStringEndToken);
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
    public void TestVariousExpressions_Invocation()
    {
        UsingStatement("""
            for (a();a();a());
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
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
    public void TestVariousExpressions_IsPattern()
    {
        UsingStatement("""
            for (a is B b;a is B b;a is B b);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.DeclarationPattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.SingleVariableDesignation);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.DeclarationPattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.SingleVariableDesignation);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.DeclarationPattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.SingleVariableDesignation);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
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
    public void TestVariousExpressions_Literal()
    {
        UsingStatement("""
            for (true;true;true);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TrueLiteralExpression);
            {
                N(SyntaxKind.TrueKeyword);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.TrueLiteralExpression);
            {
                N(SyntaxKind.TrueKeyword);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.TrueLiteralExpression);
            {
                N(SyntaxKind.TrueKeyword);
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
    public void TestVariousExpressions_MemberAccess()
    {
        UsingStatement("""
            for (a.b;a.b;a.b);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.SimpleMemberAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SimpleMemberAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SimpleMemberAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
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
    public void TestVariousExpressions_Parenthesized()
    {
        UsingStatement("""
            for ((a);(a);(a));
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void TestVariousExpressions_Postfix()
    {
        UsingStatement("""
            for (a++;a++;a++);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusPlusToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
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
    public void TestVariousExpressions_ObjectCreation1()
    {
        UsingStatement("""
            for (new A();new A();new A());
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
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
    public void TestVariousExpressions_ObjectCreation2()
    {
        UsingStatement("""
            for (new A() { };new A() { };new A() { });
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
    public void TestVariousExpressions_ObjectCreation3()
    {
        UsingStatement("""
            for (new A { };new A { };new A { });
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
    public void TestVariousExpressions_Prefix()
    {
        UsingStatement("""
            for (++a;++a;++a);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.PreIncrementExpression);
            {
                N(SyntaxKind.PlusPlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PreIncrementExpression);
            {
                N(SyntaxKind.PlusPlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.PreIncrementExpression);
            {
                N(SyntaxKind.PlusPlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
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
    public void TestVariousExpressions_Query()
    {
        UsingStatement("""
            for (from a in b select c;from a in b select c;from a in b select c);
            """,
            // (1,1): error CS1073: Unexpected token 'from'
            // for (from a in b select c;from a in b select c;from a in b select c);
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "for (from a in b select c;").WithArguments("from").WithLocation(1, 1),
            // (1,1): error CS1003: Syntax error, 'foreach' expected
            // for (from a in b select c;from a in b select c;from a in b select c);
            Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments("foreach").WithLocation(1, 1),
            // (1,18): error CS1026: ) expected
            // for (from a in b select c;from a in b select c;from a in b select c);
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "select").WithLocation(1, 18));

        N(SyntaxKind.ForEachStatement);
        {
            M(SyntaxKind.ForEachKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "from");
            }
            N(SyntaxKind.IdentifierToken, "a");
            N(SyntaxKind.InKeyword);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
            M(SyntaxKind.CloseParenToken);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "select");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestVariousExpressions_Range1()
    {
        UsingStatement("""
            for (..;..;..);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
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
    public void TestVariousExpressions_Range2()
    {
        UsingStatement("""
            for (a..;a..;a..);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
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
    public void TestVariousExpressions_Range3()
    {
        UsingStatement("""
            for (..a;..a;..a);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
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
    public void TestVariousExpressions_Range4()
    {
        UsingStatement("""
            for (a..a;a..a;a..a);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
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
    public void TestVariousExpressions_Ref1()
    {
        UsingStatement("""
            for (ref a; ref a; ref a);
            """,
            // (1,11): error CS1001: Identifier expected
            // for (ref a; ref a; ref a);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 11),
            // (1,13): error CS1525: Invalid expression term 'ref'
            // for (ref a; ref a; ref a);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(1, 13),
            // (1,20): error CS1525: Invalid expression term 'ref'
            // for (ref a; ref a; ref a);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(1, 20));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                M(SyntaxKind.VariableDeclarator);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RefExpression);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RefExpression);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
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
    public void TestVariousExpressions_Ref2()
    {
        UsingStatement("""
            for (ref int a; ref a; ref a);
            """,
            // (1,17): error CS1525: Invalid expression term 'ref'
            // for (ref int a; ref a; ref a);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(1, 17),
            // (1,24): error CS1525: Invalid expression term 'ref'
            // for (ref int a; ref a; ref a);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref a").WithArguments("ref").WithLocation(1, 24));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RefExpression);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.RefExpression);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
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
    public void TestVariousExpressions_Sizeof()
    {
        UsingStatement("""
            for (sizeof(a);sizeof(a);sizeof(a));
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.SizeOfExpression);
            {
                N(SyntaxKind.SizeOfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SizeOfExpression);
            {
                N(SyntaxKind.SizeOfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SizeOfExpression);
            {
                N(SyntaxKind.SizeOfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
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
    public void TestVariousExpressions_Switch()
    {
        UsingStatement("""
            for (a switch {};a switch {};a switch {});
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
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
    public void TestVariousExpressions_Throw()
    {
        UsingStatement("""
            for (throw a;throw a;throw a);
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ThrowExpression);
            {
                N(SyntaxKind.ThrowKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ThrowExpression);
            {
                N(SyntaxKind.ThrowKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.ThrowExpression);
            {
                N(SyntaxKind.ThrowKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
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
    public void TestVariousExpressions_Tuple()
    {
        UsingStatement("""
            for ((a, b);(a, b);(a, b));
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.CloseParenToken);
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
    public void TestVariousExpressions_Typeof()
    {
        UsingStatement("""
            for (typeof(int);typeof(int);typeof(int));
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
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
            N(SyntaxKind.SemicolonToken);
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
            N(SyntaxKind.SemicolonToken);
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestVariousExpressions_With1()
    {
        UsingStatement("""
            for (a with { }; a with { }; a with { })
            {
            }
            """,
            // (1,1): error CS1073: Unexpected token ';'
            // for (a with { }; a with { }; a with { })
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "for (a with { }").WithArguments(";").WithLocation(1, 1),
            // (1,13): error CS1002: ; expected
            // for (a with { }; a with { }; a with { })
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 13),
            // (1,13): error CS1525: Invalid expression term '{'
            // for (a with { }; a with { }; a with { })
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 13),
            // (1,13): error CS1002: ; expected
            // for (a with { }; a with { }; a with { })
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 13),
            // (1,13): error CS1026: ) expected
            // for (a with { }; a with { }; a with { })
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(1, 13));

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "with");
                }
            }
            M(SyntaxKind.SemicolonToken);
            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }
            M(SyntaxKind.SemicolonToken);
            M(SyntaxKind.CloseParenToken);
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestVariousExpressions_With2()
    {
        UsingStatement("""
            for (; a with { }; a with { })
            {
            }
            """);

        N(SyntaxKind.ForStatement);
        {
            N(SyntaxKind.ForKeyword);
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.SemicolonToken);
            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
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
}
