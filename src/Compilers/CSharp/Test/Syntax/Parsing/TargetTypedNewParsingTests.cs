// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.TargetTypedNew)]
    public class TargetTypedNewParsingTests : ParsingTests
    {
        public TargetTypedNewParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TestNoRegressionOnNew()
        {
            UsingExpression("new",
                // (1,4): error CS1031: Type expected
                // new
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 4),
                // (1,4): error CS1526: A new expression requires (), [], or {} after type
                // new
                Diagnostic(ErrorCode.ERR_BadNewExpr, "").WithLocation(1, 4)
                );

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
            UsingExpression("new(Int32,Int32)?()");

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
            UsingExpression("new[]",
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
            UsingExpression("new{}"
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
        public void TestNoRegressionOnTupleArrayCreation()
        {
            UsingExpression("new(x,y)[0]"
                );

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
            UsingExpression("new(int,int)()",
                // (1,5): error CS1525: Invalid expression term 'int'
                // new(int,int){}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 5),
                // (1,9): error CS1525: Invalid expression term 'int'
                // new(int,int){}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 9));
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ObjectCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
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
            UsingExpression("new()[0]"
                );

            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.ObjectCreationExpression);
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

        [Fact]
        public void TestInvalidTupleCreationWithInitializer()
        {
            UsingExpression("new(int,int){}",
                // (1,5): error CS1525: Invalid expression term 'int'
                // new(int,int){}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 5),
                // (1,9): error CS1525: Invalid expression term 'int'
                // new(int,int){}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 9));
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
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

        [Theory]
        [InlineData(SyntaxKind.AddExpression, SyntaxKind.PlusToken)]
        [InlineData(SyntaxKind.SubtractExpression, SyntaxKind.MinusToken)]
        [InlineData(SyntaxKind.MultiplyExpression, SyntaxKind.AsteriskToken)]
        [InlineData(SyntaxKind.DivideExpression, SyntaxKind.SlashToken)]
        [InlineData(SyntaxKind.ModuloExpression, SyntaxKind.PercentToken)]
        [InlineData(SyntaxKind.LeftShiftExpression, SyntaxKind.LessThanLessThanToken)]
        [InlineData(SyntaxKind.RightShiftExpression, SyntaxKind.GreaterThanGreaterThanToken)]
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
            UsingExpression($"new(Int32,Int32){SyntaxFacts.GetText(tokenKind),2}",
                // (1,18): error CS1733: Expected expression
                // new(Int32,Int32) +
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 19));

            UsingExpression($"new(Int32,Int32){SyntaxFacts.GetText(tokenKind),2}e");

            N(expressionKind);
            {
                N(SyntaxKind.ObjectCreationExpression);
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
            UsingExpression("new()");
            N(SyntaxKind.ObjectCreationExpression);
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
            UsingExpression("new()", options: TestOptions.Regular7_3,
                // (1,1): error CS8370: Feature 'target-typed new' is not available in C# 7.3. Please use language version 8.0 or greater.
                // new()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "new").WithArguments("target-typed new", "8.0").WithLocation(1, 1));

            N(SyntaxKind.ObjectCreationExpression);
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
            UsingExpression("new(){}");
            N(SyntaxKind.ObjectCreationExpression);
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
            UsingExpression("new(){}", options: TestOptions.Regular7_3,
                // (1,1): error CS8370: Feature 'target-typed new' is not available in C# 7.3. Please use language version 8.0 or greater.
                // new(){}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "new").WithArguments("target-typed new", "8.0").WithLocation(1, 1));

            N(SyntaxKind.ObjectCreationExpression);
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
            UsingExpression("new(1,2){x=y}");
            N(SyntaxKind.ObjectCreationExpression);
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
            UsingExpression("new(1,2){x=y}", options: TestOptions.Regular7_3,
                // (1,1): error CS8370: Feature 'target-typed new' is not available in C# 7.3. Please use language version 8.0 or greater.
                // new(1,2){x=y}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "new").WithArguments("target-typed new", "8.0").WithLocation(1, 1));

            N(SyntaxKind.ObjectCreationExpression);
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
        public void TestCollectionInitializer()
        {
            UsingExpression("new(1){2}");
            N(SyntaxKind.ObjectCreationExpression);
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
