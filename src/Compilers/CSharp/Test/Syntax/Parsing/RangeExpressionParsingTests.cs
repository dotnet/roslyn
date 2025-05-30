// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class RangeExpressionParsingTests(ITestOutputHelper output)
    : ParsingTests(output)
{
    [Fact]
    public void CastingRangeExpressionWithoutStartOrEnd()
    {
        UsingExpression("(int)..");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastingRangeExpressionWithoutStart()
    {
        UsingExpression("(int)..0");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.PredefinedType);
            {
                N(SyntaxKind.IntKeyword);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalExpressionWithEmptyRangeForWhenTrue()
    {
        UsingExpression("a ? .. : b");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalExpressionWithEmptyRangeForWhenFalse()
    {
        UsingExpression("a ? b : ..");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalExpressionWithEmptyRangeForWhenTrueAndWhenFalse()
    {
        UsingExpression("a ? .. : ..");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalExpressionWithEmptyStartRangeForWhenTrue()
    {
        UsingExpression("a ? ..b : c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "c");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalExpressionWithEmptyStartRangeForWhenFalse()
    {
        UsingExpression("a ? b : ..c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalExpressionWithEmptyStartRangeForWhenTrueAndFalse()
    {
        UsingExpression("a ? ..b : ..c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void CastingRangeExpressionInPattern1()
    {
        UsingExpression("x is (int)..");

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.ConstantPattern);
            {
                N(SyntaxKind.CastExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                }
            }
        }
        EOF();
    }

    [Fact]
    public void CastingRangeExpressionInPattern2()
    {
        UsingExpression("x is (int).",
            // (1,1): error CS1073: Unexpected token '.'
            // x is (int).
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "x is (int)").WithArguments(".").WithLocation(1, 1));

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.ParenthesizedPattern);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TypePattern);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastingRangeExpressionInPattern3()
    {
        UsingExpression("x is (int).Length",
            // (1,1): error CS1073: Unexpected token '.'
            // x is (int).Length
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "x is (int)").WithArguments(".").WithLocation(1, 1));

        N(SyntaxKind.IsPatternExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "x");
            }
            N(SyntaxKind.IsKeyword);
            N(SyntaxKind.ParenthesizedPattern);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TypePattern);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void TestUserFileRange()
    {
        var text = File.ReadAllText(@"Q:\github\roslyn\src\Code.txt");
        var tree = CSharpSyntaxTree.ParseText(text);

    }
}
