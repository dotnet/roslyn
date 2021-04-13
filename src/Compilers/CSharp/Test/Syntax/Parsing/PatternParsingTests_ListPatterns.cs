﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternParsingTests_ListPatterns : ParsingTests
    {
        private new void UsingExpression(string text, params DiagnosticDescription[] expectedErrors)
        {
            UsingExpression(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview), expectedErrors);
        }

        public PatternParsingTests_ListPatterns(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void LengthPattern_01()
        {
            UsingExpression(@"c is [0]{}");
            verify();

            UsingExpression(@"c is [0]{}", TestOptions.Regular9,
                // (1,6): error CS8652: The feature 'length pattern' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // c is [0]{}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[0]").WithArguments("length pattern").WithLocation(1, 6));
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
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.LengthPatternClause);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.PropertyPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void LengthPattern_02()
        {
            UsingExpression(@"c is [>0]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
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
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_03()
        {
            UsingExpression(@"c is [_]{P:P}");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.DiscardPattern);
                        {
                            N(SyntaxKind.UnderscoreToken);
                        }
                        N(SyntaxKind.CloseBracketToken);
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
                                    N(SyntaxKind.IdentifierToken, "P");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "P");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_04()
        {
            UsingExpression(@"c is ()[0]");

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
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
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
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_05()
        {
            UsingExpression(@"c is int[][0]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
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
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
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
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_06()
        {
            UsingExpression(@"c is int[0]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
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
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_07()
        {
            UsingExpression(@"c is List<int>[0]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
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
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
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
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_08()
        {
            UsingExpression(@"c is [{}]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
        }

        [Fact]
        public void LengthPattern_09()
        {
            UsingExpression(@"c is C1 { Prop[1]: var x }",
                // (1,18): error CS1003: Syntax error, ',' expected
                // c is C1 { Prop[1]: var x }
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",", ":").WithLocation(1, 18),
                // (1,20): error CS1003: Syntax error, ',' expected
                // c is C1 { Prop[1]: var x }
                Diagnostic(ErrorCode.ERR_SyntaxError, "var").WithArguments(",", "").WithLocation(1, 20));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C1");
                    }
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Prop");
                                }
                                N(SyntaxKind.LengthPatternClause);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.VarPattern);
                            {
                                N(SyntaxKind.VarKeyword);
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_10()
        {
            UsingExpression(@"c is [var x]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
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
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_11()
        {
            UsingExpression(@"c is [List<int>]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
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
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_12()
        {
            UsingExpression(@"c is [string[]]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
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
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_13()
        {
            UsingExpression(@"c is [string[>0]]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.LengthPatternClause);
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
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_14()
        {
            UsingExpression(@"c is [[_]]");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.LengthPatternClause);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.DiscardPattern);
                                {
                                    N(SyntaxKind.UnderscoreToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_15()
        {
            UsingExpression(@"c is []",
                // (1,7): error CS8504: Pattern missing
                // c is []
                Diagnostic(ErrorCode.ERR_MissingPattern, "]").WithLocation(1, 7));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.ConstantPattern);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void LengthPattern_16()
        {
            UsingExpression(@"c is [0]()",
                    // (1,1): error CS1073: Unexpected token '('
                    // c is [0]()
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "c is [0]").WithArguments("(").WithLocation(1, 1));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.LengthPatternClause);
                    {
                        N(SyntaxKind.OpenBracketToken);
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
            }
            EOF();
        }

        [Fact]
        public void NoRegressionOnEmptyPropertyPattern()
        {
            UsingExpression(@"c is {}");

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
        public void NoRegressionOnArrayTypePattern()
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
        public void ListPattern_01()
        {
            UsingExpression(@"c is {{}}");
            verify();

            UsingExpression(@"c is {{}}", TestOptions.Regular9);
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
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.ListPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ListPattern_02()
        {
            UsingExpression(@"c is { 1, prop: 0 }");

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
                        N(SyntaxKind.Subpattern);
                        {
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
                            N(SyntaxKind.NameColon);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "prop");
                                }
                                N(SyntaxKind.ColonToken);
                            }
                            N(SyntaxKind.ConstantPattern);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "0");
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_01()
        {
            UsingExpression(@"c is {..}");
            verify();

            UsingExpression(@"c is {..}", TestOptions.Regular9,
                // (1,7): error CS8652: The feature 'slice pattern' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // c is {..}
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "..").WithArguments("slice pattern").WithLocation(1, 7));
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
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.ListPatternClause);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.Subpattern);
                            {
                                N(SyntaxKind.SlicePattern);
                                {
                                    N(SyntaxKind.DotDotToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void SlicePattern_02()
        {
            UsingExpression(@"c is {.. var x}");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
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
                        N(SyntaxKind.CloseBraceToken);
                    }
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
            UsingExpression(@"c is {..{}}");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                                N(SyntaxKind.RecursivePattern);
                                {
                                    N(SyntaxKind.PropertyPatternClause);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_06()
        {
            UsingExpression(@"c is {.. not p}");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
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
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_07()
        {
            UsingExpression(@"c is {.. p or q}");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.SlicePattern);
                                {
                                    N(SyntaxKind.DotDotToken);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "p");
                                        }
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void SlicePattern_08()
        {
            UsingExpression(@"c is {.. p or .. q}");

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.OrPattern);
                            {
                                N(SyntaxKind.SlicePattern);
                                {
                                    N(SyntaxKind.DotDotToken);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "p");
                                        }
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
                        N(SyntaxKind.CloseBraceToken);
                    }
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
            UsingExpression(@"c is {var x ..}",
                    // (1,13): error CS1003: Syntax error, ',' expected
                    // c is {var x ..}
                    Diagnostic(ErrorCode.ERR_SyntaxError, "..").WithArguments(",", "..").WithLocation(1, 13));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.VarPattern);
                            {
                                N(SyntaxKind.VarKeyword);
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
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
            UsingExpression(@"c is {{}..}",
                // (1,9): error CS1003: Syntax error, ',' expected
                // c is {{}..}
                Diagnostic(ErrorCode.ERR_SyntaxError, "..").WithArguments(",", "..").WithLocation(1, 9));

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.RecursivePattern);
                {
                    N(SyntaxKind.ListPatternClause);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.RecursivePattern);
                            {
                                N(SyntaxKind.PropertyPatternClause);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Subpattern);
                        {
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
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
                    N(SyntaxKind.RecursivePattern);
                    {
                        N(SyntaxKind.LengthPatternClause);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.SlicePattern);
                            {
                                N(SyntaxKind.DotDotToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
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
    }
}
