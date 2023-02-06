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

    [Theory, CombinatorialData]
    public void RoleParsing(bool isExtension)
    {
        Assert.True(SyntaxFacts.IsTypeDeclaration(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration));

        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C : UnderlyingType, BaseRole1, BaseRole2 { }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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

        var expectedDiagnostics = isExtension ? new[]
        {
            // (1,13): error CS1002: ; expected
            // extension C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 13),
            // (1,13): error CS1022: Type or namespace definition, or end-of-file expected
            // extension C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 13),
            // (1,29): error CS1001: Identifier expected
            // extension C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 29),
            // (1,52): error CS1003: Syntax error, ',' expected
            // extension C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 52),
            // (1,54): error CS1002: ; expected
            // extension C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 54),
            // (1,54): error CS1022: Type or namespace definition, or end-of-file expected
            // extension C : UnderlyingType, BaseRole1, BaseRole2 { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 54)
        } : new[]
        {
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
        };
        UsingTree(text, options: TestOptions.Regular10, expectedDiagnostics);

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
                            N(SyntaxKind.IdentifierToken, isExtension ? "extension" : "role");
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithModifiers(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""public static {{keyword}} C : UnderlyingType { }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.StaticKeyword);
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithMembers(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C : UnderlyingType { void M() { } }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void RoleParsing_WithTypeParameters(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C<T1, T2> : UnderlyingType { }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithTypeParameterAndConstraints(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C<T> : UnderlyingType where T : class { }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithBaseTypeArgumentList(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C : UnderlyingType(42) { }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithParameterList(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C() { }""";

        var expectedDiagnostics = isExtension ? new[]
        {
            // (1,12): error CS1514: { expected
            // extension C() { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 12),
            // (1,12): error CS1513: } expected
            // extension C() { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 12),
            // (1,13): error CS1525: Invalid expression term ')'
            // extension C() { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 13),
            // (1,15): error CS1002: ; expected
            // extension C() { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 15)
        } : new[]
        {
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
        };

        UsingTree(text, expectedDiagnostics);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithoutBaseList(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C { }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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
                    N(SyntaxKind.IdentifierToken, isExtension ? "extension" : "role");
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithoutBody(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""{{keyword}} C : UnderlyingType;""";
        var expectedDiagnostics = isExtension ? new[]
        {
            // (1,29): error CS1003: Syntax error, ',' expected
            // extension C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 29),
            // (1,30): error CS1514: { expected
            // extension C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 30),
            // (1,30): error CS1513: } expected
            // extension C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 30)
        } : new[]
        {
            // (1,24): error CS1003: Syntax error, ',' expected
            // role C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 24),
            // (1,25): error CS1514: { expected
            // role C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 25),
            // (1,25): error CS1513: } expected
            // role C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 25)
        };
        UsingTree(text, expectedDiagnostics);
        // PROTOTYPE should parse

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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

    [Theory, CombinatorialData]
    public void NewModifier_Role(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        UsingTree($$"""new {{keyword}} C : U { }""");
        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.NewKeyword);
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
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
    [InlineData(SyntaxKind.ExtensionKeyword)]
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
        Assert.Throws<InvalidOperationException>(() => SyntaxFactory.TypeDeclaration(SyntaxKind.ExtensionDeclaration, "E"));
    }
}
