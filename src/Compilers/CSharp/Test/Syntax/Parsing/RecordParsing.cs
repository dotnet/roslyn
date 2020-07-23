// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
            UsingTree(text, options: TestOptions.Regular8,
                // (1,1): error CS8652: The feature 'top-level statements' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // record C(int X, int Y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "record C(int X, int Y);").WithArguments("top-level statements").WithLocation(1, 1)
            );
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
            var tree = ParseTree("record Point;", options: TestOptions.Regular8);
            tree.GetDiagnostics().Verify(
                // (1,1): error CS8652: The feature 'top-level statements' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // record Point;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "record Point;").WithArguments("top-level statements").WithLocation(1, 1)
            );

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
                // (1,12): error CS8652: The feature 'top-level statements' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // interface P(int x, int y);
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "(int x, int y);").WithArguments("top-level statements").WithLocation(1, 12),
                // (1,12): error CS8803: Top-level statements must precede namespace and type declarations.
                // interface P(int x, int y);
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int x, int y);").WithLocation(1, 12)
            );
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
                // (2,15): error CS1519: Invalid token 'return' in class, struct, or interface member declaration
                // record R1() { return null; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "return").WithArguments("return").WithLocation(2, 15),
                // (5,19): error CS1519: Invalid token 'return' in class, struct, or interface member declaration
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "class").WithArguments(":", "class").WithLocation(1, 23)
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
            UsingTree("abstract class C<T> where T : class;",
                // (1,36): error CS1003: Syntax error, ',' expected
                // abstract class C<T> where T : class;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",", ";").WithLocation(1, 36),
                // (1,37): error CS1514: { expected
                // abstract class C<T> where T : class;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 37),
                // (1,37): error CS1513: } expected
                // abstract class C<T> where T : class;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 37)
                );

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
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(45538, "https://github.com/dotnet/roslyn/issues/45538")]
        public void RecordParsing_TwoConstraintsAndSemiColon_Class()
        {
            UsingTree("abstract class C<T1, T2> where T1 : class where T2 : class;",
                // (1,59): error CS1003: Syntax error, ',' expected
                // abstract class C<T1, T2> where T1 : class where T2 : class;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",", ";").WithLocation(1, 59),
                // (1,60): error CS1514: { expected
                // abstract class C<T1, T2> where T1 : class where T2 : class;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 60),
                // (1,60): error CS1513: } expected
                // abstract class C<T1, T2> where T1 : class where T2 : class;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 60)
                );
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
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
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
        public void TestClassWithMultipleConstraints001()
        {
            UsingTree("class a<b> where b : c where b { }",
                // (1,32): error CS1003: Syntax error, ':' expected
                // class a<b> where b : c where b { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(":", "{").WithLocation(1, 32),
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(":", "{").WithLocation(1, 30),
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",", "").WithLocation(1, 37),
                // (1,43): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",", "").WithLocation(1, 43)
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",", "").WithLocation(1, 37),
                // (1,43): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",", "").WithLocation(1, 43),
                // (1,49): error CS1003: Syntax error, ',' expected
                // public class Goo<T> : System.Object where where where { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "where").WithArguments(",", "").WithLocation(1, 49)
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
                // (6,5): error CS8803: Top-level statements must precede namespace and type declarations.
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "3").WithArguments(",", "").WithLocation(1, 12),
                // (1,15): error CS1525: Invalid expression term ','
                // 0 with { X 3 =,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(1, 15),
                // (1,16): error CS1733: Expected expression
                // 0 with { X 3 =,
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 16),
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
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "B").WithArguments(",", "").WithLocation(1, 23));
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

        [Fact]
        public void BadParameterListAndBaseListOnClass()
        {
            var text = @$"
class C(int X, int Y)
: B(X, Y)
{{ }}";
            UsingTree(text,
                // (2,8): error CS1514: { expected
                // class C(int X, int Y)
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(2, 8),
                // (2,8): error CS1513: } expected
                // class C(int X, int Y)
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(2, 8),
                // (2,8): error CS8803: Top-level statements must precede namespace and type declarations.
                // class C(int X, int Y)
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, @"(int X, int Y)
").WithLocation(2, 8),
                // (2,22): error CS1002: ; expected
                // class C(int X, int Y)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 22),
                // (3,1): error CS1022: Type or namespace definition, or end-of-file expected
                // : B(X, Y)
                Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(3, 1),
                // (3,10): error CS1002: ; expected
                // : B(X, Y)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 10)
            );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
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
                                N(SyntaxKind.DeclarationExpression);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.DeclarationExpression);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
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

            if (!withParameters && withBaseArguments)
            {
                UsingTree(text,
                    // (2,4): error CS8861: Unexpected argument list.
                    // : B(X, Y)
                    Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(2, 4)
                    );
            }
            else
            {
                UsingTree(text);
            }

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
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",", "(").WithLocation(2, 7),
                // (2,8): error CS1003: Syntax error, ',' expected
                // : B, D(X, Y)
                Diagnostic(ErrorCode.ERR_SyntaxError, "X").WithArguments(",", "").WithLocation(2, 8),
                // (2,12): error CS1003: Syntax error, ',' expected
                // : B, D(X, Y)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",", ")").WithLocation(2, 12)
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
            UsingTree(text,
                // (1,16): error CS1003: Syntax error, ',' expected
                // interface C : B;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",", ";").WithLocation(1, 16),
                // (1,17): error CS1514: { expected
                // interface C : B;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 17),
                // (1,17): error CS1513: } expected
                // interface C : B;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 17)
                );

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
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Base_04()
        {
            var text = "interface C(int X, int Y) : B;";
            UsingTree(text,
                // (1,12): error CS1514: { expected
                // interface C(int X, int Y) : B;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "(").WithLocation(1, 12),
                // (1,12): error CS1513: } expected
                // interface C(int X, int Y) : B;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "(").WithLocation(1, 12),
                // (1,12): error CS8803: Top-level statements must precede namespace and type declarations.
                // interface C(int X, int Y) : B;
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "(int X, int Y) ").WithLocation(1, 12),
                // (1,27): error CS1002: ; expected
                // interface C(int X, int Y) : B;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 27),
                // (1,27): error CS1022: Type or namespace definition, or end-of-file expected
                // interface C(int X, int Y) : B;
                Diagnostic(ErrorCode.ERR_EOFExpected, ":").WithLocation(1, 27)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
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
                                N(SyntaxKind.DeclarationExpression);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "X");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.DeclarationExpression);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Y");
                                    }
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Base_05()
        {
            var text = "interface C : B(X, Y);";
            UsingTree(text,
                // (1,16): error CS8861: Unexpected argument list.
                // interface C : B(X, Y);
                Diagnostic(ErrorCode.ERR_UnexpectedArgumentList, "(").WithLocation(1, 16),
                // (1,22): error CS1003: Syntax error, ',' expected
                // interface C : B(X, Y);
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",", ";").WithLocation(1, 22),
                // (1,23): error CS1514: { expected
                // interface C : B(X, Y);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 23),
                // (1,23): error CS1513: } expected
                // interface C : B(X, Y);
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 23)
                );

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
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
