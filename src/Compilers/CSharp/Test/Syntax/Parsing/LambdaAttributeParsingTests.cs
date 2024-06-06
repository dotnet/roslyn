// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LambdaAttributeParsingTests : ParsingTests
    {
        public LambdaAttributeParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        [Fact]
        public void LambdaAttribute_01()
        {
            string source = "[A] x => x";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.SimpleLambdaExpression);
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
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
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
        public void LambdaAttribute_02()
        {
            string source = "[A, B] () => { }";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
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
        }

        [Fact]
        public void LambdaAttribute_03()
        {
            string source = "[A][B] (object x) => x";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
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
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
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
        public void LambdaAttribute_04()
        {
            string source = "([A] object x) => x";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
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
                                N(SyntaxKind.ObjectKeyword);
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
        public void LambdaAttribute_05()
        {
            string source = "[A] (ref x) => x";
            UsingExpression(source, TestOptions.RegularPreview,
                // (1,11): error CS1001: Identifier expected
                // [A] (ref x) => x
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 11));

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
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        M(SyntaxKind.IdentifierToken);
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

        [Fact]
        public void LambdaAttribute_06()
        {
            string source = "[A] ref x => x";
            UsingExpression(source, TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token 'ref'
                // [A] ref x => x
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "[A]").WithArguments("ref").WithLocation(1, 1));

            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void LambdaAttribute_07()
        {
            string source = "[A] in x => x";
            UsingExpression(source, TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token 'in'
                // [A] in x => x
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "[A]").WithArguments("in").WithLocation(1, 1));

            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        // [A] <modifiers> x => x
        private void LambdaExpression_01(params SyntaxKind[] modifiers)
        {
            N(SyntaxKind.SimpleLambdaExpression);
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
                foreach (var modifier in modifiers)
                {
                    N(modifier);
                }
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
        }

        // [A]<modifiers> () => { }
        private void LambdaExpression_02(params SyntaxKind[] modifiers)
        {
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
                foreach (var modifier in modifiers)
                {
                    N(modifier);
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

        // [A] <modifiers> (x) => { }
        private void LambdaExpression_03(params SyntaxKind[] modifiers)
        {
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
                foreach (var modifier in modifiers)
                {
                    N(modifier);
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
                    N(SyntaxKind.CloseBraceToken);
                }
            }
        }

        // [A] <modifiers> (object x) => { }
        private void LambdaExpression_04(params SyntaxKind[] modifiers)
        {
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
                foreach (var modifier in modifiers)
                {
                    N(modifier);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "x");
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
        }

        // [A(B)]() => { }
        private void LambdaExpression_05()
        {
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
                        N(SyntaxKind.AttributeArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.AttributeArgument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
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

        // [A, B]() => { }
        private void LambdaExpression_06()
        {
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

        // [A][B]() => { }
        private void LambdaExpression_07()
        {
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

        // [A] (ref int x) => ref x
        private void LambdaExpression_08()
        {
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
        }

        // [return: A] static x => x
        private void LambdaExpression_09()
        {
            N(SyntaxKind.SimpleLambdaExpression);
            {
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.AttributeTargetSpecifier);
                    {
                        N(SyntaxKind.ReturnKeyword);
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
        }

        // ([A] int x) => x
        private void LambdaExpression_10()
        {
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
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
                            N(SyntaxKind.IntKeyword);
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
        }

        // ([A] out int x) => { }
        private void LambdaExpression_11()
        {
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
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
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "x");
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
        }

        // ([A] ref int x) => ref x
        private void LambdaExpression_12()
        {
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
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
        }

        // ([A] x) => x
        private void LambdaExpression_13()
        {
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
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
        }

        // (int x, [A] int y) => x
        private void LambdaExpression_14()
        {
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
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
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
            }
        }

        public static IEnumerable<object[]> GetLambdaTestData()
        {
            yield return getData("[A] x => x", tests => tests.LambdaExpression_01());
            yield return getData("[A] async x => x", tests => tests.LambdaExpression_01(SyntaxKind.AsyncKeyword));
            yield return getData("[A] static x => x", tests => tests.LambdaExpression_01(SyntaxKind.StaticKeyword));
            yield return getData("[A] async static x => x", tests => tests.LambdaExpression_01(SyntaxKind.AsyncKeyword, SyntaxKind.StaticKeyword));
            yield return getData("[A] static async x => x", tests => tests.LambdaExpression_01(SyntaxKind.StaticKeyword, SyntaxKind.AsyncKeyword));

            yield return getData("[A]() => { }", tests => tests.LambdaExpression_02());
            yield return getData("[A]async () => { }", tests => tests.LambdaExpression_02(SyntaxKind.AsyncKeyword));
            yield return getData("[A]static () => { }", tests => tests.LambdaExpression_02(SyntaxKind.StaticKeyword));
            yield return getData("[A]async static () => { }", tests => tests.LambdaExpression_02(SyntaxKind.AsyncKeyword, SyntaxKind.StaticKeyword));
            yield return getData("[A]static async () => { }", tests => tests.LambdaExpression_02(SyntaxKind.StaticKeyword, SyntaxKind.AsyncKeyword));

            yield return getData("[A] (x) => { }", tests => tests.LambdaExpression_03());
            yield return getData("[A] async (x) => { }", tests => tests.LambdaExpression_03(SyntaxKind.AsyncKeyword));
            yield return getData("[A] static (x) => { }", tests => tests.LambdaExpression_03(SyntaxKind.StaticKeyword));
            yield return getData("[A] async static (x) => { }", tests => tests.LambdaExpression_03(SyntaxKind.AsyncKeyword, SyntaxKind.StaticKeyword));
            yield return getData("[A] static async (x) => { }", tests => tests.LambdaExpression_03(SyntaxKind.StaticKeyword, SyntaxKind.AsyncKeyword));

            yield return getData("[A] (object x) => { }", tests => tests.LambdaExpression_04());
            yield return getData("[A] async (object x) => { }", tests => tests.LambdaExpression_04(SyntaxKind.AsyncKeyword));
            yield return getData("[A] static (object x) => { }", tests => tests.LambdaExpression_04(SyntaxKind.StaticKeyword));
            yield return getData("[A] async static (object x) => { }", tests => tests.LambdaExpression_04(SyntaxKind.AsyncKeyword, SyntaxKind.StaticKeyword));
            yield return getData("[A] static async (object x) => { }", tests => tests.LambdaExpression_04(SyntaxKind.StaticKeyword, SyntaxKind.AsyncKeyword));

            yield return getData("[A(B)]() => { }", tests => tests.LambdaExpression_05());
            yield return getData("[A, B]() => { }", tests => tests.LambdaExpression_06());
            yield return getData("[A][B]() => { }", tests => tests.LambdaExpression_07());
            yield return getData("[A] (ref int x) => ref x", tests => tests.LambdaExpression_08());

            yield return getData("[return: A] static x => x", tests => tests.LambdaExpression_09());
            yield return getData("([A] int x) => x", tests => tests.LambdaExpression_10());
            yield return getData("([A] out int x) => { }", tests => tests.LambdaExpression_11());
            yield return getData("([A] ref int x) => ref x", tests => tests.LambdaExpression_12());
            yield return getData("([A] x) => x", tests => tests.LambdaExpression_13());
            yield return getData("(int x, [A] int y) => x", tests => tests.LambdaExpression_14());

            static object[] getData(string expr, Action<LambdaAttributeParsingTests> action) => new object[] { expr, action };
        }

        [Theory]
        [MemberData(nameof(GetLambdaTestData))]
        public void Assignment(string exprLambda, Action<LambdaAttributeParsingTests> verifyLambda)
        {
            UsingExpression($"f = {exprLambda}", TestOptions.RegularPreview);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "f");
                }
                N(SyntaxKind.EqualsToken);
                verifyLambda(this);
            }
            EOF();
        }

        [Theory]
        [MemberData(nameof(GetLambdaTestData))]
        public void Argument_01(string exprLambda, Action<LambdaAttributeParsingTests> verifyLambda)
        {
            UsingExpression($"F({exprLambda})", TestOptions.RegularPreview);

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
                        verifyLambda(this);
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Theory]
        [MemberData(nameof(GetLambdaTestData))]
        public void Argument_02(string exprLambda, Action<LambdaAttributeParsingTests> verifyLambda)
        {
            UsingExpression($"F(x, {exprLambda})", TestOptions.RegularPreview);

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
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        verifyLambda(this);
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        // Parenthesized lambda with attribute is similar to lambda with parameter attribute.
        [Fact]
        public void ParenthesizedLambdaWithAttribute()
        {
            UsingExpression("f = ([A] x => x)", TestOptions.RegularPreview);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "f");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.SimpleLambdaExpression);
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        // Lambda with attribute in collection initializer is similar to dictionary initializer.
        [Fact]
        public void CollectionInitializer_01()
        {
            UsingExpression("new B { [A] x => y }", TestOptions.RegularPreview,
                // (1,13): error CS1003: Syntax error, '=' expected
                // new B { [A] x => y }
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments("=").WithLocation(1, 13));

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.ImplicitElementAccess);
                        {
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.EqualsToken);
                        N(SyntaxKind.SimpleLambdaExpression);
                        {
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        // Lambda with attribute in collection initializer is similar to dictionary initializer.
        [Fact]
        public void CollectionInitializer_02()
        {
            UsingExpression("new B { ([A] x => y) }", TestOptions.RegularPreview);

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
                N(SyntaxKind.CollectionInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.SimpleLambdaExpression);
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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void PostfixOperator()
        {
            UsingExpression("[A] () => { } ++", TestOptions.RegularPreview);

            N(SyntaxKind.PostIncrementExpression);
            {
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
                N(SyntaxKind.PlusPlusToken);
            }
            EOF();
        }

        [Fact]
        public void PrefixOperator()
        {
            UsingExpression("-- [A] () => { }", TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token '=>'
                // -- [A] () => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "-- [A] ()").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.PreDecrementExpression);
            {
                N(SyntaxKind.MinusMinusToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void UnaryOperator()
        {
            UsingExpression("! [A] () => { }", TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token '=>'
                // ! [A] () => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "! [A] ()").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.LogicalNotExpression);
            {
                N(SyntaxKind.ExclamationToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void Cast()
        {
            UsingExpression("(F) [A] () => { }", TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token '=>'
                // (F) [A] () => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(F) [A] ()").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
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
        public void BinaryOperator_01()
        {
            UsingExpression("[A] () => { } + y", TestOptions.RegularPreview,
                // (1,15): warning CS8848: Operator '+' cannot be used here due to precedence. Use parentheses to disambiguate.
                // [A] () => { } + y
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "+").WithArguments("+").WithLocation(1, 15));

            N(SyntaxKind.AddExpression);
            {
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
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
            }
            EOF();
        }

        [Fact]
        public void BinaryOperator_02()
        {
            UsingExpression("x * [A] () => { }", TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token '=>'
                // x * [A] () => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "x * [A] ()").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.MultiplyExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void Is()
        {
            UsingExpression("[A] () => { } is E", TestOptions.RegularPreview,
                // (1,15): warning CS8848: Operator 'is' cannot be used here due to precedence. Use parentheses to disambiguate.
                // [A] () => { } is E
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "is").WithArguments("is").WithLocation(1, 15));

            N(SyntaxKind.IsExpression);
            {
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
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "E");
                }
            }
            EOF();
        }

        [Fact]
        public void As()
        {
            UsingExpression("[A] () => x as E", TestOptions.RegularPreview);

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
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.AsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "E");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalExpression_01()
        {
            UsingExpression("x ? [A] () => { } : z", TestOptions.RegularPreview);

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
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
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "z");
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalExpression_01_A()
        {
            UsingExpression("x ? () => { } : z", TestOptions.RegularPreview);

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
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
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "z");
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalExpression_02()
        {
            UsingExpression("x ? y : [A] () => { }", TestOptions.RegularPreview);

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "y");
                }
                N(SyntaxKind.ColonToken);
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

        // Lambda with attribute in conditional expression is similar to conditional element access.
        [Fact]
        public void ConditionalExpression_03()
        {
            UsingExpression("x ? ([A] () => { }) : y", TestOptions.RegularPreview);

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
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
                    N(SyntaxKind.CloseParenToken);
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
        public void SwitchExpression_01()
        {
            UsingExpression("[A] () => { } switch { }", TestOptions.RegularPreview,
                // (1,15): warning CS8848: Operator 'switch' cannot be used here due to precedence. Use parentheses to disambiguate.
                // [A] () => { } switch { }
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "switch").WithArguments("switch").WithLocation(1, 15));

            N(SyntaxKind.SwitchExpression);
            {
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
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void SwitchExpression_02()
        {
            UsingExpression("x switch { y => [A] () => { }, _ => [A] () => z }", TestOptions.RegularPreview);

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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.SwitchExpressionArm);
                {
                    N(SyntaxKind.DiscardPattern);
                    {
                        N(SyntaxKind.UnderscoreToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
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
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "z");
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void Tuple_01()
        {
            UsingExpression("([A] () => { }, y)", TestOptions.RegularPreview);

            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void Tuple_02()
        {
            UsingExpression("(x, [A] () => { })", TestOptions.RegularPreview);

            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
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
                N(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void Range_01()
        {
            UsingExpression("s[[A] x => x..]", TestOptions.RegularPreview);

            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "s");
                }
                N(SyntaxKind.BracketedArgumentList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.SimpleLambdaExpression);
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
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.RangeExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.DotDotToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Range_02()
        {
            UsingExpression("s[..[A] () => { }]", TestOptions.RegularPreview,
                // (1,12): error CS1003: Syntax error, ',' expected
                // s[..[A] () => { }]
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(1, 12));

            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "s");
                }
                N(SyntaxKind.BracketedArgumentList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.RangeExpression);
                        {
                            N(SyntaxKind.DotDotToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
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
        public void NullableType_Switch_01()
        {
            string source = "x switch { string?[] y => y }";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.RegularPreview);
            verify();

            void verify()
            {
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
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.StringKeyword);
                                    }
                                    N(SyntaxKind.QuestionToken);
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
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact]
        public void NullableType_Switch_02()
        {
            string source = "x switch { string? [,] y => y }";
            UsingExpression(source);

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
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.OmittedArraySizeExpression);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void NullableType_Switch_03()
        {
            string source = "x switch { string? [A] y => y }";
            UsingExpression(source);

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
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void NullableType_Is_01()
        {
            string source = "x is string ? [] y";
            UsingExpression(source);

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.DeclarationPattern);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.QuestionToken);
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
                    N(SyntaxKind.SingleVariableDesignation);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_Is_02()
        {
            string source = "x is string ? [A] y";
            UsingExpression(source);

            N(SyntaxKind.IsPatternExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.IsKeyword);
                N(SyntaxKind.DeclarationPattern);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.StringKeyword);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.SingleVariableDesignation);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_Is_03()
        {
            string source = "_ = x is string ? [] y => y : z";
            UsingExpression(source,
                // (1,1): error CS1073: Unexpected token '=>'
                // _ = x is string ? [] y => y : z
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "_ = x is string ? [] y").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
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
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_Is_04()
        {
            string source = "_ = x is string ? [A] y => y : z";
            UsingExpression(source,
                // (1,1): error CS1073: Unexpected token '=>'
                // _ = x is string ? [A] y => y : z
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "_ = x is string ? [A] y").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_Is_05()
        {
            string source = "_ = x is string ? [return: A] y => y : z";
            UsingExpression(source,
                // (1,1): error CS1073: Unexpected token '=>'
                // _ = x is string ? [return: A] y => y : z
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "_ = x is string ? [return: A] y").WithArguments("=>").WithLocation(1, 1),
                // (1,20): error CS1003: Syntax error, ',' expected
                // _ = x is string ? [return: A] y => y : z
                Diagnostic(ErrorCode.ERR_SyntaxError, "return").WithArguments(",").WithLocation(1, 20));

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.StringKeyword);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.ArrayRankSpecifier);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_Is_06()
        {
            string source = "_ = x is string ? ([return: A] y => y) : z";
            UsingExpression(source);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IsExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.SimpleLambdaExpression);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.AttributeTargetSpecifier);
                                {
                                    N(SyntaxKind.ReturnKeyword);
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "z");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_As_01()
        {
            string source = "x as string ? []";
            UsingExpression(source);

            N(SyntaxKind.AsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.AsKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                        N(SyntaxKind.QuestionToken);
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
        public void NullableType_As_02()
        {
            string source = "x as string ? [2, 3]";
            UsingExpression(source);

            N(SyntaxKind.AsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.AsKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_As_03()
        {
            string source = "x as string ? [A] y => y : z";
            UsingExpression(source,
                // (1,1): error CS1073: Unexpected token 'y'
                // x as string ? [A] y => y : z
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "x as string ? [A]").WithArguments("y").WithLocation(1, 1));

            N(SyntaxKind.AsExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.AsKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.StringKeyword);
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullableType_As_04()
        {
            string source = "x as string ? ([A] y => y) : z";
            UsingExpression(source);

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.AsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.AsKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.SimpleLambdaExpression);
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "z");
                }
            }
            EOF();
        }

        [Fact]
        public void CollectionInitializer_03()
        {
            string source = "new() { [A] x => x, [B] () => { } }";
            UsingExpression(source,
                // (1,13): error CS1003: Syntax error, '=' expected
                // new() { [A] x => x, [B] () => { } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments("=").WithLocation(1, 13),
                // (1,25): error CS1003: Syntax error, '=' expected
                // new() { [A] x => x, [B] () => { } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("=").WithLocation(1, 25));

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
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.ImplicitElementAccess);
                        {
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.EqualsToken);
                        N(SyntaxKind.SimpleLambdaExpression);
                        {
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.ImplicitElementAccess);
                        {
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        M(SyntaxKind.EqualsToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void ObjectInitializer_01()
        {
            string source = "new() { P = [A] x => x, Q = [B] () => { } }";
            UsingExpression(source, TestOptions.Regular9);
            verify();

            UsingExpression(source, TestOptions.Regular10);
            verify();

            void verify()
            {
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
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "P");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SimpleLambdaExpression);
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
                                N(SyntaxKind.Parameter);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Q");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void AnonymousType_01()
        {
            string source = "new { [A] x => x, [B] () => { } }";
            UsingExpression(source);

            N(SyntaxKind.AnonymousObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AnonymousObjectMemberDeclarator);
                {
                    N(SyntaxKind.SimpleLambdaExpression);
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.AnonymousObjectMemberDeclarator);
                {
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
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
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void AnonymousType_02()
        {
            string source = "new { x [B] y => y }";
            UsingExpression(source,
                // (1,13): error CS1003: Syntax error, ',' expected
                // new { x [B] y => y }
                Diagnostic(ErrorCode.ERR_SyntaxError, "y").WithArguments(",").WithLocation(1, 13));

            N(SyntaxKind.AnonymousObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AnonymousObjectMemberDeclarator);
                {
                    N(SyntaxKind.ElementAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.AnonymousObjectMemberDeclarator);
                {
                    N(SyntaxKind.SimpleLambdaExpression);
                    {
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ArrayInitializer_01()
        {
            string source = "new[] { [A] x => x, [B] () => { } }";
            UsingExpression(source);

            N(SyntaxKind.ImplicitArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleLambdaExpression);
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
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
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void StackAllocInitializer_01()
        {
            string source = "stackalloc[] { [A] x => x, [B] () => { } }";
            UsingExpression(source);

            N(SyntaxKind.ImplicitStackAllocArrayCreationExpression);
            {
                N(SyntaxKind.StackAllocKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleLambdaExpression);
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
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
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void With_01()
        {
            string source = "x with { [A] y => y, [B] () => { } }";
            UsingExpression(source);

            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleLambdaExpression);
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ParenthesizedLambdaExpression);
                    {
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
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Invoke_01()
        {
            string source = "[A] () => { } ()";
            UsingExpression(source);

            N(SyntaxKind.InvocationExpression);
            {
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Invoke_02()
        {
            string source = "([A] () => { })()";
            UsingExpression(source);

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
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
        public void Invoke_03()
        {
            string source = "([A] x => x)()";
            UsingExpression(source);

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.SimpleLambdaExpression);
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
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
        public void ExpressionStatement_01()
        {
            string source = "() => { };";
            UsingStatement(source);

            N(SyntaxKind.ExpressionStatement);
            {
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
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionStatement_02()
        {
            string source = "[A] () => { };";
            UsingStatement(source);

            N(SyntaxKind.ExpressionStatement);
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
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionStatement_03()
        {
            string source = "[A] x => x;";
            UsingStatement(source);

            N(SyntaxKind.ExpressionStatement);
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
                N(SyntaxKind.SimpleLambdaExpression);
                {
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionStatement_04()
        {
            string source = "[A] () => { } ();";
            UsingStatement(source);

            N(SyntaxKind.ExpressionStatement);
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
                N(SyntaxKind.InvocationExpression);
                {
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
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionStatement_05()
        {
            string source = "[A] (x => x) ();";
            UsingStatement(source);

            N(SyntaxKind.ExpressionStatement);
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
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.SimpleLambdaExpression);
                        {
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
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
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void AnonymousMethod_01()
        {
            string source = "[A] delegate () { }";
            verify(source, TestOptions.Regular9);
            verify(source);

            void verify(string source, ParseOptions? parseOptions = null)
            {
                UsingExpression(source, parseOptions,
                    // (1,1): error CS1073: Unexpected token 'delegate'
                    // [A] delegate () { }
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "[A]").WithArguments("delegate").WithLocation(1, 1));

                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                EOF();
            }
        }

        [Fact]
        public void AnonymousMethod_02()
        {
            string source = "[return: A] delegate () { return null; }";
            verify(source, TestOptions.Regular9);
            verify(source);

            void verify(string source, ParseOptions? parseOptions = null)
            {
                UsingExpression(source, parseOptions,
                    // (1,1): error CS1073: Unexpected token 'delegate'
                    // [return: A] delegate () { return null; }
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "[return: A]").WithArguments("delegate").WithLocation(1, 1),
                    // (1,2): error CS1041: Identifier expected; 'return' is a keyword
                    // [return: A] delegate () { return null; }
                    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "return").WithArguments("", "return").WithLocation(1, 2));

                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                EOF();
            }
        }

        [Fact]
        public void AnonymousMethod_03()
        {
            string source = "d = [A] delegate () { };";
            verify(source, TestOptions.Regular9);
            verify(source);

            void verify(string source, ParseOptions? parseOptions = null)
            {
                UsingStatement(source, parseOptions,
                    // (1,1): error CS1073: Unexpected token 'delegate'
                    // d = [A] delegate () { };
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "d = [A] ").WithArguments("delegate").WithLocation(1, 1),
                    // (1,9): error CS1002: ; expected
                    // d = [A] delegate () { };
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "delegate").WithLocation(1, 9));

                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "d");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambda1()
        {
            UsingTree("""
                // Lambda inside attribute with attributes of its own will cause us to bail out.
                [A([B]() => {})]
                class C
                {
                }
                """,
                // (2,10): error CS1003: Syntax error, ',' expected
                // [A([B]() => {})]
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(2, 10),
                // (2,14): error CS1022: Type or namespace definition, or end-of-file expected
                // [A([B]() => {})]
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(2, 14),
                // (2,14): error CS1003: Syntax error, ']' expected
                // [A([B]() => {})]
                Diagnostic(ErrorCode.ERR_SyntaxError, "}").WithArguments("]").WithLocation(2, 14),
                // (2,15): error CS1022: Type or namespace definition, or end-of-file expected
                // [A([B]() => {})]
                Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(2, 15));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambda2()
        {
            UsingTree("""
                // Lambda inside attribute without attributes of its own is fine for parsing.
                [A(() => {})]
                class C
                {
                }
                """);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
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
                            N(SyntaxKind.AttributeArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.AttributeArgument);
                                {
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambda3()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        // Because we're already in an expression, parsing an attribute list, we'll bail out of parsing the `[B]`
                        // as an attribute on an inner lambda.
                        var v = [A([B]() => {})]
                            () => {};
                    }
                }
                """,
                // (8,16): error CS1003: Syntax error, ',' expected
                //             () => {};
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(8, 16),
                // (8,19): error CS1002: ; expected
                //             () => {};
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(8, 19));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
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
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.CollectionExpression);
                                                {
                                                    N(SyntaxKind.OpenBracketToken);
                                                    N(SyntaxKind.ExpressionElement);
                                                    {
                                                        N(SyntaxKind.InvocationExpression);
                                                        {
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "A");
                                                            }
                                                            N(SyntaxKind.ArgumentList);
                                                            {
                                                                N(SyntaxKind.OpenParenToken);
                                                                N(SyntaxKind.Argument);
                                                                {
                                                                    N(SyntaxKind.ParenthesizedLambdaExpression);
                                                                    {
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
                                                                N(SyntaxKind.CloseParenToken);
                                                            }
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseBracketToken);
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.EmptyStatement);
                            {
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambda3_B()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        // This is an attributed lambda *within* a collection expression.  This is fine.
                        var v = [A([B]() => {})];
                    }
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
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
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.CollectionExpression);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.ExpressionElement);
                                                {
                                                    N(SyntaxKind.InvocationExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "A");
                                                        }
                                                        N(SyntaxKind.ArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.Argument);
                                                            {
                                                                N(SyntaxKind.ParenthesizedLambdaExpression);
                                                                {
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
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambda4()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var v = [A(() => {})] () => {};
                    }
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
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
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
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
                                                        N(SyntaxKind.AttributeArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.AttributeArgument);
                                                            {
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
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                    N(SyntaxKind.CloseBracketToken);
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
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambda5()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        var v = [A] () =>
                        {
                            // This attribute, within an expression of a lambda, should be totally fine to parse.  It
                            // is not within an attribute argument itself.
                            var y = [B] () => { };
                        };
                    }
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
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
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
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
                                                N(SyntaxKind.ParameterList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                                N(SyntaxKind.EqualsGreaterThanToken);
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
                                                                    N(SyntaxKind.ParenthesizedLambdaExpression);
                                                                    {
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
                                                            }
                                                        }
                                                        N(SyntaxKind.SemicolonToken);
                                                    }
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambda6()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        // We won't recognize this as a lambda because we'll bail out from default-parameter parsing
                        // when we see the `[` after the `=`.
                        var v = (X x = [A] () => {}) => { };
                    }
                }
                """,
                // (7,20): error CS1026: ) expected
                //         var v = (X x = [A] () => {}) => { };
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "x").WithLocation(7, 20),
                // (7,20): error CS1002: ; expected
                //         var v = (X x = [A] () => {}) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(7, 20),
                // (7,36): error CS1002: ; expected
                //         var v = (X x = [A] () => {}) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(7, 36),
                // (7,36): error CS1513: } expected
                //         var v = (X x = [A] () => {}) => { };
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(7, 36));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
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
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "X");
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.EqualsToken);
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
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.EmptyStatement);
                            {
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithCollectionExpression()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        // We won't recognize this as a lambda because we'll bail out from default-parameter parsing
                        // when we see the `[` after the `=`.
                        var v = (X x = [0]) => { };
                    }
                }
                """,
                // (7,20): error CS1026: ) expected
                //         var v = (X x = [0]) => { };
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "x").WithLocation(7, 20),
                // (7,20): error CS1002: ; expected
                //         var v = (X x = [0]) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(7, 20),
                // (7,27): error CS1002: ; expected
                //         var v = (X x = [0]) => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(7, 27),
                // (7,27): error CS1513: } expected
                //         var v = (X x = [0]) => { };
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(7, 27));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
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
                                        N(SyntaxKind.IdentifierToken, "v");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ParenthesizedExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "X");
                                                }
                                                M(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "0");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.EmptyStatement);
                            {
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73789")]
        public void ParseAttributeWithLambaPathological()
        {
            // Ensure we can parse this pathological lambda attribute code without getting into exponential time.
            int n = 30;
            var builder = new StringBuilder();
            for (int i = 0; i < n; i++)
                builder.Append("[A(");
            for (int i = 0; i < n; i++)
                builder.Append(")]");
            builder.Append("class C { }");

            var tree = ParseTree(builder.ToString(), CSharpParseOptions.Default);
        }
    }
}
