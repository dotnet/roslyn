// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LambdaReturnTypeParsingTests : ParsingTests
    {
        public LambdaReturnTypeParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        [Fact]
        public void ExplicitReturnType_01()
        {
            string source = "T () => default";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.DefaultLiteralExpression);
                    {
                        N(SyntaxKind.DefaultKeyword);
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ExplicitReturnType_02()
        {
            string source = "T (x) => { return x; }";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ReturnStatement);
                        {
                            N(SyntaxKind.ReturnKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ExplicitReturnType_03()
        {
            string source = "T (T x) => x";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ExplicitReturnType_04()
        {
            string source = "T x => y";
            UsingExpression(source, TestOptions.Regular9,
                // (1,1): error CS1073: Unexpected token 'x'
                // T x => y
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "T").WithArguments("x").WithLocation(1, 1));
            verify();

            UsingExpression(source, TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token 'x'
                // T x => y
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "T").WithArguments("x").WithLocation(1, 1));
            verify();

            void verify()
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                EOF();
            }
        }

        [Fact]
        public void ExplicitReturnType_05()
        {
            string source = "int (_) => 0";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_06()
        {
            string source = "void () => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_07()
        {
            string source = "T[] () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
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
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_08()
        {
            string source = "T* () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_09()
        {
            string source = "delegate*<void> () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.FunctionPointerType);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.FunctionPointerParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.FunctionPointerParameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.VoidKeyword);
                            }
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_10()
        {
            string source = "T? () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_11()
        {
            string source = "int? () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_12()
        {
            string source = "A.B () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_13()
        {
            string source = "A::B () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_14()
        {
            string source = "global::T () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.GlobalKeyword);
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_15()
        {
            string source = "A<B> () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_16()
        {
            string source = "A<B>.C () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_17()
        {
            string source = "(int, object) () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
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
                            N(SyntaxKind.ObjectKeyword);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_18()
        {
            string source = "(int x, object y) () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_19()
        {
            string source = "var (x, y) => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "var");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_20()
        {
            string source = "int (x, y) => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_21()
        {
            string source = "A () => () => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_22()
        {
            string source = "A () => B () => null";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NullLiteralExpression);
                    {
                        N(SyntaxKind.NullKeyword);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_23()
        {
            string source = "object () => void () => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.ObjectKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_24()
        {
            string source = "ref int (ref int x) => ref x";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_25()
        {
            string source = "ref D () => ref int () => ref x";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_26()
        {
            string source = "F(a, b)";
            UsingExpression(source);

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
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
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_27()
        {
            string source = "F(a,";
            UsingExpression(source,
                // (1,5): error CS1733: Expected expression
                // F(a,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 5),
                // (1,5): error CS1026: ) expected
                // F(a,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 5));

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
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
                    M(SyntaxKind.Argument);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_28()
        {
            string source = "F(ref a, out b, in c)";
            UsingExpression(source);

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_29()
        {
            string source = "F(ref a,";
            UsingExpression(source,
                // (1,8): error CS1001: Identifier expected
                // F(ref a,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 8),
                // (1,9): error CS1001: Identifier expected
                // F(ref a,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 9),
                // (1,9): error CS1026: ) expected
                // F(ref a,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 9),
                // (1,9): error CS1003: Syntax error, '=>' expected
                // F(ref a,
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>", "").WithLocation(1, 9),
                // (1,9): error CS1733: Expected expression
                // F(ref a,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 9));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.Parameter);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_30()
        {
            string source = "F(A a, B b)";
            UsingExpression(source,
                // (1,5): error CS1003: Syntax error, ',' expected
                // F(A a, B b)
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",", "").WithLocation(1, 5),
                // (1,10): error CS1003: Syntax error, ',' expected
                // F(A a, B b)
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",", "").WithLocation(1, 10));

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    M(SyntaxKind.CommaToken);
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
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_31()
        {
            string source = "F(ref A a, out B b, in C c)";
            UsingExpression(source,
                // (1,9): error CS1003: Syntax error, ',' expected
                // F(ref A a, out B b, in C c)
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",", "").WithLocation(1, 9),
                // (1,26): error CS1003: Syntax error, ',' expected
                // F(ref A a, out B b, in C c)
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",", "").WithLocation(1, 26));

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    M(SyntaxKind.CommaToken);
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
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.DeclarationExpression);
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
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_32()
        {
            string source = "F(ref A a,";
            UsingExpression(source,
                // (1,11): error CS1001: Identifier expected
                // F(ref A a,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 11),
                // (1,11): error CS1026: ) expected
                // F(ref A a,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, '=>' expected
                // F(ref A a,
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("=>", "").WithLocation(1, 11),
                // (1,11): error CS1733: Expected expression
                // F(ref A a,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 11));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.Parameter);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.EqualsGreaterThanToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_33()
        {
            string source = "F(a, b) => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_34()
        {
            string source = "F(ref a, out b, in c) => { }";
            UsingExpression(source,
                // (1,8): error CS1001: Identifier expected
                // F(ref a, out b, in c) => { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 8),
                // (1,15): error CS1001: Identifier expected
                // F(ref a, out b, in c) => { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 15),
                // (1,21): error CS1001: Identifier expected
                // F(ref a, out b, in c) => { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 21));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_35()
        {
            string source = "F(A a, B b) => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_36()
        {
            string source = "F(ref A a, out B b, in C c) => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "F");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_37()
        {
            string source = "async void () => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_38()
        {
            string source = "async T () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_39()
        {
            string source = "static void () => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_40()
        {
            string source = "static T () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_41()
        {
            string source = "async static void () => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_42()
        {
            string source = "static async ref int () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_43()
        {
            string source = "async async (async async) => async";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.IdentifierToken, "async");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "async");
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_44()
        {
            string source = "[A] void () => { }";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
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
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_45()
        {
            string source = "[A][B] T () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
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
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "T");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void ExplicitReturnType_46()
        {
            string source = "[A, B] ref T () => default";
            UsingExpression(source);

            N(SyntaxKind.ParenthesizedLambdaExpression);
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
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.DefaultLiteralExpression);
                {
                    N(SyntaxKind.DefaultKeyword);
                }
            }
            EOF();
        }
    }
}
