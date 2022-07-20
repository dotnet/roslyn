// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RefFieldParsingTests : ParsingTests
    {
        public RefFieldParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void FieldDeclaration_01(LanguageVersion languageVersion)
        {
            string source = "struct S { ref T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void FieldDeclaration_02(LanguageVersion languageVersion)
        {
            string source = "struct S { ref readonly T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void FieldDeclaration_03(LanguageVersion languageVersion)
        {
            string source = "struct S { out T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,12): error CS1519: Invalid token 'out' in class, record, struct, or interface member declaration
                // struct S { out T F; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "out").WithArguments("out").WithLocation(1, 12));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void FieldDeclaration_04(LanguageVersion languageVersion)
        {
            string source = "struct S { in T F; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,12): error CS1519: Invalid token 'in' in class, record, struct, or interface member declaration
                // struct S { in T F; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "in").WithArguments("in").WithLocation(1, 12));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Fixed_01(LanguageVersion languageVersion)
        {
            string source = "struct S { fixed ref int F1[1]; fixed ref readonly int F2[2]; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.FixedKeyword);
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
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F1");
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.FixedKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F2");
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "2");
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
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Fixed_02(LanguageVersion languageVersion)
        {
            string source = "struct S {  ref fixed int F1[1]; ref readonly fixed int F2[2]; }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,17): error CS1031: Type expected
                // struct S {  ref fixed int F1[1]; ref readonly fixed int F2[2]; }
                Diagnostic(ErrorCode.ERR_TypeExpected, "fixed").WithLocation(1, 17),
                // (1,47): error CS1031: Type expected
                // struct S {  ref fixed int F1[1]; ref readonly fixed int F2[2]; }
                Diagnostic(ErrorCode.ERR_TypeExpected, "fixed").WithLocation(1, 47));

            N(SyntaxKind.StructDeclaration);
            {
                N(SyntaxKind.StructKeyword);
                N(SyntaxKind.IdentifierToken, "S");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
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
                            N(SyntaxKind.IdentifierToken, "F1");
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
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
                            N(SyntaxKind.IdentifierToken, "F2");
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "2");
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
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ReadOnlyRefParameter(LanguageVersion languageVersion)
        {
            string source = "class C { void M(readonly ref int i) { } }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,1): error CS1073: Unexpected token '}'
                // class C { void M(readonly ref int i) { } }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "class C { void M(readonly ref int i) { }").WithArguments("}").WithLocation(1, 1),
                // (1,18): error CS1026: ) expected
                // class C { void M(readonly ref int i) { } }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(1, 18),
                // (1,18): error CS1002: ; expected
                // class C { void M(readonly ref int i) { } }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(1, 18),
                // (1,36): error CS1003: Syntax error, ',' expected
                // class C { void M(readonly ref int i) { } }
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(1, 36),
                // (1,40): error CS1002: ; expected
                // class C { void M(readonly ref int i) { } }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(1, 40));

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
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
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
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "i");
                        }
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Theory, WorkItem(62120, "https://github.com/dotnet/roslyn/issues/62120")]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ObjectInitializer(LanguageVersion languageVersion)
        {
            string source = "new S { F = ref t }";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "S");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory, WorkItem(62120, "https://github.com/dotnet/roslyn/issues/62120")]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void ObjectInitializer_CompoundAssignment(LanguageVersion languageVersion)
        {
            string source = "new S { F += ref t }";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,14): error CS1525: Invalid expression term 'ref'
                // new S { F += ref t }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref t").WithArguments("ref").WithLocation(1, 14)
                );

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "S");
                }
                N(SyntaxKind.CollectionInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.AddAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                        N(SyntaxKind.PlusEqualsToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefObjectInitializer_NestedInitializer(LanguageVersion languageVersion)
        {
            string source = "new S { F = ref { F2 = t } }";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,17): error CS1525: Invalid expression term '{'
                // new S { F = ref { F2 = t } }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, ',' expected
                // new S { F = ref { F2 = t } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 17)
                );

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "S");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F");
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ComplexElementInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "F2");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefCollectionInitializer(LanguageVersion languageVersion)
        {
            string source = "new S { ref t }";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "S");
                }
                N(SyntaxKind.CollectionInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RefExpression);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "t");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefDictionaryInitializer(LanguageVersion languageVersion)
        {
            string source = "new S { [0] = ref t }";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "S");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.ImplicitElementAccess);
                        {
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
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void RefComplexElementInitializer(LanguageVersion languageVersion)
        {
            string source = "new S { ref { 1, 2 } }";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(languageVersion),
                // (1,13): error CS1525: Invalid expression term '{'
                // new S { ref { 1, 2 } }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "{").WithArguments("{").WithLocation(1, 13),
                // (1,13): error CS1003: Syntax error, ',' expected
                // new S { ref { 1, 2 } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(1, 13)
                );

            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "S");
                }
                N(SyntaxKind.CollectionInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.RefExpression);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.ComplexElementInitializerExpression);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }
    }
}
