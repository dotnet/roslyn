// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class UnionParsingTests : ParsingTests
{
    public UnionParsingTests(ITestOutputHelper output) : base(output) { }

    [Theory, CombinatorialData]
    public void Union_01(bool useCSharp15)
    {
        var src = """
union U1(E1);
""";
        UsingTree(src, TestOptions.Regular14,
            // (1,12): error CS1001: Identifier expected
            // union U1(E1);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 12)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "union");
                    }
                    N(SyntaxKind.IdentifierToken, "U1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "E1");
                            }
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

        UsingTree(src, useCSharp15 ? TestOptions.RegularNext : TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Union_02()
    {
        var src = """
record union U1(E1);
""";

        UsingTree(src, TestOptions.RegularPreview,
            // (1,14): error CS1514: { expected
            // record union U1(E1);
            Diagnostic(ErrorCode.ERR_LbraceExpected, "U1").WithLocation(1, 14),
            // (1,14): error CS1513: } expected
            // record union U1(E1);
            Diagnostic(ErrorCode.ERR_RbraceExpected, "U1").WithLocation(1, 14),
            // (1,14): error CS8803: Top-level statements must precede namespace and type declarations.
            // record union U1(E1);
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "U1(E1);").WithLocation(1, 14)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.IdentifierToken, "union");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "U1");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "E1");
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

    [Theory, CombinatorialData]
    public void Union_03(bool useCSharp15)
    {
        var src = """
union M()
{
    return default;
}
""";
        UsingTree(src, TestOptions.Regular14);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "union");
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
                        N(SyntaxKind.ReturnStatement);
                        {
                            N(SyntaxKind.ReturnKeyword);
                            N(SyntaxKind.DefaultLiteralExpression);
                            {
                                N(SyntaxKind.DefaultKeyword);
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(src, useCSharp15 ? TestOptions.RegularNext : TestOptions.RegularPreview,
            // (1,9): error CS1031: Type expected
            // union M()
            Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(1, 9),
            // (3,5): error CS1519: Invalid token 'return' in a member declaration
            //     return default;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "return").WithArguments("return").WithLocation(3, 5)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "M");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.Parameter);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void Union_04(bool useCSharp15)
    {
        var src = """
partial union U1(E1);
""";
        UsingTree(src, TestOptions.Regular14,
            // (1,20): error CS1001: Identifier expected
            // partial union U1(E1);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 20)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "union");
                }
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        UsingTree(src, useCSharp15 ? TestOptions.RegularNext : TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Theory, CombinatorialData]
    public void Union_05(bool useCSharp15)
    {
        var src = """
ref union U1(E1);
""";
        UsingTree(src, TestOptions.Regular14,
            // (1,16): error CS1001: Identifier expected
            // ref union U1(E1);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 16)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalFunctionStatement);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "union");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "U1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "E1");
                            }
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

        UsingTree(src, useCSharp15 ? TestOptions.RegularNext : TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        var comp = CreateCompilation([src, "struct E1;", UnionAttributeSource, IUnionSource], parseOptions: TestOptions.RegularPreview);
        comp.VerifyDiagnostics(
            // (1,11): error CS0106: The modifier 'ref' is not valid for this item
            // ref union U1(E1);
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "U1").WithArguments("ref").WithLocation(1, 11)
            );

    }

    [Theory, CombinatorialData]
    public void Union_06(bool useCSharp15)
    {
        var src = """
ref partial union U1(E1);
""";
        UsingTree(src, TestOptions.Regular14,
            // (1,5): error CS1031: Type expected
            // ref partial union U1(E1);
            Diagnostic(ErrorCode.ERR_TypeExpected, "partial").WithLocation(1, 5),
            // (1,5): error CS1525: Invalid expression term 'partial'
            // ref partial union U1(E1);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "partial").WithArguments("partial").WithLocation(1, 5),
            // (1,5): error CS1003: Syntax error, ',' expected
            // ref partial union U1(E1);
            Diagnostic(ErrorCode.ERR_SyntaxError, "partial").WithArguments(",").WithLocation(1, 5)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
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

        UsingTree(src, useCSharp15 ? TestOptions.RegularNext : TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.RefKeyword);
                N(SyntaxKind.PartialKeyword);
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Union_07()
    {
        var src = """
union U1(E1, E2, E3);
""";
        UsingTree(src, TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E2");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E3");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Union_08()
    {
        var src = """
union U1(E1) : I1(1);
""";
        UsingTree(src, TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.PrimaryConstructorBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I1");
                        }
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
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();

        var comp = CreateCompilation([src, "struct E1; interface I1;", UnionAttributeSource, IUnionSource], parseOptions: TestOptions.RegularPreview);
        comp.VerifyDiagnostics(
            // (1,18): error CS8861: Unexpected argument list.
            // union U1(E1) : I1(1);
            Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(1)").WithLocation(1, 18)
            );
    }

    [Fact]
    public void Union_09()
    {
        var src = """
[Attr1]
public union U1<T1>(E1) : I1, I2 where T1 : class
{
    public void M1() { }
}
""";
        UsingTree(src, TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
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
                N(SyntaxKind.PublicKeyword);
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.TypeParameterList);
                {
                    N(SyntaxKind.LessThanToken);
                    N(SyntaxKind.TypeParameter);
                    {
                        N(SyntaxKind.IdentifierToken, "T1");
                    }
                    N(SyntaxKind.GreaterThanToken);
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "E1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.BaseList);
                {
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I1");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleBaseType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I2");
                        }
                    }
                }
                N(SyntaxKind.TypeParameterConstraintClause);
                {
                    N(SyntaxKind.WhereKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T1");
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.ClassConstraint);
                    {
                        N(SyntaxKind.ClassKeyword);
                    }
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M1");
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

    [Fact]
    public void Union_10()
    {
        var src = """
union U1;
""";
        UsingTree(src, TestOptions.RegularPreview);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UnionDeclaration);
            {
                N(SyntaxKind.UnionKeyword);
                N(SyntaxKind.IdentifierToken, "U1");
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void Union_11()
    {
        var src = """
record union U1;
""";
        UsingTree(src, TestOptions.RegularPreview,
            // (1,14): error CS1514: { expected
            // record union U1;
            Diagnostic(ErrorCode.ERR_LbraceExpected, "U1").WithLocation(1, 14),
            // (1,14): error CS1513: } expected
            // record union U1;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "U1").WithLocation(1, 14),
            // (1,14): error CS8803: Top-level statements must precede namespace and type declarations.
            // record union U1;
            Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "U1;").WithLocation(1, 14)
            );

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.RecordDeclaration);
            {
                N(SyntaxKind.RecordKeyword);
                N(SyntaxKind.IdentifierToken, "union");
                M(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "U1");
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }
}
