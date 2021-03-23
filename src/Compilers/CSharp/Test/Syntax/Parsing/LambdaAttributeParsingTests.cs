// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
        public void LambdaAttributes_01()
        {
            UsingExpression("f = [A] x => x", TestOptions.Regular9,
                // (1,5): error CS8652: The feature 'lambda attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // f = [A] x => x
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[A]").WithArguments("lambda attributes").WithLocation(1, 5));
            verify();

            UsingExpression("f = [A] x => x", TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "f");
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
                EOF();
            }
        }

        [Fact]
        public void LambdaAttributes_02()
        {
            UsingExpression("f = [A]() => { }", TestOptions.Regular9,
                // (1,5): error CS8652: The feature 'lambda attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // f = [A] x => x
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[A]").WithArguments("lambda attributes").WithLocation(1, 5));
            verify();

            UsingExpression("f = [A]() => { }", TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "f");
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
                EOF();
            }
        }

        [Fact]
        public void LambdaAttributes_03()
        {
            UsingExpression("f = [A(B)]() => { }", TestOptions.RegularPreview);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "f");
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
            EOF();
        }

        [Fact]
        public void LambdaAttributes_04()
        {
            UsingExpression("f = [A, B][C]() => { }", TestOptions.RegularPreview);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "f");
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
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
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

        [Fact]
        public void ParameterAttributes_01()
        {
            UsingExpression("f = ([A] int x) => x", TestOptions.Regular9,
                // (1,6): error CS8652: The feature 'lambda attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // f = ([A] int x) => x
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[A]").WithArguments("lambda attributes").WithLocation(1, 6));
            verify();

            UsingExpression("f = ([A] int x) => x", TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "f");
                    }
                    N(SyntaxKind.EqualsToken);
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
                EOF();
            }
        }

        [Fact]
        public void ParameterAttributes_02()
        {
            UsingExpression("f = ([A] x) => x", TestOptions.Regular9,
                // (1,6): error CS8652: The feature 'lambda attributes' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // f = ([A] int x) => x
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "[A]").WithArguments("lambda attributes").WithLocation(1, 6));
            verify();

            UsingExpression("f = ([A] x) => x", TestOptions.RegularPreview);
            verify();

            void verify()
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "f");
                    }
                    N(SyntaxKind.EqualsToken);
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
                EOF();
            }
        }

        [Fact]
        public void ParameterAttributes_03()
        {
            UsingExpression("f = (int x, [A] int y) => x", TestOptions.RegularPreview);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "f");
                }
                N(SyntaxKind.EqualsToken);
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
            UsingExpression("_ = new B { [A] x => y }", TestOptions.RegularPreview,
                // (1,17): error CS1003: Syntax error, '=' expected
                // _ = new B { [A] x => y }
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments("=", "").WithLocation(1, 17));

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
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
            }
            EOF();
        }

        // Lambda with attribute in collection initializer is similar to dictionary initializer.
        [Fact]
        public void CollectionInitializer_02()
        {
            UsingExpression("_ = new B { ([A] x => y) }", TestOptions.RegularPreview);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
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
            }
            EOF();
        }

        // Lambda with attribute in conditional expression is similar to conditional element access.
        [Fact]
        public void ConditionalExpression_01()
        {
            UsingExpression("_ = x ? [A] () => { } : y", TestOptions.RegularPreview,
                // (1,1): error CS1073: Unexpected token '=>'
                // _ = x ? [A] () => { } : y
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "_ = x ? [A] ()").WithArguments("=>").WithLocation(1, 1));

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.ElementBindingExpression);
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
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
            }
            EOF();
        }

        // Lambda with attribute in conditional expression is similar to conditional element access.
        [Fact]
        public void ConditionalExpression_02()
        {
            UsingExpression("_ = x ? ([A] () => { }) : y", TestOptions.RegularPreview);

            N(SyntaxKind.SimpleAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "_");
                }
                N(SyntaxKind.EqualsToken);
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
            }
        }
    }
}
