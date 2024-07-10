// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternParsingTests_ListPatterns : ParsingTests
    {
        private static CSharpParseOptions RegularWithoutListPatterns => TestOptions.Regular10;

        private new void UsingExpression(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingExpression(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), expectedErrors);
        }

        public PatternParsingTests_ListPatterns(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ListPattern_00()
        {
            UsingExpression(@"c is [[]]");
            verify();

            UsingExpression(@"c is [[]]", RegularWithoutListPatterns);
            verify();

            void verify()
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ListPattern_01()
        {
            UsingExpression(@"c is [[],] v");
            verify();

            UsingExpression(@"c is [[],] v", RegularWithoutListPatterns);
            verify();

            void verify()
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.CloseBracketToken);
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "v");
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ListPattern_02()
        {
            UsingExpression(@"c is [ 1, prop: 0 ]",
                // (1,15): error CS1003: Syntax error, ',' expected
                // c is [ 1, prop: 0 ]
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 15),
                // (1,17): error CS1003: Syntax error, ',' expected
                // c is [ 1, prop: 0 ]
                Diagnostic(ErrorCode.ERR_SyntaxError, "0").WithArguments(",").WithLocation(1, 17));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "prop");
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ListPattern_03()
        {
            UsingExpression(@"c is [ , ]",
                // (1,8): error CS8504: Pattern missing
                // c is [ , ]
                Diagnostic(ErrorCode.ERR_MissingPattern, ",").WithLocation(1, 8));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    M(SyntaxKind.ConstantPattern);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ListPattern_04()
        {
            UsingExpression(@"c is ()[]",
                // (1,1): error CS1073: Unexpected token '['
                // c is ()[]
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "c is ()").WithArguments("[").WithLocation(1, 1));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PositionalPatternClause);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ListPattern_05()
        {
            UsingExpression(@"c is {}[]",
                // (1,1): error CS1073: Unexpected token '['
                // c is {}[]
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "c is {}").WithArguments("[").WithLocation(1, 1));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PropertyPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ListPattern_06()
        {
            UsingExpression(@"c is [List<int>]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.TypePattern);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "List");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ListPattern_07()
        {
            UsingExpression(@"c is [string[]]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.TypePattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
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
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ListPattern_08()
        {
            UsingExpression(@"c is [var(x,y)]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.VarPattern);
                    {
                        N(SyntaxKind.VarKeyword);
                        N(SyntaxKind.ParenthesizedVariableDesignation);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ListPattern_09()
        {
            UsingExpression(@"c is [>0]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.RelationalPattern);
                    {
                        N(SyntaxKind.GreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void NoRegressionOnArrayTypePattern_01()
        {
            UsingExpression(@"c is string[]");

            N(SyntaxKind.IsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
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
        public void NoRegressionOnArrayTypePattern_02()
        {
            UsingExpression(@"c is a[0]");

            N(SyntaxKind.IsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_01()
        {
            UsingExpression(@"c is [..]");
            verify();

            UsingExpression(@"c is [..]", RegularWithoutListPatterns);
            verify();

            void verify()
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.SlicePattern);
                        {
                            N(SyntaxKind.DotDotToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void SlicePattern_02()
        {
            UsingExpression(@"c is [.. var x]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.VarPattern);
                        {
                            N(SyntaxKind.VarKeyword);
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_03()
        {
            UsingExpression(@"c is ..");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.SlicePattern);
                {
                    N(SyntaxKind.DotDotToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_04()
        {
            UsingExpression(@"c is ....",
                // (1,6): error CS8635: Unexpected character sequence '...'
                // c is ....
                Diagnostic(ErrorCode.ERR_TripleDotNotAllowed, "").WithLocation(1, 6)
            );

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.SlicePattern);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_05()
        {
            UsingExpression(@"c is [..[]]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_06()
        {
            UsingExpression(@"c is [.. not p]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.NotPattern);
                        {
                            N(SyntaxKind.NotKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_07()
        {
            UsingExpression(@"c is [.. p or q]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "q");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_08()
        {
            UsingExpression(@"c is [.. p or .. q]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.OrPattern);
                        {
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                            }
                            N(SyntaxKind.OrKeyword);
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                                N(SyntaxKind.ConstantPattern);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "q");
                                    }
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_09()
        {
            UsingExpression(@"c is .. var x");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.SlicePattern);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.VarPattern);
                    {
                        N(SyntaxKind.VarKeyword);
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_10()
        {
            UsingExpression(@"c is .. Type");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.SlicePattern);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_11()
        {
            UsingExpression(@"c is [var x ..]",
                    // (1,13): error CS1003: Syntax error, ',' expected
                    // c is [var x ..]
                    Diagnostic(ErrorCode.ERR_SyntaxError, "..").WithArguments(",").WithLocation(1, 13));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.VarPattern);
                    {
                        N(SyntaxKind.VarKeyword);
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_12()
        {
            UsingExpression(@"c is var x ..",
                    // (1,12): error CS1073: Unexpected token '..'
                    // c is var x ..
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "..").WithArguments("..").WithLocation(1, 12));

            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.VarPattern);
                    {
                        N(SyntaxKind.VarKeyword);
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
                N(SyntaxKind.DotDotToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_13()
        {
            UsingExpression(@"c is [[]..]",
                // (1,9): error CS1003: Syntax error, ',' expected
                // c is [[]..]
                Diagnostic(ErrorCode.ERR_SyntaxError, "..").WithArguments(",").WithLocation(1, 9));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_14()
        {
            UsingExpression(@"c is not p ..",
                // (1,13): error CS1001: Identifier expected
                // c is not p ..
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 13),
                // (1,14): error CS1001: Identifier expected
                // c is not p ..
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 14));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.NotPattern);
                {
                    N(SyntaxKind.NotKeyword);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "p");
                                }
                                N(SyntaxKind.DotToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_15()
        {
            UsingExpression(@"c is not ..");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.NotPattern);
                {
                    N(SyntaxKind.NotKeyword);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_16()
        {
            UsingExpression(@"c is [..] ..",
                // (1,11): error CS1073: Unexpected token '..'
                // c is [..] ..
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "..").WithArguments("..").WithLocation(1, 11));

            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.ListPattern);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.SlicePattern);
                        {
                            N(SyntaxKind.DotDotToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.DotDotToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_17()
        {
            UsingExpression(@"c is a .. or b ..",
                // (1,9): error CS1001: Identifier expected
                // c is a .. or b ..
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 9),
                // (1,16): error CS1073: Unexpected token '..'
                // c is a .. or b ..
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "..").WithArguments("..").WithLocation(1, 16));

            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.DotToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "or");
                            }
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                }
                N(SyntaxKind.DotDotToken);
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_18()
        {
            UsingExpression(@"c is (var x) .. > 0",
                // (1,14): error CS1073: Unexpected token '..'
                // c is (var x) .. > 0
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "..").WithArguments("..").WithLocation(1, 14));

            N(SyntaxKind.GreaterThanExpression);
            {
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.ParenthesizedPattern);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.VarPattern);
                            {
                                N(SyntaxKind.VarKeyword);
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.DotDotToken);
                }
                N(SyntaxKind.GreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_19()
        {
            UsingExpression(@"c is [..>5]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.RelationalPattern);
                        {
                            N(SyntaxKind.GreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "5");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_20()
        {
            UsingExpression(@"c is [.. string?]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.TypePattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_21()
        {
            UsingExpression(@"c is [.. string? slice]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_22()
        {
            UsingExpression(@"c is [.. string? slice, ')']");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.CharacterLiteralExpression);
                        {
                            N(SyntaxKind.CharacterLiteralToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_23()
        {
            UsingExpression(@"c is [.. string? slice ')']",
                // (1,24): error CS1003: Syntax error, ',' expected
                // c is [.. string? slice ')']
                Diagnostic(ErrorCode.ERR_SyntaxError, "')'").WithArguments(",").WithLocation(1, 24));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.CharacterLiteralExpression);
                        {
                            N(SyntaxKind.CharacterLiteralToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_24()
        {
            UsingExpression(@"c is [.. string[]? slice "")""]",
                // (1,26): error CS1003: Syntax error, ',' expected
                // c is [.. string[]? slice ")"]
                Diagnostic(ErrorCode.ERR_SyntaxError, @""")""").WithArguments(",").WithLocation(1, 26));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.StringKeyword);
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
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.StringLiteralExpression);
                        {
                            N(SyntaxKind.StringLiteralToken, "\")\"");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_25()
        {
            UsingExpression(@"c is [.. int[]? slice 5]",
                // (1,23): error CS1003: Syntax error, ',' expected
                // c is [.. int[]? slice 5]
                Diagnostic(ErrorCode.ERR_SyntaxError, "5").WithArguments(",").WithLocation(1, 23));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
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
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ConstantPattern);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "5");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_26()
        {
            UsingExpression(@"c is [.. int[]? slice int i]",
                // (1,23): error CS1003: Syntax error, ',' expected
                // c is [.. int[]? slice int i]
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(1, 23));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
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
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_27()
        {
            UsingExpression(@"c is [.. string[]? slice string s]",
                // (1,26): error CS1003: Syntax error, ',' expected
                // c is [.. string[]? slice string s]
                Diagnostic(ErrorCode.ERR_SyntaxError, "string").WithArguments(",").WithLocation(1, 26));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.StringKeyword);
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
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "s");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void SlicePattern_28()
        {
            UsingExpression(@"c is [.. char[]? slice char ch]",
                // (1,24): error CS1003: Syntax error, ',' expected
                // c is [.. char[]? slice char ch]
                Diagnostic(ErrorCode.ERR_SyntaxError, "char").WithArguments(",").WithLocation(1, 24));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.ListPattern);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.SlicePattern);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.CharKeyword);
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
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "slice");
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.CharKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "ch");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }
    }
}

