// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public sealed class UsingDirectiveParsingTests : ParsingTests
{
    public UsingDirectiveParsingTests(ITestOutputHelper output) : base(output) { }

    protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
    {
        return SyntaxFactory.ParseSyntaxTree(text, options: options ?? TestOptions.RegularPreview);
    }

    [Fact]
    public void SimpleUsingDirectiveNamePointer()
    {
        UsingTree(
@"using A*;",
            // (1,8): error CS1002: ; expected
            // using A*;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "*").WithLocation(1, 8),
            // (1,9): error CS1525: Invalid expression term ';'
            // using A*;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(1, 9));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.PointerIndirectionExpression);
                    {
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.IdentifierName);
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
    public void SimpleUsingDirectiveRefType()
    {
        UsingTree(
@"using ref int;",
            // (1,14): error CS1001: Identifier expected
            // using ref int;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 14));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
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
    }

    [Fact]
    public void SimpleUsingDirectiveFunctionPointer()
    {
        UsingTree(
@"using delegate*<int, void>;",
            // (1,27): error CS1001: Identifier expected
            // using delegate*<int, void>;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 27));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.VariableDeclaration);
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
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
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
    public void SimpleUsingDirectivePredefinedType()
    {
        UsingTree(
@"using int;",
            // (1,10): error CS1001: Identifier expected
            // using int;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 10));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
    public void SimpleUsingDirectivePredefinedTypePointer()
    {
        UsingTree(
@"using int*;",
            // (1,11): error CS1001: Identifier expected
            // using int*;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.AsteriskToken);
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
    public void SimpleUsingDirectiveTuple()
    {
        UsingTree(
@"using (int, int);",
            // (1,11): error CS1001: Identifier expected
            // using (int, int);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(1, 11),
            // (1,13): error CS1001: Identifier expected
            // using (int, int);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 13),
            // (1,13): error CS1026: ) expected
            // using (int, int);
            Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(1, 13),
            // (1,16): error CS1001: Identifier expected
            // using (int, int);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 16),
            // (1,16): error CS1002: ; expected
            // using (int, int);
            Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(1, 16),
            // (1,16): error CS1022: Type or namespace definition, or end-of-file expected
            // using (int, int);
            Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(1, 16));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.UsingStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
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
    public void StaticUsingDirectiveNamePointer()
    {
        UsingTree(
@"using static A*;",
            // (1,15): error CS1002: ; expected
            // using static A*;
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "*").WithLocation(1, 15),
            // (1,16): error CS1525: Invalid expression term ';'
            // using static A*;
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(1, 16));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "A");
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.PointerIndirectionExpression);
                    {
                        N(SyntaxKind.AsteriskToken);
                        M(SyntaxKind.IdentifierName);
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
    public void StaticUsingDirectiveRefType()
    {
        var text = @"using static x = ref int;";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using static x = ref int;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static x = ref int;").WithLocation(1, 1),
            // (1,14): error CS8085: A 'using static' directive cannot be used to declare an alias
            // using static x = ref int;
            Diagnostic(ErrorCode.ERR_NoAliasHere, "x").WithLocation(1, 14),
            // (1,14): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using static x = ref int;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 14),
            // (1,18): error CS9105: Using alias cannot be a 'ref' type.
            // using static x = ref int;
            Diagnostic(ErrorCode.ERR_BadRefInUsingAlias, "ref").WithLocation(1, 18));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void StaticUsingDirectiveFunctionPointer()
    {
        UsingTree(
@"using static delegate*<int, void>;",
            // (1,7): error CS0106: The modifier 'static' is not valid for this item
            // using static delegate*<int, void>;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(1, 7),
            // (1,34): error CS1001: Identifier expected
            // using static delegate*<int, void>;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 34));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.VariableDeclaration);
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
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
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
    public void StaticUsingDirectivePredefinedType()
    {
        UsingTree(
@"using static int;",
            // (1,7): error CS0106: The modifier 'static' is not valid for this item
            // using static int;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(1, 7),
            // (1,17): error CS1001: Identifier expected
            // using static int;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
    public void StaticUsingDirectivePredefinedTypePointer()
    {
        UsingTree(
@"using static int*;",
            // (1,7): error CS0106: The modifier 'static' is not valid for this item
            // using static int*;
            Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(1, 7),
            // (1,18): error CS1001: Identifier expected
            // using static int*;
            Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 18));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PointerType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.AsteriskToken);
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
    public void StaticUsingDirectiveTuple()
    {
        UsingTree(
@"using static (int, int);",
            // (1,14): error CS1001: Identifier expected
            // using static (int, int);
            Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 14),
            // (1,14): error CS1002: ; expected
            // using static (int, int);
            Diagnostic(ErrorCode.ERR_SemicolonExpected, "(").WithLocation(1, 14),
            // (1,15): error CS1525: Invalid expression term 'int'
            // using static (int, int);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 15),
            // (1,20): error CS1525: Invalid expression term 'int'
            // using static (int, int);
            Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 20));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.StaticKeyword);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.GlobalStatement);
            {
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.TupleExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
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

    [Fact]
    public void AliasUsingDirectiveNamePointer1()
    {
        var text =
@"using x = A*;

struct A { }";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = A*;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = A*;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = A*;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7),
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using x = A*;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "A*").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "A");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectiveNamePointer2()
    {
        var text =
@"using unsafe x = A*;

struct A { }";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using unsafe x = A*;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe x = A*;").WithLocation(1, 1),
            // (1,14): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using unsafe x = A*;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 14));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "A");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectiveFunctionPointer1()
    {
        var text = @"using x = delegate*<int, void>;";

        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = delegate*<int, void>;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = delegate*<int, void>;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = delegate*<int, void>;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7),
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using x = delegate*<int, void>;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
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
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
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
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectiveFunctionPointer2()
    {
        var text = @"using unsafe x = delegate*<int, void>;";

        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using unsafe x = delegate*<int, void>;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe x = delegate*<int, void>;").WithLocation(1, 1),
            // (1,14): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using unsafe x = delegate*<int, void>;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 14));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
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
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.CommaToken);
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
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void UsingUnsafeNonAlias()
    {
        var text = @"using unsafe System;";

        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using unsafe System;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe System;").WithLocation(1, 1),
            // (1,7): error CS9106: Only a using alias can be 'unsafe'.
            // using unsafe System;
            Diagnostic(ErrorCode.ERR_BadUnsafeInUsingDirective, "unsafe").WithLocation(1, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void UsingStaticUnsafeNonAlias()
    {
        var text = @"using static unsafe System.Console;";

        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using static unsafe System.Console;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static unsafe System.Console;").WithLocation(1, 1),
            // (1,14): error CS9106: Only a using alias can be 'unsafe'.
            // using static unsafe System.Console;
            Diagnostic(ErrorCode.ERR_BadUnsafeInUsingDirective, "unsafe").WithLocation(1, 14));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Console");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectivePredefinedType_CSharp11()
    {
        var text = @"using x = int;";
        UsingTree(text);
        CreateCompilation(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = int;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = int;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = int;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7),
            // (1,11): error CS8652: The feature 'using type alias' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // using x = int;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int").WithArguments("using type alias").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectivePredefinedType_CSharp12()
    {
        var text = @"using x = int;";
        UsingTree(text);
        CreateCompilation(text, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = int;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = int;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = int;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectivePredefinedType_Preview()
    {
        var text = @"using x = int;";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = int;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = int;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = int;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectiveRefType()
    {
        var text = @"using x = ref int;";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = ref int;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = ref int;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = ref int;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7),
            // (1,11): error CS9105: Using alias cannot be a 'ref' type.
            // using x = ref int;
            Diagnostic(ErrorCode.ERR_BadRefInUsingAlias, "ref").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectivePredefinedTypePointer1()
    {
        var text = @"using x = int*;";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = int*;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = int*;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = int*;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7),
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using x = int*;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectivePredefinedTypePointer2()
    {
        var text = @"using unsafe x = int*;";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using unsafe x = int*;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe x = int*;").WithLocation(1, 1),
            // (1,14): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using unsafe x = int*;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 14));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectivePredefinedTypePointer3()
    {
        var text = @"
using unsafe X = int*;

namespace N
{
    using Y = X;
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (6,5): hidden CS8019: Unnecessary using directive.
            //     using Y = X;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = X;").WithLocation(6, 5),
            // (6,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     using Y = X;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(6, 15));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
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
    public void AliasUsingDirectivePredefinedTypePointer4()
    {
        var text = @"
using unsafe X = int*;

namespace N
{
    using unsafe Y = X;
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (6,5): hidden CS8019: Unnecessary using directive.
            //     using unsafe Y = X;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe Y = X;").WithLocation(6, 5));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
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
    public void AliasUsingDirectivePredefinedTypePointer5()
    {
        var text = @"
using X = int*;

namespace N
{
    using unsafe Y = X;
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (2,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using X = int*;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 11),
            // (6,5): hidden CS8019: Unnecessary using directive.
            //     using unsafe Y = X;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe Y = X;").WithLocation(6, 5));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
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
    public void AliasUsingDirectivePredefinedTypePointer6()
    {
        var text = @"
using unsafe X = int*;

namespace N
{
    using Y = X[];
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (6,5): hidden CS8019: Unnecessary using directive.
            //     using Y = X[];
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = X[];").WithLocation(6, 5),
            // (6,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     using Y = X[];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(6, 15));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectivePredefinedTypePointer7()
    {
        var text = @"
using unsafe X = int*;

namespace N
{
    using unsafe Y = X[];
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (6,5): hidden CS8019: Unnecessary using directive.
            //     using unsafe Y = X[];
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using unsafe Y = X[];").WithLocation(6, 5));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.ArrayType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectiveTuple1()
    {
        var text = @"using x = (int, int);";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = (int, int);
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = (int, int);").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = (int, int);
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
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
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectiveTuple2()
    {
        var text = """
            using X = (int, int);

            class C
            {
                X x = (0, 0);
            }
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (5,7): warning CS0414: The field 'C.x' is assigned but its value is never used
            //     X x = (0, 0);
            Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "x").WithArguments("C.x").WithLocation(5, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
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
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.TupleExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "0");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "0");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
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

    [Fact]
    public void AliasUsingDirectiveTuple3()
    {
        var text = """
            using X = (int, int);

            class C
            {
                X x = (true, false);
            }
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (5,12): error CS0029: Cannot implicitly convert type 'bool' to 'int'
            //     X x = (true, false);
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "true").WithArguments("bool", "int").WithLocation(5, 12),
            // (5,18): error CS0029: Cannot implicitly convert type 'bool' to 'int'
            //     X x = (true, false);
            Diagnostic(ErrorCode.ERR_NoImplicitConv, "false").WithArguments("bool", "int").WithLocation(5, 18));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
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
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.TupleExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.TrueLiteralExpression);
                                        {
                                            N(SyntaxKind.TrueKeyword);
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.FalseLiteralExpression);
                                        {
                                            N(SyntaxKind.FalseKeyword);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
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

    [Fact]
    public void AliasUsingNullableValueType()
    {
        var text = @"using x = int?;";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = int?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = int?;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = int?;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingNullableReferenceType1()
    {
        var text = @"using x = string?;";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using x = string?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = string?;").WithLocation(1, 1),
            // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
            // using x = string?;
            Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7),
            // (1,17): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            // using x = string?;
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(1, 17),
            // (1,17): error CS9107: Using alias cannot be a nullable reference type.
            // using x = string?;
            Diagnostic(ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias, "?").WithLocation(1, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingNullableReferenceType2()
    {
        var text = """
            #nullable enable
            using X = string?;
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using X = string?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = string?;").WithLocation(2, 1),
            // (2,17): error CS9107: Using alias cannot be a nullable reference type.
            // using X = string?;
            Diagnostic(ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias, "?").WithLocation(2, 17));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingNullableReferenceType3()
    {
        var text = """
            using X = string;
            namespace N
            {
                using Y = X?;
            }
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (4,5): hidden CS8019: Unnecessary using directive.
            //     using Y = X?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = X?;").WithLocation(4, 5),
            // (4,16): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            //     using Y = X?;
            Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotation, "?").WithLocation(4, 16),
            // (4,16): error CS9107: Using alias cannot be a nullable reference type.
            //     using Y = X?;
            Diagnostic(ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias, "?").WithLocation(4, 16));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.QuestionToken);
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
    public void AliasUsingNullableReferenceType4()
    {
        var text = """
            #nullable enable
            using X = string;
            namespace N
            {
                using Y = X?;
            }
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (5,5): hidden CS8019: Unnecessary using directive.
            //     using Y = X?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = X?;").WithLocation(5, 5),
            // (5,16): error CS9107: Using alias cannot be a nullable reference type.
            //     using Y = X?;
            Diagnostic(ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias, "?").WithLocation(5, 16));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.NamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.QuestionToken);
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
    public void AliasUsingVoidPointer1()
    {
        var text = @"using unsafe VP = void*;

class C
{
    void M(VP vp) { }
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (5,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     void M(VP vp) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "VP").WithLocation(5, 12));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "VP");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "VP");
                            }
                            N(SyntaxKind.IdentifierToken, "vp");
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
    }

    [Fact]
    public void AliasUsingVoidPointer2()
    {
        var text = @"using unsafe VP = void*;

class C
{
    unsafe void M(VP vp) { }
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics();

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.UnsafeKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "VP");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "VP");
                            }
                            N(SyntaxKind.IdentifierToken, "vp");
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
    }

    [Fact]
    public void AliasUsingVoidPointer3()
    {
        var text = @"using VP = void*;

class C
{
    unsafe void M(VP vp) { }
}";
        UsingTree(text);
        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (1,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using VP = void*;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "void*").WithLocation(1, 12));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "VP");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PointerType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.AsteriskToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.UnsafeKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "VP");
                            }
                            N(SyntaxKind.IdentifierToken, "vp");
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
    }

    [Fact]
    public void AliasUsingVoid1()
    {
        var text = @"using V = void;

class C
{
    void M(V v) { }
}";
        UsingTree(text,
            // (1,11): error CS1547: Keyword 'void' cannot be used in this context
            // using V = void;
            Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(1, 11));

        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (1,11): error CS1547: Keyword 'void' cannot be used in this context
            // using V = void;
            Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "V");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "V");
                            }
                            N(SyntaxKind.IdentifierToken, "v");
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
    }

    [Fact]
    public void AliasUsingVoid2()
    {
        var text = @"using V = void;

class C
{
    V M() { }
}";
        UsingTree(text,
            // (1,11): error CS1547: Keyword 'void' cannot be used in this context
            // using V = void;
            Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(1, 11));

        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (1,11): error CS1547: Keyword 'void' cannot be used in this context
            // using V = void;
            Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(1, 11));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "V");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "V");
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
    public void AliasUsingVoid3()
    {
        var text = @"using V = void[];

class C
{
    V M() { }
}";
        UsingTree(text,
            // (1,11): error CS1547: Keyword 'void' cannot be used in this context
            // using V = void;
            Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(1, 11));

        CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
            // (1,11): error CS1547: Keyword 'void' cannot be used in this context
            // using V = void[];
            Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(1, 11),
            // (5,7): error CS0161: 'C.M()': not all code paths return a value
            //     V M() { }
            Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M()").WithLocation(5, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "V");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.VoidKeyword);
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
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "C");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "V");
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
    public void UsingDirectiveDynamic1()
    {
        var text = @"
using dynamic;";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using dynamic;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using dynamic;").WithLocation(2, 1),
            // (2,7): error CS0246: The type or namespace name 'dynamic' could not be found (are you missing a using directive or an assembly reference?)
            // using dynamic;
            Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "dynamic").WithArguments("dynamic").WithLocation(2, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "dynamic");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDirectiveDynamic1()
    {
        var text = @"
using D = dynamic;

class C
{
    void M(D d)
    {
        d.Goo();
    }
}";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics();

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "dynamic");
                }
                N(SyntaxKind.SemicolonToken);
            }
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "D");
                            }
                            N(SyntaxKind.IdentifierToken, "d");
                        }
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
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "d");
                                    }
                                    N(SyntaxKind.DotToken);
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
    public void AliasUsingDirectiveDynamic2()
    {
        var text = @"
using D = System.Collections.Generic.List<dynamic>;

class C
{
    void M(D d)
    {
        d[0].Goo();
    }
}";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics();

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "System");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Collections");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Generic");
                        }
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "List");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "dynamic");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "D");
                            }
                            N(SyntaxKind.IdentifierToken, "d");
                        }
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
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "d");
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
    public void AliasUsingDirectiveDynamic3()
    {
        var text = @"
using D = dynamic[];

class C
{
    void M(D d)
    {
        d[0].Goo();
    }
}";
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics();

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "D");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "dynamic");
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
                N(SyntaxKind.SemicolonToken);
            }
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
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "D");
                            }
                            N(SyntaxKind.IdentifierToken, "d");
                        }
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
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "d");
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
    public void AliasUsingDuplicate1()
    {
        var text = """
            using X = int?;
            using X = System;
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using X = int?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = int?;").WithLocation(1, 1),
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using X = System;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = System;").WithLocation(2, 1),
            // (2,7): error CS1537: The using alias 'X' appeared previously in this namespace
            // using X = System;
            Diagnostic(ErrorCode.ERR_DuplicateAlias, "X").WithArguments("X").WithLocation(2, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "System");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDuplicate2()
    {
        var text = """
            using X = int?;
            using X = int;
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using X = int?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = int?;").WithLocation(1, 1),
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using X = int;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = int;").WithLocation(2, 1),
            // (2,7): error CS1537: The using alias 'X' appeared previously in this namespace
            // using X = int;
            Diagnostic(ErrorCode.ERR_DuplicateAlias, "X").WithArguments("X").WithLocation(2, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingDuplicate3()
    {
        var text = """
            using X = int?;
            using X = System.Int32;
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using X = int?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = int?;").WithLocation(1, 1),
            // (2,1): hidden CS8019: Unnecessary using directive.
            // using X = System.Int32;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = System.Int32;").WithLocation(2, 1),
            // (2,7): error CS1537: The using alias 'X' appeared previously in this namespace
            // using X = System.Int32;
            Diagnostic(ErrorCode.ERR_DuplicateAlias, "X").WithArguments("X").WithLocation(2, 7));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "System");
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Int32");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void AliasUsingNotDuplicate1()
    {
        var text = """
            using X = int?;
            namespace N;
            using X = int;
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (1,1): hidden CS8019: Unnecessary using directive.
            // using X = int?;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = int?;").WithLocation(1, 1),
            // (3,1): hidden CS8019: Unnecessary using directive.
            // using X = int;
            Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = int;").WithLocation(3, 1));

        N(SyntaxKind.CompilationUnit);
        {
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.NullableType);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.QuestionToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.FileScopedNamespaceDeclaration);
            {
                N(SyntaxKind.NamespaceKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "N");
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.UsingDirective);
                {
                    N(SyntaxKind.UsingKeyword);
                    N(SyntaxKind.NameEquals);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.EqualsToken);
                    }
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
        }
        EOF();
    }

    [Fact]
    public void TestObsolete1()
    {
        var text = """
            using System;
            using X = C;

            [Obsolete("", error: true)]
            class C
            {
            }

            class D
            {
                X x;
                C c;
            }
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (11,5): error CS0619: 'C' is obsolete: ''
            //     X x;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "X").WithArguments("C", "").WithLocation(11, 5),
            // (11,7): warning CS0169: The field 'D.x' is never used
            //     X x;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("D.x").WithLocation(11, 7),
            // (12,5): error CS0619: 'C' is obsolete: ''
            //     C c;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "").WithLocation(12, 5),
            // (12,7): warning CS0169: The field 'D.c' is never used
            //     C c;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "c").WithArguments("D.c").WithLocation(12, 7));

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
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Obsolete");
                        }
                        N(SyntaxKind.AttributeArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.AttributeArgument);
                            {
                                N(SyntaxKind.StringLiteralExpression);
                                {
                                    N(SyntaxKind.StringLiteralToken, "\"\"");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.AttributeArgument);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "error");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
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
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "D");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
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

    [Fact]
    public void TestObsolete2()
    {
        var text = """
            using System;
            using X = C[];

            [Obsolete("", error: true)]
            class C
            {
            }

            class D
            {
                X x1;
                C[] c1;
            }
            """;
        UsingTree(text);
        CreateCompilation(text).VerifyDiagnostics(
            // (11,5): error CS0619: 'C' is obsolete: ''
            //     X x1;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "X").WithArguments("C", "").WithLocation(11, 5),
            // (11,7): warning CS0169: The field 'D.x1' is never used
            //     X x1;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "x1").WithArguments("D.x1").WithLocation(11, 7),
            // (12,5): error CS0619: 'C' is obsolete: ''
            //     C[] c1;
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "C").WithArguments("C", "").WithLocation(12, 5),
            // (12,9): warning CS0169: The field 'D.c1' is never used
            //     C[] c1;
            Diagnostic(ErrorCode.WRN_UnreferencedField, "c1").WithArguments("D.c1").WithLocation(12, 9));

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
            N(SyntaxKind.UsingDirective);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.NameEquals);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                }
                N(SyntaxKind.ArrayType);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
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
                N(SyntaxKind.SemicolonToken);
            }
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.AttributeList);
                {
                    N(SyntaxKind.OpenBracketToken);
                    N(SyntaxKind.Attribute);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Obsolete");
                        }
                        N(SyntaxKind.AttributeArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.AttributeArgument);
                            {
                                N(SyntaxKind.StringLiteralExpression);
                                {
                                    N(SyntaxKind.StringLiteralToken, "\"\"");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.AttributeArgument);
                            {
                                N(SyntaxKind.NameColon);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "error");
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.TrueLiteralExpression);
                                {
                                    N(SyntaxKind.TrueKeyword);
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
            N(SyntaxKind.ClassDeclaration);
            {
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "D");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x1");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
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
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "c1");
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
