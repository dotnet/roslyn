// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class RoleParsingTests : ParsingTests
{
    private new SyntaxTree UsingTree(string text, params DiagnosticDescription[] expectedErrors)
        => UsingTree(text, TestOptions.RegularNext, expectedErrors);

    private void UsingNode(string text, CSharpParseOptions? options = null, DiagnosticDescription[]? expectedParsingDiagnostics = null, DiagnosticDescription[]? expectedBindingDiagnostics = null)
    {
        options ??= TestOptions.RegularPreview;
        expectedParsingDiagnostics ??= Array.Empty<DiagnosticDescription>();
        expectedBindingDiagnostics ??= expectedParsingDiagnostics;

        var tree = UsingTree(text, options, expectedParsingDiagnostics);
        Validate(text, (CSharpSyntaxNode)tree.GetRoot(), expectedParsingDiagnostics);

        var comp = CreateCompilation(tree);
        comp.VerifyDiagnostics(expectedBindingDiagnostics);
    }

    public RoleParsingTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void RoleParsing()
    {
        Assert.True(SyntaxFacts.IsTypeDeclaration(SyntaxKind.RoleDeclaration));

        var text = "role C : UnderlyingType, BaseRole1, BaseRole2 { }";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseRole1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseRole2");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(text, options: TestOptions.Regular10,
            // (1,8): error CS1002: ; expected
            // role C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 8),
            // (1,8): error CS1022: Type or namespace definition, or end-of-file expected
            // role C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 8),
            // (1,24): error CS1001: Identifier expected
            // role C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 24),
            // (1,47): error CS1003: Syntax error, ',' expected
            // role C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 47),
            // (1,49): error CS1002: ; expected
            // role C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 49),
            // (1,49): error CS1022: Type or namespace definition, or end-of-file expected
            // role C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 49)
            );

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
                            N(SyntaxKind.IdentifierToken, "role");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
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
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseRole1");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "BaseRole2");
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void RoleParsing_WithModifiers()
    {
        var text = "public static role C : UnderlyingType { }";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void RoleParsing_WithTypeParameters()
    {
        var text = "role C<T1, T2> : UnderlyingType { }";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T1");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T2");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void RoleParsing_WithTypeParameterAndConstraints()
    {
        var text = "role C<T> : UnderlyingType where T : class { }";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                    }
                }
                N(SyntaxKind.TypeParameterConstraintClause);
                {
                    N(SyntaxKind.WhereKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.ClassConstraint);
                    {
                        N(SyntaxKind.ClassKeyword);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void RoleParsing_WithBaseTypeArgumentList()
    {
        var text = "role C : UnderlyingType(42) { }";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.PrimaryConstructorBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.NumericLiteralExpression);
                                {
                                    N(SyntaxKind.NumericLiteralToken, "42");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(text, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(
            // PROTOTYPE underlying type required
            );
    }

    [Fact]
    public void RoleParsing_WithParameterList()
    {
        var text = "role C() { }";
        UsingTree(text,
            // (1,7): error CS1514: { expected
            // role C() { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 7),
            // (1,7): error CS1513: } expected
            // role C() { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 7),
            // (1,8): error CS1525: Invalid expression term ')'
            // role C() { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 8),
            // (1,10): error CS1002: ; expected
            // role C() { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 10)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void RoleParsing_WithoutBaseList()
    {
        var text = "role C { }";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        CreateCompilation(text, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(
            // PROTOTYPE underlying type required
            );

        UsingTree(text, options: TestOptions.Regular10);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "role");
                }
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void RoleParsing_WithoutBody()
    {
        var text = "role C : UnderlyingType;";
        UsingTree(text,
            // (1,24): error CS1003: Syntax error, ',' expected
            // role C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 24),
            // (1,25): error CS1514: { expected
            // role C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 25),
            // (1,25): error CS1513: } expected
            // role C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 25)
            );
        // PROTOTYPE should parse

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "UnderlyingType");
                        }
                    }
                }
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void NewModifier_Role()
    {
        UsingTree("new role C : U { }");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory]
    [InlineData(SyntaxKind.RoleKeyword)]
    public void FileModifier_11(SyntaxKind typeKeyword)
    {
        UsingNode($$"""public file {{SyntaxFacts.GetText(typeKeyword)}} C { }""");
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxFacts.GetBaseTypeDeclarationKind(typeKeyword));
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.FileKeyword);
                N(typeKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void SyntaxFactory_TypeDeclaration()
    {
        Assert.Throws<InvalidOperationException>(() => SyntaxFactory.TypeDeclaration(SyntaxKind.RoleDeclaration, "R"));
    }
}
