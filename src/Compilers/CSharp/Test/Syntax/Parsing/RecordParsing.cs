// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public sealed class RecordParsingTests : ParsingTests
    {
        private SyntaxTree UsingTree(string text, CSharpParseOptions? options, params DiagnosticDescription[] expectedErrors)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text, options);
            UsingNode(text, tree.GetCompilationUnitRoot(), expectedErrors);
            return tree;
        }

        private SyntaxTree UsingTree(string text, params DiagnosticDescription[] expectedErrors)
            => UsingTree(text, TestOptions.RegularPreview, expectedErrors);

        private new void UsingExpression(string text, params DiagnosticDescription[] expectedErrors)
            => UsingExpression(text, TestOptions.RegularPreview, expectedErrors);

        private new void UsingStatement(string text, params DiagnosticDescription[] expectedErrors)
            => UsingStatement(text, TestOptions.RegularPreview, expectedErrors);

        public RecordParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void RecordParsing01()
        {
            var text = "data class C(int X, int Y);";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.DataKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
            EOF();
        }

        [Fact]
        public void RecordParsing02()
        {
            var text = "class C(int X, int Y);";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
            EOF();
        }

        [Fact]
        public void RecordParsing03()
        {
            var text = "data class C;";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.DataKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.SemicolonToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
            EOF();
        }

        [Fact]
        public void RecordParsing04()
        {
            var text = "class C { public int data; }";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "data");
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            N(SyntaxKind.EndOfFileToken);
            EOF();
        }

        [Fact]
        public void RecordParsing05()
        {
            var tree = ParseTree("class Point;", options: null);
            tree.GetDiagnostics().Verify(
                // (1,12): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.	
                // class Point;	
                Diagnostic(ErrorCode.ERR_FeatureInPreview, ";").WithArguments("records").WithLocation(1, 12)
            );

            UsingNode((CSharpSyntaxNode)tree.GetRoot());

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Point");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void RecordParsing06()
        {
            var tree = ParseTree("interface P;", options: null);
            tree.GetDiagnostics().Verify(
                // (1,12): error CS1514: { expected	
                // interface P;	
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(1, 12),
                // (1,12): error CS1513: } expected	
                // interface P;	
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(1, 12)
            );

            UsingNode((CSharpSyntaxNode)tree.GetRoot());

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "P");
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void RecordParsing07()
        {
            var tree = ParseTree("interface P(int x, int y);", options: null);
            tree.GetDiagnostics().Verify(
                // (1,12): error CS1514: { expected
                // interface P(int x, int y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 12),
                // (1,12): error CS1513: } expected
                // interface P(int x, int y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 12),
                // (1,12): error CS8652: The feature 'simple programs' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // interface P(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y);").WithArguments("simple programs").WithLocation(1, 12),
                // (1,12): error CS9002: Top-level statements must precede namespace and type declarations.
                // interface P(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(1, 12)
            );
        }

        [Fact]
        public void WithParsingLangVer()
        {
            var text = @"
class C
{
    int x = 0 with {};
}";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Regular8);
            tree.GetDiagnostics().Verify(
                // (4,15): error CS8652: The feature 'records' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     int x = 0 with {};
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "with").WithArguments("records").WithLocation(4, 15)
            );
        }

        [Fact]
        public void WithParsing1()
        {
            var text = @"
class C
{
    with { };
    x with { };
    int x = with { };
    int x = 0 with { };
}";
            UsingTree(text,
                // (4,10): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                //     with { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(4, 10),
                // (4,10): error CS1519: Invalid token '{' in class, struct, or interface member declaration
                //     with { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(4, 10),
                // (5,15): error CS1597: Semicolon after method or accessor block is not valid
                //     x with { };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(5, 15),
                // (6,5): error CS9002: Top-level statements must precede namespace and type declarations.
                //     int x = with { };
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "int x = with { ").WithLocation(6, 5),
                // (6,18): error CS1003: Syntax error, ',' expected
                //     int x = with { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(6, 18),
                // (6,20): error CS1002: ; expected
                //     int x = with { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(6, 20),
                // (6,20): error CS1022: Type or namespace definition, or end-of-file expected
                //     int x = with { };
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 20),
                // (8,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(8, 1)
            );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            var n = N(SyntaxKind.IdentifierToken, "with");
                            Assert.Equal(" { ", n.GetTrailingTrivia().ToString());
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IdentifierToken, "with");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "with");
                                    }
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.WithExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "0");
                                        }
                                        N(SyntaxKind.WithKeyword);
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.CloseBraceToken);
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
        public void WithParsing2()
        {
            var text = @"
class C
{
    int M()
    {
        int x = M() with { } + 3;
    }
}";
            UsingTree(text);
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
                            N(SyntaxKind.IntKeyword);
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.AddExpression);
                                            {
                                                N(SyntaxKind.WithExpression);
                                                {
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
                                                    N(SyntaxKind.WithKeyword);
                                                    N(SyntaxKind.OpenBraceToken);
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                                N(SyntaxKind.PlusToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "3");
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing3()
        {
            var text = "0 with {";

            UsingExpression(text,
                // (1,9): error CS1513: } expected
                // 0 with {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 9)
            );

            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                M(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing4()
        {
            var text = "0 with { X";

            UsingExpression(text,
                // (1,11): error CS1513: } expected
                // 0 with { X
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 11)
            );

            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AnonymousObjectMemberDeclarator);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                M(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing5()
        {
            var text = "0 with { X 3 =,";

            UsingExpression(text,
                // (1,12): error CS1003: Syntax error, ',' expected
                // 0 with { X 3 =,
                Diagnostic(ErrorCode.ERR_SyntaxError, "3").WithArguments(",", "").WithLocation(1, 12),
                // (1,15): error CS1525: Invalid expression term ','
                // 0 with { X 3 =,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 15),
                // (1,16): error CS1513: } expected
                // 0 with { X 3 =,
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 16)
            );

            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.AnonymousObjectMemberDeclarator);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.AnonymousObjectMemberDeclarator);
                {
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                        N(SyntaxKind.EqualsToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.CommaToken);
                M(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing6()
        {
            var text = @"M() with { } switch { }";

            UsingExpression(text);

            N(SyntaxKind.SwitchExpression);
            {
                N(SyntaxKind.WithExpression);
                {
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
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing7()
        {
            var text = @"M() with { } + 3";

            UsingExpression(text);

            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.WithExpression);
                {
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
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "3");
                }
            }
            EOF();
        }

        [Fact]
        public void WithParsing8()
        {
            var text = @"M() with { }.ToString()";

            UsingExpression(text,
                // (1,1): error CS1073: Unexpected token '.'
                // M() with { }.ToString()
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "M() with { }").WithArguments(".").WithLocation(1, 1)
            );

            N(SyntaxKind.WithExpression);
            {
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
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing9()
        {
            var text = @"M() with { } with { }";

            UsingExpression(text);

            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.WithExpression);
                {
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
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing10()
        {
            UsingStatement("int x = await with { };", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.WithExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "await");
                                }
                                N(SyntaxKind.WithKeyword);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing11()
        {
            UsingStatement("await with;", options: TestOptions.RegularPreview);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "await");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing12()
        {
            var text = @"M() switch { } with { }";

            UsingExpression(text);

            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.SwitchExpression);
                {
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
                    N(SyntaxKind.SwitchKeyword);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing13()
        {
            var text = @"M(out await with)";
            UsingExpression(text);
            N(SyntaxKind.InvocationExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "M");
                }
                N(SyntaxKind.ArgumentList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.DeclarationExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "await");
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "with");
                            }
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void WithParsing14()
        {
            // Precedence inversion
            var text = @"x is int y with {}";
            UsingExpression(text,
                // (1,12): error CS1073: Unexpected token 'with'
                // x is int y with {}
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "with").WithArguments("with").WithLocation(1, 12)
            );
            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IsPatternExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.DeclarationPattern);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                    }
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing15()
        {
            var text = @"x with { X = ""2"" }";
            UsingExpression(text);
            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.StringLiteralExpression);
                    {
                        N(SyntaxKind.StringLiteralToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing16()
        {
            var text = @"x with { X = ""2"" };";
            // PROTOTYPE
            // The parser doesn't see this as an invalid expression
            // statement, but as a broken declaration, e.g.
            //      x with <missing ,> { X = ""2 "" <missing ;> };
            UsingStatement(text,
                // (1,8): error CS1003: Syntax error, ',' expected
                // x with { X = "2" };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",", "{").WithLocation(1, 8));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "with");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing17()
        {
            var text = @"x = x with { X = ""2"" };";
            UsingStatement(text);
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.WithExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.WithKeyword);
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.StringLiteralExpression);
                            {
                                N(SyntaxKind.StringLiteralToken);
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void WithParsing18()
        {
            var text = @"x with { A = e is T y B = y }";
            UsingExpression(text,
                // (1,23): error CS1003: Syntax error, ',' expected
                // x with { A = e is T y B = y }
                Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",", "").WithLocation(1, 23));
            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IsPatternExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                        N(SyntaxKind.IsKeyword);
                        N(SyntaxKind.DeclarationPattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.SingleVariableDesignation);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "B");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }
    }
}
