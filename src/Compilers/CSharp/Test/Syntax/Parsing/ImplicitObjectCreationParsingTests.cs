// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ImplicitObjectCreationParsingTests : ParsingTests
    {
        private static readonly CSharpParseOptions DefaultParseOptions = TestOptions.Regular9;

        public ImplicitObjectCreationParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TestNoRegressionOnNew()
        {
            UsingExpression("new", DefaultParseOptions,
                // (1,4): error CS1526: A new expression requires an argument list or (), [], or {} after type
                // new
                Diagnostic(ErrorCode.ERR_BadNewExpr, "").WithLocation(1, 4));

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.ArgumentList);
                {
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestNoRegressionOnNullableTuple()
        {
            UsingExpression("new(Int32,Int32)?()", DefaultParseOptions);

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Int32");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Int32");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestNoRegressionOnImplicitArrayCreation()
        {
            UsingExpression("new[]", DefaultParseOptions,
                // (1,6): error CS1514: { expected
                // new[]
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 6),
                // (1,6): error CS1513: } expected
                // new[]
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 6)
                );

            N(SyntaxKind.ImplicitArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
                M(SyntaxKind.ArrayInitializerExpression);
                {
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestNoRegressionOnAnonymousObjectCreation()
        {
            UsingExpression("new{}", DefaultParseOptions
                );

            N(SyntaxKind.AnonymousObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void TestNoRegressionOnConditional()
        {
            UsingExpression("new (a, b) ? x : y",
                // (1,12): error CS1526: A new expression requires (), [], or {} after type
                // new (a, b) ? x : y
                Diagnostic(ErrorCode.ERR_BadNewExpr, "?").WithLocation(1, 12));
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.ObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.ArgumentList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
            EOF();
        }

        [Fact]
        public void TestNoRegressionOnTupleArrayCreation()
        {
            UsingExpression("new(x,y)[0]", DefaultParseOptions);
            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
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
        public void TestInvalidTupleCreation()
        {
            UsingExpression("new(int,int)()", DefaultParseOptions);
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestInvalidTupleArrayCreation()
        {
            UsingExpression("new()[0]", DefaultParseOptions);
            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.ImplicitObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
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
            EOF();
        }

        [Theory]
        [InlineData(SyntaxKind.AddExpression, SyntaxKind.PlusToken)]
        [InlineData(SyntaxKind.SubtractExpression, SyntaxKind.MinusToken)]
        [InlineData(SyntaxKind.MultiplyExpression, SyntaxKind.AsteriskToken)]
        [InlineData(SyntaxKind.DivideExpression, SyntaxKind.SlashToken)]
        [InlineData(SyntaxKind.ModuloExpression, SyntaxKind.PercentToken)]
        [InlineData(SyntaxKind.LeftShiftExpression, SyntaxKind.LessThanLessThanToken)]
        [InlineData(SyntaxKind.RightShiftExpression, SyntaxKind.GreaterThanGreaterThanToken)]
        [InlineData(SyntaxKind.UnsignedRightShiftExpression, SyntaxKind.GreaterThanGreaterThanGreaterThanToken)]
        [InlineData(SyntaxKind.LogicalOrExpression, SyntaxKind.BarBarToken)]
        [InlineData(SyntaxKind.LogicalAndExpression, SyntaxKind.AmpersandAmpersandToken)]
        [InlineData(SyntaxKind.BitwiseOrExpression, SyntaxKind.BarToken)]
        [InlineData(SyntaxKind.BitwiseAndExpression, SyntaxKind.AmpersandToken)]
        [InlineData(SyntaxKind.ExclusiveOrExpression, SyntaxKind.CaretToken)]
        [InlineData(SyntaxKind.EqualsExpression, SyntaxKind.EqualsEqualsToken)]
        [InlineData(SyntaxKind.NotEqualsExpression, SyntaxKind.ExclamationEqualsToken)]
        [InlineData(SyntaxKind.LessThanExpression, SyntaxKind.LessThanToken)]
        [InlineData(SyntaxKind.LessThanOrEqualExpression, SyntaxKind.LessThanEqualsToken)]
        [InlineData(SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanToken)]
        [InlineData(SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanEqualsToken)]
        public void TestBinaryOperators(SyntaxKind expressionKind, SyntaxKind tokenKind)
        {
            UsingExpression($"new(Int32,Int32){SyntaxFacts.GetText(tokenKind),3}", DefaultParseOptions,
                // (1,20): error CS1733: Expected expression
                // new(Int32,Int32)  +
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 20));

            N(expressionKind);
            {
                N(SyntaxKind.ImplicitObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Int32");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Int32");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(tokenKind);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            EOF();

            UsingExpression($"new(Int32,Int32){SyntaxFacts.GetText(tokenKind),2}e", DefaultParseOptions);

            N(expressionKind);
            {
                N(SyntaxKind.ImplicitObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Int32");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Int32");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(tokenKind);
                N(SyntaxKind.IdentifierName, "e");
                {
                    N(SyntaxKind.IdentifierToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestEmptyArgList()
        {
            UsingExpression("new()", DefaultParseOptions);
            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestEmptyArgList_LangVersion()
        {
            UsingExpression("new()", options: TestOptions.Regular8,
                // (1,1): error CS8400: Feature 'target-typed object creation' is not available in C# 8.0. Please use language version 9.0 or greater.
                // new()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "new").WithArguments("target-typed object creation", "9.0").WithLocation(1, 1)
                );

            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestEmptyObjectInitializer()
        {
            UsingExpression("new(){}", DefaultParseOptions);
            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
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
            EOF();
        }

        [Fact]
        public void TestEmptyObjectInitializer_LangVersion()
        {
            UsingExpression("new(){}", options: TestOptions.Regular8,
                // (1,1): error CS8400: Feature 'target-typed object creation' is not available in C# 8.0. Please use language version 9.0 or greater.
                // new(){}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "new").WithArguments("target-typed object creation", "9.0").WithLocation(1, 1)
                );

            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
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
            EOF();
        }

        [Fact]
        public void TestObjectInitializer()
        {
            UsingExpression("new(1,2){x=y}", DefaultParseOptions);
            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestObjectInitializer_LangVersion()
        {
            UsingExpression("new(a,b){x=y}", options: TestOptions.Regular8,
                // (1,1): error CS8400: Feature 'target-typed object creation' is not available in C# 8.0. Please use language version 9.0 or greater.
                // new(a,b){x=y}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "new").WithArguments("target-typed object creation", "9.0").WithLocation(1, 1)
                );

            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
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
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestCollectionInitializer()
        {
            UsingExpression("new(1){2}", DefaultParseOptions);
            N(SyntaxKind.ImplicitObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.CollectionInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }
    }
}
