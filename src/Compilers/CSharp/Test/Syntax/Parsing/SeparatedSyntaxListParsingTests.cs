// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SeparatedSyntaxListParsingTests : ParsingTests
    {
        public SeparatedSyntaxListParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71379")]
        public void TypeArguments()
        {
            UsingTree(@"
class C
{
    A<> a1;
    A<T> a2;
    A<,> a3;
    A<T U> a4;
    A<,,> a5;
    A<T,> a6;
    A<,T> a7;
    A<T U,,> a8;
}
",
                // (7,9): error CS1003: Syntax error, ',' expected
                //     A<T U> a4;
                Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(7, 9),
                // (11,9): error CS1003: Syntax error, ',' expected
                //     A<T U,,> a8;
                Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(11, 9));
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
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
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
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a2");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a3");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "T");
                                    }
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "U");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a4");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a5");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "T");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a6");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "T");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a7");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "T");
                                    }
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "U");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.OmittedTypeArgument);
                                    {
                                        N(SyntaxKind.OmittedTypeArgumentToken);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a8");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();

            // Validate binding diagnostics for cases that now parse without syntax errors
            var source = """
                class C
                {
                    A<> a1;
                    A<T> a2;
                    A<,> a3;
                    A<T U> a4;
                    A<,,> a5;
                    A<T,> a6;
                    A<,T> a7;
                    A<T U,,> a8;
                }
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS1003: Syntax error, ',' expected
                //     A<T U> a4;
                Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(6, 9),
                // (6,9): error CS0246: The type or namespace name 'U' could not be found (are you missing a using directive or an assembly reference?)
                //     A<T U> a4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "U").WithArguments("U").WithLocation(6, 9),
                // (10,9): error CS1003: Syntax error, ',' expected
                //     A<T U,,> a8;
                Diagnostic(ErrorCode.ERR_SyntaxError, "U").WithArguments(",").WithLocation(10, 9),
                // (7,5): error CS0246: The type or namespace name 'A<,,>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<,,> a5;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<,,>").WithArguments("A<,,>").WithLocation(7, 5),
                // (8,5): error CS0246: The type or namespace name 'A<,>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<T,> a6;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<T,>").WithArguments("A<,>").WithLocation(8, 5),
                // (9,5): error CS0246: The type or namespace name 'A<,>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<,T> a7;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<,T>").WithArguments("A<,>").WithLocation(9, 5),
                // (10,5): error CS0246: The type or namespace name 'A<,,,>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<T U,,> a8;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<T U,,>").WithArguments("A<,,,>").WithLocation(10, 5),
                // (3,5): error CS0246: The type or namespace name 'A<>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<> a1;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<>").WithArguments("A<>").WithLocation(3, 5),
                // (4,5): error CS0246: The type or namespace name 'A<>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<T> a2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<T>").WithArguments("A<>").WithLocation(4, 5),
                // (4,7): error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)
                //     A<T> a2;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "T").WithArguments("T").WithLocation(4, 7),
                // (5,5): error CS0246: The type or namespace name 'A<,>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<,> a3;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<,>").WithArguments("A<,>").WithLocation(5, 5),
                // (6,5): error CS0246: The type or namespace name 'A<,>' could not be found (are you missing a using directive or an assembly reference?)
                //     A<T U> a4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<T U>").WithArguments("A<,>").WithLocation(6, 5),
                // (6,7): error CS0246: The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)
                //     A<T U> a4;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "T").WithArguments("T").WithLocation(6, 7),
                // (5,10): warning CS0169: The field 'C.a3' is never used
                //     A<,> a3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a3").WithArguments("C.a3").WithLocation(5, 10),
                // (9,11): warning CS0169: The field 'C.a7' is never used
                //     A<,T> a7;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a7").WithArguments("C.a7").WithLocation(9, 11),
                // (10,14): warning CS0169: The field 'C.a8' is never used
                //     A<T U,,> a8;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a8").WithArguments("C.a8").WithLocation(10, 14),
                // (8,11): warning CS0169: The field 'C.a6' is never used
                //     A<T,> a6;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a6").WithArguments("C.a6").WithLocation(8, 11),
                // (3,9): warning CS0169: The field 'C.a1' is never used
                //     A<> a1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a1").WithArguments("C.a1").WithLocation(3, 9),
                // (6,12): warning CS0169: The field 'C.a4' is never used
                //     A<T U> a4;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a4").WithArguments("C.a4").WithLocation(6, 12),
                // (7,11): warning CS0169: The field 'C.a5' is never used
                //     A<,,> a5;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a5").WithArguments("C.a5").WithLocation(7, 11),
                // (4,10): warning CS0169: The field 'C.a2' is never used
                //     A<T> a2;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a2").WithArguments("C.a2").WithLocation(4, 10));
        }

        [Fact]
        public void TypeArguments2()
        {
            var tree = UsingTree(@"
class C
{
    new C<>();
    new C<, >();
    C<C<>> a1;
    C<A<>> a1;
    object a1 = typeof(C<C<, >, int>);
    object a2 = Swap<>(1, 1);
}

class M<,> { }
", options: TestOptions.Regular,
                // (4,12): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
                //     new C<>();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(4, 12),
                // (4,13): error CS8124: Tuple must contain at least two elements.
                //     new C<>();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 13),
                // (4,14): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     new C<>();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 14),
                // (5,14): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(5, 14),
                // (5,15): error CS8124: Tuple must contain at least two elements.
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 15),
                // (5,16): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 16),
                // (12,9): error CS1001: Identifier expected
                // class M<,> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(12, 9),
                // (12,10): error CS1001: Identifier expected
                // class M<,> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(12, 10));

            CheckTypeArguments2();
        }

        void CheckTypeArguments2()
        {
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.OmittedTypeArgument);
                                {
                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.TupleElement);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.OmittedTypeArgument);
                                {
                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.OmittedTypeArgument);
                                {
                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.TupleType);
                        {
                            N(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.TupleElement);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.OmittedTypeArgument);
                                            {
                                                N(SyntaxKind.OmittedTypeArgumentToken);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.GenericName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "A");
                                        N(SyntaxKind.TypeArgumentList);
                                        {
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.OmittedTypeArgument);
                                            {
                                                N(SyntaxKind.OmittedTypeArgumentToken);
                                            }
                                            N(SyntaxKind.GreaterThanToken);
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.TypeOfExpression);
                                    {
                                        N(SyntaxKind.TypeOfKeyword);
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "C");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.GenericName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "C");
                                                    N(SyntaxKind.TypeArgumentList);
                                                    {
                                                        N(SyntaxKind.LessThanToken);
                                                        N(SyntaxKind.OmittedTypeArgument);
                                                        {
                                                            N(SyntaxKind.OmittedTypeArgumentToken);
                                                        }
                                                        N(SyntaxKind.CommaToken);
                                                        N(SyntaxKind.OmittedTypeArgument);
                                                        {
                                                            N(SyntaxKind.OmittedTypeArgumentToken);
                                                        }
                                                        N(SyntaxKind.GreaterThanToken);
                                                    }
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a2");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.GenericName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Swap");
                                            N(SyntaxKind.TypeArgumentList);
                                            {
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.OmittedTypeArgument);
                                                {
                                                    N(SyntaxKind.OmittedTypeArgumentToken);
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                            }
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
                                            N(SyntaxKind.CommaToken);
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
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "M");
                    N(SyntaxKind.TypeParameterList);
                    {
                        N(SyntaxKind.LessThanToken);
                        M(SyntaxKind.TypeParameter);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.TypeParameter);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.GreaterThanToken);
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TypeArguments2WithCSharp6()
        {
            var text = @"
class C
{
    new C<>();
    new C<, >();
    C<C<>> a1;
    C<A<>> a1;
    object a1 = typeof(C<C<, >, int>);
    object a2 = Swap<>(1, 1);
}

class M<,> { }
";

            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (4,12): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
                //     new C<>();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(4, 12),
                // (4,13): error CS8124: Tuple must contain at least two elements.
                //     new C<>();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 13),
                // (4,14): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     new C<>();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 14),
                // (5,14): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(5, 14),
                // (5,15): error CS8124: Tuple must contain at least two elements.
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 15),
                // (5,16): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 16),
                // (6,5): error CS0308: The non-generic type 'C' cannot be used with type arguments
                //     C<C<>> a1;
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<C<>>").WithArguments("C", "type").WithLocation(6, 5),
                // (6,7): error CS0308: The non-generic type 'C' cannot be used with type arguments
                //     C<C<>> a1;
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<>").WithArguments("C", "type").WithLocation(6, 7),
                // (6,12): warning CS0169: The field 'C.a1' is never used
                //     C<C<>> a1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a1").WithArguments("C.a1").WithLocation(6, 12),
                // (7,5): error CS0308: The non-generic type 'C' cannot be used with type arguments
                //     C<A<>> a1;
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<A<>>").WithArguments("C", "type").WithLocation(7, 5),
                // (7,7): error CS0246: The type or namespace name 'A<>' could not be found (are you missing a using directive or an assembly reference?)
                //     C<A<>> a1;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A<>").WithArguments("A<>").WithLocation(7, 7),
                // (7,12): error CS0102: The type 'C' already contains a definition for 'a1'
                //     C<A<>> a1;
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "a1").WithArguments("C", "a1").WithLocation(7, 12),
                // (7,12): warning CS0169: The field 'C.a1' is never used
                //     C<A<>> a1;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a1").WithArguments("C.a1").WithLocation(7, 12),
                // (8,12): error CS0102: The type 'C' already contains a definition for 'a1'
                //     object a1 = typeof(C<C<, >, int>);
                Diagnostic(ErrorCode.ERR_DuplicateNameInClass, "a1").WithArguments("C", "a1").WithLocation(8, 12),
                // (8,24): error CS0308: The non-generic type 'C' cannot be used with type arguments
                //     object a1 = typeof(C<C<, >, int>);
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<C<, >, int>").WithArguments("C", "type").WithLocation(8, 24),
                // (8,26): error CS0308: The non-generic type 'C' cannot be used with type arguments
                //     object a1 = typeof(C<C<, >, int>);
                Diagnostic(ErrorCode.ERR_HasNoTypeVars, "C<, >").WithArguments("C", "type").WithLocation(8, 26),
                // (9,17): error CS0103: The name 'Swap' does not exist in the current context
                //     object a2 = Swap<>(1, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Swap<>").WithArguments("Swap").WithLocation(9, 17),
                // (12,9): error CS1001: Identifier expected
                // class M<,> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(12, 9),
                // (12,10): error CS1001: Identifier expected
                // class M<,> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(12, 10),
                // (12,10): error CS0692: Duplicate type parameter ''
                // class M<,> { }
                Diagnostic(ErrorCode.ERR_DuplicateTypeParameter, "").WithArguments("").WithLocation(12, 10));

            var tree = UsingTree(text, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6),
                // (4,12): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
                //     new C<>();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(4, 12),
                // (4,13): error CS8124: Tuple must contain at least two elements.
                //     new C<>();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 13),
                // (4,14): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     new C<>();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 14),
                // (5,14): error CS1519: Invalid token '(' in class, record, struct, or interface member declaration
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "(").WithArguments("(").WithLocation(5, 14),
                // (5,15): error CS8124: Tuple must contain at least two elements.
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 15),
                // (5,16): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     new C<, >();
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 16),
                // (12,9): error CS1001: Identifier expected
                // class M<,> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(12, 9),
                // (12,10): error CS1001: Identifier expected
                // class M<,> { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(12, 10));

            CheckTypeArguments2();
        }

        [Fact]
        public void ArrayRankSpecifiers()
        {
            UsingTree(@"
class C
{
    object a1 = new int[];
    object a1 = new int[1];
    object a1 = new int[,];
    object a1 = new int[1 2];
    object a1 = new int[,,];
    object a1 = new int[1,];
    object a1 = new int[,1];
    object a1 = new int[1 1 ,,];
}
",
                // (7,27): error CS1003: Syntax error, ',' expected
                //     object a1 = new int[1 2];
                Diagnostic(ErrorCode.ERR_SyntaxError, "2").WithArguments(",").WithLocation(7, 27),
                // (9,27): error CS0443: Syntax error; value expected
                //     object a1 = new int[1,];
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(9, 27),
                // (10,25): error CS0443: Syntax error; value expected
                //     object a1 = new int[,1];
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(10, 25),
                // (11,27): error CS1003: Syntax error, ',' expected
                //     object a1 = new int[1 1 ,,];
                Diagnostic(ErrorCode.ERR_SyntaxError, "1").WithArguments(",").WithLocation(11, 27),
                // (11,30): error CS0443: Syntax error; value expected
                //     object a1 = new int[1 1 ,,];
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(11, 30),
                // (11,31): error CS0443: Syntax error; value expected
                //     object a1 = new int[1 1 ,,];
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(11, 31));

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
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
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
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.OmittedArraySizeExpression);
                                                {
                                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.OmittedArraySizeExpression);
                                                {
                                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                                M(SyntaxKind.CommaToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "2");
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.OmittedArraySizeExpression);
                                                {
                                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.OmittedArraySizeExpression);
                                                {
                                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.OmittedArraySizeExpression);
                                                {
                                                    N(SyntaxKind.OmittedArraySizeExpressionToken);
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                                N(SyntaxKind.CommaToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ArrayCreationExpression);
                                    {
                                        N(SyntaxKind.NewKeyword);
                                        N(SyntaxKind.ArrayType);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.ArrayRankSpecifier);
                                            {
                                                N(SyntaxKind.OpenBracketToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                                M(SyntaxKind.CommaToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                                N(SyntaxKind.CommaToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                N(SyntaxKind.CommaToken);
                                                M(SyntaxKind.IdentifierName);
                                                {
                                                    M(SyntaxKind.IdentifierToken);
                                                }
                                                N(SyntaxKind.CloseBracketToken);
                                            }
                                        }
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
    }
}
