// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DeclarationScopeParsingTests : ParsingTests
    {
        public DeclarationScopeParsingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Method_01(LanguageVersion langVersion)
        {
            string source = "void F(scoped x, ref scoped y) { }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Method_02(LanguageVersion langVersion)
        {
            string source = "void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Method_03(LanguageVersion langVersion)
        {
            string source = "void F(ref scoped int b, in scoped int c, out scoped int d) { }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Method_04()
        {
            string source = "scoped R F() => default;";
            UsingDeclaration(source, TestOptions.Regular11);

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.ScopedKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "R");
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ArrowExpressionClause);
                {
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.DefaultLiteralExpression);
                    {
                        N(SyntaxKind.DefaultKeyword);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Method_05()
        {
            string source = "ref scoped R F() => default;";
            UsingDeclaration(source, TestOptions.Regular11,
                // (1,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped R F() => default;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(1, 5)
                );

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "R");
                    }
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.ArrowExpressionClause);
                {
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.DefaultLiteralExpression);
                    {
                        N(SyntaxKind.DefaultKeyword);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Method_06(LanguageVersion langVersion)
        {
            string source =
@"scoped F1() => default;
ref scoped F2() => default;
scoped F3() { }
ref scoped F4() { }";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped F2() => default;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 5),
                // (4,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped F4() { }
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(4, 5),
                // (4,15): error CS1525: Invalid expression term ')'
                // ref scoped F4() { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(4, 15),
                // (4,17): error CS1002: ; expected
                // ref scoped F4() { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(4, 17)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "F1");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.DefaultLiteralExpression);
                            {
                                N(SyntaxKind.DefaultKeyword);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F2");
                        }
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedLambdaExpression);
                        {
                            N(SyntaxKind.ParameterList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.DefaultLiteralExpression);
                            {
                                N(SyntaxKind.DefaultKeyword);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "F3");
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
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "F4");
                        }
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.CloseParenToken);
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
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Method_06_Escaped(LanguageVersion langVersion)
        {
            string source = @"
ref @scoped F2() => default;
ref @scoped F4() { }";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                N(SyntaxKind.IdentifierToken, "@scoped");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "F2");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.DefaultLiteralExpression);
                            {
                                N(SyntaxKind.DefaultKeyword);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "@scoped");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "F4");
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Method_07()
        {
            string source = "void F(scoped scoped ref int i) { }";
            UsingDeclaration(source, TestOptions.Regular11);

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "i");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void Method_08()
        {
            string source = "void F(ref scoped scoped R r) { }";
            UsingDeclaration(source, TestOptions.Regular11);

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "r");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Method_09(LanguageVersion langVersion)
        {
            string source = "void F(scoped scoped x, ref scoped y, ref scoped scoped z, scoped ref scoped w) { }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "z");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "w");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Method_10(LanguageVersion langVersion)
        {
            string source = "void F(scoped.nested x, ref scoped.nested y) { }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "nested");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "nested");
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_01(LanguageVersion langVersion)
        {
            string source = "(scoped x, ref scoped y) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_02(LanguageVersion langVersion)
        {
            string source = "(scoped int a, scoped ref int b, scoped in int c, scoped out int d) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "d");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_03(LanguageVersion langVersion)
        {
            string source = "(ref scoped int a, out scoped int b, in scoped int c) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_04(LanguageVersion langVersion)
        {
            string source = "(scoped R a, scoped ref R b, ref scoped R c) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void Lambda_05()
        {
            string source = "(scoped scoped ref int i) => null";
            UsingExpression(source, TestOptions.Regular11);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "i");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact]
        public void Lambda_06()
        {
            string source = "(ref scoped scoped R r) => { }";
            UsingExpression(source, TestOptions.Regular11);

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "r");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_07(LanguageVersion langVersion)
        {
            string source = "([A] scoped R a, [B] scoped ref R b, [C] ref scoped R c) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "A");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "B");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.AttributeList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Attribute);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "C");
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_08(LanguageVersion langVersion)
        {
            string source = $"scoped () => t";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "scoped");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "t");
                }
            }
            EOF();
        }

        [Fact]
        public void Params_01()
        {
            string source = "void F(scoped params object[] args);";
            UsingDeclaration(source, TestOptions.Regular11);

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.ParamsKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
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
                        N(SyntaxKind.IdentifierToken, "args");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void Params_02()
        {
            string source = "void F(params scoped object[] args);";
            UsingDeclaration(source, TestOptions.Regular11);

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.VoidKeyword);
                }
                N(SyntaxKind.IdentifierToken, "F");
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ParamsKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.ArrayType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.ObjectKeyword);
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
                        N(SyntaxKind.IdentifierToken, "args");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_01(LanguageVersion langVersion)
        {
            string source =
@"class Program
{
    static void Main()
    {
        scoped a;
        ref scoped b;
        ref readonly scoped c;
        ref readonly scoped c c2;
    }
}";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (6,13): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                //         ref scoped b;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(6, 13),
                // (6,21): error CS1001: Identifier expected
                //         ref scoped b;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(6, 21),
                // (7,22): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                //         ref readonly scoped c;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(7, 22),
                // (7,30): error CS1001: Identifier expected
                //         ref readonly scoped c;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(7, 30),
                // (8,22): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                //         ref readonly scoped c c2;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(8, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.RefType);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.RefType);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.ReadOnlyKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.RefType);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.ReadOnlyKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c2");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_01_Escaped(LanguageVersion langVersion)
        {
            string source =
@"class Program
{
    static void Main()
    {
        ref readonly @scoped c;
    }
}";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                                    N(SyntaxKind.RefType);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.ReadOnlyKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "@scoped");
                                        }
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
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_02(LanguageVersion langVersion)
        {
            string source =
@$"class Program
{{
    static void Main()
    {{
        scoped int a;
        scoped ref int b;
        scoped ref readonly int c;
    }}
}}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.ScopedKeyword);
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
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.ScopedKeyword);
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
                                        N(SyntaxKind.IdentifierToken, "c");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_02_RefScoped(LanguageVersion langVersion)
        {
            string source = """
class Program
{
    static void Main()
    {
        ref scoped int d;
        ref readonly scoped int e;
    }
}
""";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (5,13): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                //         ref scoped int d;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(5, 13),
                // (6,22): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                //         ref readonly scoped int e;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(6, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.IdentifierToken, "d");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
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
                                        N(SyntaxKind.IdentifierToken, "e");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_03(LanguageVersion langVersion)
        {
            string source =
@"scoped int a;
scoped ref int b;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
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
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_03_RefScoped(LanguageVersion langVersion)
        {
            string source = @"
ref scoped int c;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped int c;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 5)
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
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_04(LanguageVersion langVersion)
        {
            string source =
@"scoped ref readonly S a;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "S");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_04_RefScoped(LanguageVersion langVersion)
        {
            string source = @"
ref readonly scoped S b;
scoped ref readonly scoped S c;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,14): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref readonly scoped S b;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 14),
                // (3,21): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // scoped ref readonly scoped S c;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(3, 21)
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
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "S");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "S");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_05(LanguageVersion langVersion)
        {
            string source =
@"scoped a;
ref scoped b;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped b;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 5)
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
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void TypeNamedScoped(LanguageVersion langVersion)
        {
            string source = @"
class scoped { }
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "scoped");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void TypeNamedScoped_Escaped(LanguageVersion langVersion)
        {
            string source = @"
class @scoped { }
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "@scoped");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory, WorkItem(62950, "https://github.com/dotnet/roslyn/issues/62950")]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_06(LanguageVersion langVersion)
        {
            string source =
@"scoped.nested a;
ref scoped.nested b;
ref readonly scoped.nested c;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped.nested b;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 5),
                // (2,11): error CS1031: Type expected
                // ref scoped.nested b;
                Diagnostic(ErrorCode.ERR_TypeExpected, ".").WithLocation(2, 11),
                // (2,11): error CS1022: Type or namespace definition, or end-of-file expected
                // ref scoped.nested b;
                Diagnostic(ErrorCode.ERR_EOFExpected, ".").WithLocation(2, 11),
                // (3,14): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref readonly scoped.nested c;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(3, 14),
                // (3,20): error CS1031: Type expected
                // ref readonly scoped.nested c;
                Diagnostic(ErrorCode.ERR_TypeExpected, ".").WithLocation(3, 20),
                // (3,20): error CS1022: Type or namespace definition, or end-of-file expected
                // ref readonly scoped.nested c;
                Diagnostic(ErrorCode.ERR_EOFExpected, ".").WithLocation(3, 20)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "nested");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "nested");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "nested");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_06_Escaped(LanguageVersion langVersion)
        {
            string source =
@"@scoped.nested a;
ref @scoped.nested b;
ref readonly @scoped.nested c;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "@scoped");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "nested");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "@scoped");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "nested");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "@scoped");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "nested");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_07(LanguageVersion langVersion)
        {
            string source =
@"scoped scoped a;
scoped ref scoped b;
scoped ref readonly scoped c;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,12): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // scoped ref scoped b;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 12),
                // (3,21): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // scoped ref readonly scoped c;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(3, 21)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
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
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
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

        [Theory, WorkItem(62950, "https://github.com/dotnet/roslyn/issues/62950")]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_07_WithInitializer(LanguageVersion langVersion)
        {
            string source =
@"scoped scoped a = default;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.DefaultLiteralExpression);
                                    {
                                        N(SyntaxKind.DefaultKeyword);
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_07_RefScoped(LanguageVersion langVersion)
        {
            string source = @"
ref scoped scoped d;
ref readonly scoped scoped e;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped scoped d;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 5),
                // (3,14): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref readonly scoped scoped e;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(3, 14)
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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_08(LanguageVersion langVersion)
        {
            string source =
@"scoped var a;
scoped ref var b;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_08_RefScoped(LanguageVersion langVersion)
        {
            string source = @"
ref scoped var c;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped var c;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 5)
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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_09(LanguageVersion langVersion)
        {
            string source =
@"scoped ref readonly var a;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_09_RefReadonlyScoped(LanguageVersion langVersion)
        {
            string source = @"
ref readonly scoped var b;
scoped ref readonly scoped var c;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,14): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref readonly scoped var b;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 14),
                // (3,21): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // scoped ref readonly scoped var c;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(3, 21)
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
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_10(LanguageVersion langVersion)
        {
            string source =
@"scoped var;
ref scoped var;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped var;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 5)
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
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
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
        public void Local_11()
        {
            string source =
@"ref scoped readonly S a;
";
            UsingTree(source, TestOptions.Regular11,
                // (1,5): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // ref scoped readonly S a;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(1, 5),
                // (1,12): error CS1031: Type expected
                // ref scoped readonly S a;
                Diagnostic(ErrorCode.ERR_TypeExpected, "readonly").WithLocation(1, 12),
                // (1,12): error CS0106: The modifier 'readonly' is not valid for this item
                // ref scoped readonly S a;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(1, 12));

            N(SyntaxKind.CompilationUnit);
            {
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "S");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
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
        public void Local_12()
        {
            string source =
@"scoped scoped int a;
scoped scoped var b;
";
            UsingTree(source, TestOptions.Regular11,
                // (1,15): error CS1003: Syntax error, ',' expected
                // scoped scoped int a;
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(1, 15),
                // (2,8): error CS1031: Type expected
                // scoped scoped var b;
                Diagnostic(ErrorCode.ERR_TypeExpected, "scoped").WithLocation(2, 8));

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
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void FunctionPointer_01(LanguageVersion langVersion)
        {
            string source = @"delegate*<scoped, ref scoped> f;";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.LocalDeclarationStatement);
            {
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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "f");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void FunctionPointer_02(LanguageVersion langVersion)
        {
            string source = @"delegate*<scoped R, ref scoped R, scoped ref int, void> f;";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.LocalDeclarationStatement);
            {
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
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "R");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "R");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.FunctionPointerParameter);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefKeyword);
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
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "f");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Type_01(LanguageVersion langVersion)
        {
            string source =
@"scoped struct A { }
scoped ref struct B { }
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Type_02()
        {
            string source =
@"scoped record A { }
scoped readonly record struct B;
readonly scoped record struct C();
";
            UsingTree(source, TestOptions.Regular11);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.RecordDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Type_03()
        {
            string source =
@"delegate scoped int A();
";
            UsingTree(source, TestOptions.Regular11,
                // (1,17): error CS1001: Identifier expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, '(' expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("(").WithLocation(1, 17),
                // (1,22): error CS1003: Syntax error, ',' expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(1, 22),
                // (1,23): error CS8124: Tuple must contain at least two elements.
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, 23),
                // (1,24): error CS1001: Identifier expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 24),
                // (1,24): error CS1026: ) expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 24));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
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
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Type_04(LanguageVersion langVersion)
        {
            string source =
@"delegate scoped A();
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Type_05(LanguageVersion langVersion)
        {
            string source =
@"delegate ref scoped int B();
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (1,14): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(1, 14)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Type_06(LanguageVersion langVersion)
        {
            string source =
@"delegate ref scoped B();
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (1,14): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // delegate ref scoped B();
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(1, 14),
                // (1,22): error CS1001: Identifier expected
                // delegate ref scoped B();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 22));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "B");
                        }
                    }
                    M(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Type_07(LanguageVersion langVersion)
        {
            string source =
@"[A] scoped struct A { }
[A, B] scoped ref struct B { }
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "B");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void LocalAssignment_01(LanguageVersion langVersion)
        {
            string source =
@"class Program
{
    static void Main()
    {
        bool scoped;
        scoped = true;
    }
}";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "Main");
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
                                        N(SyntaxKind.BoolKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.TrueLiteralExpression);
                                    {
                                        N(SyntaxKind.TrueKeyword);
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void LocalAssignment_02(LanguageVersion langVersion)
        {
            string source =
@"bool scoped;
scoped = true;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.BoolKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Using_01(LanguageVersion langVersion)
        {
            string source =
@"using scoped s;
using ref scoped r;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,11): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // using ref scoped r;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 11),
                // (2,19): error CS1001: Identifier expected
                // using ref scoped r;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "s");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "r");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Using_02(LanguageVersion langVersion)
        {
            string source =
@"using scoped R r1;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "R");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "r1");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Using_02_RefScoped(LanguageVersion langVersion)
        {
            string source = @"
using ref scoped R r2;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,11): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // using ref scoped R r2;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 11)
                );

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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "R");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "r2");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Using_03(LanguageVersion langVersion)
        {
            string source =
@"await using scoped s;
await using ref scoped;
await using ref scoped r;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,17): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // await using ref scoped;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 17),
                // (2,23): error CS1031: Type expected
                // await using ref scoped;
                Diagnostic(ErrorCode.ERR_TypeExpected, ";").WithLocation(2, 23),
                // (2,23): error CS1001: Identifier expected
                // await using ref scoped;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 23),
                // (3,17): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // await using ref scoped r;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(3, 17),
                // (3,25): error CS1001: Identifier expected
                // await using ref scoped r;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(3, 25));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "s");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.UsingKeyword);
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "r");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Using_04(LanguageVersion langVersion)
        {
            string source =
@"await using scoped R r1;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "R");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "r1");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Using_04_RefScoped(LanguageVersion langVersion)
        {
            string source = @"
await using ref scoped R r2;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,17): error CS9061: Unexpected contextual keyword 'scoped'. Did you mean 'scoped ref' or '@scoped'?
                // await using ref scoped R r2;
                Diagnostic(ErrorCode.ERR_MisplacedScoped, "scoped").WithLocation(2, 17)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "R");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "r2");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
