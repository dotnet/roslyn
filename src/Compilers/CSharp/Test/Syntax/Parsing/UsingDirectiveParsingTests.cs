// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public sealed class UsingDirectiveParsingTests : ParsingTests
    {
        public UsingDirectiveParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions? options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options ?? TestOptions.Regular9);
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
            UsingTree(text,
                // (1,18): error CS9105: Using alias cannot be a 'ref' type.
                // using static x = ref int;
                Diagnostic(ErrorCode.ERR_BadRefInUsingAlias, "ref int").WithLocation(1, 18));
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
                Diagnostic(ErrorCode.ERR_BadRefInUsingAlias, "ref int").WithLocation(1, 18));

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

            UsingTree(text,
                // (1,7): error CS9106: Only a using alias can be 'unsafe'.
                // using unsafe System;
                Diagnostic(ErrorCode.ERR_BadUnsafeInUsingDirective, "unsafe").WithLocation(1, 7));
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

            UsingTree(text,
                // (1,14): error CS9106: Only a using alias can be 'unsafe'.
                // using static unsafe System.Console;
                Diagnostic(ErrorCode.ERR_BadUnsafeInUsingDirective, "unsafe").WithLocation(1, 14));
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
        public void AliasUsingDirectivePredefinedType()
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
            UsingTree(text,
                // (1,11): error CS9000: Using alias cannot be a 'ref' type.
                // using x = ref int;
                Diagnostic(ErrorCode.ERR_BadRefInUsingAlias, "ref int").WithLocation(1, 11));
            CreateCompilation(text).VerifyDiagnostics(
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using x = ref int;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using x = ref int;").WithLocation(1, 1),
                // (1,7): warning CS8981: The type name 'x' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // using x = ref int;
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "x").WithArguments("x").WithLocation(1, 7),
                // (1,11): error CS9105: Using alias cannot be a 'ref' type.
                // using x = ref int;
                Diagnostic(ErrorCode.ERR_BadRefInUsingAlias, "ref int").WithLocation(1, 11));

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
        public void AliasUsingDirectiveTuple()
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
        public void AliasUsingNullableReferenceType()
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
                // (1,17): error CS9107: Using alias cannot be a nullable reference type..
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
    }
}
