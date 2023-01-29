// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CollectionLiteralParsingTests : ParsingTests
    {
        public CollectionLiteralParsingTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.Preview)]
        public void CollectionLiteralParsingDoesNotProduceLangVersionError(LanguageVersion languageVersion)
        {
            UsingExpression("[A, B]", TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionDotAccess()
        {
            UsingTree("_ = [A, B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess()
        {
            UsingTree("[A, B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_GlobalAttributeAmbiguity1()
        {
            UsingTree("[assembly: A, B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.DictionaryElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "assembly");
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_AttributeAmbiguity2A()
        {
            UsingTree("[return: A, B].C();",
                // (1,2): error CS1041: Identifier expected; 'return' is a keyword
                // [return: A, B].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "return").WithArguments("", "return").WithLocation(1, 2));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.DictionaryElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "return");
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_AttributeAmbiguity2B()
        {
            UsingTree("[return: A, B] void F() { }");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "F");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_AttributeAmbiguity3A()
        {
            UsingTree("[method: A, B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.DictionaryElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "method");
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_AttributeAmbiguity3B()
        {
            UsingTree("[method: A, B] void F() { }");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.AttributeTargetSpecifier);
                            {
                                N(SyntaxKind.MethodKeyword);
                                N(SyntaxKind.ColonToken);
                            }
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "F");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_AttributeAmbiguity4A()
        {
            UsingTree("[return: A].C();",
                // (1,2): error CS1041: Identifier expected; 'return' is a keyword
                // [return: A, B].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "return").WithArguments("", "return").WithLocation(1, 2));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.DictionaryElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "return");
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_AttributeAmbiguity4B()
        {
            UsingTree("[return: A] void F() { }");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "F");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelDotAccess_GlobalAttributeAmbiguity2()
        {
            UsingTree("[module: A, B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.DictionaryElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "module");
                                        }
                                        N(SyntaxKind.ColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionNullSafeAccess()
        {
            UsingTree("_ = [A, B]?.C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.MemberBindingExpression);
                                    {
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
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
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelNullSafeAccess()
        {
            UsingTree("[A, B]?.C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ConditionalAccessExpression);
                        {
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.MemberBindingExpression);
                                {
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionPointerAccess()
        {
            UsingTree("_ = [A, B]->C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.PointerMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "A");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.MinusGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelPointerAccess()
        {
            UsingTree("[A, B]->C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.PointerMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.MinusGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AttributeOnTopLevelDotAccessStatement()
        {
            UsingTree("[A] [B].C();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
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
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AttemptToImmediatelyIndexInTopLevelStatement()
        {
            UsingTree(
                """["A", "B"][0].C();""",
                // (1,2): error CS1001: Identifier expected
                // ["A", "B"][0].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"""A""").WithLocation(1, 2),
                // (1,2): error CS1003: Syntax error, ',' expected
                // ["A", "B"][0].C();
                Diagnostic(ErrorCode.ERR_SyntaxError, @"""A""").WithArguments(",").WithLocation(1, 2),
                // (1,7): error CS1001: Identifier expected
                // ["A", "B"][0].C();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"""B""").WithLocation(1, 7));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.Attribute);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
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
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AlwaysParsedAsAttributeInsideNamespace()
        {
            UsingTree("""
                namespace A;
                [B].C();
                """,
                // (2,3): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // [B].C();
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "]").WithLocation(2, 3),
                // (2,4): error CS1022: Type or namespace definition, or end-of-file expected
                // [B].C();
                Diagnostic(ErrorCode.ERR_EOFExpected, ".").WithLocation(2, 4));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FileScopedNamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.SemicolonToken);
                    N(SyntaxKind.ConstructorDeclaration);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionIs()
        {
            UsingTree("_ = [A, B] is [A, B];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.ListPattern);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ConstantPattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ConstantPattern);
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
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionWith()
        {
            UsingTree("_ = [A, B] with { };");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.WithExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.WithKeyword);
                                N(SyntaxKind.WithInitializerExpression);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void ExpressionSwitch()
        {
            UsingTree("_ = [A, B] switch { _ => M() };");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.SwitchExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchExpressionArm);
                                {
                                    N(SyntaxKind.DiscardPattern);
                                    {
                                        N(SyntaxKind.UnderscoreToken);
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "M");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TopLevelSwitch()
        {
            UsingTree(
                "[A, B] switch { _ => M() };",
                // (1,15): error CS1525: Invalid expression term '{'
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 15),
                // (1,15): error CS8515: Parentheses are required around the switch governing expression.
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_SwitchGoverningExpressionRequiresParens, "{").WithLocation(1, 15),
                // (1,17): error CS1513: } expected
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "_").WithLocation(1, 17),
                // (1,26): error CS1002: ; expected
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 26),
                // (1,26): error CS1022: Type or namespace definition, or end-of-file expected
                // [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 26));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.SwitchStatement);
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
                        N(SyntaxKind.SwitchKeyword);
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleLambdaExpression);
                        {
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "M");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void StatementLevelSwitch()
        {
            UsingTree(@"
class C
{
    void M()
    {
        [A, B] switch { _ => M() };
    }
}
",
                // (6,23): error CS1525: Invalid expression term '{'
                //         [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(6, 23),
                // (6,23): error CS8515: Parentheses are required around the switch governing expression.
                //         [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_SwitchGoverningExpressionRequiresParens, "{").WithLocation(6, 23),
                // (6,25): error CS1513: } expected
                //         [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_RbraceExpected, "_").WithLocation(6, 25),
                // (6,34): error CS1002: ; expected
                //         [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(6, 34),
                // (6,35): error CS1597: Semicolon after method or accessor block is not valid
                //         [A, B] switch { _ => M() };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(6, 35),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1));

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
                            N(SyntaxKind.SwitchStatement);
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
                                N(SyntaxKind.SwitchKeyword);
                                M(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                M(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleLambdaExpression);
                                {
                                    N(SyntaxKind.Parameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "_");
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "M");
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
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
        public void BinaryOperator()
        {
            UsingTree("_ = [A, B] + [C, D];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.CollectionCreationExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "D");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void EmptyCollection()
        {
            UsingTree("_ = [];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CollectionOfEmptyCollection()
        {
            UsingTree("_ = [[]];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryOfEmptyCollections()
        {
            UsingTree("_ = [[]: []];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.DictionaryElement);
                                {
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.CollectionCreationExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionarySyntaxMissingKey()
        {
            UsingTree(
                "_ = [:B];",
                // (1,6): error CS1525: Invalid expression term ':'
                // _ = [:B];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(1, 6));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.DictionaryElement);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionarySyntaxMissingValue()
        {
            UsingTree(
                "_ = [A:];",
                // (1,8): error CS1525: Invalid expression term ']'
                // _ = [A:];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "]").WithArguments("]").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.DictionaryElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                    N(SyntaxKind.ColonToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionarySyntaxMissingKeyAndValue()
        {
            UsingTree(
                "_ = [:];",
                // (1,6): error CS1525: Invalid expression term ':'
                // _ = [:];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(1, 6),
                // (1,7): error CS1525: Invalid expression term ']'
                // _ = [:];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "]").WithArguments("]").WithLocation(1, 7));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.DictionaryElement);
                                {
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.ColonToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithTypeExpressions()
        {
            UsingTree("_ = [A::B: C::D];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "_");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.CollectionCreationExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.DictionaryElement);
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
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.AliasQualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                        }
                                        N(SyntaxKind.ColonColonToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "D");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithConditional1()
        {
            UsingExpression("[a ? b : c : d]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithConditional2()
        {
            UsingExpression("[a : b ? c : d]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "d");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithConditional3()
        {
            UsingExpression("[a ? b : c : d ? e : f]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "d");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "f");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithNullCoalesce1()
        {
            UsingExpression("[a ?? b : c]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.CoalesceExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.QuestionQuestionToken);
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
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithNullCoalesce2()
        {
            UsingExpression("[a : b ?? c]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.CoalesceExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.QuestionQuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithNullCoalesce3()
        {
            UsingExpression("[a ?? b : c ?? d]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.CoalesceExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.QuestionQuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.CoalesceExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                        N(SyntaxKind.QuestionQuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "d");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithQuery1()
        {
            UsingExpression("[from x in y select x : c]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.QueryExpression);
                    {
                        N(SyntaxKind.FromClause);
                        {
                            N(SyntaxKind.FromKeyword);
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.InKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.QueryBody);
                        {
                            N(SyntaxKind.SelectClause);
                            {
                                N(SyntaxKind.SelectKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithQuery2()
        {
            UsingExpression("[a : from x in y select x]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.QueryExpression);
                    {
                        N(SyntaxKind.FromClause);
                        {
                            N(SyntaxKind.FromKeyword);
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.InKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.QueryBody);
                        {
                            N(SyntaxKind.SelectClause);
                            {
                                N(SyntaxKind.SelectKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void DictionaryWithQuery3()
        {
            UsingExpression("[from a in b select a : from x in y select x]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.QueryExpression);
                    {
                        N(SyntaxKind.FromClause);
                        {
                            N(SyntaxKind.FromKeyword);
                            N(SyntaxKind.IdentifierToken, "a");
                            N(SyntaxKind.InKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.QueryBody);
                        {
                            N(SyntaxKind.SelectClause);
                            {
                                N(SyntaxKind.SelectKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.QueryExpression);
                    {
                        N(SyntaxKind.FromClause);
                        {
                            N(SyntaxKind.FromKeyword);
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.InKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.QueryBody);
                        {
                            N(SyntaxKind.SelectClause);
                            {
                                N(SyntaxKind.SelectKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void ConditionalAmbiguity1()
        {
            UsingExpression("[a ? [b] : c]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.CollectionCreationExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void ConditionalAmbiguity1A()
        {
            UsingExpression("[A] ? [B] : C");

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.CollectionCreationExpression);
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
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalAmbiguity1B()
        {
            UsingExpression("[A] ? [B] . C");

            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.CollectionCreationExpression);
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
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleMemberAccessExpression);
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
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ConditionalAmbiguity2()
        {
            UsingExpression("[(a ? [b]) : c]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.DictionaryElement);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.ConditionalAccessExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.ElementBindingExpression);
                            {
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity1()
        {
            UsingExpression("(type)[1, 2, 3]");

            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "type");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity2()
        {
            UsingExpression("(ImmutableArray<int>)[1, 2, 3]");

            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "ImmutableArray");
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity3()
        {
            UsingExpression("(Dotted.ImmutableArray<int>)[1, 2, 3]");

            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Dotted");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "ImmutableArray");
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity4()
        {
            UsingExpression("(ColonColon::ImmutableArray<int>)[1, 2, 3]");

            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "ColonColon");
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "ImmutableArray");
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity5()
        {
            UsingExpression("(NotCast())[1, 2, 3]");

            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "NotCast");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.BracketedArgumentList);
                {
                    N(SyntaxKind.OpenBracketToken);
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
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity6()
        {
            UsingExpression("(Not + Cast)[1, 2, 3]");

            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.AddExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Not");
                        }
                        N(SyntaxKind.PlusToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Cast");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.BracketedArgumentList);
                {
                    N(SyntaxKind.OpenBracketToken);
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
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity7()
        {
            UsingExpression("(List<int>?)[1, 2, 3]");

            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.NullableType);
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
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity8()
        {
            UsingExpression("(int[])[1, 2, 3]");

            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void CastVersusIndexAmbiguity9()
        {
            UsingExpression("((int,int)[])[(1,2), (2,3), (3,4)]");

            N(SyntaxKind.CastExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ArrayType);
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
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
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
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.TupleExpression);
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
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.TupleExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "2");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "3");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.TupleExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "3");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
                                }
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
        public void SpreadOfQuery()
        {
            UsingExpression("[.. from x in y select x]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.QueryExpression);
                    {
                        N(SyntaxKind.FromClause);
                        {
                            N(SyntaxKind.FromKeyword);
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.InKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                        N(SyntaxKind.QueryBody);
                        {
                            N(SyntaxKind.SelectClause);
                            {
                                N(SyntaxKind.SelectKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void InvokedCollectionLiteral1()
        {
            UsingExpression("[A, B]()");

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
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
            EOF();
        }

        [Fact]
        public void InvokedCollectionLiteral2()
        {
            UsingExpression("++[A, B]()");

            N(SyntaxKind.PreIncrementExpression);
            {
                N(SyntaxKind.PlusPlusToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.CollectionCreationExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
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
        public void AttributeOnStartOfLambda()
        {
            UsingExpression("[A, B]() =>",
                // (1,12): error CS1733: Expected expression
                // [A, B]() =>
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 12));

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
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestTrailingComma1()
        {
            UsingExpression("[A,]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestTrailingComma2()
        {
            UsingExpression("[A,B,]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestTrailingComma3()
        {
            UsingExpression("[A,B,,]",
                // (1,6): error CS1525: Invalid expression term ','
                // [A,B,,]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 6));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.ExpressionElement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }


        [Fact]
        public void TestTrailingComma4()
        {
            UsingExpression("[A,B,,,]",
                // (1,6): error CS1525: Invalid expression term ','
                // [A,B,,,]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 6),
                // (1,7): error CS1525: Invalid expression term ','
                // [A,B,,,]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 7));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.ExpressionElement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.ExpressionElement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestNegatedLiteral()
        {
            UsingExpression("-[A]");

            N(SyntaxKind.UnaryMinusExpression);
            {
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.CollectionCreationExpression);
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
            EOF();
        }

        [Fact]
        public void TestNullCoalescing1()
        {
            UsingExpression("[A] ?? [B]");

            N(SyntaxKind.CoalesceExpression);
            {
                N(SyntaxKind.CollectionCreationExpression);
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
                N(SyntaxKind.QuestionQuestionToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestNullCoalescing2()
        {
            UsingExpression("[..x ?? y]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.CoalesceExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.QuestionQuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestNullSuppression()
        {
            UsingExpression("[A]!");

            N(SyntaxKind.SuppressNullableWarningExpression);
            {
                N(SyntaxKind.CollectionCreationExpression);
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
                N(SyntaxKind.ExclamationToken);
            }
            EOF();
        }

        [Fact]
        public void TestPreIncrement()
        {
            UsingExpression("++[A]");

            N(SyntaxKind.PreIncrementExpression);
            {
                N(SyntaxKind.PlusPlusToken);
                N(SyntaxKind.CollectionCreationExpression);
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
            EOF();
        }

        [Fact]
        public void TestPostIncrement()
        {
            UsingExpression("[A]++");

            N(SyntaxKind.PostIncrementExpression);
            {
                N(SyntaxKind.CollectionCreationExpression);
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
                N(SyntaxKind.PlusPlusToken);
            }
            EOF();
        }

        [Fact]
        public void TestAwaitParsedAsElementAccess()
        {
            UsingExpression("await [A]");

            N(SyntaxKind.ElementAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "await");
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
            EOF();
        }

        [Fact]
        public void TestAwaitParsedAsElementAccessTopLevel()
        {
            UsingTree("await [A];");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AwaitExpression);
                        {
                            N(SyntaxKind.AwaitKeyword);
                            N(SyntaxKind.CollectionCreationExpression);
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
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestAwaitInAsyncContext()
        {
            UsingTree(@"
class C
{
    async void F()
    {
        await [A];
    }
}");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "F");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.CollectionCreationExpression);
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

        [Fact]
        public void TestAwaitInNonAsyncContext()
        {
            UsingTree(@"
class C
{
    void F()
    {
        await [A];
    }
}");

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
                        N(SyntaxKind.IdentifierToken, "F");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
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

        [Fact]
        public void TestSimpleSpread()
        {
            UsingExpression("[..e]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestSpreadOfRange1()
        {
            UsingExpression("[.. ..]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestSpreadOfRange2()
        {
            UsingExpression("[.. ..e]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestSpreadOfRange3()
        {
            UsingExpression("[.. e..]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.DotDotToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestSpreadOfRange4()
        {
            UsingExpression("[.. e1..e2]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e1");
                        }
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e2");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestThrowExpression()
        {
            UsingExpression("[..throw e]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.ThrowExpression);
                    {
                        N(SyntaxKind.ThrowKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestMemberAccess()
        {
            UsingExpression("[..x.y]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestAssignment()
        {
            UsingExpression("[..x = y]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
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
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestLambda()
        {
            UsingExpression("[..x => y]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
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
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestConditional()
        {
            UsingExpression("[..x ? y : z]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "z");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestPartialRange()
        {
            UsingExpression("[..e..]");

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.DotDotToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestNewArray1()
        {
            UsingExpression("new T?[1]");

            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void TestNewArray2()
        {
            UsingExpression("new T?[1] { }");

            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestNewArray3()
        {
            UsingExpression("new T[]?[1]");

            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
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
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void TestNewArray4()
        {
            UsingExpression("new T[]?[1] { }");

            N(SyntaxKind.ArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.NullableType);
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
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.ArrayRankSpecifier);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void TestNewArray5()
        {
            UsingExpression("new T[]?[1].Length");

            N(SyntaxKind.SimpleMemberAccessExpression);
            {
                N(SyntaxKind.ArrayCreationExpression);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.NullableType);
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
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.ArrayRankSpecifier);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Length");
                }
            }
            EOF();
        }

        [Fact]
        public void TestError1()
        {
            UsingExpression("[,]",
                // (1,2): error CS1525: Invalid expression term ','
                // [,]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 2));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                M(SyntaxKind.ExpressionElement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestError2()
        {
            UsingExpression("[,A]",
                // (1,2): error CS1525: Invalid expression term ','
                // [,A]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 2));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                M(SyntaxKind.ExpressionElement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
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
        public void TestError3()
        {
            UsingExpression("[,,]",
                // (1,2): error CS1525: Invalid expression term ','
                // [,,]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 2),
                // (1,3): error CS1525: Invalid expression term ','
                // [,,]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 3));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                M(SyntaxKind.ExpressionElement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.ExpressionElement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestError4()
        {
            UsingExpression("[..]",
                // (1,4): error CS1525: Invalid expression term ']'
                // [..]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "]").WithArguments("]").WithLocation(1, 4));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestError5()
        {
            UsingExpression("[...e]",
                // (1,2): error CS8635: Unexpected character sequence '...'
                // [...e]
                Diagnostic(ErrorCode.ERR_TripleDotNotAllowed, "").WithLocation(1, 2),
                // (1,4): error CS1525: Invalid expression term '.'
                // [...e]
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ".").WithArguments(".").WithLocation(1, 4));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void TestError6()
        {
            UsingExpression("[....]",
                // (1,2): error CS8635: Unexpected character sequence '...'
                // [....]
                Diagnostic(ErrorCode.ERR_TripleDotNotAllowed, "").WithLocation(1, 2));

            N(SyntaxKind.CollectionCreationExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.SpreadElement);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }

        [Fact]
        public void GenericNameWithBrackets1()
        {
            UsingExpression("A < B?[] > D",
                // (1,1): error CS1073: Unexpected token 'D'
                // A < B?[] > D
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "A < B?[] >").WithArguments("D").WithLocation(1, 1));

            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "A");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
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
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            EOF();
        }

        [Fact]
        public void GenericNameWithBrackets2()
        {
            UsingStatement("A < B?[] > D",
                // (1,13): error CS1002: ; expected
                // A < B?[] > D
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 13));

            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.NullableType);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
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
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void GenericNameWithBrackets3()
        {
            UsingExpression("nameof(A < B?[] > D)",
                // (1,19): error CS1003: Syntax error, ',' expected
                // nameof(A < B?[] > D)
                Diagnostic(ErrorCode.ERR_SyntaxError, "D").WithArguments(",").WithLocation(1, 19));

            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "nameof");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.ArrayType);
                                {
                                    N(SyntaxKind.NullableType);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
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
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void GenericNameWithBrackets4()
        {
            UsingExpression("typeof(A < B?[] > D)",
                // (1,1): error CS1073: Unexpected token 'D'
                // typeof(A < B?[] > D)
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "typeof(A < B?[] > ").WithArguments("D").WithLocation(1, 1),
                // (1,19): error CS1026: ) expected
                // typeof(A < B?[] > D)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "D").WithLocation(1, 19));

            N(SyntaxKind.TypeOfExpression);
            {
                N(SyntaxKind.TypeOfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
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
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                M(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void GenericNameWithBrackets5()
        {
            UsingExpression("default(A < B?[] > D)",
                // (1,1): error CS1073: Unexpected token 'D'
                // default(A < B?[] > D)
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "default(A < B?[] > ").WithArguments("D").WithLocation(1, 1),
                // (1,20): error CS1026: ) expected
                // default(A < B?[] > D)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "D").WithLocation(1, 20));

            N(SyntaxKind.DefaultExpression);
            {
                N(SyntaxKind.DefaultKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.TypeArgumentList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
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
                        N(SyntaxKind.GreaterThanToken);
                    }
                }
                M(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void Interpolation1()
        {
            UsingExpression(""" $"{[A:B]}" """);

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.Interpolation);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CollectionCreationExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.DictionaryElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Interpolation2()
        {
            UsingExpression(""" $"{[:]}" """,
                // (1,6): error CS1525: Invalid expression term ':'
                //  $"{[:]}" 
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(1, 6),
                // (1,7): error CS1525: Invalid expression term ']'
                //  $"{[:]}" 
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "]").WithArguments("]").WithLocation(1, 7));

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.Interpolation);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CollectionCreationExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.DictionaryElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.ColonToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void Addressof1()
        {
            UsingExpression("&[A]");

            N(SyntaxKind.AddressOfExpression);
            {
                N(SyntaxKind.AmpersandToken);
                N(SyntaxKind.CollectionCreationExpression);
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
            EOF();
        }

        [Fact]
        public void Addressof2()
        {
            UsingExpression("&[A, B]");

            N(SyntaxKind.AddressOfExpression);
            {
                N(SyntaxKind.AmpersandToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Addressof3()
        {
            UsingExpression("&[A, B][C]");

            N(SyntaxKind.AddressOfExpression);
            {
                N(SyntaxKind.AmpersandToken);
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.CollectionCreationExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void Deref1()
        {
            UsingExpression("*[]");

            N(SyntaxKind.PointerIndirectionExpression);
            {
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Deref2()
        {
            UsingExpression("*[A]");

            N(SyntaxKind.PointerIndirectionExpression);
            {
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.CollectionCreationExpression);
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
            EOF();
        }

        [Fact]
        public void Deref3()
        {
            UsingExpression("*[A, B]");

            N(SyntaxKind.PointerIndirectionExpression);
            {
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.CollectionCreationExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Deref4()
        {
            UsingExpression("*[A, B][C]");

            N(SyntaxKind.PointerIndirectionExpression);
            {
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.CollectionCreationExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void New1()
        {
            UsingExpression("new [A]",
                // (1,6): error CS0178: Invalid rank specifier: expected ',' or ']'
                // new [A]
                Diagnostic(ErrorCode.ERR_InvalidArray, "A").WithLocation(1, 6),
                // (1,8): error CS1514: { expected
                // new [A]
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 8),
                // (1,8): error CS1513: } expected
                // new [A]
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 8));

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
        public void New2()
        {
            UsingExpression("new [A, B]",
                // (1,6): error CS0178: Invalid rank specifier: expected ',' or ']'
                // new [A, B]
                Diagnostic(ErrorCode.ERR_InvalidArray, "A").WithLocation(1, 6),
                // (1,9): error CS0178: Invalid rank specifier: expected ',' or ']'
                // new [A, B]
                Diagnostic(ErrorCode.ERR_InvalidArray, "B").WithLocation(1, 9),
                // (1,11): error CS1514: { expected
                // new [A, B]
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 11),
                // (1,11): error CS1513: } expected
                // new [A, B]
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 11));

            N(SyntaxKind.ImplicitArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CommaToken);
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
        public void New3()
        {
            UsingExpression("new [A, B][C]",
                // (1,6): error CS0178: Invalid rank specifier: expected ',' or ']'
                // new [A, B][C]
                Diagnostic(ErrorCode.ERR_InvalidArray, "A").WithLocation(1, 6),
                // (1,9): error CS0178: Invalid rank specifier: expected ',' or ']'
                // new [A, B][C]
                Diagnostic(ErrorCode.ERR_InvalidArray, "B").WithLocation(1, 9),
                // (1,11): error CS1514: { expected
                // new [A, B][C]
                Diagnostic(ErrorCode.ERR_LbraceExpected, "[").WithLocation(1, 11),
                // (1,14): error CS1513: } expected
                // new [A, B][C]
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 14));

            N(SyntaxKind.ImplicitArrayCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.CloseBracketToken);
                N(SyntaxKind.ArrayInitializerExpression);
                {
                    M(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CollectionCreationExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }
    }
}
