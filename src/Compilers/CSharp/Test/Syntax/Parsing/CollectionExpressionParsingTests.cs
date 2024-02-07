// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class CollectionExpressionParsingTests : ParsingTests
{
    public CollectionExpressionParsingTests(ITestOutputHelper output) : base(output) { }

    [Theory]
    [InlineData(LanguageVersion.CSharp11)]
    [InlineData(LanguageVersion.Preview)]
    public void CollectionExpressionParsingDoesNotProduceLangVersionError(LanguageVersion languageVersion)
    {
        UsingExpression("[A, B]", TestOptions.Regular.WithLanguageVersion(languageVersion));

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
        UsingTree("[assembly: A, B].C();",
            // (1,10): error CS1003: Syntax error, ',' expected
            // [assembly: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 10),
            // (1,12): error CS1003: Syntax error, ',' expected
            // [assembly: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "A").WithArguments(",").WithLocation(1, 12));

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
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "assembly");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
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
    public void TopLevelDotAccess_AttributeAmbiguity2A()
    {
        UsingTree("[return: A, B].C();",
            // (1,2): error CS1003: Syntax error, ']' expected
            // [return: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "return").WithArguments("]").WithLocation(1, 2),
            // (1,2): error CS1002: ; expected
            // [return: A, B].C();
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "return").WithLocation(1, 2),
            // (1,8): error CS1525: Invalid expression term ':'
            // [return: A, B].C();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(1, 8),
            // (1,8): error CS1002: ; expected
            // [return: A, B].C();
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 8),
            // (1,8): error CS1022: Type or namespace definition, or end-of-file expected
            // [return: A, B].C();
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 8),
            // (1,11): error CS1001: Identifier expected
            // [return: A, B].C();
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 11),
            // (1,14): error CS1003: Syntax error, ',' expected
            // [return: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "]").WithArguments(",").WithLocation(1, 14));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ReturnStatement);
                {
                    N(SyntaxKind.ReturnKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
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
        UsingTree("[method: A, B].C();",
            // (1,8): error CS1003: Syntax error, ',' expected
            // [method: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 8),
            // (1,10): error CS1003: Syntax error, ',' expected
            // [method: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "A").WithArguments(",").WithLocation(1, 10));

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
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "method");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
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
            // (1,2): error CS1003: Syntax error, ']' expected
            // [return: A].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "return").WithArguments("]").WithLocation(1, 2),
            // (1,2): error CS1002: ; expected
            // [return: A].C();
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "return").WithLocation(1, 2),
            // (1,8): error CS1525: Invalid expression term ':'
            // [return: A].C();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(1, 8),
            // (1,8): error CS1002: ; expected
            // [return: A].C();
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 8),
            // (1,8): error CS1022: Type or namespace definition, or end-of-file expected
            // [return: A].C();
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 8),
            // (1,11): error CS1001: Identifier expected
            // [return: A].C();
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "]").WithLocation(1, 11),
            // (1,11): error CS1003: Syntax error, ',' expected
            // [return: A].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "]").WithArguments(",").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.CloseBracketToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ReturnStatement);
                {
                    N(SyntaxKind.ReturnKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
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
        UsingTree("[module: A, B].C();",
            // (1,8): error CS1003: Syntax error, ',' expected
            // [module: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 8),
            // (1,10): error CS1003: Syntax error, ',' expected
            // [module: A, B].C();
            Diagnostic(ErrorCode.ERR_SyntaxError, "A").WithArguments(",").WithLocation(1, 10));

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
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "module");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
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
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.ElementAccessExpression);
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
            """["A", "B"][0].C();""");

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
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.StringLiteralExpression);
                                        {
                                            N(SyntaxKind.StringLiteralToken, "\"A\"");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.StringLiteralExpression);
                                        {
                                            N(SyntaxKind.StringLiteralToken, "\"B\"");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
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
            "[A, B] switch { _ => M() };");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SwitchExpression);
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
        UsingTree("""
            class C
            {
                void M()
                {
                    [A, B] switch { _ => M() };
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SwitchExpression);
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
                            N(SyntaxKind.CollectionExpression);
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
                        N(SyntaxKind.CollectionExpression);
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
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.CollectionExpression);
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
        UsingTree("_ = [[]: []];",
            // (1,8): error CS1003: Syntax error, ',' expected
            // _ = [[]: []];
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 8),
            // (1,10): error CS1003: Syntax error, ',' expected
            // _ = [[]: []];
            Diagnostic(ErrorCode.ERR_SyntaxError, "[").WithArguments(",").WithLocation(1, 10));

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
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.CollectionExpression);
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
            // (1,6): error CS1001: Identifier expected
            // _ = [:B];
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 6));

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
                        N(SyntaxKind.CollectionExpression);
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
            // (1,7): error CS1003: Syntax error, ',' expected
            // _ = [A:];
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 7));

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
            // (1,6): error CS1001: Identifier expected
            // _ = [:];
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 6));

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
                        N(SyntaxKind.CollectionExpression);
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
    public void DictionaryWithTypeExpressions()
    {
        UsingTree("_ = [A::B: C::D];",
            // (1,10): error CS1003: Syntax error, ',' expected
            // _ = [A::B: C::D];
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 10),
            // (1,12): error CS1003: Syntax error, ',' expected
            // _ = [A::B: C::D];
            Diagnostic(ErrorCode.ERR_SyntaxError, "C").WithArguments(",").WithLocation(1, 12));

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
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
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
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.ExpressionElement);
                            {
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
        UsingExpression("[a ? b : c : d]",
            // (1,12): error CS1003: Syntax error, ',' expected
            // [a ? b : c : d]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 12),
            // (1,14): error CS1003: Syntax error, ',' expected
            // [a ? b : c : d]
            Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(",").WithLocation(1, 14));

        N(SyntaxKind.CollectionExpression);
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
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
        UsingExpression("[a : b ? c : d]",
            // (1,4): error CS1003: Syntax error, ',' expected
            // [a : b ? c : d]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 4),
            // (1,6): error CS1003: Syntax error, ',' expected
            // [a : b ? c : d]
            Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(1, 6));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
        UsingExpression("[a ? b : c : d ? e : f]",
            // (1,12): error CS1003: Syntax error, ',' expected
            // [a ? b : c : d ? e : f]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 12),
            // (1,14): error CS1003: Syntax error, ',' expected
            // [a ? b : c : d ? e : f]
            Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(",").WithLocation(1, 14));

        N(SyntaxKind.CollectionExpression);
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
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
        UsingExpression("[a ?? b : c]",
            // (1,9): error CS1003: Syntax error, ',' expected
            // [a ?? b : c]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 9),
            // (1,11): error CS1003: Syntax error, ',' expected
            // [a ?? b : c]
            Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(1, 11));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
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
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
        UsingExpression("[a : b ?? c]",
            // (1,4): error CS1003: Syntax error, ',' expected
            // [a : b ?? c]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 4),
            // (1,6): error CS1003: Syntax error, ',' expected
            // [a : b ?? c]
            Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(1, 6));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
        UsingExpression("[a ?? b : c ?? d]",
            // (1,9): error CS1003: Syntax error, ',' expected
            // [a ?? b : c ?? d]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 9),
            // (1,11): error CS1003: Syntax error, ',' expected
            // [a ?? b : c ?? d]
            Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(1, 11));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
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
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
        UsingExpression("[from x in y select x : c]",
            // (1,23): error CS1003: Syntax error, ',' expected
            // [from x in y select x : c]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 23),
            // (1,25): error CS1003: Syntax error, ',' expected
            // [from x in y select x : c]
            Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(1, 25));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
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
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
        UsingExpression("[a : from x in y select x]",
            // (1,4): error CS1003: Syntax error, ',' expected
            // [a : from x in y select x]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 4),
            // (1,6): error CS1003: Syntax error, ',' expected
            // [a : from x in y select x]
            Diagnostic(ErrorCode.ERR_SyntaxError, "from").WithArguments(",").WithLocation(1, 6));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
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
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Fact]
    public void DictionaryWithQuery3()
    {
        UsingExpression("[from a in b select a : from x in y select x]",
            // (1,23): error CS1003: Syntax error, ',' expected
            // [from a in b select a : from x in y select x]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 23),
            // (1,25): error CS1003: Syntax error, ',' expected
            // [from a in b select a : from x in y select x]
            Diagnostic(ErrorCode.ERR_SyntaxError, "from").WithArguments(",").WithLocation(1, 25));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
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
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
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
            }
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity1()
    {
        UsingExpression("[a ? [b] : c]");

        N(SyntaxKind.CollectionExpression);
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
                    N(SyntaxKind.CollectionExpression);
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
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.CollectionExpression);
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
        UsingExpression("[(a ? [b]) : c]",
            // (1,12): error CS1003: Syntax error, ',' expected
            // [(a ? [b]) : c]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 12),
            // (1,14): error CS1003: Syntax error, ',' expected
            // [(a ? [b]) : c]
            Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(1, 14));

        N(SyntaxKind.CollectionExpression);
        {
            N(SyntaxKind.OpenBracketToken);
            N(SyntaxKind.ExpressionElement);
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
            }
            M(SyntaxKind.CommaToken);
            N(SyntaxKind.ExpressionElement);
            {
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
    public void ConditionalAmbiguity3()
    {
        UsingExpression("a ? [b] : c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.CollectionExpression);
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
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity3A()
    {
        UsingExpression("a ? [b].M() : c");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.CollectionExpression);
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
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "M");
                    }
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
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
    public void ConditionalAmbiguity4()
    {
        UsingExpression("a ? b?[c] : d");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity4A()
    {
        UsingExpression("a ? b?[c].M() : d");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InvocationExpression);
                {
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
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity5()
    {
        UsingExpression("a ? b ? [c] : d : e");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "e");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity5A()
    {
        UsingExpression("a ? b ? [c].M() : d : e");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "e");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity6()
    {
        UsingExpression("a?[c] ? b : d");

        N(SyntaxKind.ConditionalExpression);
        {
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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity6A()
    {
        UsingExpression("a?[c].M() ? b : d");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.InvocationExpression);
                {
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
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "b");
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity7()
    {
        UsingExpression("a?[c] ? b : d : e");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "e");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity7A()
    {
        UsingExpression("a?[c].M() ? b : d : e");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "M");
                        }
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "d");
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "e");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity8()
    {
        UsingExpression("a ? b?[() => { var v = x ? [y] : z; }] : d");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ElementBindingExpression);
                {
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
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
                                                    N(SyntaxKind.ConditionalExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "x");
                                                        }
                                                        N(SyntaxKind.QuestionToken);
                                                        N(SyntaxKind.CollectionExpression);
                                                        {
                                                            N(SyntaxKind.OpenBracketToken);
                                                            N(SyntaxKind.ExpressionElement);
                                                            {
                                                                N(SyntaxKind.IdentifierName);
                                                                {
                                                                    N(SyntaxKind.IdentifierToken, "y");
                                                                }
                                                            }
                                                            N(SyntaxKind.CloseBracketToken);
                                                        }
                                                        N(SyntaxKind.ColonToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "z");
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
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity9()
    {
        UsingExpression("a ? b?[delegate { var v = x ? [y] : z; }] : d");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ElementBindingExpression);
                {
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.AnonymousMethodExpression);
                            {
                                N(SyntaxKind.DelegateKeyword);
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
                                                    N(SyntaxKind.ConditionalExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "x");
                                                        }
                                                        N(SyntaxKind.QuestionToken);
                                                        N(SyntaxKind.CollectionExpression);
                                                        {
                                                            N(SyntaxKind.OpenBracketToken);
                                                            N(SyntaxKind.ExpressionElement);
                                                            {
                                                                N(SyntaxKind.IdentifierName);
                                                                {
                                                                    N(SyntaxKind.IdentifierToken, "y");
                                                                }
                                                            }
                                                            N(SyntaxKind.CloseBracketToken);
                                                        }
                                                        N(SyntaxKind.ColonToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "z");
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
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity10()
    {
        UsingExpression("a ? b?[() => x ? [y] : z] : d");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ElementBindingExpression);
                {
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.ConditionalExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.QuestionToken);
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.ColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "z");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "d");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity11()
    {
        UsingExpression("a ? b?[c] : d ? e?[f] : g");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
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
                N(SyntaxKind.ConditionalAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
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
                                    N(SyntaxKind.IdentifierToken, "f");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "g");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity12()
    {
        UsingExpression("a ? b?[c] : d ? e ? f?[g] : h",
            // (1,30): error CS1003: Syntax error, ':' expected
            // a ? b?[c] : d ? e ? f?[g] : h
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 30),
            // (1,30): error CS1733: Expected expression
            // a ? b?[c] : d ? e ? f?[g] : h
            Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 30));

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
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
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "f");
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
                                        N(SyntaxKind.IdentifierToken, "g");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "h");
                    }
                }
                M(SyntaxKind.ColonToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity12A()
    {
        UsingExpression("a ? b?[c] : d ? e ? f?[g] : h : i");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
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
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "f");
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
                                        N(SyntaxKind.IdentifierToken, "g");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "h");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "i");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity13()
    {
        UsingExpression("a ? b?[c] : d ? e ? f?[g] : h : i : j");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalAccessExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
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
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "f");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "g");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "h");
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "i");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "j");
                }
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity14()
    {
        UsingExpression("a ? b?[c] : d ? e ? f?[g] : h : i : j : k");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "f");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "g");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "h");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "j");
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "k");
            }
        }
        EOF();
    }

    [Fact]
    public void ConditionalAmbiguity15()
    {
        UsingExpression("a ? b?[c] : d ? e ? f?[g] : h : i : j : k : m",
            // (1,1): error CS1073: Unexpected token ':'
            // a ? b?[c] : d ? e ? f?[g] : h : i : j : k : m
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "a ? b?[c] : d ? e ? f?[g] : h : i : j : k").WithArguments(":").WithLocation(1, 1));

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "a");
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.ConditionalExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "f");
                            }
                            N(SyntaxKind.QuestionToken);
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "g");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.ColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "h");
                            }
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "j");
                    }
                }
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "k");
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity1()
    {
        // As a non-generic expression, we assume that `(type)` is an expression, and we are indexing into it.
        UsingExpression("(type)[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "type");
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
            N(SyntaxKind.CollectionExpression);
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
            N(SyntaxKind.CollectionExpression);
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
            N(SyntaxKind.CollectionExpression);
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
            N(SyntaxKind.CollectionExpression);
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
            N(SyntaxKind.CollectionExpression);
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
            N(SyntaxKind.CollectionExpression);
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
    public void CastVersusIndexAmbiguity10()
    {
        UsingExpression("((A, B))[1, 2, 3]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.TupleType);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.TupleElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
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
    public void CastVersusIndexAmbiguity11()
    {
        UsingExpression("((A))[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.CloseParenToken);
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
    public void CastVersusIndexAmbiguity12()
    {
        UsingExpression("(int[]?)[1, 2, 3]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
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
    public void CastVersusIndexAmbiguity13()
    {
        UsingExpression("(int?[])[1, 2, 3]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ArrayType);
            {
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
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
    public void CastVersusIndexAmbiguity14()
    {
        // Parenthesizing RHS should make this a cast.
        UsingExpression("(type)([1, 2, 3])");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "type");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CollectionExpression);
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
                N(SyntaxKind.CloseParenToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity15()
    {
        UsingExpression("(alias::type)[1, 2, 3]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.AliasQualifiedName);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "alias");
                }
                N(SyntaxKind.ColonColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "type");
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
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
    public void CastVersusIndexAmbiguity16()
    {
        // something that starts looking like an array, but isn't.
        UsingExpression("(a[b])[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
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
    public void CastVersusIndexAmbiguity17()
    {
        // Something that starts looking nullable, but isn't.
        UsingExpression("(a ? b : c)[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
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
    public void CastVersusIndexAmbiguity18()
    {
        // something that starts looking like a pointer, but isn't.
        UsingExpression("(a * b)[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.MultiplyExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
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
    public void CastVersusIndexAmbiguity19()
    {
        // something that starts looking generic, but isn't.
        UsingExpression("(a < b > c)[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.GreaterThanExpression);
                {
                    N(SyntaxKind.LessThanExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
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
    public void CastVersusIndexAmbiguity20()
    {
        UsingExpression("(alias::type.member)[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.AliasQualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "alias");
                        }
                        N(SyntaxKind.ColonColonToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "type");
                        }
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "member");
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
    public void CastVersusIndexAmbiguity21()
    {
        UsingExpression("(alias::type<int>)[1, 2, 3]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.AliasQualifiedName);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "alias");
                }
                N(SyntaxKind.ColonColonToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "type");
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
            N(SyntaxKind.CollectionExpression);
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
    public void CastVersusIndexAmbiguity22()
    {
        UsingExpression("(alias::type.type2<int>)[1, 2, 3]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.QualifiedName);
            {
                N(SyntaxKind.AliasQualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "alias");
                    }
                    N(SyntaxKind.ColonColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "type");
                    }
                }
                N(SyntaxKind.DotToken);
                N(SyntaxKind.GenericName);
                {
                    N(SyntaxKind.IdentifierToken, "type2");
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
            N(SyntaxKind.CollectionExpression);
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
    public void CastVersusIndexAmbiguity23()
    {
        UsingExpression("(A[])[0]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ArrayType);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
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
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity24_A()
    {
        UsingExpression("(A)[]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "A");
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity24_B()
    {
        // No errors here syntactically.  But user will likely get one semantically.
        // We may want a dedicated error to tell them to parenthesize the brackets if they're trying to cast this as a list.
        UsingExpression("(A)[1]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
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
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity24_C()
    {
        // This is not a great diagnostic.  Users could easily run into this and be confused.  Can we do
        // better. For example:
        //
        // 1. tell them we think this is an indexer, and `1:2` isn't a valid argument.
        // 2. tell them to parenthesize the brackets if they're trying to cast this as a list.
        UsingExpression("(A)[1:2]",
            // (1,6): error CS1003: Syntax error, ',' expected
            // (A)[1:2]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 6),
            // (1,7): error CS1003: Syntax error, ',' expected
            // (A)[1:2]
            Diagnostic(ErrorCode.ERR_SyntaxError, "2").WithArguments(",").WithLocation(1, 7));

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
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
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity24_D()
    {
        // No errors here syntactically.  But user will likely get one semantically.  Specifically, this
        // could look like a case of a collection expression with a spread element in it, or as indexing into a
        // parenthesized expression with a range expression.
        //
        // We may want a dedicated error to tell them to parenthesize the brackets if they're trying to cast this as a list.
        UsingExpression("(A)[..B]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.BracketedArgumentList);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity25()
    {
        UsingExpression("(A[])[]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.ArrayType);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
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
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity26()
    {
        UsingExpression("((int, int))[]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
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
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity27()
    {
        UsingExpression("(a < b > . c)[1, 2, 3]");

        N(SyntaxKind.ElementAccessExpression);
        {
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.SimpleMemberAccessExpression);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
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
    public void CastVersusIndexAmbiguity28()
    {
        UsingExpression("(A<>)[]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "A");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.OmittedTypeArgument);
                    {
                        N(SyntaxKind.OmittedTypeArgumentToken);
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity29()
    {
        UsingExpression("(A<,>)[]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "A");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.OmittedTypeArgument);
                    {
                        N(SyntaxKind.OmittedTypeArgumentToken);
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.OmittedTypeArgument);
                    {
                        N(SyntaxKind.OmittedTypeArgumentToken);
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void CastVersusIndexAmbiguity30()
    {
        UsingExpression("(ImmutableArray<List<Int32>>)[[1]]");

        N(SyntaxKind.CastExpression);
        {
            N(SyntaxKind.OpenParenToken);
            N(SyntaxKind.GenericName);
            {
                N(SyntaxKind.IdentifierToken, "ImmutableArray");
                N(SyntaxKind.TypeArgumentList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "List");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Int32");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
            }
            N(SyntaxKind.CloseParenToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.CollectionExpression);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.ExpressionElement);
                        {
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "1");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69508")]
    public void CastVersusIndexAmbiguity31()
    {
        UsingStatement("var x = (A<B>)[1];");

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
                    N(SyntaxKind.IdentifierToken, "x");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.CastExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
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
                            N(SyntaxKind.CloseParenToken);
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
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
                }
            }
            N(SyntaxKind.SemicolonToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69508")]
    public void CastVersusIndexAmbiguity31_GlobalStatement()
    {
        UsingTree("var x = (A<B>)[1];");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
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
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.CastExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
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
                                    N(SyntaxKind.CloseParenToken);
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
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
    public void SpreadOfQuery()
    {
        UsingExpression("[.. from x in y select x]");

        N(SyntaxKind.CollectionExpression);
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
    public void InvokedCollectionExpression1()
    {
        UsingExpression("[A, B]()");

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
    public void InvokedCollectionExpression2()
    {
        UsingExpression("++[A, B]()");

        N(SyntaxKind.PreIncrementExpression);
        {
            N(SyntaxKind.PlusPlusToken);
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
    public void TestTrailingComma1()
    {
        UsingExpression("[A,]");

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
            N(SyntaxKind.CommaToken);
            N(SyntaxKind.CloseBracketToken);
        }
        EOF();
    }

    [Fact]
    public void TestTrailingComma2()
    {
        UsingExpression("[A,B,]");

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
        EOF();
    }

    [Fact]
    public void TestNullCoalescing1()
    {
        UsingExpression("[A] ?? [B]");

        N(SyntaxKind.CoalesceExpression);
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
            N(SyntaxKind.QuestionQuestionToken);
            N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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
        EOF();
    }

    [Fact]
    public void TestPostIncrement()
    {
        UsingExpression("[A]++");

        N(SyntaxKind.PostIncrementExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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

        N(SyntaxKind.CollectionExpression);
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
    public void GenericNameWithBrackets6()
    {
        UsingExpression("A < B?[] : D");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.LessThanExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                N(SyntaxKind.LessThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "B");
                }
            }
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.CloseBracketToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "D");
            }
        }
        EOF();
    }

    [Fact]
    public void Interpolation1()
    {
        UsingExpression(""" $"{[A:B]}" """,
            // (1,7): error CS1003: Syntax error, ',' expected
            //  $"{[A:B]}" 
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 7),
            // (1,8): error CS1003: Syntax error, ',' expected
            //  $"{[A:B]}" 
            Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(1, 8));

        N(SyntaxKind.InterpolatedStringExpression);
        {
            N(SyntaxKind.InterpolatedStringStartToken);
            N(SyntaxKind.Interpolation);
            {
                N(SyntaxKind.OpenBraceToken);
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
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ExpressionElement);
                    {
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
            // (1,6): error CS1001: Identifier expected
            //  $"{[:]}" 
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ":").WithLocation(1, 6));

        N(SyntaxKind.InterpolatedStringExpression);
        {
            N(SyntaxKind.InterpolatedStringStartToken);
            N(SyntaxKind.Interpolation);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
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
        EOF();
    }

    [Fact]
    public void Addressof2()
    {
        UsingExpression("&[A, B]");

        N(SyntaxKind.AddressOfExpression);
        {
            N(SyntaxKind.AmpersandToken);
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
    public void Addressof4()
    {
        UsingExpression("&[A:B]",
            // (1,4): error CS1003: Syntax error, ',' expected
            // &[A:B]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 4),
            // (1,5): error CS1003: Syntax error, ',' expected
            // &[A:B]
            Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(1, 5));

        N(SyntaxKind.AddressOfExpression);
        {
            N(SyntaxKind.AmpersandToken);
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
                M(SyntaxKind.CommaToken);
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
    public void Deref1()
    {
        UsingExpression("*[]");

        N(SyntaxKind.PointerIndirectionExpression);
        {
            N(SyntaxKind.AsteriskToken);
            N(SyntaxKind.CollectionExpression);
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
        EOF();
    }

    [Fact]
    public void Deref3()
    {
        UsingExpression("*[A, B]");

        N(SyntaxKind.PointerIndirectionExpression);
        {
            N(SyntaxKind.AsteriskToken);
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
    public void Deref5()
    {
        UsingExpression("*[A:B]",
            // (1,4): error CS1003: Syntax error, ',' expected
            // *[A:B]
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 4),
            // (1,5): error CS1003: Syntax error, ',' expected
            // *[A:B]
            Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(1, 5));

        N(SyntaxKind.PointerIndirectionExpression);
        {
            N(SyntaxKind.AsteriskToken);
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
                M(SyntaxKind.CommaToken);
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
                N(SyntaxKind.CollectionExpression);
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

    [Fact]
    public void LiteralContainingLambda1()
    {
        UsingExpression("_ = [Main, () => { }]");

        N(SyntaxKind.SimpleAssignmentExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "_");
            }
            N(SyntaxKind.EqualsToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Main");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
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
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void LiteralContainingLambda2()
    {
        UsingExpression("_ = [() => { }, () => { }]");

        N(SyntaxKind.SimpleAssignmentExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "_");
            }
            N(SyntaxKind.EqualsToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
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
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void LiteralContainingLambda3()
    {
        UsingExpression("_ = [() => { }, Main]");

        N(SyntaxKind.SimpleAssignmentExpression);
        {
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "_");
            }
            N(SyntaxKind.EqualsToken);
            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
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
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Main");
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
        }
        EOF();
    }

    [Fact]
    public void LiteralContainingLambda4()
    {
        UsingTree("""
            using System;
            class Program
            {
                static void F(Action[] a) { }
                static void Main()
                {
                    F([Main, () => { }]);
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Main");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.ExpressionElement);
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
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
    public void LiteralContainingLambda5()
    {
        UsingTree("""
            using System;
            class Program
            {
                static void F(Action[] a) { }
                static void Main()
                {
                    F([Main, Main, () => { }]);
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Main");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Main");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.ExpressionElement);
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
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
    public void LiteralContainingLambda6()
    {
        UsingTree("""
            using System;
            class Program
            {
                static void F(Action[] a) { }
                static void Main()
                {
                    F([Main(), () => { }]);
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.InvocationExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Main");
                                                    }
                                                    N(SyntaxKind.ArgumentList);
                                                    {
                                                        N(SyntaxKind.OpenParenToken);
                                                        N(SyntaxKind.CloseParenToken);
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.ExpressionElement);
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
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
    public void LiteralContainingLambda7()
    {
        UsingTree("""
            using System;
            class Program
            {
                static void F(Action[] a) { }
                static void Main()
                {
                    F([X () => {});
                }
            }
            """,
            // (7,22): error CS1003: Syntax error, ']' expected
            //         F([X () => {});
            Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(7, 22));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.ParenthesizedLambdaExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "X");
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
                                            M(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
    public void LiteralContainingLambda8()
    {
        UsingTree("""
            using System;
            class Program
            {
                static void F(Action[] a) { }
                static void Main()
                {
                    F([X, Y () => {});
                }
            }
            """,
            // (7,25): error CS1003: Syntax error, ']' expected
            //         F([X, Y () => {});
            Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(7, 25));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "X");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.ParenthesizedLambdaExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Y");
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
                                            M(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
    public void LiteralContainingLambda9()
    {
        UsingTree("""
            using System;
            class Program
            {
                static void F(Action[] a) { }
                static void Main()
                {
                    F([X Y () => {});
                }
            }
            """,
            // (7,14): error CS1003: Syntax error, ',' expected
            //         F([X Y () => {});
            Diagnostic(ErrorCode.ERR_SyntaxError, "Y").WithArguments(",").WithLocation(7, 14),
            // (7,24): error CS1003: Syntax error, ']' expected
            //         F([X Y () => {});
            Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(7, 24));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "F");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Action");
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
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "X");
                                                }
                                            }
                                            M(SyntaxKind.CommaToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.ParenthesizedLambdaExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Y");
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
                                            M(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
    public void MemberAccess1()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [1].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "1");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess1A()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [Main].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Main");
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess2()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [1]?.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
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
                                            N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess2A()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [Main]?.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Main");
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
                                            N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess3()
    {
        UsingTree("""
            [1].GetHashCode();
            """);

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
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess3A()
    {
        UsingTree("""
            [Main].GetHashCode();
            """);

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
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Main");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess4()
    {
        UsingTree("""
            [1]?.GetHashCode();
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
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
                                    N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess4A()
    {
        UsingTree("""
            [Main]?.GetHashCode();
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Main");
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
                                    N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess5()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    // Indexing into collection, then invoking member.
                    [1][0].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
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
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess5A()
    {
        UsingTree("""
            // Indexing into collection, then invoking member.
            [1][0].GetHashCode();
            """);

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
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
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
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess6()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    // Indexing into collection, then invoking member.
                    [1][Main].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
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
                                                    N(SyntaxKind.IdentifierToken, "Main");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess6A()
    {
        UsingTree("""
            // Indexing into collection, then invoking member.
            [1][Main].GetHashCode();
            """);

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
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "1");
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
                                            N(SyntaxKind.IdentifierToken, "Main");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess7()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    // Indexing into collection, then invoking member.
                    [Main][1].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Main");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
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
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess7A()
    {
        UsingTree("""
            // Indexing into collection, then invoking member.
            [Main][1].GetHashCode();
            """);

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
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Main");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
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
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess8()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    // Indexing into collection, then invoking member.
                    [Main][Main].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Main");
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
                                                    N(SyntaxKind.IdentifierToken, "Main");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess8A()
    {
        UsingTree("""
            // Indexing into collection, then invoking member.
            [Main][Main].GetHashCode();
            """);

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
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Main");
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
                                            N(SyntaxKind.IdentifierToken, "Main");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess9()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess9A()
    {
        UsingTree("""
            [].GetHashCode();
            """);

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
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess10()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    []?.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.ConditionalAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
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
                                            N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess10A()
    {
        UsingTree("""
            []?.GetHashCode();
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
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
                                    N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess11()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [][0].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.CloseBracketToken);
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
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess11A()
    {
        UsingTree("""
            [][0].GetHashCode();
            """);

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
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.CloseBracketToken);
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
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess12()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    []!.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess12A()
    {
        UsingTree("""
            []!.GetHashCode();
            """);

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
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess13()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [A]!.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
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
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess13A()
    {
        UsingTree("""
            [A]!.GetHashCode();
            """);

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
                            N(SyntaxKind.SuppressNullableWarningExpression);
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
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess14()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [A:B]!.GetHashCode();
                }
            }
            """,
            // (5,11): error CS1003: Syntax error, ',' expected
            //         [A:B]!.GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(5, 11),
            // (5,12): error CS1003: Syntax error, ',' expected
            //         [A:B]!.GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(5, 12));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
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
                                            M(SyntaxKind.CommaToken);
                                            N(SyntaxKind.ExpressionElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "B");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess14A()
    {
        UsingTree("""
            [A:B]!.GetHashCode();
            """,
            // (1,3): error CS1003: Syntax error, ',' expected
            // [A:B]!.GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 3),
            // (1,4): error CS1003: Syntax error, ',' expected
            // [A:B]!.GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(1, 4));

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
                            N(SyntaxKind.SuppressNullableWarningExpression);
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
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.ExpressionElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess15()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [A()]!.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
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
                                                        N(SyntaxKind.CloseParenToken);
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess15A()
    {
        UsingTree("""
            [A()]!.GetHashCode();
            """);

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
                            N(SyntaxKind.SuppressNullableWarningExpression);
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
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess16()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [A()][0]!.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
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
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
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
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess16A()
    {
        UsingTree("""
            [A()][0]!.GetHashCode();
            """);

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
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.ElementAccessExpression);
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
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
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
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess17()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [][0]!.GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.SuppressNullableWarningExpression);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.CollectionExpression);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.CloseBracketToken);
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
                                        N(SyntaxKind.ExclamationToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess17A()
    {
        UsingTree("""
            [][0]!.GetHashCode();
            """);

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
                            N(SyntaxKind.SuppressNullableWarningExpression);
                            {
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
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
                                N(SyntaxKind.ExclamationToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess18()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [A:B][C:D].GetHashCode();
                }
            }
            """,
            // (5,11): error CS1003: Syntax error, ',' expected
            //         [A:B][C:D].GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(5, 11),
            // (5,12): error CS1003: Syntax error, ',' expected
            //         [A:B][C:D].GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(5, 12));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
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
                                            M(SyntaxKind.CommaToken);
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
                                                N(SyntaxKind.NameColon);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "C");
                                                    }
                                                    N(SyntaxKind.ColonToken);
                                                }
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "D");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess18A()
    {
        UsingTree("""
            [A:B][C:D].GetHashCode();
            """,
            // (1,3): error CS1003: Syntax error, ',' expected
            // [A:B][C:D].GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments(",").WithLocation(1, 3),
            // (1,4): error CS1003: Syntax error, ',' expected
            // [A:B][C:D].GetHashCode();
            Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(1, 4));

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
                            N(SyntaxKind.ElementAccessExpression);
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
                                    M(SyntaxKind.CommaToken);
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
                                        N(SyntaxKind.NameColon);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "C");
                                            }
                                            N(SyntaxKind.ColonToken);
                                        }
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "D");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess19()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [..A][..B].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.SpreadElement);
                                            {
                                                N(SyntaxKind.DotDotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "A");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.RangeExpression);
                                                {
                                                    N(SyntaxKind.DotDotToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "B");
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess19A()
    {
        UsingTree("""
            [..A][..B].GetHashCode();
            """);

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
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.SpreadElement);
                                    {
                                        N(SyntaxKind.DotDotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.RangeExpression);
                                        {
                                            N(SyntaxKind.DotDotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "B");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess20()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [[A]].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
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
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess20A()
    {
        UsingTree("""
            [[A]].GetHashCode();
            """);

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
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
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
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess21()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [A([B])].GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
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
                                                        N(SyntaxKind.CollectionExpression);
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
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess21A()
    {
        UsingTree("""
            [A([B])].GetHashCode();
            """);

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
                                                N(SyntaxKind.CollectionExpression);
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
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "GetHashCode");
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
    public void MemberAccess22()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    [A([B])] GetHashCode();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                                            N(SyntaxKind.CollectionExpression);
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
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "GetHashCode");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
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
    public void MemberAccess22A()
    {
        UsingTree("""
            [A([B])] GetHashCode();
            """,
            // (1,21): error CS1001: Identifier expected
            // [A([B])] GetHashCode();
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 21));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.CollectionExpression);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "GetHashCode");
                    }
                    M(SyntaxKind.IdentifierToken);
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
    public void MemberAccess23()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    []++;
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.PostIncrementExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.PlusPlusToken);
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
    public void MemberAccess23A()
    {
        UsingTree("""
            []++;
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.PostIncrementExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.PlusPlusToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MemberAccess24()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    []--;
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.PostDecrementExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.MinusMinusToken);
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
    public void MemberAccess24A()
    {
        UsingTree("""
            []--;
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.PostDecrementExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.MinusMinusToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MemberAccess25()
    {
        UsingTree("""
            class Program
            {
                static void Main()
                {
                    []->Goo();
                }
            }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "Program");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Main");
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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.PointerMemberAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.MinusGreaterThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Goo");
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
    public void MemberAccess25A()
    {
        UsingTree("""
            []->Goo;
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.PointerMemberAccessExpression);
                    {
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.MinusGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Goo");
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
    public void AttributeOnTopLevelFunction1()
    {
        UsingTree("""
            [A([B])] void Goo() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.CollectionExpression);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
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
    public void AttributeOnTopLevelFunction2()
    {
        UsingTree("""
            [A([B])] A Goo() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.CollectionExpression);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
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
    public void AttributeOnTopLevelFunction3()
    {
        UsingTree("""
            [A([B])] (A, B) Goo() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.CollectionExpression);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
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
    public void AttributeOnTopLevelFunction4()
    {
        UsingTree("""
            [A([B])] (A, B) Goo<A,B>() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.CollectionExpression);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
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
    public void AttributeOnTopLevelFunction5()
    {
        UsingTree("""
            [A([B])] (C, D) Goo<[E]F,[G([H])]I>() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
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
                                    N(SyntaxKind.CollectionExpression);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "D");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "Goo");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "E");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "G");
                                    }
                                    N(SyntaxKind.AttributeArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.AttributeArgument);
                                        {
                                            N(SyntaxKind.CollectionExpression);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.ExpressionElement);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "H");
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
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
    public void LambdaAttributeVersusCollectionLookahead1()
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
    public void LambdaAttributeVersusCollectionLookahead2()
    {
        UsingExpression("[A][B] (C, D)? e => f",
            // (1,22): error CS1003: Syntax error, ':' expected
            // [A][B] (C, D)? e => f
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(1, 22),
            // (1,22): error CS1733: Expected expression
            // [A][B] (C, D)? e => f
            Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 22));

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.SimpleLambdaExpression);
            {
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "f");
                }
            }
            M(SyntaxKind.ColonToken);
            M(SyntaxKind.IdentifierName);
            {
                M(SyntaxKind.IdentifierToken);
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead2A()
    {
        UsingExpression("[A][B](C, D) ? e : f");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead3()
    {
        UsingExpression("[A][B] (C, D)? (e) => f");

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
            N(SyntaxKind.NullableType);
            {
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.QuestionToken);
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "f");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead3A()
    {
        UsingExpression("[A][B](C, D) ? (e) : f");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "f");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead4()
    {
        UsingExpression("[A][B] (C, D)? (e, f) => g");

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
            N(SyntaxKind.NullableType);
            {
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.QuestionToken);
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.IdentifierToken, "f");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "g");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead4A()
    {
        UsingExpression("[A][B](C, D) ? (e, f) : g");

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.TupleExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.CommaToken);
                N(SyntaxKind.Argument);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "f");
                    }
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "g");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead5()
    {
        UsingExpression("[A][B] (C, D)? ([e] f) => g");

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
            N(SyntaxKind.NullableType);
            {
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.QuestionToken);
            }
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
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.IdentifierToken, "f");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "g");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead5A()
    {
        UsingExpression("[A][B](C, D) ? ([e] f) : g",
            // (1,1): error CS1073: Unexpected token ')'
            // [A][B](C, D) ? ([e] f) : g
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "[A][B](C, D) ? ([e] f").WithArguments(")").WithLocation(1, 1),
            // (1,21): error CS1026: ) expected
            // [A][B](C, D) ? ([e] f) : g
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "f").WithLocation(1, 21),
            // (1,21): error CS1003: Syntax error, ':' expected
            // [A][B](C, D) ? ([e] f) : g
            Diagnostic(ErrorCode.ERR_SyntaxError, "f").WithArguments(":").WithLocation(1, 21));

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.CollectionExpression);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.ExpressionElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                    }
                    N(SyntaxKind.CloseBracketToken);
                }
                M(SyntaxKind.CloseParenToken);
            }
            M(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "f");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead6()
    {
        UsingExpression("[A][B] (C, D)? ((e,f) g) => h");

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
            N(SyntaxKind.NullableType);
            {
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.QuestionToken);
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "f");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "g");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "h");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead6A()
    {
        UsingExpression("[A][B](C, D) ? ((e,f) g) : h",
            // (1,1): error CS1073: Unexpected token ')'
            // [A][B](C, D) ? ((e,f) g) : h
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "[A][B](C, D) ? ((e,f) g").WithArguments(")").WithLocation(1, 1),
            // (1,23): error CS1026: ) expected
            // [A][B](C, D) ? ((e,f) g) : h
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "g").WithLocation(1, 23),
            // (1,23): error CS1003: Syntax error, ':' expected
            // [A][B](C, D) ? ((e,f) g) : h
            Diagnostic(ErrorCode.ERR_SyntaxError, "g").WithArguments(":").WithLocation(1, 23));

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "f");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.CloseParenToken);
            }
            M(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "g");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead7()
    {
        UsingExpression("[A][B] (C, D)? ((e,f)[] g) => h");

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
            N(SyntaxKind.NullableType);
            {
                N(SyntaxKind.TupleType);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TupleElement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "D");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.QuestionToken);
            }
            N(SyntaxKind.ParameterList);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.Parameter);
                {
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "e");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "f");
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
                    N(SyntaxKind.IdentifierToken, "g");
                }
                N(SyntaxKind.CloseParenToken);
            }
            N(SyntaxKind.EqualsGreaterThanToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "h");
            }
        }
        EOF();
    }

    [Fact]
    public void LambdaAttributeVersusCollectionLookahead7A()
    {
        UsingExpression("[A][B](C, D) ? ((e,f)[] g) : h",
            // (1,1): error CS1073: Unexpected token ')'
            // [A][B](C, D) ? ((e,f)[] g) : h
            Diagnostic(ErrorCode.ERR_UnexpectedToken, "[A][B](C, D) ? ((e,f)[] g").WithArguments(")").WithLocation(1, 1),
            // (1,23): error CS0443: Syntax error; value expected
            // [A][B](C, D) ? ((e,f)[] g) : h
            Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(1, 23),
            // (1,25): error CS1026: ) expected
            // [A][B](C, D) ? ((e,f)[] g) : h
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "g").WithLocation(1, 25),
            // (1,25): error CS1003: Syntax error, ':' expected
            // [A][B](C, D) ? ((e,f)[] g) : h
            Diagnostic(ErrorCode.ERR_SyntaxError, "g").WithArguments(":").WithLocation(1, 25));

        N(SyntaxKind.ConditionalExpression);
        {
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.ElementAccessExpression);
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
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                    }
                    N(SyntaxKind.CommaToken);
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
            N(SyntaxKind.QuestionToken);
            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ElementAccessExpression);
                {
                    N(SyntaxKind.TupleExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "f");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BracketedArgumentList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        M(SyntaxKind.Argument);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                M(SyntaxKind.CloseParenToken);
            }
            M(SyntaxKind.ColonToken);
            N(SyntaxKind.IdentifierName);
            {
                N(SyntaxKind.IdentifierToken, "g");
            }
        }
        EOF();
    }

    [Fact]
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity1()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [() => {}][rand.Next()]();
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
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
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.SimpleMemberAccessExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "rand");
                                                    }
                                                    N(SyntaxKind.DotToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Next");
                                                    }
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity1A()
    {
        UsingTree("""
            [() => {}][rand.Next()]();
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.ElementAccessExpression);
                        {
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
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
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "rand");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Next");
                                            }
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
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
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity2()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [() => {}][rand.Next()](A);
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
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
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.SimpleMemberAccessExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "rand");
                                                    }
                                                    N(SyntaxKind.DotToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Next");
                                                    }
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
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
                                    N(SyntaxKind.CloseParenToken);
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity2A()
    {
        UsingTree("""
            [() => {}][rand.Next()](A);
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.ElementAccessExpression);
                        {
                            N(SyntaxKind.CollectionExpression);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ExpressionElement);
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
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "rand");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Next");
                                            }
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity3()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [() => {}][rand.Next()](A)[0];
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
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
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.InvocationExpression);
                                                {
                                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "rand");
                                                        }
                                                        N(SyntaxKind.DotToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Next");
                                                        }
                                                    }
                                                    N(SyntaxKind.ArgumentList);
                                                    {
                                                        N(SyntaxKind.OpenParenToken);
                                                        N(SyntaxKind.CloseParenToken);
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity3A()
    {
        UsingTree("""
            [() => {}][rand.Next()](A)[0];
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ElementAccessExpression);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
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
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "rand");
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Next");
                                                }
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
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
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity4()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [() => {}][rand.Next()](A)(B);
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
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
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.InvocationExpression);
                                                {
                                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "rand");
                                                        }
                                                        N(SyntaxKind.DotToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Next");
                                                        }
                                                    }
                                                    N(SyntaxKind.ArgumentList);
                                                    {
                                                        N(SyntaxKind.OpenParenToken);
                                                        N(SyntaxKind.CloseParenToken);
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
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
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity4A()
    {
        UsingTree("""
            [() => {}][rand.Next()](A)(B);
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
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
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "rand");
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Next");
                                                }
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity5()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [() => {}][rand.Next()](A).B();
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.CollectionExpression);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.ExpressionElement);
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
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                            N(SyntaxKind.BracketedArgumentList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.InvocationExpression);
                                                    {
                                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                                        {
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "rand");
                                                            }
                                                            N(SyntaxKind.DotToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Next");
                                                            }
                                                        }
                                                        N(SyntaxKind.ArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
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
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity5A()
    {
        UsingTree("""
            [() => {}][rand.Next()](A).B();
            """);

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
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
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
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.SimpleMemberAccessExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "rand");
                                                    }
                                                    N(SyntaxKind.DotToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Next");
                                                    }
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
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
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity6()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [() => {}][rand.Next()](A)++;
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.PostIncrementExpression);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.CollectionExpression);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.ExpressionElement);
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
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.InvocationExpression);
                                                {
                                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "rand");
                                                        }
                                                        N(SyntaxKind.DotToken);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "Next");
                                                        }
                                                    }
                                                    N(SyntaxKind.ArgumentList);
                                                    {
                                                        N(SyntaxKind.OpenParenToken);
                                                        N(SyntaxKind.CloseParenToken);
                                                    }
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
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
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.PlusPlusToken);
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity6A()
    {
        UsingTree("""
            [() => {}][rand.Next()](A)++;
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.PostIncrementExpression);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.ElementAccessExpression);
                            {
                                N(SyntaxKind.CollectionExpression);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.ExpressionElement);
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
                                    N(SyntaxKind.CloseBracketToken);
                                }
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "rand");
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Next");
                                                }
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
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
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                        N(SyntaxKind.PlusPlusToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity7()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [() => {}][rand.Next()](A)[0] = 1;
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.CollectionExpression);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.ExpressionElement);
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
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                            N(SyntaxKind.BracketedArgumentList);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.InvocationExpression);
                                                    {
                                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                                        {
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "rand");
                                                            }
                                                            N(SyntaxKind.DotToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "Next");
                                                            }
                                                        }
                                                        N(SyntaxKind.ArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
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
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "1");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity7A()
    {
        UsingTree("""
            [() => {}][rand.Next()](A)[0] = 1;
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.ElementAccessExpression);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.ElementAccessExpression);
                                {
                                    N(SyntaxKind.CollectionExpression);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.ExpressionElement);
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
                                        N(SyntaxKind.CloseBracketToken);
                                    }
                                    N(SyntaxKind.BracketedArgumentList);
                                    {
                                        N(SyntaxKind.OpenBracketToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.SimpleMemberAccessExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "rand");
                                                    }
                                                    N(SyntaxKind.DotToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Next");
                                                    }
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseBracketToken);
                                    }
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
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity8()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [Attr] (A, B) LocalFunc() { }
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Attr");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity8A()
    {
        UsingTree("""
            [Attr] (A, B) LocalFunc() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity9()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [Attr1][Attr2] (A, B) LocalFunc() { }
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Attr1");
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
                                        N(SyntaxKind.IdentifierToken, "Attr2");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity9A()
    {
        UsingTree("""
            [Attr1][Attr2] (A, B) LocalFunc() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr1");
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
                                N(SyntaxKind.IdentifierToken, "Attr2");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity10()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [Attr1][Attr2] (A, B)? LocalFunc() { }
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Attr1");
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
                                        N(SyntaxKind.IdentifierToken, "Attr2");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity10A()
    {
        UsingTree("""
            [Attr1][Attr2] (A, B)? LocalFunc() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr1");
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
                                N(SyntaxKind.IdentifierToken, "Attr2");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity11()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [Attr1][Attr2] (A, B)[] LocalFunc() { }
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Attr1");
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
                                        N(SyntaxKind.IdentifierToken, "Attr2");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
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
                            N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity11A()
    {
        UsingTree("""
            [Attr1][Attr2] (A, B)[] LocalFunc() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr1");
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
                                N(SyntaxKind.IdentifierToken, "Attr2");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
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
                    N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity12()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [Attr1][Attr2] (A, B)[,] LocalFunc() { }
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Attr1");
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
                                        N(SyntaxKind.IdentifierToken, "Attr2");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
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
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                            N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity12A()
    {
        UsingTree("""
            [Attr1][Attr2] (A, B)[,] LocalFunc() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr1");
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
                                N(SyntaxKind.IdentifierToken, "Attr2");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.OmittedArraySizeExpression);
                            {
                                N(SyntaxKind.OmittedArraySizeExpressionToken);
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity13()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [Attr1][Attr2] (A, B)* LocalFunc() { }
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Attr1");
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
                                        N(SyntaxKind.IdentifierToken, "Attr2");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.TupleType);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "A");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TupleElement);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "B");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.AsteriskToken);
                            }
                            N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity13A()
    {
        UsingTree("""
            [Attr1][Attr2] (A, B)* LocalFunc() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr1");
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
                                N(SyntaxKind.IdentifierToken, "Attr2");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.PointerType);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.AsteriskToken);
                    }
                    N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity14()
    {
        UsingTree("""
            class C
            {
                void M()
                {
                    [Attr1][Attr2] (A a, B b) LocalFunc() { }
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
                        N(SyntaxKind.LocalFunctionStatement);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Attribute);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Attr1");
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
                                        N(SyntaxKind.IdentifierToken, "Attr2");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                    }
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "B");
                                    }
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.IdentifierToken, "LocalFunc");
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
    public void InvokedCollectionExpressionVersusLocalFunctionAmbiguity14A()
    {
        UsingTree("""
            [Attr1][Attr2] (A a, B b) LocalFunc() { }
            """);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Attr1");
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
                                N(SyntaxKind.IdentifierToken, "Attr2");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.IdentifierToken, "LocalFunc");
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

    [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1934136")]
    public void ByteArrayAmbiguityWithAttributes()
    {
        UsingTree("class C { public ReadOnlySpan<byte> B => [0, 1, 2, 3, 4, 5, 6, 7]; }");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "ReadOnlySpan");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ByteKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
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
                            N(SyntaxKind.CommaToken);
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "4");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "5");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "6");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "7");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1934136")]
    public void TreatKeywordAsAttributeTarget()
    {
        UsingTree("class C { public ReadOnlySpan<byte> B => [true: A] () => { }; }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "ReadOnlySpan");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ByteKeyword);
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.ParenthesizedLambdaExpression);
                        {
                            N(SyntaxKind.AttributeList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.AttributeTargetSpecifier);
                                {
                                    N(SyntaxKind.TrueKeyword);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact, WorkItem("https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1934136")]
    public void TreatKeywordAsCollectionExprElement()
    {
        UsingTree("class C { public bool[] B => [true]; }");

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.BoolKeyword);
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
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.CollectionExpression);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ExpressionElement);
                            {
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }
}
