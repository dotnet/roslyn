// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.Extensions)]
public class ExtensionsParsingTests : ParsingTests
{
    public ExtensionsParsingTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void LangVer13()
    {
        // PROTOTYPE consider giving a LangVer error to trigger UpgradeProject
        UsingTree("""
class C
{
    extension<T>(object o) where T : struct { }
}
""",
            TestOptions.Regular13,
            // (3,17): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
            //     extension<T>(object o) where T : struct { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(3, 17),
            // (3,26): error CS8124: Tuple must contain at least two elements.
            //     extension<T>(object o) where T : struct { }
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 26),
            // (3,34): error CS1002: ; expected
            //     extension<T>(object o) where T : struct { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "T").WithLocation(3, 34),
            // (3,36): error CS1519: Invalid token ':' in class, record, struct, or interface member declaration
            //     extension<T>(object o) where T : struct { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ":").WithArguments(":").WithLocation(3, 36),
            // (3,36): error CS1519: Invalid token ':' in class, record, struct, or interface member declaration
            //     extension<T>(object o) where T : struct { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ":").WithArguments(":").WithLocation(3, 36),
            // (3,45): error CS1001: Identifier expected
            //     extension<T>(object o) where T : struct { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(3, 45));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "extension");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TupleElement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.ObjectKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "o");
                            }
                            M(SyntaxKind.CommaToken);
                            M(SyntaxKind.TupleElement);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "where");
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void LangVer14()
    {
        UsingTree("""
class C
{
    extension<T>(object o) where T : struct { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.StructConstraint);
                        {
                            N(SyntaxKind.StructKeyword);
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MultipleConstraints()
    {
        UsingTree("""
class C
{
    extension<T1, T2>(object o) where T1 : struct where T2 : class { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
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
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.ObjectKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "o");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T1");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.StructConstraint);
                        {
                            N(SyntaxKind.StructKeyword);
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T2");
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
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithName()
    {
        UsingTree("""
class C
{
    extension Name(Type) { }
}
""",
            TestOptions.RegularNext,
            // (3,15): error CS1003: Syntax error, '(' expected
            //     extension Name(Type) { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "Name").WithArguments("(").WithLocation(3, 15),
            // (3,19): error CS1001: Identifier expected
            //     extension Name(Type) { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(3, 19),
            // (3,19): error CS1026: ) expected
            //     extension Name(Type) { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "(").WithLocation(3, 19),
            // (3,19): error CS1514: { expected
            //     extension Name(Type) { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(3, 19),
            // (3,19): error CS1513: } expected
            //     extension Name(Type) { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(3, 19),
            // (3,24): error CS8124: Tuple must contain at least two elements.
            //     extension Name(Type) { }
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 24),
            // (3,26): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
            //     extension Name(Type) { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(3, 26),
            // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    M(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Name");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        M(SyntaxKind.TupleElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ExtensionType()
    {
        UsingTree("""
class extension
{
    extension(Type constructorParameter) { }
}
""",
            TestOptions.Regular13);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "extension");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.IdentifierToken, "extension");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                            N(SyntaxKind.IdentifierToken, "constructorParameter");
                        }
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

        // PROTOTYPE report error for declaring type named "extension"
        // Note: break from C# 13
        UsingTree("""
class extension
{
    extension(Type constructorParameter) { }
}
""",
        TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "extension");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "constructorParameter");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void NoReceiverParameterName()
    {
        UsingTree("""
class C
{
    extension(Type) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MultipleReceiverParameters()
    {
        UsingTree("""
class C
{
    extension(Type x1, Type x2) { }
}
""",
            TestOptions.RegularNext,
            // (3,22): error CS1026: ) expected
            //     extension(Type x1, Type x2) { }
            Diagnostic(ErrorCode.ERR_CloseParenExpected, ",").WithLocation(3, 22),
            // (3,22): error CS1514: { expected
            //     extension(Type x1, Type x2) { }
            Diagnostic(ErrorCode.ERR_LbraceExpected, ",").WithLocation(3, 22),
            // (3,22): error CS1513: } expected
            //     extension(Type x1, Type x2) { }
            Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(3, 22),
            // (3,22): error CS1519: Invalid token ',' in class, record, struct, interface, or extension member declaration
            //     extension(Type x1, Type x2) { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ",").WithArguments(",").WithLocation(3, 22),
            // (3,31): error CS1003: Syntax error, ',' expected
            //     extension(Type x1, Type x2) { }
            Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(3, 31),
            // (3,35): error CS1002: ; expected
            //     extension(Type x1, Type x2) { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(3, 35),
            // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "x1");
                    }
                    M(SyntaxKind.CloseParenToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x2");
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void NoClosingBrace()
    {
        UsingTree("""
class C
{
    extension(Type) { void M() { }
}
""",
            TestOptions.RegularNext,
            // (4,2): error CS1513: } expected
            // }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
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
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TopLevel()
    {
        UsingTree("""
extension(Type) { }
""",
            TestOptions.RegularNext,
            // (1,17): error CS1002: ; expected
            // extension(Type) { }
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(1, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Type");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
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

    [Fact]
    public void InNestedType()
    {
        // PROTOTYPE should be a semantic error
        UsingTree("""
class C
{
    class Nested
    {
        extension(Type) { }
    }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Nested");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ExtensionContainer);
                    {
                        N(SyntaxKind.ExtensionKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.ReceiverParameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void InExtension()
    {
        // PROTOTYPE should be a semantic error
        UsingTree("""
class C
{
    extension(Type1)
    {
        extension(Type2) { }
    }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type1");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ExtensionContainer);
                    {
                        N(SyntaxKind.ExtensionKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.ReceiverParameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type2");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithAttributes()
    {
        // PROTOTYPE should be a semantic error
        UsingTree("""
class C
{
    [MyAttribute]
    extension(Type) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "MyAttribute");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithModifiers()
    {
        // PROTOTYPE should be a semantic error
        UsingTree("""
class C
{
    public extension(Type) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ConstMember()
    {
        UsingTree("""
class C
{
    extension(Type) 
    {
        const int i = 0;
    }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.ConstKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "i");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void FixedFieldMember()
    {
        UsingTree("""
class C
{
    extension(Type) 
    {
        fixed int field[10];
    }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.FixedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "field");
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "10");
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void EventFieldMember()
    {
        UsingTree("""
class C
{
    extension(Type) 
    {
        event EventHandler eventField;
    }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "EventHandler");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "eventField");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithRef()
    {
        UsingTree("""
class C
{
    ref extension(Type) { }
}
""",
            TestOptions.RegularNext,
            // (3,18): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
            //     ref extension(Type) { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(3, 18),
            // (3,23): error CS8124: Tuple must contain at least two elements.
            //     ref extension(Type) { }
            Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 23),
            // (3,25): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
            //     ref extension(Type) { }
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(3, 25),
            // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
            // }
            Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));
        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "extension");
                        }
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        M(SyntaxKind.TupleElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithAttributeOnParameter()
    {
        UsingTree("""
class C
{
    extension([MyAttribute] Type) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "MyAttribute");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithModifierOnParameter()
    {
        UsingTree("""
class C
{
    extension(ref Type) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithModifierOnParameter_Scoped()
    {
        UsingTree("""
class C
{
    extension(scoped Type x) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithModifierOnParameter_ScopedRef()
    {
        UsingTree("""
class C
{
    extension(scoped ref Type x) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void PartialIdentifier()
    {
        UsingTree("""
class C
{
    extension(Type partial) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AwaitIdentifier()
    {
        UsingTree("""
class C
{
    extension(Type await) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void OnIdentifier()
    {
        UsingTree("""
class C
{
    extension(Type on) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "on");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void ByIdentifier()
    {
        UsingTree("""
class C
{
    extension(Type by) { }
}
""",
            TestOptions.RegularNext);

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                        N(SyntaxKind.IdentifierToken, "by");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_SemiColon()
    {
        UsingTree("""
class C
{
    extension(Type) { ;
    class D { }
}
""",
            TestOptions.RegularNext,
            // (3,23): error CS1519: Invalid token ';' in class, record, struct, interface, or extension member declaration
            //     extension(Type) { ;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 23),
            // (5,2): error CS1513: } expected
            // }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "D");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_SemiColon_02()
    {
        UsingTree("""
class C
{
    extension(Type) { ;
}
""",
            TestOptions.RegularNext,
            // (3,23): error CS1519: Invalid token ';' in class, record, struct, interface, or extension member declaration
            //     extension(Type) { ;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 23),
            // (4,2): error CS1513: } expected
            // }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_SemiColon_03()
    {
        UsingTree("""
class C
{
    extension<T>(Type) where T : struct ;
    class D { }
}
""",
            TestOptions.RegularNext,
            // (3,41): error CS1514: { expected
            //     extension<T>(Type) where T : struct ;
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(3, 41),
            // (3,41): error CS1513: } expected
            //     extension<T>(Type) where T : struct ;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(3, 41),
            // (3,41): error CS1519: Invalid token ';' in class, record, struct, interface, or extension member declaration
            //     extension<T>(Type) where T : struct ;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 41));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.StructConstraint);
                        {
                            N(SyntaxKind.StructKeyword);
                        }
                    }
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_SemiColon_04()
    {
        UsingTree("""
class C
{
    extension ;
    class D { }
}
""",
            TestOptions.RegularNext,
            // (3,15): error CS1003: Syntax error, '(' expected
            //     extension ;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("(").WithLocation(3, 15),
            // (3,15): error CS1031: Type expected
            //     extension ;
            Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(3, 15),
            // (3,15): error CS1001: Identifier expected
            //     extension ;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(3, 15),
            // (3,15): error CS1026: ) expected
            //     extension ;
            Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(3, 15),
            // (3,15): error CS1514: { expected
            //     extension ;
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(3, 15),
            // (3,15): error CS1513: } expected
            //     extension ;
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(3, 15),
            // (3,15): error CS1519: Invalid token ';' in class, record, struct, interface, or extension member declaration
            //     extension ;
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 15));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.ReceiverParameter);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_SemiColon_05()
    {
        UsingTree("""
class C
{
    extension(Type);
    class D { }
}
""",
            TestOptions.RegularNext,
            // (3,20): error CS1514: { expected
            //     extension(Type);
            Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(3, 20),
            // (3,20): error CS1513: } expected
            //     extension(Type);
            Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(3, 20),
            // (3,20): error CS1519: Invalid token ';' in class, record, struct, interface, or extension member declaration
            //     extension(Type);
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(3, 20));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_SemiColon_06()
    {
        UsingTree("""
class C
{
    extension<T ;
    class D { }
}
""",
            TestOptions.RegularNext,
            // (3,17): error CS1003: Syntax error, ',' expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(3, 17),
            // (3,18): error CS1003: Syntax error, '>' expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">").WithLocation(3, 18),
            // (3,18): error CS1003: Syntax error, '(' expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(3, 18),
            // (3,18): error CS1031: Type expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(3, 18),
            // (3,18): error CS1001: Identifier expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 18),
            // (3,18): error CS1026: ) expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(3, 18),
            // (3,18): error CS1514: { expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(3, 18),
            // (3,18): error CS1513: } expected
            //     extension<T ;
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 18));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        M(SyntaxKind.GreaterThanToken);
                    }
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.ReceiverParameter);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_OpenBrace()
    {
        UsingTree("""
class C
{
    extension(Type) { {
}
""",
            TestOptions.RegularNext,
            // (3,23): error CS1519: Invalid token '{' in class, record, struct, interface, or extension member declaration
            //     extension(Type) { {
            Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(3, 23),
            // (4,2): error CS1513: } expected
            // }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_OpenBrace_02()
    {
        UsingTree("""
class C
{
    extension<T>(Type) where { }
}
""",
            TestOptions.RegularNext,
            // (3,30): error CS1001: Identifier expected
            //     extension<T>(Type) where { }
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(3, 30),
            // (3,30): error CS1003: Syntax error, ':' expected
            //     extension<T>(Type) where { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(":").WithLocation(3, 30),
            // (3,30): error CS1031: Type expected
            //     extension<T>(Type) where { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(3, 30));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.ColonToken);
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_OpenBrace_03()
    {
        UsingTree("""
class C
{
    extension<T>(Type) where T { }
    class D { }
}
""",
            TestOptions.RegularNext,
            // (3,32): error CS1003: Syntax error, ':' expected
            //     extension<T>(Type) where T { }
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(":").WithLocation(3, 32),
            // (3,32): error CS1031: Type expected
            //     extension<T>(Type) where T { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(3, 32));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        M(SyntaxKind.ColonToken);
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_OpenBrace_04()
    {
        UsingTree("""
class C
{
    extension<T>(Type) where T : { }
}
""",
            TestOptions.RegularNext,
            // (3,34): error CS1031: Type expected
            //     extension<T>(Type) where T : { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(3, 34));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_OpenBrace_05()
    {
        UsingTree("""
class C
{
    extension<T>(Type) where T : stuct, { }
}
""",
            TestOptions.RegularNext,
            // (3,41): error CS1031: Type expected
            //     extension<T>(Type) where T : stuct, { }
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(3, 41));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "stuct");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void WithTerminator_OpenBrace_06()
    {
        UsingTree("""
class C
{
    extension<T {
    class D { }
}
""",
            TestOptions.RegularNext,
            // (3,17): error CS1003: Syntax error, '>' expected
            //     extension<T {
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(">").WithLocation(3, 17),
            // (3,17): error CS1003: Syntax error, '(' expected
            //     extension<T {
            Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("(").WithLocation(3, 17),
            // (3,17): error CS1031: Type expected
            //     extension<T {
            Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(3, 17),
            // (3,17): error CS1001: Identifier expected
            //     extension<T {
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(3, 17),
            // (3,17): error CS1026: ) expected
            //     extension<T {
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(3, 17),
            // (5,2): error CS1513: } expected
            // }
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 2));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        M(SyntaxKind.GreaterThanToken);
                    }
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.ReceiverParameter);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.ClassKeyword);
                        N(SyntaxKind.IdentifierToken, "D");
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                M(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void MissingBraces_WithMethod()
    {
        UsingTree("""
class C
{
    extension(Type)
    void M() { }
}
""",
            TestOptions.RegularNext,
            // (3,20): error CS1514: { expected
            //     extension(Type)
            Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(3, 20),
            // (3,20): error CS1513: } expected
            //     extension(Type)
            Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 20));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.ExtensionContainer);
                {
                    N(SyntaxKind.ExtensionKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.ReceiverParameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Type");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
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

    [Fact]
    public void SyntaxFactsAPIs()
    {
        Assert.Equal("extension", SyntaxFacts.GetText(SyntaxKind.ExtensionKeyword));
        Assert.Contains(SyntaxKind.ExtensionKeyword , SyntaxFacts.GetContextualKeywordKinds());
        Assert.True(SyntaxFacts.IsContextualKeyword(SyntaxKind.ExtensionKeyword));
        Assert.Equal(SyntaxKind.ExtensionKeyword, SyntaxFacts.GetContextualKeywordKind("extension"));
    }
}
