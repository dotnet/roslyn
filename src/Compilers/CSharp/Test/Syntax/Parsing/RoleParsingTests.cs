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


    public RoleParsingTests(ITestOutputHelper output) : base(output) { }

    [Theory, CombinatorialData]
    public void RoleParsing(bool isExtension)
    {
        Assert.True(SyntaxFacts.IsTypeDeclaration(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration));

        var keyword = isExtension ? "extension" : "role     ";
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

        UsingTree(text, options: TestOptions.Regular10,
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
    public void RoleParsing_WithPartial(bool isExtension)
    {
        Assert.True(SyntaxFacts.IsTypeDeclaration(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration));

        var keyword = isExtension ? "extension" : "role     ";
        var text = $$"""partial {{keyword}} C : UnderlyingType { }""";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
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

        UsingTree(text, options: TestOptions.Regular10,
            // (1,1): error CS1031: Type expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 1),
            // (1,1): error CS1525: Invalid expression term 'partial'
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 1),
            // (1,1): error CS1003: Syntax error, ',' expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 1),
            // (1,21): error CS1002: ; expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 21),
            // (1,21): error CS1022: Type or namespace definition, or end-of-file expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 21),
            // (1,38): error CS1001: Identifier expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 38),
            // (1,38): error CS1003: Syntax error, ',' expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 38),
            // (1,40): error CS1002: ; expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 40),
            // (1,40): error CS1022: Type or namespace definition, or end-of-file expected
            // partial extension C : UnderlyingType { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 40)
            );

        N(SyntaxKind.CompilationUnit);
        {
            M(SyntaxKind.GlobalStatement);
            {
                M(SyntaxKind.LocalDeclarationStatement);
                {
                    M(SyntaxKind.VariableDeclaration);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
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
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void RoleParsing_WithReadonlyPartial()
    {
        var text = "readonly partial role S : U { }";
        UsingTree(text);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "S");
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

        UsingTree(text, options: TestOptions.Regular10,
            // (1,1): error CS0106: The modifier 'readonly' is not valid for this item
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(1, 1),
            // (1,10): error CS1031: Type expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 10),
            // (1,10): error CS1525: Invalid expression term 'partial'
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 10),
            // (1,10): error CS1003: Syntax error, ',' expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 10),
            // (1,25): error CS1002: ; expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 25),
            // (1,25): error CS1022: Type or namespace definition, or end-of-file expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 25),
            // (1,29): error CS1001: Identifier expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 29),
            // (1,29): error CS1003: Syntax error, ',' expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 29),
            // (1,31): error CS1002: ; expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 31),
            // (1,31): error CS1022: Type or namespace definition, or end-of-file expected
            // readonly partial role S : U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(1, 31)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    M(SyntaxKind.VariableDeclaration);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
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
        var keyword = isExtension ? "extension" : "role     ";
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
        var keyword = isExtension ? "extension" : "role     ";
        var text = $$"""{{keyword}} C() { }""";

        UsingTree(text,
            // (1,12): error CS1514: { expected
            // extension C() { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 12),
            // (1,12): error CS1513: } expected
            // extension C() { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 12),
            // (1,12): error CS8803: Top-level statements must precede namespace and type declarations.
            // extension C() { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "() ").WithLocation(1, 12),
            // (1,13): error CS1525: Invalid expression term ')'
            // extension C() { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 13),
            // (1,15): error CS1002: ; expected
            // extension C() { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 15)
            );

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
    public void RoleParsing_WithParameterList_WithoutBody(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role     ";
        var text = $$"""{{keyword}} C();""";

        UsingTree(text,
            // (1,12): error CS1514: { expected
            // extension C();
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 12),
            // (1,12): error CS1513: } expected
            // extension C();
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 12),
            // (1,12): error CS8803: Top-level statements must precede namespace and type declarations.
            // extension C();
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "();").WithLocation(1, 12),
            // (1,13): error CS1525: Invalid expression term ')'
            // extension C();
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(1, 13)
            );

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
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void RoleParsing_WithParameterList_WithOneParameter(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role     ";
        var text = $$"""{{keyword}} C(int i) { }""";

        UsingTree(text,
            // (1,12): error CS1514: { expected
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 12),
            // (1,12): error CS1513: } expected
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 12),
            // (1,12): error CS8803: Top-level statements must precede namespace and type declarations.
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int ").WithLocation(1, 12),
            // (1,13): error CS1525: Invalid expression term 'int'
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 13),
            // (1,17): error CS1026: ) expected
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "i").WithLocation(1, 17),
            // (1,17): error CS1002: ; expected
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "i").WithLocation(1, 17),
            // (1,18): error CS1001: Identifier expected
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 18),
            // (1,18): error CS1002: ; expected
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(1, 18),
            // (1,18): error CS1022: Type or namespace definition, or end-of-file expected
            // extension C(int i) { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(1, 18)
            );

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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        M(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
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
        var keyword = isExtension ? "extension" : "role     ";
        var text = $$"""{{keyword}} C : UnderlyingType;""";
        UsingTree(text,
            // (1,29): error CS1003: Syntax error, ',' expected
            // extension C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(1, 29),
            // (1,30): error CS1514: { expected
            // extension C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 30),
            // (1,30): error CS1513: } expected
            // extension C : UnderlyingType;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 30)
            );
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
        UsingTree($$"""public file {{SyntaxFacts.GetText(typeKeyword)}} C { }""");
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

    [Theory, CombinatorialData]
    public void RoleParsing_AfterIncompleteUsing(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""
using
partial {{keyword}} R : U { }
""";

        UsingTree(text,
            // (1,6): error CS1031: Type expected
            // using
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 6),
            // (1,6): error CS1525: Invalid expression term 'partial'
            // using
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("partial").WithLocation(1, 6),
            // (1,6): error CS1002: ; expected
            // using
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 6)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    M(SyntaxKind.VariableDeclaration);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "R");
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

        var expectedDiagnostics = isExtension ? new[]
        {
            // (1,6): error CS1031: Type expected
            // using
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 6),
            // (1,6): error CS1525: Invalid expression term 'partial'
            // using
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("partial").WithLocation(1, 6),
            // (1,6): error CS1003: Syntax error, ',' expected
            // using
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 6),
            // (2,9): error CS1002: ; expected
            // partial extension R : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "extension").WithLocation(2, 9),
            // (2,21): error CS1002: ; expected
            // partial extension R : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(2, 21),
            // (2,21): error CS1022: Type or namespace definition, or end-of-file expected
            // partial extension R : U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(2, 21),
            // (2,25): error CS1001: Identifier expected
            // partial extension R : U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 25),
            // (2,25): error CS1003: Syntax error, ',' expected
            // partial extension R : U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(2, 25),
            // (2,27): error CS1002: ; expected
            // partial extension R : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 27),
            // (2,27): error CS1022: Type or namespace definition, or end-of-file expected
            // partial extension R : U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(2, 27)
        } : new[]
        {
            // (1,6): error CS1031: Type expected
            // using
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(1, 6),
            // (1,6): error CS1525: Invalid expression term 'partial'
            // using
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("partial").WithLocation(1, 6),
            // (1,6): error CS1003: Syntax error, ',' expected
            // using
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 6),
            // (2,9): error CS1002: ; expected
            // partial role R : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "role").WithLocation(2, 9),
            // (2,16): error CS1002: ; expected
            // partial role R : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(2, 16),
            // (2,16): error CS1022: Type or namespace definition, or end-of-file expected
            // partial role R : U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(2, 16),
            // (2,20): error CS1001: Identifier expected
            // partial role R : U { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 20),
            // (2,20): error CS1003: Syntax error, ',' expected
            // partial role R : U { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(2, 20),
            // (2,22): error CS1002: ; expected
            // partial role R : U { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 22),
            // (2,22): error CS1022: Type or namespace definition, or end-of-file expected
            // partial role R : U { }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(2, 22)
        };

        UsingTree(text, options: TestOptions.Regular10, expectedDiagnostics);
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    M(SyntaxKind.VariableDeclaration);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.IdentifierToken, isExtension ? "extension" : "role");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
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
                            N(SyntaxKind.IdentifierToken, "U");
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
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
    public void RoleParsing_PartialInIncompleteNamespace()
    {
        var text = """
namespace N
partial role S : U { }
""";
        UsingTree(text,
            // (1,12): error CS1514: { expected
            // namespace N
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 12),
            // (2,23): error CS1513: } expected
            // partial role S : U { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(2, 23)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                M(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.RoleDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RoleKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
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
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void RoleParsing_BeforeTopLevelStatement(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role";
        var text = $$"""
{{keyword}} R : U { }
Write();
""";

        UsingTree(text,
            // (2,1): error CS8803: Top-level statements must precede namespace and type declarations.
            // Write();
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "Write();").WithLocation(2, 1)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "R");
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
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Write");
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

    [Theory, CombinatorialData]
    public void RoleParsing_WithoutBody_WithoutBaseList(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role     ";
        var text = $$"""{{keyword}} C;""";
        UsingTree(text,
            // (1,12): error CS1514: { expected
            // extension C;
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(1, 12),
            // (1,12): error CS1513: } expected
            // extension C;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(1, 12)
            );
        // PROTOTYPE should parse

        N(SyntaxKind.CompilationUnit);
        {
            N(isExtension ? SyntaxKind.ExtensionDeclaration : SyntaxKind.RoleDeclaration);
            {
                N(isExtension ? SyntaxKind.ExtensionKeyword : SyntaxKind.RoleKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void RoleParsing_WithoutBody_WithoutBaseList_WithTypeParameter(bool isExtension)
    {
        var keyword = isExtension ? "extension" : "role     ";
        var text = $$"""{{keyword}} C<T>;""";
        UsingTree(text,
            // (1,15): error CS1514: { expected
            // extension C<T>;
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(1, 15),
            // (1,15): error CS1513: } expected
            // extension C<T>;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(1, 15)
            );
        // PROTOTYPE should parse

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
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
                N(SyntaxKind.SemicolonToken);
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
