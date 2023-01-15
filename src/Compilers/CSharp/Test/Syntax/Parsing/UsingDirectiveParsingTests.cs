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
            UsingTree(
@"using x = ref int;",
                // (1,11): error CS8652: The feature 'using type alias' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // using x = ref int;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref int").WithArguments("using type alias").WithLocation(1, 11),
                // (1,11): error CS9000: Using alias cannot be a 'ref' type.
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
        public void AliasUsingDirectiveNamePointer()
        {
            UsingTree(
@"using x = A*;",
                // (1,11): error CS8652: The feature 'using type alias' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // using x = A*;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "A*").WithArguments("using type alias").WithLocation(1, 11));

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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AliasUsingDirectiveFunctionPointer()
        {
            UsingTree(
@"using x = delegate*<int, void>;",
                // (1,11): error CS8652: The feature 'using type alias' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // using x = delegate*<int, void>;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*<int, void>").WithArguments("using type alias").WithLocation(1, 11));

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
        public void AliasUsingDirectivePredefinedType()
        {
            UsingTree(
@"using x = int;",
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
        public void AliasUsingDirectiveRefType()
        {
            UsingTree(
@"using x = ref int;",
                // (1,11): error CS8652: The feature 'using type alias' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // using x = ref int;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "ref int").WithArguments("using type alias").WithLocation(1, 11),
                // (1,11): error CS9000: Using alias cannot be a 'ref' type.
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
        public void AliasUsingDirectivePredefinedTypePointer()
        {
            UsingTree(
@"using x = int*;",
                // (1,11): error CS8652: The feature 'using type alias' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // using x = int*;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("using type alias").WithLocation(1, 11));

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
        public void AliasUsingDirectiveTuple()
        {
            UsingTree(
@"using x = (int, int);",
                // (1,11): error CS8652: The feature 'using type alias' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // using x = (int, int);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int, int)").WithArguments("using type alias").WithLocation(1, 11));

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
    }
}
