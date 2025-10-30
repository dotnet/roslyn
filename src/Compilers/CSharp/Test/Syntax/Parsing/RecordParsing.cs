// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public sealed class RecordParsingTests : ParsingTests
    {
        private new SyntaxTree UsingTree(string text, params DiagnosticDescription[] expectedErrors)
            => UsingTree(text, TestOptions.Regular9, expectedErrors);

        private new void UsingExpression(string text, params DiagnosticDescription[] expectedErrors)
            => UsingExpression(text, TestOptions.Regular9, expectedErrors);

        private new void UsingStatement(string text, params DiagnosticDescription[] expectedErrors)
            => UsingStatement(text, TestOptions.Regular9, expectedErrors);

        public RecordParsingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void FieldNamedData()
        {
            var text = @"
class C
{
    int data;
}";
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
        public void RecordParsing01()
        {
            var text = "record C(int X, int Y);";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            // In langversion 8, this is a method
            UsingTree(text, options: TestOptions.Regular8);
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "record C(int X, int Y);").WithArguments("top-level statements", "9.0").WithLocation(1, 1),
                // (1,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 1),
                // (1,8): error CS8112: Local function 'C(int, int)' must declare a body because it is not marked 'static extern'.
                // record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "C").WithArguments("C(int, int)").WithLocation(1, 8),
                // (1,8): warning CS8321: The local function 'C' is declared but never used
                // record C(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "C").WithArguments("C").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
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
            }
            EOF();
        }

        [Fact]
        public void RecordParsing02()
        {
            var text = "record C(int X, int Y);";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void RecordParsing03()
        {
            var text = "record C;";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void RecordParsing04()
        {
            var text = "record C { public int record; }";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
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
                                N(SyntaxKind.IdentifierToken, "record");
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
        public void RecordParsing05()
        {
            var text = "record Point;";
            var tree = ParseTree(text, options: TestOptions.Regular8);
            tree.GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // record Point;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "record Point;").WithArguments("top-level statements", "9.0").WithLocation(1, 1),
                // (1,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record Point;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 1),
                // (1,8): warning CS0168: The variable 'Point' is declared but never used
                // record Point;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Point").WithArguments("Point").WithLocation(1, 8));

            UsingNode((CSharpSyntaxNode)tree.GetRoot());

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
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "Point");
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
        public void RecordParsing06()
        {
            var tree = ParseTree("interface P;", options: null);
            tree.GetDiagnostics().Verify();

            UsingNode((CSharpSyntaxNode)tree.GetRoot());

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void RecordParsing07()
        {
            var text = "interface P(int x, int y);";
            var tree = ParseTree(text, options: TestOptions.Regular8);
            tree.GetDiagnostics().Verify();

            tree = ParseTree(text, options: TestOptions.RegularPreview);
            tree.GetDiagnostics().Verify();

            var comp = CreateCompilation(text).VerifyDiagnostics(
                // (1,12): error CS9122: Unexpected parameter list.
                // interface P(int x, int y);
                Diagnostic(ErrorCode.ERR_UnexpectedParameterList, "(int x, int y)").WithLocation(1, 12)
                );

            Assert.Empty(comp.GetTypeByMetadataName("P").GetMembers());
            Assert.Null(((SourceMemberContainerTypeSymbol)comp.GetTypeByMetadataName("P")).PrimaryConstructor);

            UsingNode(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "P");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "y");
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
        public void RecordParsingAmbiguities()
        {
            var text = @"
record R1() { return null; }
abstract record D
{
    record R2() { return null; }
    abstract record R3();
}";
            UsingTree(text,
                // (2,15): error CS1519: Invalid token 'return' in class, record, struct, or interface member declaration
                // record R1() { return null; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "return").WithArguments("return").WithLocation(2, 15),
                // (5,19): error CS1519: Invalid token 'return' in class, record, struct, or interface member declaration
                //     record R2() { return null; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "return").WithArguments("return").WithLocation(5, 19));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.AbstractKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "D");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "R2");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.AbstractKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "R3");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_ConstraintAndSemiColon()
        {
            UsingTree("record R<T> where T : class;");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_ConstraintAndSemiColon_MissingColon()
        {
            UsingTree("record R<T> where T   class;",
                // (1,23): error CS1003: Syntax error, ':' expected
                // record R<T> where T   class;
                Diagnostic(ErrorCode.ERR_SyntaxError, "class").WithArguments(":").WithLocation(1, 23)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        M(SyntaxKind.ColonToken);
                        N(SyntaxKind.ClassConstraint);
                        {
                            N(SyntaxKind.ClassKeyword);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_TwoConstraintsAndSemiColon()
        {
            UsingTree("record R<T1, T2> where T1 : class where T2 : class;");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_ConstraintAndSemiColon_Class()
        {
            UsingTree("abstract class C<T> where T : class;");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.AbstractKeyword);
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_TwoConstraintsAndSemiColon_Class()
        {
            UsingTree("abstract class C<T1, T2> where T1 : class where T2 : class;");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.AbstractKeyword);
                    N(SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void AbstractMethod_ConstraintsAndSemiColon()
        {
            UsingTree("abstract record R { abstract void M<T>() where T : class; }");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.AbstractKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AbstractKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.TypeParameterList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.TypeParameter);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
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
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void RecordParsing_BlockBodyAndSemiColon()
        {
            // Type declarations allow an optional semi-colon after block body
            var text = "record C { };";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestClassWithMultipleConstraints001()
        {
            UsingTree("class a<b> where b : c where b { }",
                // (1,32): error CS1003: Syntax error, ':' expected
                // class a<b> where b : c where b { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(":").WithLocation(1, 32),
                // (1,32): error CS1031: Type expected
                // class a<b> where b : c where b { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(1, 32)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestClassWithMultipleConstraints002()
        {
            UsingTree("class a<b> where b : c where { }",
                // (1,30): error CS1001: Identifier expected
                // class a<b> where b : c where { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(1, 30),
                // (1,30): error CS1003: Syntax error, ':' expected
                // class a<b> where b : c where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(":").WithLocation(1, 30),
                // (1,30): error CS1031: Type expected
                // class a<b> where b : c where { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "{").WithLocation(1, 30)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "a");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.TypeParameterConstraintClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                    }
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_ConstraintsAndCurlyBraces()
        {
            UsingTree("record R<T> where T : class { }");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
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

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_ConstraintAndCommaAndSemiColon()
        {
            UsingTree("record R<T> where T : class, ;",
                // (1,30): error CS1031: Type expected
                // record R<T> where T : class, ;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(1, 30)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
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
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_ConstraintAndCommaAndNewAndSemiColon()
        {
            UsingTree("record R<T> where T : class, new();");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "R");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        N(SyntaxKind.TypeParameter);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.GreaterThanToken);
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
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.ConstructorConstraint);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestWhereWhere()
        {
            UsingTree("public class Goo<T> : System.Object where where { }",
                // (1,37): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",").WithLocation(1, 37),
                // (1,43): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",").WithLocation(1, 43)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Goo");
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
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "System");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Object");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "where");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "where");
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
        public void TestWhereWhereWhere()
        {
            UsingTree("public class Goo<T> : System.Object where where where { }",
                // (1,37): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",").WithLocation(1, 37),
                // (1,43): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",").WithLocation(1, 43),
                // (1,49): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",").WithLocation(1, 49)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Goo");
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
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "System");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Object");
                                }
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "where");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "where");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "where");
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
        public void WithParsingLangVer()
        {
            var text = @"
class C
{
    int x = 0 with {};
}";
            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (4,13): error CS8400: Feature 'with on structs' is not available in C# 8.0. Please use language version 10.0 or greater.
                //     int x = 0 with {};
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "0 with {}").WithArguments("with on structs", "10.0").WithLocation(4, 13),
                // (4,15): error CS8400: Feature 'records' is not available in C# 8.0. Please use language version 9.0 or greater.
                //     int x = 0 with {};
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "with").WithArguments("records", "9.0").WithLocation(4, 15));

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.Regular8);
            tree.GetDiagnostics().Verify();
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
                // (4,10): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //     with { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(4, 10),
                // (4,10): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //     with { };
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(4, 10),
                // (5,15): error CS1597: Semicolon after method or accessor block is not valid
                //     x with { };
                Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";").WithLocation(5, 15),
                // (6,5): error CS8803: Top-level statements must precede namespace and type declarations.
                //     int x = with { };
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "int x = with { ").WithLocation(6, 5),
                // (6,18): error CS1003: Syntax error, ',' expected
                //     int x = with { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(6, 18),
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
                            N(SyntaxKind.IdentifierToken, "with");
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
                                        N(SyntaxKind.WithInitializerExpression);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.CloseBraceToken);
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
                                                    N(SyntaxKind.WithInitializerExpression);
                                                    {
                                                        N(SyntaxKind.OpenBraceToken);
                                                        N(SyntaxKind.CloseBraceToken);
                                                    }
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
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
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
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "3").WithArguments(",").WithLocation(1, 12),
                // (1,15): error CS1525: Invalid expression term ','
                // 0 with { X 3 =,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 15),
                // (1,16): error CS1513: } expected
                // 0 with { X 3 =,
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 16));

            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "0");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                    M(SyntaxKind.CommaToken);
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
                    N(SyntaxKind.CommaToken);
                    M(SyntaxKind.CloseBraceToken);
                }
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
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
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
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
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
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void WithParsing10()
        {
            UsingStatement("int x = await with { };", options: TestOptions.Regular9);
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
                                N(SyntaxKind.WithInitializerExpression);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
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
            UsingStatement("await with;", options: TestOptions.Regular9);
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
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
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
                N(SyntaxKind.WithInitializerExpression);
                {
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
            EOF();
        }

        [Fact]
        public void WithParsing16()
        {
            var text = @"x with { X = ""2"" };";
            // https://github.com/dotnet/roslyn/issues/44688
            // The parser doesn't see this as an invalid expression
            // statement, but as a broken declaration, e.g.
            //      x with <missing ,> { X = ""2 "" <missing ;> };
            UsingStatement(text,
                // (1,8): error CS1003: Syntax error, ',' expected
                // x with { X = "2" };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 8));
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
                        N(SyntaxKind.WithInitializerExpression);
                        {
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",").WithLocation(1, 23));
            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.WithInitializerExpression);
                {
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
            }
            EOF();
        }

        [Fact]
        public void WithParsing19()
        {
            var x = @"x with { , A = 10 }";

            UsingExpression(x,
                // (1,10): error CS1525: Invalid expression term ','
                // x with { , A = 10 }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 10));
            N(SyntaxKind.WithExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "x");
                }
                N(SyntaxKind.WithKeyword);
                N(SyntaxKind.WithInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "10");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75074")]
        public void WithParsingInConditionalExpression1()
        {
            var text = "x is X ? record with { } : record with { }";

            UsingExpression(text);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.WithExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.WithExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75074")]
        public void WithParsingInConditionalExpression2()
        {
            var text = "x is X.Y ? record with { } : record with { }";

            UsingExpression(text);
            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "X");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Y");
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.WithExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.WithExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
                    N(SyntaxKind.WithKeyword);
                    N(SyntaxKind.WithInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75074")]
        public void WithParsingInConditionalExpression_Incomplete()
        {
            var text = "x is X ? record with";

            UsingExpression(text,
                // (1,17): error CS1003: Syntax error, ':' expected
                // x is X ? record with
                Diagnostic(ErrorCode.ERR_SyntaxError, "with").WithArguments(":").WithLocation(1, 17));

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "X");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "record");
                }
                M(SyntaxKind.ColonToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "with");
                }
            }
            EOF();
        }

        [Fact]
        public void ParameterListAndBaseListOnClass()
        {
            var text = @$"
class C(int X, int Y)
: B(X, Y)
{{ }}";
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
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.PrimaryConstructorBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
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
        }

        [Theory]
        [CombinatorialData]
        public void Base_01(
            [CombinatorialValues("record")] string typeKeyword,
            [CombinatorialValues(true, false)] bool withParameters,
            [CombinatorialValues(true, false)] bool withBaseArguments,
            [CombinatorialValues(true, false)] bool withBody)
        {
            var text = typeKeyword + " C" + (withParameters ? "(int X, int Y)" : "") + @"
: B" + (withBaseArguments ? "(X, Y)" : "") + @"
" + (withBody ? "{ }" : ";");

            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    if (withParameters)
                    {
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
                    }

                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(withBaseArguments ? SyntaxKind.PrimaryConstructorBaseType : SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }

                            if (withBaseArguments)
                            {
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "X");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Y");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                    }

                    if (withBody)
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    else
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [CombinatorialData]
        public void Base_02([CombinatorialValues(true, false)] bool withBody)
        {
            var text = "record C(int X, int Y)" + @"
: B, D(X, Y)" + @"
" + (withBody ? "{ }" : ";");

            UsingTree(text,
                // (2,7): error CS1003: Syntax error, ',' expected
                // : B, D(X, Y)
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(2, 7),
                // (2,8): error CS1003: Syntax error, ',' expected
                // : B, D(X, Y)
                Diagnostic(ErrorCode.ERR_SyntaxError, "X").WithArguments(",").WithLocation(2, 8),
                // (2,12): error CS1003: Syntax error, ',' expected
                // : B, D(X, Y)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
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
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "D");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Y");
                            }
                        }
                    }
                    if (withBody)
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    else
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Base_03()
        {
            var text = "interface C : B;";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Base_04()
        {
            var text = "interface C(int X, int Y) : B;";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
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
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Base_05()
        {
            var text = "interface C : B(X, Y);";
            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.PrimaryConstructorBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
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
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_RecordNamedStruct()
        {
            var text = "record struct(int X, int Y);";

            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record struct(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(1, 8),
                // (1,14): error CS1001: Identifier expected
                // record struct(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 14));
            UsingTree(text, options: TestOptions.Regular9,
                // (1,14): error CS1001: Identifier expected
                // record struct(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 14));
            verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (1,14): error CS1001: Identifier expected
                // record struct(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 14));
            UsingTree(text, options: TestOptions.Regular10,
                // (1,14): error CS1001: Identifier expected
                // record struct(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 14));
            verify();

            void verify()
            {
                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.RecordStructDeclaration);
                    {
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.StructKeyword);
                        M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing()
        {
            var text = "record struct C(int X, int Y);";

            CreateCompilation(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics();
            UsingTree(text, options: TestOptions.Regular10);
            verifyParsedAsRecord();

            CreateCompilation(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics();
            UsingTree(text, options: TestOptions.Regular9);
            verifyParsedAsRecord();

            UsingTree(text, options: TestOptions.Regular8,
                // (1,8): error CS1001: Identifier expected
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 8)
                );

            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "record ").WithArguments("top-level statements", "9.0").WithLocation(1, 1),
                // (1,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 1),
                // (1,8): error CS1001: Identifier expected
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 8),
                // (1,16): error CS8400: Feature 'primary constructors' is not available in C# 8.0. Please use language version 12.0 or greater.
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "(int X, int Y)").WithArguments("primary constructors", "12.0").WithLocation(1, 16),
                // (1,21): warning CS9113: Parameter 'X' is unread.
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "X").WithArguments("X").WithLocation(1, 21),
                // (1,28): warning CS9113: Parameter 'Y' is unread.
                // record struct C(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Y").WithArguments("Y").WithLocation(1, 28)
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
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            void verifyParsedAsRecord()
            {
                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.RecordStructDeclaration);
                    {
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.StructKeyword);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_WithBody()
        {
            var text = "record struct C(int X, int Y) { }";
            UsingTree(text, options: TestOptions.Regular10);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
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
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordClassParsing()
        {
            var text = "record class C(int X, int Y);";

            CreateCompilation(text, parseOptions: TestOptions.Regular10).VerifyDiagnostics(
                // (1,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "X").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(1, 20),
                // (1,27): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Y").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(1, 27));
            UsingTree(text, options: TestOptions.Regular10);
            verifyParsedAsRecord();

            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,8): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "class").WithArguments("record structs", "10.0").WithLocation(1, 8),
                // (1,20): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "X").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(1, 20),
                // (1,27): error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "Y").WithArguments("System.Runtime.CompilerServices.IsExternalInit").WithLocation(1, 27));
            UsingTree(text, options: TestOptions.Regular9);
            verifyParsedAsRecord();

            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "record ").WithArguments("top-level statements", "9.0").WithLocation(1, 1),
                // (1,1): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 1),
                // (1,8): error CS1001: Identifier expected
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "class").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "class").WithLocation(1, 8),
                // (1,15): error CS8400: Feature 'primary constructors' is not available in C# 8.0. Please use language version 12.0 or greater.
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "(int X, int Y)").WithArguments("primary constructors", "12.0").WithLocation(1, 15),
                // (1,20): warning CS9113: Parameter 'X' is unread.
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "X").WithArguments("X").WithLocation(1, 20),
                // (1,27): warning CS9113: Parameter 'Y' is unread.
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.WRN_UnreadPrimaryConstructorParameter, "Y").WithArguments("Y").WithLocation(1, 27)
                );

            UsingTree(text, options: TestOptions.Regular8,
                // (1,8): error CS1001: Identifier expected
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "class").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // record class C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "class").WithLocation(1, 8)
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
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            void verifyParsedAsRecord()
            {
                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.RecordKeyword);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordInterfaceParsing()
        {
            var text = "record interface C(int X, int Y);";
            UsingTree(text, options: TestOptions.RegularPreview,
                // (1,8): error CS1001: Identifier expected
                // record interface C(int X, int Y);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "interface").WithLocation(1, 8),
                // (1,8): error CS1514: { expected
                // record interface C(int X, int Y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "interface").WithLocation(1, 8),
                // (1,8): error CS1513: } expected
                // record interface C(int X, int Y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "interface").WithLocation(1, 8)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    M(SyntaxKind.IdentifierToken);
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordRecordParsing()
        {
            var text = "record record C(int X, int Y);";
            UsingTree(text, options: TestOptions.RegularPreview,
                // (1,15): error CS1514: { expected
                // record record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "C").WithLocation(1, 15),
                // (1,15): error CS1513: } expected
                // record record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "C").WithLocation(1, 15),
                // (1,15): error CS8803: Top-level statements must precede namespace and type declarations.
                // record record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "C(int X, int Y);").WithLocation(1, 15),
                // (1,17): error CS1525: Invalid expression term 'int'
                // record record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 17),
                // (1,21): error CS1003: Syntax error, ',' expected
                // record record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SyntaxError, "X").WithArguments(",").WithLocation(1, 21),
                // (1,24): error CS1525: Invalid expression term 'int'
                // record record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 24),
                // (1,28): error CS1003: Syntax error, ',' expected
                // record record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SyntaxError, "Y").WithArguments(",").WithLocation(1, 28)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "record");
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
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
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
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
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

        [Fact, CompilerTrait(CompilerFeature.RecordStructs), WorkItem(59534, "https://github.com/dotnet/roslyn/issues/59534")]
        public void RecordStructParsing_WrongOrder_CSharp10()
        {
            var text = "struct record C(int X, int Y);";
            UsingTree(text, options: TestOptions.Regular10,
                // (1,8): error CS9012: Unexpected keyword 'record'. Did you mean 'record struct' or 'record class'?
                // struct record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_MisplacedRecord, "record").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    M(SyntaxKind.StructKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs), WorkItem(59534, "https://github.com/dotnet/roslyn/issues/59534")]
        public void RecordStructParsing_WrongOrder_CSharp9()
        {
            var text = "struct record C(int X, int Y);";
            UsingTree(text, options: TestOptions.Regular9,
                // (1,8): error CS9012: Unexpected keyword 'record'. Did you mean 'record struct' or 'record class'?
                // struct record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_MisplacedRecord, "record").WithLocation(1, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    M(SyntaxKind.StructKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs), WorkItem(59534, "https://github.com/dotnet/roslyn/issues/59534")]
        public void StructNamedRecord_CSharp8()
        {
            var text = "struct record { }";
            UsingTree(text, options: TestOptions.Regular8);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "record");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs), WorkItem(59534, "https://github.com/dotnet/roslyn/issues/59534")]
        public void StructNamedRecord_CSharp9()
        {
            var text = "struct record { }";
            UsingTree(text, options: TestOptions.Regular9);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "record");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs), WorkItem(59534, "https://github.com/dotnet/roslyn/issues/59534")]
        public void StructNamedRecord_CSharp10()
        {
            var text = "struct record { }";
            UsingTree(text, options: TestOptions.Regular10);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "record");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordClassParsing_WrongOrder_CSharp10()
        {
            var text = "class record C(int X, int Y);";
            UsingTree(text, options: TestOptions.Regular10,
                // (1,7): error CS9012: Unexpected keyword 'record'. Did you mean 'record struct' or 'record class'?
                // class record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_MisplacedRecord, "record").WithLocation(1, 7));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    M(SyntaxKind.ClassKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordClassParsing_WrongOrder_CSharp9()
        {
            var text = "class record C(int X, int Y);";
            UsingTree(text, options: TestOptions.Regular9,
                // (1,7): error CS9012: Unexpected keyword 'record'. Did you mean 'record struct' or 'record class'?
                // class record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_MisplacedRecord, "record").WithLocation(1, 7));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    M(SyntaxKind.ClassKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordInterfaceParsing_WrongOrder()
        {
            var text = "interface record C(int X, int Y);";
            UsingTree(text, options: TestOptions.RegularPreview,
                // (1,18): error CS1514: { expected
                // interface record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "C").WithLocation(1, 18),
                // (1,18): error CS1513: } expected
                // interface record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "C").WithLocation(1, 18),
                // (1,18): error CS8803: Top-level statements must precede namespace and type declarations.
                // interface record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "C(int X, int Y);").WithLocation(1, 18),
                // (1,20): error CS1525: Invalid expression term 'int'
                // interface record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 20),
                // (1,24): error CS1003: Syntax error, ',' expected
                // interface record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SyntaxError, "X").WithArguments(",").WithLocation(1, 24),
                // (1,27): error CS1525: Invalid expression term 'int'
                // interface record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(1, 27),
                // (1,31): error CS1003: Syntax error, ',' expected
                // interface record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_SyntaxError, "Y").WithArguments(",").WithLocation(1, 31)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "record");
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
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
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
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
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

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_Partial()
        {
            var text = "partial record struct S;";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordClassParsing_Partial()
        {
            var text = "partial record class S;";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordParsing_Partial()
        {
            var text = "partial record S;";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_Partial_WithParameterList()
        {
            var text = "partial record struct S(int X);";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_Partial_WithParameterList_AndMembers()
        {
            var text = "partial record struct S(int X) { }";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_Readonly()
        {
            var text = "readonly record struct S;";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_ReadonlyPartial()
        {
            var text = "readonly partial record struct S;";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.PartialKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_PartialReadonly()
        {
            var text = "partial readonly record struct S;";
            UsingTree(text, options: TestOptions.RegularPreview,
                // (1,9): error CS1585: Member modifier 'readonly' must precede the member type and name
                // partial readonly record struct S;
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "readonly").WithArguments("readonly").WithLocation(1, 9)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "partial");
                    }
                }
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_New()
        {
            var text = "new record struct S;";
            UsingTree(text, options: TestOptions.RegularPreview,
                // (1,12): error CS1526: A new expression requires an argument list or (), [], or {} after type
                // new record struct S;
                Diagnostic(ErrorCode.ERR_BadNewExpr, "struct").WithLocation(1, 12),
                // (1,12): error CS1002: ; expected
                // new record struct S;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ObjectCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                            M(SyntaxKind.ArgumentList);
                            {
                                M(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_Ref()
        {
            var text = "ref record struct S;";

            CreateCompilation(text, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,19): error CS0106: The modifier 'ref' is not valid for this item
                // ref record struct S;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("ref").WithLocation(1, 19));
            UsingTree(text, options: TestOptions.RegularPreview);
            verifyParsedAsRecord();

            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (1,5): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // ref record struct S;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "record").WithLocation(1, 5),
                // (1,20): error CS8400: Feature 'primary constructors' is not available in C# 8.0. Please use language version 12.0 or greater.
                // ref record struct S;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, ";").WithArguments("primary constructors", "12.0").WithLocation(1, 20)
                );
            UsingTree(text, options: TestOptions.Regular8,
                // (1,5): error CS0116: A namespace cannot directly contain members such as fields or methods
                // ref record struct S;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "record").WithLocation(1, 5)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            CreateCompilation(text, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,12): error CS8773: Feature 'record structs' is not available in C# 9.0. Please use language version 10.0 or greater.
                // ref record struct S;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "struct").WithArguments("record structs", "10.0").WithLocation(1, 12),
                // (1,19): error CS0106: The modifier 'ref' is not valid for this item
                // ref record struct S;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S").WithArguments("ref").WithLocation(1, 19));
            UsingTree(text, options: TestOptions.Regular9);
            verifyParsedAsRecord();

            void verifyParsedAsRecord()
            {
                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.RecordStructDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.StructKeyword);
                        N(SyntaxKind.IdentifierToken, "S");
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordParsing_Ref()
        {
            var text = "ref record R;";
            UsingTree(text, options: TestOptions.RegularPreview);

            verifyParsedAsRecord();

            UsingTree(text, options: TestOptions.Regular8);

            CreateCompilation(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (1,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                // ref record R;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "ref record R;").WithArguments("top-level statements", "9.0").WithLocation(1, 1),
                // (1,5): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                // ref record R;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(1, 5),
                // (1,12): error CS8174: A declaration of a by-reference variable must have an initializer
                // ref record R;
                Diagnostic(ErrorCode.ERR_ByReferenceVariableMustBeInitialized, "R").WithLocation(1, 12),
                // (1,12): warning CS0168: The variable 'R' is declared but never used
                // ref record R;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "R").WithArguments("R").WithLocation(1, 12));

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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "record");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "R");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            UsingTree(text, options: TestOptions.Regular9);

            verifyParsedAsRecord();

            void verifyParsedAsRecord()
            {
                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.RecordDeclaration);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.RecordKeyword);
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_Const()
        {
            var text = "const record struct S;";
            UsingTree(text, options: TestOptions.RegularPreview,
                // (1,14): error CS1001: Identifier expected
                // const record struct S;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(1, 14),
                // (1,14): error CS0145: A const field requires a value to be provided
                // const record struct S;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "struct").WithLocation(1, 14),
                // (1,14): error CS1002: ; expected
                // const record struct S;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 14)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ConstKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_Fixed()
        {
            var text = "fixed record struct S;";
            UsingTree(text, options: TestOptions.RegularPreview,
                // (1,14): error CS1001: Identifier expected
                // fixed record struct S;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, '[' expected
                // fixed record struct S;
                Diagnostic(ErrorCode.ERR_SyntaxError, "struct").WithArguments("[").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, ']' expected
                // fixed record struct S;
                Diagnostic(ErrorCode.ERR_SyntaxError, "struct").WithArguments("]").WithLocation(1, 14),
                // (1,14): error CS0443: Syntax error; value expected
                // fixed record struct S;
                Diagnostic(ErrorCode.ERR_ValueExpected, "struct").WithLocation(1, 14),
                // (1,14): error CS1002: ; expected
                // fixed record struct S;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 14)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.FixedKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "record");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            M(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                M(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OmittedArraySizeExpression);
                                    {
                                        N(SyntaxKind.OmittedArraySizeExpressionToken);
                                    }
                                }
                                M(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_BaseListWithParens()
        {
            var text = "record struct S : Base(1);";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.PrimaryConstructorBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
        }

        [Fact, CompilerTrait(CompilerFeature.RecordStructs)]
        public void RecordStructParsing_BaseListWithParens_WithPositionalParameterList()
        {
            var text = "record struct S(int X) : Base(1);";
            UsingTree(text, options: TestOptions.RegularPreview);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "S");
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.PrimaryConstructorBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
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
        }

        [Fact, WorkItem(51590, "https://github.com/dotnet/roslyn/issues/51590")]
        public void ParseIncompleteRecordSyntax()
        {
            ParseIncompleteSyntax("public sealed record C() { }");
        }

        [Fact]
        public void ParseIncompleteRecordStructSyntax()
        {
            ParseIncompleteSyntax("public sealed record struct C() { }");
        }

        [Theory]
        [CombinatorialData]
        public void IncompleteParameterList_09(bool @struct)
        {
            var text = @"record 
" + (@struct ? "struct" : "class") + @" C<T>
(T x,
where T : class;";
            UsingTree(text,
                // (3,6): error CS1031: Type expected
                // (T x,
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(3, 6),
                // (3,6): error CS1001: Identifier expected
                // (T x,
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 6),
                // (3,6): error CS1026: ) expected
                // (T x,
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(3, 6)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(@struct ? SyntaxKind.RecordStructDeclaration : SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.RecordKeyword);
                    N(@struct ? SyntaxKind.StructKeyword : SyntaxKind.ClassKeyword);
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
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.Parameter);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
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
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
