// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // (2,2): error CS1041: Identifier expected; 'case' is a keyword
            // {
            Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "case").WithLocation(2, 2),
            // (3,17): error CS1041: Identifier expected; 'case' is a keyword
            //     case 0 => 1,
            Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "case").WithLocation(3, 17));
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
    public void TestErrantCaseInSwitchExpression2()
    {
        UsingExpression("""
            x switch
            {
                case 0: 1,
                case 1: 2,
            }
            """,
            // (2,2): error CS1041: Identifier expected; 'case' is a keyword
            // {
            Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "case").WithLocation(2, 2),
            // (3,11): error CS1003: Syntax error, '=>' expected
            //     case 0: 1,
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("=>").WithLocation(3, 11),
            // (3,15): error CS1041: Identifier expected; 'case' is a keyword
            //     case 0: 1,
            Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "case").WithLocation(3, 15),
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
            // (3,12): error CS1041: Identifier expected; 'default' is a keyword
            //     0 => 1,
            Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "default").WithLocation(3, 12),
            // (4,12): error CS1525: Invalid expression term ':'
            //     default: 2,
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(4, 12),
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
                M(SyntaxKind.ConstantPattern);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
    public void TestNormalDefaultInSwitchExpression1()
    {
        UsingExpression("""
            x switch
            {
                0 => 1,
                default => 2,
            }
            """);
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
        EOF();
    }
}
