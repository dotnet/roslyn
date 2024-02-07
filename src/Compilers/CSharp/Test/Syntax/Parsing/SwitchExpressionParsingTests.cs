// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class SwitchExpressionParsingTests : ParsingTests
{
    public SwitchExpressionParsingTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestErrantCaseInSwitchExpression1()
    {
        UsingExpression("""
            x switch
            {
                case 0 => 1,
                case 1 => 2,
            }
            """,
            // (3,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 0 => 1,
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(3, 5),
            // (4,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 1 => 2,
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(4, 5));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantCaseInSwitchExpression1_Semicolons()
    {
        UsingExpression("""
            x switch
            {
                case 0 => 1;
                case 1 => 2;
            }
            """,
            // (3,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 0 => 1;
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(3, 5),
            // (3,16): error CS1003: Syntax error, ',' expected
            //     case 0 => 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 16),
            // (4,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 1 => 2;
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(4, 5),
            // (4,16): error CS1003: Syntax error, ',' expected
            //     case 1 => 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 16));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantCaseInSwitchExpression2()
    {
        UsingExpression("""
            x switch
            {
                case 0: 1,
                case 1: 2,
            }
            """,
            // (3,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 0: 1,
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(3, 5),
            // (3,11): error CS1003: Syntax error, '=>' expected
            //     case 0: 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 11),
            // (4,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 1: 2,
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(4, 5),
            // (4,11): error CS1003: Syntax error, '=>' expected
            //     case 1: 2,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 11));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantCaseInSwitchExpression2_Semicolons()
    {
        UsingExpression("""
            x switch
            {
                case 0: 1;
                case 1: 2;
            }
            """,
            // (3,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 0: 1;
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(3, 5),
            // (3,11): error CS1003: Syntax error, '=>' expected
            //     case 0: 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 11),
            // (3,14): error CS1003: Syntax error, ',' expected
            //     case 0: 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 14),
            // (4,5): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //     case 1: 2;
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(4, 5),
            // (4,11): error CS1003: Syntax error, '=>' expected
            //     case 1: 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 11),
            // (4,14): error CS1003: Syntax error, ',' expected
            //     case 1: 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 14));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantCaseInSwitchExpression3()
    {
        UsingStatement("""
            {
                var y = x switch
                {
                    case 0:
                        Goo();
                        return Bar;
                    case 1:
                    {
                        Baz();
                        throw new Quux();
                    }
                };
            }
            """,
            // (4,9): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //         case 0:
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(4, 9),
            // (4,15): error CS1003: Syntax error, '=>' expected
            //         case 0:
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 15),
            // (5,18): error CS1003: Syntax error, ',' expected
            //             Goo();
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(5, 18),
            // (5,19): error CS1513: } expected
            //             Goo();
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 19),
            // (5,19): error CS1002: ; expected
            //             Goo();
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 19),
            // (6,24): error CS1003: Syntax error, 'switch' expected
            //             return Bar;
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("switch").WithLocation(6, 24));
        N(SyntaxKind.Block);
        {
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SwitchExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchExpressionArm);
                                {
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "0");
                                        }
                                    }
                                    M(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Goo");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ReturnStatement);
            {
                N(SyntaxKind.ReturnKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Bar");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.SwitchStatement);
            {
                M(SyntaxKind.SwitchKeyword);
                M(SyntaxKind.OpenParenToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.CloseParenToken);
                M(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CaseSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Baz");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.ThrowStatement);
                        {
                            N(SyntaxKind.ThrowKeyword);
                            N(SyntaxKind.ObjectCreationExpression);
                            {
                                N(SyntaxKind.NewKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Quux");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EmptyStatement);
            {
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    [InlineData("case")]
    [InlineData("// leading\r\ncase")]
    [InlineData("/*leading*/ case")]
    [InlineData("case // trailing")]
    [InlineData("case /* trailing */")]
    [InlineData("case /* trailing\r\n*/")]
    [InlineData("/*leading*/ case // trailing")]
    [InlineData("/*leading*/ case /* trailing */")]
    public void TestErrantCaseInSwitchExpression4(string caseString)
    {
        UsingTree($$"""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        {{caseString}}
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case"),
            // (6,17): error CS8504: Pattern missing
            //             case
            Diagnostic(ErrorCode.ERR_MissingPattern, ""),
            // (6,17): error CS1003: Syntax error, '=>' expected
            //             case
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>"),
            // (6,17): error CS1525: Invalid expression term '}'
            //             case
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}"));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.SwitchExpressionArm);
                            {
                                M(SyntaxKind.ConstantPattern);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression5()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case,
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case,
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,17): error CS8504: Pattern missing
            //             case,
            Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(6, 17),
            // (6,17): error CS1003: Syntax error, '=>' expected
            //             case,
            Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments("=>").WithLocation(6, 17),
            // (6,17): error CS1525: Invalid expression term ','
            //             case,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.SwitchExpressionArm);
                            {
                                M(SyntaxKind.ConstantPattern);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression6()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case;
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case;
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,17): error CS8504: Pattern missing
            //             case;
            Diagnostic(ErrorCode.ERR_MissingPattern, ";").WithLocation(6, 17),
            // (6,17): error CS1003: Syntax error, '=>' expected
            //             case;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("=>").WithLocation(6, 17),
            // (6,17): error CS1525: Invalid expression term ';'
            //             case;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 17),
            // (6,17): error CS1003: Syntax error, ',' expected
            //             case;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(6, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.SwitchExpressionArm);
                            {
                                M(SyntaxKind.ConstantPattern);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression7()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case =>
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case =>
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,18): error CS8504: Pattern missing
            //             case =>
            Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(6, 18),
            // (6,20): error CS1525: Invalid expression term '}'
            //             case =>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 20));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                M(SyntaxKind.ConstantPattern);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression8()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case when true
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case when true
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,23): error CS1003: Syntax error, '=>' expected
            //             case when true
            Diagnostic(ErrorCode.ERR_SyntaxError, "true").WithArguments("=>").WithLocation(6, 23));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression9()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case when true =>
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case when true =>
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,23): error CS1003: Syntax error, '=>' expected
            //             case when true =>
            Diagnostic(ErrorCode.ERR_SyntaxError, "true").WithArguments("=>").WithLocation(6, 23),
            // (6,28): error CS1003: Syntax error, ',' expected
            //             case when true =>
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(6, 28),
            // (6,28): error CS8504: Pattern missing
            //             case when true =>
            Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(6, 28),
            // (6,30): error CS1525: Invalid expression term '}'
            //             case when true =>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 30));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                M(SyntaxKind.ConstantPattern);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression10()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case true =>
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case true =>
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,25): error CS1525: Invalid expression term '}'
            //             case true =>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 25));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.TrueLiteralExpression);
                                    {
                                        N(SyntaxKind.TrueKeyword);
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression11()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case when
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case when
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,22): error CS1003: Syntax error, '=>' expected
            //             case when
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>").WithLocation(6, 22),
            // (6,22): error CS1525: Invalid expression term '}'
            //             case when
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 22));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression12()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        case when =>
                    };
            }
            """,
            // (6,13): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             case when =>
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 13),
            // (6,25): error CS1525: Invalid expression term '}'
            //             case when =>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 25));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression13()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when case
                    };
            }
            """,
            // (6,18): error CS1003: Syntax error, '=>' expected
            //             when case
            Diagnostic(ErrorCode.ERR_SyntaxError, "case").WithArguments("=>").WithLocation(6, 18),
            // (6,18): error CS1525: Invalid expression term 'case'
            //             when case
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "case").WithArguments("case").WithLocation(6, 18),
            // (6,18): error CS1003: Syntax error, ',' expected
            //             when case
            Diagnostic(ErrorCode.ERR_SyntaxError, "case").WithArguments(",").WithLocation(6, 18),
            // (6,18): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             when case
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 18),
            // (6,22): error CS8504: Pattern missing
            //             when case
            Diagnostic(ErrorCode.ERR_MissingPattern, "").WithLocation(6, 22),
            // (6,22): error CS1003: Syntax error, '=>' expected
            //             when case
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>").WithLocation(6, 22),
            // (6,22): error CS1525: Invalid expression term '}'
            //             when case
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 22));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            M(SyntaxKind.SwitchExpressionArm);
                            {
                                M(SyntaxKind.ConstantPattern);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantCaseInSwitchExpression14()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when case 0
                    };
            }
            """,
            // (6,18): error CS1003: Syntax error, '=>' expected
            //             when case 0
            Diagnostic(ErrorCode.ERR_SyntaxError, "case").WithArguments("=>").WithLocation(6, 18),
            // (6,18): error CS1525: Invalid expression term 'case'
            //             when case 0
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "case").WithArguments("case").WithLocation(6, 18),
            // (6,18): error CS1003: Syntax error, ',' expected
            //             when case 0
            Diagnostic(ErrorCode.ERR_SyntaxError, "case").WithArguments(",").WithLocation(6, 18),
            // (6,18): error CS9134: A switch expression arm does not begin with a 'case' keyword.
            //             when case 0
            Diagnostic(ErrorCode.ERR_BadCaseInSwitchArm, "case").WithLocation(6, 18),
            // (6,24): error CS1003: Syntax error, '=>' expected
            //             when case 0
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>").WithLocation(6, 24),
            // (6,24): error CS1525: Invalid expression term '}'
            //             when case 0
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 24));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression1()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when
                    };
            }
            """,
            // (6,17): error CS1003: Syntax error, '=>' expected
            //             when
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>").WithLocation(6, 17),
            // (6,17): error CS1525: Invalid expression term '}'
            //             when
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression2()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when,
                    };
            }
            """,
            // (6,17): error CS1003: Syntax error, '=>' expected
            //             when,
            Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments("=>").WithLocation(6, 17),
            // (6,17): error CS1525: Invalid expression term ','
            //             when,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression3()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when;
                    };
            }
            """,
            // (6,17): error CS1003: Syntax error, '=>' expected
            //             when;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("=>").WithLocation(6, 17),
            // (6,17): error CS1525: Invalid expression term ';'
            //             when;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 17),
            // (6,17): error CS1003: Syntax error, ',' expected
            //             when;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(6, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression4()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when =>
                    };
            }
            """,
            // (6,20): error CS1525: Invalid expression term '}'
            //             when =>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 20));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression5()
    {
        // No parse error here.  `when` is a legal `constant pattern`.
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when => true
                    };
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
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression6()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when true
                    };
            }
            """,
            // (6,18): error CS1003: Syntax error, '=>' expected
            //             when true
            Diagnostic(ErrorCode.ERR_SyntaxError, "true").WithArguments("=>").WithLocation(6, 18));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression7()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when true,
                    };
            }
            """,
            // (6,18): error CS1003: Syntax error, '=>' expected
            //             when true,
            Diagnostic(ErrorCode.ERR_SyntaxError, "true").WithArguments("=>").WithLocation(6, 18));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression8()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when true;
                    };
            }
            """,
            // (6,18): error CS1003: Syntax error, '=>' expected
            //             when true;
            Diagnostic(ErrorCode.ERR_SyntaxError, "true").WithArguments("=>").WithLocation(6, 18),
            // (6,22): error CS1003: Syntax error, ',' expected
            //             when true;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(6, 22));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71957")]
    public void TestErrantWhenInSwitchExpression9()
    {
        UsingTree("""
            class C
            {
                public static int X()
                    => 5 switch
                    {
                        when true =>
                    };
            }
            """,
            // (6,18): error CS1003: Syntax error, '=>' expected
            //             when true =>
            Diagnostic(ErrorCode.ERR_SyntaxError, "true").WithArguments("=>").WithLocation(6, 18),
            // (6,23): error CS1003: Syntax error, ',' expected
            //             when true =>
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(6, 23),
            // (6,23): error CS8504: Pattern missing
            //             when true =>
            Diagnostic(ErrorCode.ERR_MissingPattern, "=>").WithLocation(6, 23),
            // (6,25): error CS1525: Invalid expression term '}'
            //             when true =>
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(6, 25));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "X");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "when");
                                    }
                                }
                                M(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                M(SyntaxKind.ConstantPattern);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
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
    public void TestErrantColonsInSwitchExpression1()
    {
        UsingExpression("""
            x switch
            {
                0: 1,
                1: 2,
            }
            """,
            // (3,6): error CS1003: Syntax error, '=>' expected
            //     0: 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 6),
            // (4,6): error CS1003: Syntax error, '=>' expected
            //     1: 2,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 6));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantColonsInSwitchExpression1_Semicolons()
    {
        UsingExpression("""
            x switch
            {
                0: 1;
                1: 2;
            }
            """,
            // (3,6): error CS1003: Syntax error, '=>' expected
            //     0: 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 6),
            // (3,9): error CS1003: Syntax error, ',' expected
            //     0: 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 9),
            // (4,6): error CS1003: Syntax error, '=>' expected
            //     1: 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 6),
            // (4,9): error CS1003: Syntax error, ',' expected
            //     1: 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 9));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantDefaultInSwitchExpression1()
    {
        UsingExpression("""
            x switch
            {
                0 => 1,
                default: 2,
            }
            """,
            // (4,12): error CS1003: Syntax error, '=>' expected
            //     default: 2,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 12));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.DefaultLiteralExpression);
                    {
                        N(SyntaxKind.DefaultKeyword);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantDefaultInSwitchExpression1_Semicolons()
    {
        UsingExpression("""
            x switch
            {
                0 => 1;
                default: 2;
            }
            """,
            // (3,11): error CS1003: Syntax error, ',' expected
            //     0 => 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 11),
            // (4,12): error CS1003: Syntax error, '=>' expected
            //     default: 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 12),
            // (4,15): error CS1003: Syntax error, ',' expected
            //     default: 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 15));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.DefaultLiteralExpression);
                    {
                        N(SyntaxKind.DefaultKeyword);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantDefaultInSwitchExpression2()
    {
        UsingExpression("""
            x switch
            {
                0 => 1,
                default(int): 2,
            }
            """,
            // (4,17): error CS1003: Syntax error, '=>' expected
            //     default(int): 2,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 17));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
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
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestErrantDefaultInSwitchExpression2_Semicolons()
    {
        UsingExpression("""
            x switch
            {
                0 => 1;
                default(int): 2;
            }
            """,
            // (3,11): error CS1003: Syntax error, ',' expected
            //     0 => 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 11),
            // (4,17): error CS1003: Syntax error, '=>' expected
            //     default(int): 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 17),
            // (4,20): error CS1003: Syntax error, ',' expected
            //     default(int): 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 20));
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
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
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestNormalDefaultInSwitchExpression1()
    {
        // 'default' legal syntactically.  Only a binding error.
        var code = """
            var v = 0 switch
            {
                0 => 1,
                default => 2,
            };
            """;
        CreateCompilation(code).VerifyDiagnostics(
            // (4,5): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
            //     default => 2,
            Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(4, 5));

        UsingStatement(code);
        N(SyntaxKind.LocalDeclarationStatement);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "var");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "v");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.DefaultLiteralExpression);
                                    {
                                        N(SyntaxKind.DefaultKeyword);
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void TestNormalDefaultInSwitchExpression1_Semicolons()
    {
        // ;default' legal syntactically.  Only a binding error.
        var code = """
            var v = 0 switch
            {
                0 => 1;
                default => 2;
            };
            """;
        CreateCompilation(code).VerifyDiagnostics(
            // (3,11): error CS1003: Syntax error, ',' expected
            //     0 => 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 11),
            // (4,5): error CS8505: A default literal 'default' is not valid as a pattern. Use another literal (e.g. '0' or 'null') as appropriate. To match everything, use a discard pattern '_'.
            //     default => 2;
            Diagnostic(ErrorCode.ERR_DefaultPattern, "default").WithLocation(4, 5),
            // (4,17): error CS1003: Syntax error, ',' expected
            //     default => 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 17));

        UsingStatement(code,
            // (3,11): error CS1003: Syntax error, ',' expected
            //     0 => 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 11),
            // (4,17): error CS1003: Syntax error, ',' expected
            //     default => 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 17));
        N(SyntaxKind.LocalDeclarationStatement);
        {
            N(SyntaxKind.VariableDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "var");
                }
                N(SyntaxKind.VariableDeclarator);
                {
                    N(SyntaxKind.IdentifierToken, "v");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.SwitchExpression);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
                            }
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.SwitchExpressionArm);
                            {
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.DefaultLiteralExpression);
                                    {
                                        N(SyntaxKind.DefaultKeyword);
                                    }
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact]
    public void TestNormalDefaultInSwitchExpression2()
    {
        UsingExpression("""
            x switch
            {
                0 => 1,
                default(int) => 2,
            }
            """);
        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
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
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestNormalDefaultInSwitchExpression2_Semicolons()
    {
        UsingExpression("""
            x switch
            {
                0 => 1;
                default(int) => 2;
            }
            """,
            // (3,11): error CS1003: Syntax error, ',' expected
            //     0 => 1;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 11),
            // (4,22): error CS1003: Syntax error, ',' expected
            //     default(int) => 2;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 22));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ConstantPattern);
                {
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
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedRecursivePattern1()
    {
        UsingExpression("""
            obj switch
            {
                Type { Prop: Type { } => 1,
                Type { Prop: Type { } => 2
            }
            """,
            // (3,27): error CS1513: } expected
            //     Type { Prop: Type { } => 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(3, 27),
            // (4,27): error CS1513: } expected
            //     Type { Prop: Type { } => 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(4, 27));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedRecursivePattern1_Colon()
    {
        UsingExpression("""
            obj switch
            {
                Type { Prop: Type { } : 1,
                Type { Prop: Type { } : 2
            }
            """,
            // (3,27): error CS1513: } expected
            //     Type { Prop: Type { } : 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 27),
            // (3,27): error CS1003: Syntax error, '=>' expected
            //     Type { Prop: Type { } : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 27),
            // (4,27): error CS1513: } expected
            //     Type { Prop: Type { } : 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 27),
            // (4,27): error CS1003: Syntax error, '=>' expected
            //     Type { Prop: Type { } : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 27));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedRecursivePattern2()
    {
        UsingExpression("""
            obj switch
            {
                Type { Prop: Type { => 1,
                Type { Prop: Type { => 2
            }
            """,
            // (3,25): error CS1513: } expected
            //     Type { Prop: Type { => 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(3, 25),
            // (3,25): error CS1513: } expected
            //     Type { Prop: Type { => 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(3, 25),
            // (4,25): error CS1513: } expected
            //     Type { Prop: Type { => 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(4, 25),
            // (4,25): error CS1513: } expected
            //     Type { Prop: Type { => 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(4, 25));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedRecursivePattern2_Colon()
    {
        UsingExpression("""
            obj switch
            {
                Type { Prop: Type { : 1,
                Type { Prop: Type { : 2
            }
            """,
            // (3,25): error CS1513: } expected
            //     Type { Prop: Type { : 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 25),
            // (3,25): error CS1513: } expected
            //     Type { Prop: Type { : 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 25),
            // (3,25): error CS1003: Syntax error, '=>' expected
            //     Type { Prop: Type { : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 25),
            // (4,25): error CS1513: } expected
            //     Type { Prop: Type { : 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 25),
            // (4,25): error CS1513: } expected
            //     Type { Prop: Type { : 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 25),
            // (4,25): error CS1003: Syntax error, '=>' expected
            //     Type { Prop: Type { : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 25));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedRecursivePattern3()
    {
        UsingExpression("""
            obj switch
            {
                Type { Prop: { Prop: { => 1,
                Type { Prop: { Prop: { => 2
            }
            """,
            // (3,28): error CS1513: } expected
            //     Type { Prop: { Prop: { => 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(3, 28),
            // (3,28): error CS1513: } expected
            //     Type { Prop: { Prop: { => 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(3, 28),
            // (3,28): error CS1513: } expected
            //     Type { Prop: { Prop: { => 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(3, 28),
            // (4,28): error CS1513: } expected
            //     Type { Prop: { Prop: { => 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(4, 28),
            // (4,28): error CS1513: } expected
            //     Type { Prop: { Prop: { => 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(4, 28),
            // (4,28): error CS1513: } expected
            //     Type { Prop: { Prop: { => 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, "=>").WithLocation(4, 28));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Prop");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.RecursivePattern);
                                        {
                                            N(SyntaxKind.PropertyPatternClause);
                                            {
                                                N(SyntaxKind.OpenBraceToken);
                                                M(SyntaxKind.CloseBraceToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Prop");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.RecursivePattern);
                                        {
                                            N(SyntaxKind.PropertyPatternClause);
                                            {
                                                N(SyntaxKind.OpenBraceToken);
                                                M(SyntaxKind.CloseBraceToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedRecursivePattern3_Colon()
    {
        UsingExpression("""
            obj switch
            {
                Type { Prop: { Prop: { : 1,
                Type { Prop: { Prop: { : 2
            }
            """,
            // (3,28): error CS1513: } expected
            //     Type { Prop: { Prop: { : 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 28),
            // (3,28): error CS1513: } expected
            //     Type { Prop: { Prop: { : 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 28),
            // (3,28): error CS1513: } expected
            //     Type { Prop: { Prop: { : 1,
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 28),
            // (3,28): error CS1003: Syntax error, '=>' expected
            //     Type { Prop: { Prop: { : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 28),
            // (4,28): error CS1513: } expected
            //     Type { Prop: { Prop: { : 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 28),
            // (4,28): error CS1513: } expected
            //     Type { Prop: { Prop: { : 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 28),
            // (4,28): error CS1513: } expected
            //     Type { Prop: { Prop: { : 2
            Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 28),
            // (4,28): error CS1003: Syntax error, '=>' expected
            //     Type { Prop: { Prop: { : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 28));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Prop");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.RecursivePattern);
                                        {
                                            N(SyntaxKind.PropertyPatternClause);
                                            {
                                                N(SyntaxKind.OpenBraceToken);
                                                M(SyntaxKind.CloseBraceToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Type");
                    }
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Prop");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.RecursivePattern);
                                        {
                                            N(SyntaxKind.PropertyPatternClause);
                                            {
                                                N(SyntaxKind.OpenBraceToken);
                                                M(SyntaxKind.CloseBraceToken);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedListPattern1()
    {
        UsingExpression("""
            obj switch
            {
                [ => 1,
                [ => 2
            }
            """,
            // (3,7): error CS1003: Syntax error, ']' expected
            //     [ => 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(3, 7),
            // (4,7): error CS1003: Syntax error, ']' expected
            //     [ => 2
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(4, 7));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    M(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    M(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedListPattern1_Colon()
    {
        UsingExpression("""
            obj switch
            {
                [ : 1,
                [ : 2
            }
            """,
            // (3,7): error CS1003: Syntax error, ']' expected
            //     [ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 7),
            // (3,7): error CS1003: Syntax error, '=>' expected
            //     [ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 7),
            // (4,7): error CS1003: Syntax error, ']' expected
            //     [ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 7),
            // (4,7): error CS1003: Syntax error, '=>' expected
            //     [ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 7));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    M(SyntaxKind.CloseBracketToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    M(SyntaxKind.CloseBracketToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedListPattern2()
    {
        UsingExpression("""
            obj switch
            {
                [[ => 1,
                [[ => 2
            }
            """,
            // (3,8): error CS1003: Syntax error, ']' expected
            //     [[ => 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(3, 8),
            // (3,8): error CS1003: Syntax error, ']' expected
            //     [[ => 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(3, 8),
            // (4,8): error CS1003: Syntax error, ']' expected
            //     [[ => 2
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(4, 8),
            // (4,8): error CS1003: Syntax error, ']' expected
            //     [[ => 2
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(4, 8));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedListPattern2_Colon()
    {
        UsingExpression("""
            obj switch
            {
                [[ : 1,
                [[ : 2
            }
            """,
            // (3,8): error CS1003: Syntax error, ']' expected
            //     [[ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 8),
            // (3,8): error CS1003: Syntax error, ']' expected
            //     [[ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 8),
            // (3,8): error CS1003: Syntax error, '=>' expected
            //     [[ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 8),
            // (4,8): error CS1003: Syntax error, ']' expected
            //     [[ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 8),
            // (4,8): error CS1003: Syntax error, ']' expected
            //     [[ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 8),
            // (4,8): error CS1003: Syntax error, '=>' expected
            //     [[ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 8));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedListPattern3()
    {
        UsingExpression("""
            obj switch
            {
                [[[ => 1,
                [[[ => 2
            }
            """,
            // (3,9): error CS1003: Syntax error, ']' expected
            //     [[[ => 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(3, 9),
            // (3,9): error CS1003: Syntax error, ']' expected
            //     [[[ => 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(3, 9),
            // (3,9): error CS1003: Syntax error, ']' expected
            //     [[[ => 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(3, 9),
            // (4,9): error CS1003: Syntax error, ']' expected
            //     [[[ => 2
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(4, 9),
            // (4,9): error CS1003: Syntax error, ']' expected
            //     [[[ => 2
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(4, 9),
            // (4,9): error CS1003: Syntax error, ']' expected
            //     [[[ => 2
            Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments("]").WithLocation(4, 9));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.CloseBracketToken);
                        }
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.CloseBracketToken);
                        }
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67876")]
    public void TestUnclosedListPattern3_Colon()
    {
        UsingExpression("""
            obj switch
            {
                [[[ : 1,
                [[[ : 2
            }
            """,
            // (3,9): error CS1003: Syntax error, ']' expected
            //     [[[ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 9),
            // (3,9): error CS1003: Syntax error, ']' expected
            //     [[[ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 9),
            // (3,9): error CS1003: Syntax error, ']' expected
            //     [[[ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 9),
            // (3,9): error CS1003: Syntax error, '=>' expected
            //     [[ : 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 9),
            // (4,9): error CS1003: Syntax error, ']' expected
            //     [[[ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 9),
            // (4,9): error CS1003: Syntax error, ']' expected
            //     [[[ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 9),
            // (4,9): error CS1003: Syntax error, ']' expected
            //     [[[ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 9),
            // (4,9): error CS1003: Syntax error, '=>' expected
            //     [[ : 2
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(4, 9));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.CloseBracketToken);
                        }
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.CloseBracketToken);
                        }
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBracketToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            N(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }

    [Fact]
    public void TestIncompleteSwitchExpression()
    {
        UsingExpression("""
            obj switch
            {
                { Prop: 1, { Prop: 2 }
            """,
            // (3,27): error CS1513: } expected
            //     { Prop: 1, { Prop: 2 }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 27),
            // (3,27): error CS1003: Syntax error, '=>' expected
            //     { Prop: 1, { Prop: 2 }
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>").WithLocation(3, 27),
            // (3,27): error CS1733: Expected expression
            //     { Prop: 1, { Prop: 2 }
            Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(3, 27),
            // (3,27): error CS1003: Syntax error, ',' expected
            //     { Prop: 1, { Prop: 2 }
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(3, 27),
            // (3,27): error CS1513: } expected
            //     { Prop: 1, { Prop: 2 }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 27));

        N(SyntaxKind.SwitchExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "obj");
            }
            N(SyntaxKind.SwitchKeyword);
            N(SyntaxKind.OpenBraceToken);
            N(SyntaxKind.SwitchExpressionArm);
            {
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.Subpattern);
                                    {
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Prop");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.ConstantPattern);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "2");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            M(SyntaxKind.CommaToken);
            M(SyntaxKind.CloseBraceToken);
        }
        EOF();
    }
}
