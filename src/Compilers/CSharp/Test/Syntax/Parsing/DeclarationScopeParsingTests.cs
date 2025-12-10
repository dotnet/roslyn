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
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
                // (1,6): error CS0177: The out parameter 'd' must be assigned to before control leaves the current method
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "F").WithArguments("d").WithLocation(1, 6),
                // (1,6): warning CS8321: The local function 'F' is declared but never used
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(1, 6),
                // (1,12): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(1, 12),
                // (1,12): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 12),
                // (1,29): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(1, 29),
                // (1,29): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 29),
                // (1,47): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(1, 47),
                // (1,47): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 47));

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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
                // (1,14): error CS1003: Syntax error, '=' expected
                // ref scoped R F() => default;
                Diagnostic(ErrorCode.ERR_SyntaxError, "F").WithArguments("=").WithLocation(1, 14));

            N(SyntaxKind.FieldDeclaration);
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
                        N(SyntaxKind.IdentifierToken, "R");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            M(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ParenthesizedLambdaExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "F");
                                }
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
                        }
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
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
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
            UsingDeclaration(source, TestOptions.Regular11,
                // (1,22): error CS1003: Syntax error, ',' expected
                // void F(scoped scoped ref int i) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(1, 22)
                );

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
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
            UsingDeclaration(source, TestOptions.Regular11,
                // (1,28): error CS1003: Syntax error, ',' expected
                // void F(ref scoped scoped R r) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "r").WithArguments(",").WithLocation(1, 28),
                // (1,29): error CS1001: Identifier expected
                // void F(ref scoped scoped R r) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 29));

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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics();

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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "z");
                        }
                        M(SyntaxKind.IdentifierToken);
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
        public void Method_11(LanguageVersion langVersion)
        {
            string source = "void F(scoped readonly int a) { }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(langVersion));
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics();

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
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "a");
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
        public void Method_12(LanguageVersion langVersion)
        {
            string source = "void F(scoped ref readonly int a) { }";
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
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "a");
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
        public void Method_13(LanguageVersion langVersion)
        {
            string source = "void F(out scoped ref int a) { }";
            UsingDeclaration(source, TestOptions.Regular.WithLanguageVersion(langVersion));
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics();

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
                        N(SyntaxKind.OutKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "a");
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
        public void Lambda_03_Ref(LanguageVersion langVersion)
        {
            string source = "(ref scoped int a) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
                // (1,6): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // (ref scoped int a) => null
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(1, 6),
                // (1,6): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // (ref scoped int a) => null
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 6),
                // (1,27): error CS1002: ; expected
                // (ref scoped int a) => null
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 27));

            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                M(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_03_In(LanguageVersion langVersion)
        {
            string source = "(in scoped int a) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
                // (1,5): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // (in scoped int a) => null
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(1, 5),
                // (1,5): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // (in scoped int a) => null
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 5),
                // (1,26): error CS1002: ; expected
                // (in scoped int a) => null
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 26));

            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_03_Out(LanguageVersion langVersion)
        {
            string source = "(out scoped int a) => null";
            UsingExpression(source, TestOptions.Regular.WithLanguageVersion(langVersion));
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics(
                // (1,6): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                // (out scoped int a) => null
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(1, 6),
                // (1,6): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // (out scoped int a) => null
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 6),
                // (1,23): error CS0177: The out parameter 'a' must be assigned to before control leaves the current method
                // (out scoped int a) => null
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "null").WithArguments("a").WithLocation(1, 23),
                // (1,27): error CS1002: ; expected
                // (out scoped int a) => null
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 27));

            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Lambda_04(LanguageVersion langVersion)
        {
            string source = "(scoped R a, scoped ref R b) => null";
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
            UsingExpression(source, TestOptions.Regular11,
                // (1,1): error CS1073: Unexpected token 'scoped'
                // (scoped scoped ref int i) => null
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(scoped ").WithArguments("scoped").WithLocation(1, 1),
                // (1,9): error CS1026: ) expected
                // (scoped scoped ref int i) => null
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "scoped").WithLocation(1, 9)
                );

            N(SyntaxKind.ParenthesizedExpression);
            {
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "scoped");
                }
                M(SyntaxKind.CloseParenToken);
            }
            EOF();
        }

        [Fact]
        public void Lambda_06()
        {
            string source = "(ref scoped scoped R r) => { }";
            UsingExpression(source, TestOptions.Regular11);
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (1,6): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 6),
                // (1,13): error CS9348: The 'scoped' modifier cannot immediately follow the 'scoped' modifier.
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_InvalidModifierAfterScoped, "scoped").WithArguments("scoped").WithLocation(1, 13),
                // (1,13): error CS9347: The 'scoped' modifier cannot come after an 'in', 'out', 'ref' or 'readonly' modifier.
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_ScopedAfterInOutRefReadonly, "scoped").WithLocation(1, 13),
                // (1,20): error CS0246: The type or namespace name 'R' could not be found (are you missing a using directive or an assembly reference?)
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "R").WithArguments("R").WithLocation(1, 20),
                // (1,31): error CS1002: ; expected
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 31));

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
            string source = "([A] scoped R a, [B] scoped ref R b) => null";
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
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (1,6): error CS8112: Local function 'F(params scoped object[])' must declare a body because it is not marked 'static extern'.
                // void F(scoped params object[] args);
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "F").WithArguments("F(params scoped object[])").WithLocation(1, 6),
                // (1,6): warning CS8321: The local function 'F' is declared but never used
                // void F(scoped params object[] args);
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(1, 6),
                // (1,8): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                // void F(scoped params object[] args);
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped params object[] args").WithLocation(1, 8),
                // (1,15): error CS9348: The 'params' modifier cannot immediately follow the 'scoped' modifier.
                // void F(scoped params object[] args);
                Diagnostic(ErrorCode.ERR_InvalidModifierAfterScoped, "params").WithArguments("params").WithLocation(1, 15));

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
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
                // (8,31): error CS1002: ; expected
                //         ref readonly scoped c c2;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "c2").WithLocation(8, 31)
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
                                            N(SyntaxKind.IdentifierToken, "scoped");
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
                                        N(SyntaxKind.IdentifierToken, "c");
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
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "c2");
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
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.ScopedType);
                                    {
                                        N(SyntaxKind.ScopedKeyword);
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
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
                                    N(SyntaxKind.ScopedType);
                                    {
                                        N(SyntaxKind.ScopedKeyword);
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.ScopedType);
                                    {
                                        N(SyntaxKind.ScopedKeyword);
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.ReadOnlyKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
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
                // (5,20): error CS1001: Identifier expected
                //         ref scoped int d;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(5, 20),
                // (5,20): error CS1002: ; expected
                //         ref scoped int d;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(5, 20),
                // (6,29): error CS1001: Identifier expected
                //         ref readonly scoped int e;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(6, 29),
                // (6,29): error CS1002: ; expected
                //         ref readonly scoped int e;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(6, 29)
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
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
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
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                    }
                                    M(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
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
                // (2,5): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // ref scoped int c;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "scoped").WithLocation(2, 5)
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
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
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
                // (2,23): error CS1003: Syntax error, ',' expected
                // ref readonly scoped S b;
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 23),
                // (3,30): error CS1003: Syntax error, ',' expected
                // scoped ref readonly scoped S c;
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(3, 30)
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
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "S");
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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "S");
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
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.IdentifierToken, "scoped");
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
                                        N(SyntaxKind.IdentifierToken, "scoped");
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
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
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
                // (2,19): error CS1003: Syntax error, ',' expected
                // ref scoped scoped d;
                Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(",").WithLocation(2, 19),
                // (3,28): error CS1003: Syntax error, ',' expected
                // ref readonly scoped scoped e;
                Diagnostic(ErrorCode.ERR_SyntaxError, "e").WithArguments(",").WithLocation(3, 28)
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
                                N(SyntaxKind.IdentifierToken, "scoped");
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
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
                // (2,16): error CS1003: Syntax error, ',' expected
                // ref scoped var c;
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(2, 16)
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
                                N(SyntaxKind.IdentifierToken, "var");
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
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
                // (2,25): error CS1003: Syntax error, ',' expected
                // ref readonly scoped var b;
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 25),
                // (3,32): error CS1003: Syntax error, ',' expected
                // scoped ref readonly scoped var c;
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(3, 32)
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
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void Local_11()
        {
            string source =
@"ref scoped readonly S a;
";
            UsingTree(source, TestOptions.Regular11,
                // (1,12): error CS1585: Member modifier 'readonly' must precede the member type and name
                // ref scoped readonly S a;
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "readonly").WithArguments("readonly").WithLocation(1, 12),
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
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
                // (2,19): error CS1003: Syntax error, ',' expected
                // scoped scoped var b;
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 19));

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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void Local_13(LanguageVersion langVersion)
        {
            string source =
@"class Program
{
    static void Main()
    {
        scoped extern ref int b;
    }
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (5,16): error CS1002: ; expected
                //         scoped extern ref int b;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "extern").WithLocation(5, 16),
                // (5,16): error CS0106: The modifier 'extern' is not valid for this item
                //         scoped extern ref int b;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "extern").WithArguments("extern").WithLocation(5, 16)
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.ExternKeyword);
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
        public void Local_14()
        {
            string source =
@"scoped scoped R x = default;
scoped scoped ref R z = ref x;
";
            UsingTree(source, TestOptions.RegularPreview,
                // (1,17): error CS1003: Syntax error, ',' expected
                // scoped scoped R x = default;
                Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(1, 17),
                // (2,15): error CS1003: Syntax error, ',' expected
                // scoped scoped ref R z = ref x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_15(LanguageVersion langVersion)
        {
            string source =
@"class Program
{
    static void Main()
    {
        scoped scoped extern ref int b;
    }
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (5,23): error CS1002: ; expected
                //         scoped scoped extern ref int b;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "extern").WithLocation(5, 23),
                // (5,23): error CS0106: The modifier 'extern' is not valid for this item
                //         scoped scoped extern ref int b;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "extern").WithArguments("extern").WithLocation(5, 23)
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
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.ExternKeyword);
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
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Local_16(LanguageVersion langVersion)
        {
            string source =
@"
scoped record A;
";

            var parseOptions = TestOptions.Regular.WithLanguageVersion(langVersion);
            if (langVersion == LanguageVersion.CSharp8)
            {
                CreateCompilation(source, parseOptions: parseOptions).VerifyDiagnostics(
                    // (2,1): error CS8400: Feature 'top-level statements' is not available in C# 8.0. Please use language version 9.0 or greater.
                    // scoped record A;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "scoped record A;").WithArguments("top-level statements", "9.0").WithLocation(2, 1),
                    // (2,1): error CS8400: Feature 'ref fields' is not available in C# 8.0. Please use language version 11.0 or greater.
                    // scoped record A;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "scoped").WithArguments("ref fields", "11.0").WithLocation(2, 1),
                    // (2,8): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                    // scoped record A;
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 8),
                    // (2,15): warning CS0168: The variable 'A' is declared but never used
                    // scoped record A;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "A").WithArguments("A").WithLocation(2, 15));
            }
            else if (langVersion == LanguageVersion.CSharp10)
            {
                CreateCompilation(source, parseOptions: parseOptions).VerifyDiagnostics(
                    // (2,1): error CS8936: Feature 'ref fields' is not available in C# 10.0. Please use language version 11.0 or greater.
                    // scoped record A;
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "scoped").WithArguments("ref fields", "11.0").WithLocation(2, 1),
                    // (2,8): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                    // scoped record A;
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 8),
                    // (2,15): warning CS0168: The variable 'A' is declared but never used
                    // scoped record A;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "A").WithArguments("A").WithLocation(2, 15));
            }
            else if (langVersion == LanguageVersion.CSharp11)
            {
                CreateCompilation(source, parseOptions: parseOptions).VerifyDiagnostics(
                    // (2,8): error CS0246: The type or namespace name 'record' could not be found (are you missing a using directive or an assembly reference?)
                    // scoped record A;
                    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "record").WithArguments("record").WithLocation(2, 8),
                    // (2,15): warning CS0168: The variable 'A' is declared but never used
                    // scoped record A;
                    Diagnostic(ErrorCode.WRN_UnreferencedVar, "A").WithArguments("A").WithLocation(2, 15));
            }

            UsingTree(source, parseOptions);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "record");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
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
        public void Local_17()
        {
            string source =
@"
scoped R x;
";
            UsingTree(source, TestOptions.Script);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.ScopedType);
                        {
                            N(SyntaxKind.ScopedKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "R");
                            }
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Local_18()
        {
            string source =
@"
scoped ref R x = M;
";
            UsingTree(source, TestOptions.Script,
                // (2,16): error CS1003: Syntax error, '(' expected
                // scoped ref R x = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("(").WithLocation(2, 16),
                // (2,16): error CS1001: Identifier expected
                // scoped ref R x = M;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=").WithLocation(2, 16),
                // (2,19): error CS1001: Identifier expected
                // scoped ref R x = M;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 19),
                // (2,19): error CS1026: ) expected
                // scoped ref R x = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(2, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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

        [Fact]
        public void Local_19()
        {
            string source =
@"
scoped ref readonly R x = M;
";
            UsingTree(source, TestOptions.Script,
                // (2,25): error CS1003: Syntax error, '(' expected
                // scoped ref readonly R x = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "=").WithArguments("(").WithLocation(2, 25),
                // (2,25): error CS1001: Identifier expected
                // scoped ref readonly R x = M;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=").WithLocation(2, 25),
                // (2,28): error CS1001: Identifier expected
                // scoped ref readonly R x = M;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 28),
                // (2,28): error CS1026: ) expected
                // scoped ref readonly R x = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(2, 28)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "R");
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "x");
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_01(LanguageVersion langVersion)
        {
            string source =
@"
(scoped a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_02(LanguageVersion langVersion)
        {
            string source =
@"
(ref scoped b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_03(LanguageVersion langVersion)
        {
            string source =
@"
(ref scoped int b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,2): error CS1525: Invalid expression term 'ref'
                // (ref scoped int b, var c) = M;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 2),
                // (2,13): error CS1026: ) expected
                // (ref scoped int b, var c) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(2, 13),
                // (2,13): error CS1002: ; expected
                // (ref scoped int b, var c) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(2, 13),
                // (2,20): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (ref scoped int b, var c) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 20),
                // (2,24): error CS1003: Syntax error, ',' expected
                // (ref scoped int b, var c) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(2, 24));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_04(LanguageVersion langVersion)
        {
            string source =
@"
(ref scoped a b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,2): error CS1525: Invalid expression term 'ref'
                // (ref scoped a b, var c) = M;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 2),
                // (2,13): error CS1026: ) expected
                // (ref scoped a b, var c) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "a").WithLocation(2, 13),
                // (2,13): error CS1002: ; expected
                // (ref scoped a b, var c) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "a").WithLocation(2, 13),
                // (2,18): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (ref scoped a b, var c) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 18),
                // (2,22): error CS1003: Syntax error, ',' expected
                // (ref scoped a b, var c) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(2, 22));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_05(LanguageVersion langVersion)
        {
            string source =
@"
(ref readonly scoped c, var d) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
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
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "d");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_06(LanguageVersion langVersion)
        {
            string source =
@"
(ref readonly scoped int c, var d) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,2): error CS1525: Invalid expression term 'ref'
                // (ref readonly scoped int c, var d) = M;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(2, 2),
                // (2,6): error CS1525: Invalid expression term 'readonly'
                // (ref readonly scoped int c, var d) = M;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 6),
                // (2,6): error CS1026: ) expected
                // (ref readonly scoped int c, var d) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 6),
                // (2,6): error CS1002: ; expected
                // (ref readonly scoped int c, var d) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(2, 6),
                // (2,6): error CS0106: The modifier 'readonly' is not valid for this item
                // (ref readonly scoped int c, var d) = M;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 6),
                // (2,29): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (ref readonly scoped int c, var d) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 29),
                // (2,33): error CS1003: Syntax error, ',' expected
                // (ref readonly scoped int c, var d) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(",").WithLocation(2, 33));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_07(LanguageVersion langVersion)
        {
            string source =
@"
(ref scoped readonly int c, var d) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,2): error CS1525: Invalid expression term 'ref'
                // (ref scoped readonly int c, var d) = M;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 2),
                // (2,13): error CS1026: ) expected
                // (ref scoped readonly int c, var d) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 13),
                // (2,13): error CS1002: ; expected
                // (ref scoped readonly int c, var d) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(2, 13),
                // (2,13): error CS0106: The modifier 'readonly' is not valid for this item
                // (ref scoped readonly int c, var d) = M;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 13),
                // (2,29): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (ref scoped readonly int c, var d) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 29),
                // (2,33): error CS1003: Syntax error, ',' expected
                // (ref scoped readonly int c, var d) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "d").WithArguments(",").WithLocation(2, 33));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_08(LanguageVersion langVersion)
        {
            string source =
@"
(scoped int a, var d) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "d");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_09(LanguageVersion langVersion)
        {
            string source =
@"
(@scoped int a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1026: ) expected
                // (@scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(2, 10),
                // (2,10): error CS1002: ; expected
                // (@scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(2, 10),
                // (2,17): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (@scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 17),
                // (2,21): error CS1003: Syntax error, ',' expected
                // (@scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 21));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "@scoped");
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_10(LanguageVersion langVersion)
        {
            string source =
@"
(scoped ref int b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_11(LanguageVersion langVersion)
        {
            string source =
@"
(@scoped ref int b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1026: ) expected
                // (@scoped ref int b, var c) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(2, 10),
                // (2,10): error CS1002: ; expected
                // (@scoped ref int b, var c) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(2, 10),
                // (2,21): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (@scoped ref int b, var c) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 21),
                // (2,25): error CS1003: Syntax error, ',' expected
                // (@scoped ref int b, var c) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(2, 25));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "@scoped");
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_12(LanguageVersion langVersion)
        {
            string source =
@"
(scoped ref readonly int a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.ReadOnlyKeyword);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_13(LanguageVersion langVersion)
        {
            string source =
@"
(@scoped ref readonly int a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1026: ) expected
                // (@scoped ref readonly int a, var b) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(2, 10),
                // (2,10): error CS1002: ; expected
                // (@scoped ref readonly int a, var b) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(2, 10),
                // (2,30): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (@scoped ref readonly int a, var b) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 30),
                // (2,34): error CS1003: Syntax error, ',' expected
                // (@scoped ref readonly int a, var b) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 34));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "@scoped");
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_14(LanguageVersion langVersion)
        {
            string source =
@"
(scoped S a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "S");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_15(LanguageVersion langVersion)
        {
            string source =
@"
(scoped ref S b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "S");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_16(LanguageVersion langVersion)
        {
            string source =
@"
(scoped ref readonly S a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.ReadOnlyKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "S");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_17(LanguageVersion langVersion)
        {
            string source =
@"
(scoped.nested a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
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
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_18(LanguageVersion langVersion)
        {
            string source =
@"
(scoped scoped a, var b) =  M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_20(LanguageVersion langVersion)
        {
            string source =
@"
(scoped var a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_21(LanguageVersion langVersion)
        {
            string source =
@"
(scoped ref var b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_22(LanguageVersion langVersion)
        {
            string source =
@"
(scoped ref readonly var c, var d) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.ReadOnlyKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "d");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_23(LanguageVersion langVersion)
        {
            string source =
@"
(scoped var, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_24(LanguageVersion langVersion)
        {
            string source =
@"
(ref scoped var, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_25(LanguageVersion langVersion)
        {
            string source =
@"
(scoped scoped int a, var b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,9): error CS1026: ) expected
                // (scoped scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "scoped").WithLocation(2, 9),
                // (2,9): error CS1002: ; expected
                // (scoped scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "scoped").WithLocation(2, 9),
                // (2,23): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (scoped scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 23),
                // (2,27): error CS1003: Syntax error, ',' expected
                // (scoped scoped int a, var b) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 27));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_26(LanguageVersion langVersion)
        {
            string source =
@"
(scoped scoped var b, var c) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,9): error CS1026: ) expected
                // (scoped scoped var b, var c) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "scoped").WithLocation(2, 9),
                // (2,9): error CS1002: ; expected
                // (scoped scoped var b, var c) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "scoped").WithLocation(2, 9),
                // (2,23): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
                // (scoped scoped var b, var c) = M;
                Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "var").WithLocation(2, 23),
                // (2,27): error CS1003: Syntax error, ',' expected
                // (scoped scoped var b, var c) = M;
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(2, 27));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
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
        public void DeclExpr_27(LanguageVersion langVersion)
        {
            string source =
@"
scoped var (a, b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,14): error CS1001: Identifier expected
                // scoped var (a, b) = M;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ",").WithLocation(2, 14),
                // (2,17): error CS1001: Identifier expected
                // scoped var (a, b) = M;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(2, 17),
                // (2,19): error CS1002: ; expected
                // scoped var (a, b) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "=").WithLocation(2, 19),
                // (2,19): error CS1525: Invalid expression term '='
                // scoped var (a, b) = M;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(2, 19)
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
                        N(SyntaxKind.IdentifierToken, "var");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                                M(SyntaxKind.IdentifierToken);
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
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_28(LanguageVersion langVersion)
        {
            string source =
@"
scoped ref var (a, b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,12): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // scoped ref var (a, b) = M;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "var").WithLocation(2, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
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
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_29(LanguageVersion langVersion)
        {
            string source =
@"
scoped ref readonly var (a, b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,21): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // scoped ref readonly var (a, b) = M;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "var").WithLocation(2, 21)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_30(LanguageVersion langVersion)
        {
            string source =
@"
(name: scoped int a, var d) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "name");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "d");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_31(LanguageVersion langVersion)
        {
            string source =
@"
(var a, scoped int b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_32(LanguageVersion langVersion)
        {
            string source =
@"
(var a, name: scoped int b) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "name");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void DeclExpr_33(LanguageVersion langVersion)
        {
            string source =
@"
(var a, scoped var (b, c)) = M;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,16): error CS1026: ) expected
                // (var a, scoped var (b, c)) = M;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "var").WithLocation(2, 16),
                // (2,16): error CS1002: ; expected
                // (var a, scoped var (b, c)) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(2, 16),
                // (2,26): error CS1002: ; expected
                // (var a, scoped var (b, c)) = M;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(2, 26),
                // (2,26): error CS1022: Type or namespace definition, or end-of-file expected
                // (var a, scoped var (b, c)) = M;
                Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(2, 26),
                // (2,28): error CS1525: Invalid expression term '='
                // (var a, scoped var (b, c)) = M;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(2, 28)
                );

            N(SyntaxKind.CompilationUnit);
            {
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
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            M(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "c");
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
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "M");
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
        public void OutDeclExpr_01(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_02(LanguageVersion langVersion)
        {
            string source =
@"
M(out ref scoped b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_03(LanguageVersion langVersion)
        {
            string source =
@"
M(out ref scoped int b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,7): error CS1525: Invalid expression term 'ref'
                // M(out ref scoped int b);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 7),
                // (2,18): error CS1003: Syntax error, ',' expected
                // M(out ref scoped int b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 18),
                // (2,18): error CS1525: Invalid expression term 'int'
                // M(out ref scoped int b);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 18),
                // (2,22): error CS1003: Syntax error, ',' expected
                // M(out ref scoped int b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 22)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.RefExpression);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                    }
                                }
                                M(SyntaxKind.CommaToken);
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
                                        N(SyntaxKind.IdentifierToken, "b");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_04(LanguageVersion langVersion)
        {
            string source =
@"
M(out ref scoped a b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,20): error CS1003: Syntax error, ',' expected
                // M(out ref scoped a b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 20)
                );

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
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_05(LanguageVersion langVersion)
        {
            string source =
@"
M(out ref readonly scoped c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.ReadOnlyKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_06(LanguageVersion langVersion)
        {
            string source =
@"
M(out ref readonly scoped int c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,7): error CS1525: Invalid expression term 'ref'
                // M(out ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(2, 7),
                // (2,11): error CS1525: Invalid expression term 'readonly'
                // M(out ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 11),
                // (2,11): error CS1026: ) expected
                // M(out ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 11),
                // (2,11): error CS1002: ; expected
                // M(out ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(2, 11),
                // (2,11): error CS0106: The modifier 'readonly' is not valid for this item
                // M(out ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 11),
                // (2,32): error CS1003: Syntax error, ',' expected
                // M(out ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 32));

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.RefExpression);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
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
        public void OutDeclExpr_07(LanguageVersion langVersion)
        {
            string source =
@"
M(out ref scoped readonly int c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,7): error CS1525: Invalid expression term 'ref'
                // M(out ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 7),
                // (2,18): error CS1026: ) expected
                // M(out ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 18),
                // (2,18): error CS1002: ; expected
                // M(out ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(2, 18),
                // (2,18): error CS0106: The modifier 'readonly' is not valid for this item
                // M(out ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 18),
                // (2,32): error CS1003: Syntax error, ',' expected
                // M(out ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 32));

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.RefExpression);
                                    {
                                        N(SyntaxKind.RefKeyword);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
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
        public void OutDeclExpr_08(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_09(LanguageVersion langVersion)
        {
            string source =
@"
M(out @scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,15): error CS1003: Syntax error, ',' expected
                // M(out @scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 15),
                // (2,15): error CS1525: Invalid expression term 'int'
                // M(out @scoped int a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 15),
                // (2,19): error CS1003: Syntax error, ',' expected
                // M(out @scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 19)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "@scoped");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
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
                                        N(SyntaxKind.IdentifierToken, "a");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_10(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped ref int b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_11(LanguageVersion langVersion)
        {
            string source =
@"
M(out @scoped ref int b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,15): error CS1003: Syntax error, ',' expected
                // M(out @scoped ref int b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 15),
                // (2,19): error CS1525: Invalid expression term 'int'
                // M(out @scoped ref int b);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 19),
                // (2,23): error CS1003: Syntax error, ',' expected
                // M(out @scoped ref int b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 23)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "@scoped");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.RefKeyword);
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
                                        N(SyntaxKind.IdentifierToken, "b");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_12(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped ref readonly int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.ReadOnlyKeyword);
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_13(LanguageVersion langVersion)
        {
            string source =
@"
M(out @scoped ref readonly int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,15): error CS1003: Syntax error, ',' expected
                // M(out @scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 15),
                // (2,19): error CS1525: Invalid expression term 'readonly'
                // M(out @scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 19),
                // (2,19): error CS1026: ) expected
                // M(out @scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 19),
                // (2,19): error CS1002: ; expected
                // M(out @scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "readonly").WithLocation(2, 19),
                // (2,19): error CS0106: The modifier 'readonly' is not valid for this item
                // M(out @scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 19),
                // (2,33): error CS1003: Syntax error, ',' expected
                // M(out @scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 33));

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "@scoped");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.CloseParenToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_14(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped S a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "S");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_15(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped ref S b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "S");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_16(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped ref readonly S a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.ReadOnlyKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "S");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_17(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped.nested a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_18(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped scoped a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_20(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped var a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_21(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped ref var b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "b");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_22(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped ref readonly var c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.RefType);
                                            {
                                                N(SyntaxKind.RefKeyword);
                                                N(SyntaxKind.ReadOnlyKeyword);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "c");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_23(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped var);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_24(LanguageVersion langVersion)
        {
            string source =
@"
M(out ref scoped var);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.RefType);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_25(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,21): error CS1003: Syntax error, ',' expected
                // M(out scoped scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 21),
                // (2,21): error CS1525: Invalid expression term 'int'
                // M(out scoped scoped int a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 21),
                // (2,25): error CS1003: Syntax error, ',' expected
                // M(out scoped scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 25)
                );

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
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                    }
                                }
                                M(SyntaxKind.CommaToken);
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
                                        N(SyntaxKind.IdentifierToken, "a");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_26(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped scoped var b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,25): error CS1003: Syntax error, ',' expected
                // M(out scoped scoped var b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 25)
                );

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "scoped");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "b");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_27(LanguageVersion langVersion)
        {
            string source =
@"
M(ref out scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,7): error CS1525: Invalid expression term 'out'
                // M(ref out scoped int a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(2, 7),
                // (2,7): error CS1003: Syntax error, ',' expected
                // M(ref out scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",").WithLocation(2, 7)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_28(LanguageVersion langVersion)
        {
            string source =
@"
M(scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1003: Syntax error, ',' expected
                // M(scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 10),
                // (2,10): error CS1525: Invalid expression term 'int'
                // M(scoped int a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 10),
                // (2,14): error CS1003: Syntax error, ',' expected
                // M(scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 14)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
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
                                        N(SyntaxKind.IdentifierToken, "a");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_29(LanguageVersion langVersion)
        {
            string source =
@"
M(scoped ref int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1003: Syntax error, ',' expected
                // M(scoped ref int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 10),
                // (2,14): error CS1525: Invalid expression term 'int'
                // M(scoped ref int a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 14),
                // (2,18): error CS1003: Syntax error, ',' expected
                // M(scoped ref int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 18)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.RefKeyword);
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
                                        N(SyntaxKind.IdentifierToken, "a");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_30(LanguageVersion langVersion)
        {
            string source =
@"
M(ref out scoped S a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,7): error CS1525: Invalid expression term 'out'
                // M(ref out scoped S a);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(2, 7),
                // (2,7): error CS1003: Syntax error, ',' expected
                // M(ref out scoped S a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",").WithLocation(2, 7)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.OutKeyword);
                                    N(SyntaxKind.DeclarationExpression);
                                    {
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "S");
                                            }
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_31(LanguageVersion langVersion)
        {
            string source =
@"
M(scoped S a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1003: Syntax error, ',' expected
                // M(scoped S a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "S").WithArguments(",").WithLocation(2, 10),
                // (2,12): error CS1003: Syntax error, ',' expected
                // M(scoped S a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 12)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_32(LanguageVersion langVersion)
        {
            string source =
@"
M(scoped ref S a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1003: Syntax error, ',' expected
                // M(scoped ref S a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 10),
                // (2,16): error CS1003: Syntax error, ',' expected
                // M(scoped ref S a);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 16)
                );

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
                                N(SyntaxKind.IdentifierToken, "M");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                }
                                M(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_33(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped var _);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ScopedType);
                                        {
                                            N(SyntaxKind.ScopedKeyword);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                        }
                                        N(SyntaxKind.DiscardDesignation);
                                        {
                                            N(SyntaxKind.UnderscoreToken);
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void OutDeclExpr_34(LanguageVersion langVersion)
        {
            string source =
@"
M(out scoped _);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                            N(SyntaxKind.IdentifierToken, "scoped");
                                        }
                                        N(SyntaxKind.DiscardDesignation);
                                        {
                                            N(SyntaxKind.UnderscoreToken);
                                        }
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void New_01(LanguageVersion langVersion)
        {
            string source =
@"
new scoped();
";

            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void New_02(LanguageVersion langVersion)
        {
            string source =
@"
new scoped int();
";

            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,12): error CS1526: A new expression requires an argument list or (), [], or {} after type
                // new scoped int();
                Diagnostic(ErrorCode.ERR_BadNewExpr, "int").WithLocation(2, 12),
                // (2,12): error CS1002: ; expected
                // new scoped int();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(2, 12),
                // (2,12): error CS1525: Invalid expression term 'int'
                // new scoped int();
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 12)
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
                                N(SyntaxKind.IdentifierToken, "scoped");
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ArgumentList);
                            {
                                N(SyntaxKind.OpenParenToken);
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void New_03(LanguageVersion langVersion)
        {
            string source =
@"
new scoped S();
";

            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                    N(SyntaxKind.IdentifierToken, "S");
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
        public void New_04(LanguageVersion langVersion)
        {
            string source =
@"
new scoped int M();
";

            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,12): error CS1526: A new expression requires an argument list or (), [], or {} after type
                // new scoped int M();
                Diagnostic(ErrorCode.ERR_BadNewExpr, "int").WithLocation(2, 12),
                // (2,12): error CS1002: ; expected
                // new scoped int M();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(2, 12)
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
                                N(SyntaxKind.IdentifierToken, "scoped");
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
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
        public void New_05(LanguageVersion langVersion)
        {
            string source =
@"
new scoped ref int M();
";

            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,12): error CS1526: A new expression requires an argument list or (), [], or {} after type
                // new scoped ref int M();
                Diagnostic(ErrorCode.ERR_BadNewExpr, "ref").WithLocation(2, 12),
                // (2,12): error CS1002: ; expected
                // new scoped ref int M();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(2, 12)
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
                                N(SyntaxKind.IdentifierToken, "scoped");
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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalFunctionStatement);
                    {
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
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
        public void New_06(LanguageVersion langVersion)
        {
            string source =
@"
new ref int M();
";

            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.NewKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "M");
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
        public void For_01(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_02(LanguageVersion langVersion)
        {
            string source =
@"
for (ref scoped b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_03(LanguageVersion langVersion)
        {
            string source =
@"
for (ref scoped int b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,17): error CS1001: Identifier expected
                // for (ref scoped int b;;);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(2, 17),
                // (2,17): error CS1003: Syntax error, ',' expected
                // for (ref scoped int b;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 17)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_04(LanguageVersion langVersion)
        {
            string source =
@"
for (ref scoped a b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,19): error CS1003: Syntax error, ',' expected
                // for (ref scoped a b;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 19)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_05(LanguageVersion langVersion)
        {
            string source =
@"
for (ref readonly scoped c;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_06(LanguageVersion langVersion)
        {
            string source =
@"
for (ref readonly scoped int c;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,26): error CS1001: Identifier expected
                // for (ref readonly scoped int c;;);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(2, 26),
                // (2,26): error CS1003: Syntax error, ',' expected
                // for (ref readonly scoped int c;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 26)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_07(LanguageVersion langVersion)
        {
            string source =
@"
for (ref scoped readonly int c;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,17): error CS1001: Identifier expected
                // for (ref scoped readonly int c;;);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "readonly").WithLocation(2, 17),
                // (2,17): error CS1003: Syntax error, ',' expected
                // for (ref scoped readonly int c;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "readonly").WithArguments(",").WithLocation(2, 17)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                    M(SyntaxKind.VariableDeclarator);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_08(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped int a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_09(LanguageVersion langVersion)
        {
            string source =
@"
for (@scoped int a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,14): error CS1003: Syntax error, ',' expected
                // for (@scoped int a;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 14),
                // (2,14): error CS1525: Invalid expression term 'int'
                // for (@scoped int a;;);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 14),
                // (2,18): error CS1003: Syntax error, ',' expected
                // for (@scoped int a;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 18)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@scoped");
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_10(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped ref int b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_11(LanguageVersion langVersion)
        {
            string source =
@"
for (@scoped ref int b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,14): error CS1003: Syntax error, ',' expected
                // for (@scoped ref int b;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 14),
                // (2,14): error CS1525: Invalid expression term 'ref'
                // for (@scoped ref int b;;);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref int").WithArguments("ref").WithLocation(2, 14),
                // (2,18): error CS1525: Invalid expression term 'int'
                // for (@scoped ref int b;;);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 18),
                // (2,22): error CS1003: Syntax error, ',' expected
                // for (@scoped ref int b;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 22)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@scoped");
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_12(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped ref readonly int a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_13(LanguageVersion langVersion)
        {
            string source =
@"
for (@scoped ref readonly int a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,14): error CS1003: Syntax error, ',' expected
                // for (@scoped ref readonly int a;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 14),
                // (2,14): error CS1525: Invalid expression term 'ref'
                // for (@scoped ref readonly int a;;);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(2, 14),
                // (2,18): error CS1525: Invalid expression term 'readonly'
                // for (@scoped ref readonly int a;;);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 18),
                // (2,18): error CS1003: Syntax error, ',' expected
                // for (@scoped ref readonly int a;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "readonly").WithArguments(",").WithLocation(2, 18),
                // (2,27): error CS1003: Syntax error, ',' expected
                // for (@scoped ref readonly int a;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 27),
                // (2,27): error CS1525: Invalid expression term 'int'
                // for (@scoped ref readonly int a;;);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 27),
                // (2,31): error CS1003: Syntax error, ',' expected
                // for (@scoped ref readonly int a;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "a").WithArguments(",").WithLocation(2, 31)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@scoped");
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                M(SyntaxKind.CommaToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_14(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped S a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
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
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_15(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped ref S b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "S");
                            }
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_16(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped ref readonly S a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "S");
                            }
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_17(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped.nested a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_18(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped scoped a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_19(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped scoped a =  default;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
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
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_20(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped var a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
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
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_21(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped ref var b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();

        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_22(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped ref readonly var c;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.RefType);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_23(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped var;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_24(LanguageVersion langVersion)
        {
            string source =
@"
for (ref scoped var;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_25(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped scoped int a;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,20): error CS1003: Syntax error, ',' expected
                // for (scoped scoped int a;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 20)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
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
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void For_26(LanguageVersion langVersion)
        {
            string source =
@"
for (scoped scoped var b;;);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,24): error CS1003: Syntax error, ',' expected
                // for (scoped scoped var b;;);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 24)
                );

            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.ScopedType);
                    {
                        N(SyntaxKind.ScopedKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
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
            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(langVersion)).VerifyDiagnostics();

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
        public void Foreach_01(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "scoped");
                }
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_02(LanguageVersion langVersion)
        {
            string source =
@"
foreach (ref scoped b in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                N(SyntaxKind.IdentifierToken, "b");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_04(LanguageVersion langVersion)
        {
            string source =
@"
foreach (ref scoped int b in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,1): error CS1073: Unexpected token 'in'
                // foreach (ref scoped int b in collection);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (ref scoped int b ").WithArguments("in").WithLocation(2, 1),
                // (2,10): error CS1525: Invalid expression term 'ref'
                // foreach (ref scoped int b in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 10),
                // (2,21): error CS1515: 'in' expected
                // foreach (ref scoped int b in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "int").WithLocation(2, 21),
                // (2,21): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (ref scoped int b in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "int").WithLocation(2, 21),
                // (2,21): error CS1525: Invalid expression term 'int'
                // foreach (ref scoped int b in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 21),
                // (2,25): error CS1026: ) expected
                // foreach (ref scoped int b in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "b").WithLocation(2, 25),
                // (2,27): error CS1002: ; expected
                // foreach (ref scoped int b in collection);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 27)
                );

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_05(LanguageVersion langVersion)
        {
            string source =
@"
foreach (ref readonly scoped c in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                N(SyntaxKind.IdentifierToken, "c");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_06(LanguageVersion langVersion)
        {
            string source =
@"
foreach (ref readonly scoped int c in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1525: Invalid expression term 'ref'
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(2, 10),
                // (2,14): error CS1525: Invalid expression term 'readonly'
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 14),
                // (2,14): error CS1515: 'in' expected
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "readonly").WithLocation(2, 14),
                // (2,14): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "readonly").WithLocation(2, 14),
                // (2,14): error CS1525: Invalid expression term 'readonly'
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 14),
                // (2,14): error CS1026: ) expected
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 14),
                // (2,14): error CS0106: The modifier 'readonly' is not valid for this item
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 14),
                // (2,36): error CS1003: Syntax error, ',' expected
                // foreach (ref readonly scoped int c in collection);
                Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(",").WithLocation(2, 36));

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                M(SyntaxKind.InKeyword);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.ScopedType);
                        {
                            N(SyntaxKind.ScopedKeyword);
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
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_07(LanguageVersion langVersion)
        {
            string source =
@"
foreach (ref scoped readonly int c in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,10): error CS1525: Invalid expression term 'ref'
                // foreach (ref scoped readonly int c in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 10),
                // (2,21): error CS1515: 'in' expected
                // foreach (ref scoped readonly int c in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "readonly").WithLocation(2, 21),
                // (2,21): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (ref scoped readonly int c in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "readonly").WithLocation(2, 21),
                // (2,21): error CS1525: Invalid expression term 'readonly'
                // foreach (ref scoped readonly int c in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 21),
                // (2,21): error CS1026: ) expected
                // foreach (ref scoped readonly int c in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 21),
                // (2,21): error CS0106: The modifier 'readonly' is not valid for this item
                // foreach (ref scoped readonly int c in collection);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 21),
                // (2,36): error CS1003: Syntax error, ',' expected
                // foreach (ref scoped readonly int c in collection);
                Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(",").WithLocation(2, 36));

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                M(SyntaxKind.InKeyword);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_08(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped int a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_09(LanguageVersion langVersion)
        {
            string source =
@"
foreach (@scoped int a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,1): error CS1073: Unexpected token 'in'
                // foreach (@scoped int a in collection);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (@scoped int a ").WithArguments("in").WithLocation(2, 1),
                // (2,18): error CS1515: 'in' expected
                // foreach (@scoped int a in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "int").WithLocation(2, 18),
                // (2,18): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (@scoped int a in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "int").WithLocation(2, 18),
                // (2,18): error CS1525: Invalid expression term 'int'
                // foreach (@scoped int a in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 18),
                // (2,22): error CS1026: ) expected
                // foreach (@scoped int a in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "a").WithLocation(2, 22),
                // (2,24): error CS1002: ; expected
                // foreach (@scoped int a in collection);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 24)
                );

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@scoped");
                }
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_10(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped ref int b in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                }
                N(SyntaxKind.IdentifierToken, "b");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_11(LanguageVersion langVersion)
        {
            string source =
@"
foreach (@scoped ref int b in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,1): error CS1073: Unexpected token 'in'
                // foreach (@scoped ref int b in collection);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (@scoped ref int b ").WithArguments("in").WithLocation(2, 1),
                // (2,18): error CS1515: 'in' expected
                // foreach (@scoped ref int b in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "ref").WithLocation(2, 18),
                // (2,18): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (@scoped ref int b in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "ref").WithLocation(2, 18),
                // (2,18): error CS1525: Invalid expression term 'ref'
                // foreach (@scoped ref int b in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref int").WithArguments("ref").WithLocation(2, 18),
                // (2,22): error CS1525: Invalid expression term 'int'
                // foreach (@scoped ref int b in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 22),
                // (2,26): error CS1026: ) expected
                // foreach (@scoped ref int b in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "b").WithLocation(2, 26),
                // (2,28): error CS1002: ; expected
                // foreach (@scoped ref int b in collection);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 28)
                );

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@scoped");
                }
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_12(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped ref readonly int a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                }
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_13(LanguageVersion langVersion)
        {
            string source =
@"
foreach (@scoped ref readonly int a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,18): error CS1515: 'in' expected
                // foreach (@scoped ref readonly int a in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "ref").WithLocation(2, 18),
                // (2,18): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (@scoped ref readonly int a in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "ref").WithLocation(2, 18),
                // (2,18): error CS1525: Invalid expression term 'ref'
                // foreach (@scoped ref readonly int a in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(2, 18),
                // (2,22): error CS1525: Invalid expression term 'readonly'
                // foreach (@scoped ref readonly int a in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 22),
                // (2,22): error CS1026: ) expected
                // foreach (@scoped ref readonly int a in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 22),
                // (2,22): error CS0106: The modifier 'readonly' is not valid for this item
                // foreach (@scoped ref readonly int a in collection);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 22),
                // (2,37): error CS1003: Syntax error, ',' expected
                // foreach (@scoped ref readonly int a in collection);
                Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(",").WithLocation(2, 37));

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "@scoped");
                }
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.LocalDeclarationStatement);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
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
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_14(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped S a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "S");
                    }
                }
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_15(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped ref S b in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "S");
                        }
                    }
                }
                N(SyntaxKind.IdentifierToken, "b");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_16(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped ref readonly S a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "S");
                        }
                    }
                }
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_17(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped.nested a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
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
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_18(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped scoped a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_20(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped var a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                }
                N(SyntaxKind.IdentifierToken, "a");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_21(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped ref var b in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                    }
                }
                N(SyntaxKind.IdentifierToken, "b");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_22(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped ref readonly var c in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                    }
                }
                N(SyntaxKind.IdentifierToken, "c");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_23(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped var in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "scoped");
                }
                N(SyntaxKind.IdentifierToken, "var");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_24(LanguageVersion langVersion)
        {
            string source =
@"
foreach (ref scoped var in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                N(SyntaxKind.IdentifierToken, "var");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_25(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped scoped int a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,1): error CS1073: Unexpected token 'in'
                // foreach (scoped scoped int a in collection);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (scoped scoped int a ").WithArguments("in").WithLocation(2, 1),
                // (2,24): error CS1515: 'in' expected
                // foreach (scoped scoped int a in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "int").WithLocation(2, 24),
                // (2,24): error CS1525: Invalid expression term 'int'
                // foreach (scoped scoped int a in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 24),
                // (2,28): error CS1026: ) expected
                // foreach (scoped scoped int a in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "a").WithLocation(2, 28),
                // (2,30): error CS1002: ; expected
                // foreach (scoped scoped int a in collection);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 30)
                );

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "scoped");
                }
                N(SyntaxKind.IdentifierToken, "scoped");
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_26(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped scoped var b in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,1): error CS1073: Unexpected token 'in'
                // foreach (scoped scoped var b in collection);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (scoped scoped var b ").WithArguments("in").WithLocation(2, 1),
                // (2,28): error CS1515: 'in' expected
                // foreach (scoped scoped var b in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "b").WithLocation(2, 28),
                // (2,30): error CS1026: ) expected
                // foreach (scoped scoped var b in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "in").WithLocation(2, 30),
                // (2,30): error CS1525: Invalid expression term 'in'
                // foreach (scoped scoped var b in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "in").WithArguments("in").WithLocation(2, 30),
                // (2,30): error CS1002: ; expected
                // foreach (scoped scoped var b in collection);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 30)
                );

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                N(SyntaxKind.IdentifierToken, "var");
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
                M(SyntaxKind.CloseParenToken);
                M(SyntaxKind.ExpressionStatement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_27(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped var (b, c) in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,1): error CS1073: Unexpected token 'in'
                // foreach (scoped var (b, c) in collection);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (scoped var (b, c) ").WithArguments("in").WithLocation(2, 1),
                // (2,21): error CS1515: 'in' expected
                // foreach (scoped var (b, c) in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "(").WithLocation(2, 21),
                // (2,28): error CS1026: ) expected
                // foreach (scoped var (b, c) in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "in").WithLocation(2, 28),
                // (2,28): error CS1525: Invalid expression term 'in'
                // foreach (scoped var (b, c) in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "in").WithArguments("in").WithLocation(2, 28),
                // (2,28): error CS1002: ; expected
                // foreach (scoped var (b, c) in collection);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 28)
                );

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "scoped");
                }
                N(SyntaxKind.IdentifierToken, "var");
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "c");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                M(SyntaxKind.CloseParenToken);
                M(SyntaxKind.ExpressionStatement);
                {
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_28(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped (int b, int c) in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,18): error CS1525: Invalid expression term 'int'
                // foreach (scoped (int b, int c) in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 18),
                // (2,22): error CS1003: Syntax error, ',' expected
                // foreach (scoped (int b, int c) in collection);
                Diagnostic(ErrorCode.ERR_SyntaxError, "b").WithArguments(",").WithLocation(2, 22),
                // (2,25): error CS1525: Invalid expression term 'int'
                // foreach (scoped (int b, int c) in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 25),
                // (2,29): error CS1003: Syntax error, ',' expected
                // foreach (scoped (int b, int c) in collection);
                Diagnostic(ErrorCode.ERR_SyntaxError, "c").WithArguments(",").WithLocation(2, 29),
                // (2,32): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (scoped (int b, int c) in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(2, 32)
                );

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
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
                                N(SyntaxKind.IdentifierToken, "b");
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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_29(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped (b, c) d in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.ForEachStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.ScopedType);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.IdentifierToken, "d");
                N(SyntaxKind.InKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "collection");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.EmptyStatement);
                {
                    N(SyntaxKind.SemicolonToken);
                }
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Foreach_30(LanguageVersion langVersion)
        {
            string source =
@"
foreach (scoped ref int[M(out var b)] a in collection);
";
            UsingStatement(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,1): error CS1073: Unexpected token 'in'
                // foreach (scoped ref int[M(out var b)] a in collection);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "foreach (scoped ref int[M(out var b)] a ").WithArguments("in").WithLocation(2, 1),
                // (2,17): error CS1515: 'in' expected
                // foreach (scoped ref int[M(out var b)] a in collection);
                Diagnostic(ErrorCode.ERR_InExpected, "ref").WithLocation(2, 17),
                // (2,17): error CS0230: Type and identifier are both required in a foreach statement
                // foreach (scoped ref int[M(out var b)] a in collection);
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "ref").WithLocation(2, 17),
                // (2,17): error CS1525: Invalid expression term 'ref'
                // foreach (scoped ref int[M(out var b)] a in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref int[M(out var b)]").WithArguments("ref").WithLocation(2, 17),
                // (2,21): error CS1525: Invalid expression term 'int'
                // foreach (scoped ref int[M(out var b)] a in collection);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(2, 21),
                // (2,39): error CS1026: ) expected
                // foreach (scoped ref int[M(out var b)] a in collection);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "a").WithLocation(2, 39),
                // (2,41): error CS1002: ; expected
                // foreach (scoped ref int[M(out var b)] a in collection);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(2, 41)
                );

            N(SyntaxKind.ForEachVariableStatement);
            {
                N(SyntaxKind.ForEachKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "scoped");
                }
                M(SyntaxKind.InKeyword);
                N(SyntaxKind.RefExpression);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.ElementAccessExpression);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.BracketedArgumentList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Argument);
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
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.OutKeyword);
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "b");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                    }
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.ExpressionStatement);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    M(SyntaxKind.SemicolonToken);
                }
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
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (1,8): error CS1001: Identifier expected
                // scoped struct A { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "struct").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // scoped struct A { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(1, 8),
                // (2,12): error CS1031: Type expected
                // scoped ref struct B { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(2, 12)
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
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
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
            UsingTree(source, TestOptions.Regular11,
                // (2,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // scoped readonly record struct B;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "scoped").WithLocation(2, 1),
                // (3,1): error CS8803: Top-level statements must precede namespace and type declarations.
                // readonly scoped record struct C();
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "readonly scoped record ").WithLocation(3, 1),
                // (3,1): error CS0106: The modifier 'readonly' is not valid for this item
                // readonly scoped record struct C();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(3, 1),
                // (3,24): error CS1002: ; expected
                // readonly scoped record struct C();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "struct").WithLocation(3, 24));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.ScopedKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "record");
                    }
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.ScopedKeyword);
                }
                N(SyntaxKind.RecordStructDeclaration);
                {
                    N(SyntaxKind.ReadOnlyKeyword);
                    N(SyntaxKind.RecordKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "B");
                    N(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "record");
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
                // (1,21): error CS1001: Identifier expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 21),
                // (1,21): error CS1003: Syntax error, '(' expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("(").WithLocation(1, 21),
                // (1,26): error CS1003: Syntax error, ',' expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",").WithLocation(1, 26),
                // (1,27): error CS8124: Tuple must contain at least two elements.
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, 27),
                // (1,28): error CS1001: Identifier expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 28),
                // (1,28): error CS1026: ) expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 28)
                );

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
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
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
                            N(SyntaxKind.IdentifierToken, "B");
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
        public void Type_06(LanguageVersion langVersion)
        {
            string source =
@"delegate ref scoped B();
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.IdentifierToken, "scoped");
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
        public void Type_07(LanguageVersion langVersion)
        {
            string source =
@"[A] scoped struct A { }
[A, B] scoped ref struct B { }
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (1,5): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // [A] scoped struct A { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "scoped").WithLocation(1, 5),
                // (2,19): error CS1031: Type expected
                // [A, B] scoped ref struct B { }
                Diagnostic(ErrorCode.ERR_TypeExpected, "struct").WithLocation(2, 19)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
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
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "A");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.IncompleteMember);
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
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
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
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "r");
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "R");
                                }
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
                // (2,20): error CS1002: ; expected
                // using ref scoped R r2;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "r2").WithLocation(2, 20)
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
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "R");
                            }
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
                            N(SyntaxKind.IdentifierToken, "r2");
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
                // (2,23): error CS1001: Identifier expected
                // await using ref scoped;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(2, 23)
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
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
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
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "r");
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "R");
                                }
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
                // (2,26): error CS1002: ; expected
                // await using ref scoped R r2;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "r2").WithLocation(2, 26)
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
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "R");
                            }
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
                            N(SyntaxKind.IdentifierToken, "r2");
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
        public void Using_05(LanguageVersion langVersion)
        {
            string source =
@"using scoped ref scoped r1;
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
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
        public void Using_06(LanguageVersion langVersion)
        {
            string source =
@"await using scoped ref scoped r1;
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
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "scoped");
                                    }
                                }
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
        public void UsingStmt_01(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_02(LanguageVersion langVersion)
        {
            string source =
@"
using (ref scoped b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_03(LanguageVersion langVersion)
        {
            string source =
@"
using (ref scoped int b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,8): error CS1525: Invalid expression term 'ref'
                // using (ref scoped int b);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 8),
                // (2,19): error CS1026: ) expected
                // using (ref scoped int b);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(2, 19),
                // (2,24): error CS1003: Syntax error, ',' expected
                // using (ref scoped int b);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 24));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
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
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_04(LanguageVersion langVersion)
        {
            string source =
@"
using (ref scoped a b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,21): error CS1026: ) expected
                // using (ref scoped a b);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "b").WithLocation(2, 21),
                // (2,22): error CS1002: ; expected
                // using (ref scoped a b);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(2, 22),
                // (2,22): error CS1022: Type or namespace definition, or end-of-file expected
                // using (ref scoped a b);
                Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(2, 22)
                );

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
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        M(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_05(LanguageVersion langVersion)
        {
            string source =
@"
using (ref readonly scoped c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_06(LanguageVersion langVersion)
        {
            string source =
@"
using (ref readonly scoped int c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,8): error CS1525: Invalid expression term 'ref'
                // using (ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref ").WithArguments("ref").WithLocation(2, 8),
                // (2,12): error CS1525: Invalid expression term 'readonly'
                // using (ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "readonly").WithArguments("readonly").WithLocation(2, 12),
                // (2,12): error CS1026: ) expected
                // using (ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 12),
                // (2,12): error CS0106: The modifier 'readonly' is not valid for this item
                // using (ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 12),
                // (2,33): error CS1003: Syntax error, ',' expected
                // using (ref readonly scoped int c);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 33));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.LocalDeclarationStatement);
                        {
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.VariableDeclaration);
                            {
                                N(SyntaxKind.ScopedType);
                                {
                                    N(SyntaxKind.ScopedKeyword);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_07(LanguageVersion langVersion)
        {
            string source =
@"
using (ref scoped readonly int c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,8): error CS1525: Invalid expression term 'ref'
                // using (ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(2, 8),
                // (2,19): error CS1026: ) expected
                // using (ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "readonly").WithLocation(2, 19),
                // (2,19): error CS0106: The modifier 'readonly' is not valid for this item
                // using (ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(2, 19),
                // (2,33): error CS1003: Syntax error, ',' expected
                // using (ref scoped readonly int c);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 33));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                        }
                        M(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.LocalDeclarationStatement);
                        {
                            N(SyntaxKind.ReadOnlyKeyword);
                            N(SyntaxKind.VariableDeclaration);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "c");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_08(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_09(LanguageVersion langVersion)
        {
            string source =
@"
using (@scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,16): error CS1026: ) expected
                // using (@scoped int a);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(2, 16),
                // (2,21): error CS1003: Syntax error, ',' expected
                // using (@scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 21));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "@scoped");
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
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_10(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped ref int b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_11(LanguageVersion langVersion)
        {
            string source =
@"
using (@scoped ref int b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,16): error CS1026: ) expected
                // using (@scoped ref int b);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(2, 16),
                // (2,25): error CS1003: Syntax error, ',' expected
                // using (@scoped ref int b);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 25));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "@scoped");
                        }
                        M(SyntaxKind.CloseParenToken);
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
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_12(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped ref readonly int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_13(LanguageVersion langVersion)
        {
            string source =
@"
using (@scoped ref readonly int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,16): error CS1026: ) expected
                // using (@scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(2, 16),
                // (2,34): error CS1003: Syntax error, ',' expected
                // using (@scoped ref readonly int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 34));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "@scoped");
                        }
                        M(SyntaxKind.CloseParenToken);
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
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_14(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped S a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
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
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_15(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped ref S b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_16(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped ref readonly S a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "S");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_17(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped.nested a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_18(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped scoped a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_19(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped scoped a = default);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
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
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_20(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped var a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
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
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_21(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped ref var b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_22(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped ref readonly var c);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.ReadOnlyKeyword);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "c");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_23(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped var);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_24(LanguageVersion langVersion)
        {
            string source =
@"
using (ref scoped var);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_25(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped scoped int a);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,22): error CS1026: ) expected
                // using (scoped scoped int a);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(2, 22),
                // (2,27): error CS1003: Syntax error, ',' expected
                // using (scoped scoped int a);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(2, 27));

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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
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
                                N(SyntaxKind.VariableDeclarator);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void UsingStmt_26(LanguageVersion langVersion)
        {
            string source =
@"
using (scoped scoped var b);
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (2,26): error CS1026: ) expected
                // using (scoped scoped var b);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "b").WithLocation(2, 26),
                // (2,27): error CS1002: ; expected
                // using (scoped scoped var b);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(2, 27),
                // (2,27): error CS1022: Type or namespace definition, or end-of-file expected
                // using (scoped scoped var b);
                Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(2, 27)
                );

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
                        M(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_01(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    scoped ref int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.RefType);
                                {
                                    N(SyntaxKind.RefKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_02(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    const scoped int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,18): error CS1001: Identifier expected
                //     const scoped int F3;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(4, 18),
                // (4,18): error CS0145: A const field requires a value to be provided
                //     const scoped int F3;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "int").WithLocation(4, 18),
                // (4,18): error CS1002: ; expected
                //     const scoped int F3;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(4, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.ConstKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
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
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_03(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    const scoped ref int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,18): error CS1001: Identifier expected
                //     const scoped ref int F3;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(4, 18),
                // (4,18): error CS0145: A const field requires a value to be provided
                //     const scoped ref int F3;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "ref").WithLocation(4, 18),
                // (4,18): error CS1002: ; expected
                //     const scoped ref int F3;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "ref").WithLocation(4, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.ConstKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
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
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_04(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    fixed scoped int F3[2];
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,18): error CS1001: Identifier expected
                //     fixed scoped int F3[2];
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(4, 18),
                // (4,18): error CS1003: Syntax error, '[' expected
                //     fixed scoped int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("[").WithLocation(4, 18),
                // (4,18): error CS1525: Invalid expression term 'int'
                //     fixed scoped int F3[2];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(4, 18),
                // (4,22): error CS1003: Syntax error, ',' expected
                //     fixed scoped int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, "F3").WithArguments(",").WithLocation(4, 22),
                // (4,27): error CS1003: Syntax error, ',' expected
                //     fixed scoped int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 27),
                // (4,27): error CS0443: Syntax error; value expected
                //     fixed scoped int F3[2];
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(4, 27),
                // (4,27): error CS1003: Syntax error, ']' expected
                //     fixed scoped int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(4, 27)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.FixedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    M(SyntaxKind.OpenBracketToken);
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
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "F3");
                                            }
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
                                    M(SyntaxKind.CommaToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CloseBracketToken);
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_05(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    fixed scoped ref int F3[2];
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,18): error CS1001: Identifier expected
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(4, 18),
                // (4,18): error CS1003: Syntax error, '[' expected
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments("[").WithLocation(4, 18),
                // (4,18): error CS1525: Invalid expression term 'ref'
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref int").WithArguments("ref").WithLocation(4, 18),
                // (4,22): error CS1525: Invalid expression term 'int'
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(4, 22),
                // (4,26): error CS1003: Syntax error, ',' expected
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, "F3").WithArguments(",").WithLocation(4, 26),
                // (4,31): error CS1003: Syntax error, ',' expected
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(4, 31),
                // (4,31): error CS0443: Syntax error; value expected
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(4, 31),
                // (4,31): error CS1003: Syntax error, ']' expected
                //     fixed scoped ref int F3[2];
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(4, 31)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.FixedKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    M(SyntaxKind.OpenBracketToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.RefExpression);
                                        {
                                            N(SyntaxKind.RefKeyword);
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                    }
                                    M(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.ElementAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "F3");
                                            }
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
                                    M(SyntaxKind.CommaToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    M(SyntaxKind.CloseBracketToken);
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_06(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    scoped const int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,12): error CS1519: Invalid token 'const' in class, record, struct, or interface member declaration
                //     scoped const int F3;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "const").WithArguments("const").WithLocation(4, 12),
                // (4,22): error CS0145: A const field requires a value to be provided
                //     scoped const int F3;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "F3").WithLocation(4, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                    }
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
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_07(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    scoped ref const int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,16): error CS1031: Type expected
                //     scoped ref const int F3;
                Diagnostic(ErrorCode.ERR_TypeExpected, "const").WithLocation(4, 16),
                // (4,26): error CS0145: A const field requires a value to be provided
                //     scoped ref const int F3;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "F3").WithLocation(4, 26)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.ScopedKeyword);
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
                        N(SyntaxKind.ConstKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_08(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    scoped fixed int F3[2];
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,12): error CS1519: Invalid token 'fixed' in class, record, struct, or interface member declaration
                //     scoped fixed int F3[2];
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "fixed").WithArguments("fixed").WithLocation(4, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
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
                                N(SyntaxKind.IdentifierToken, "F3");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_09(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    scoped ref fixed int F3[2];
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,16): error CS1031: Type expected
                //     scoped ref fixed int F3[2];
                Diagnostic(ErrorCode.ERR_TypeExpected, "fixed").WithLocation(4, 16)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.ScopedKeyword);
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
                                N(SyntaxKind.IdentifierToken, "F3");
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_10(LanguageVersion langVersion)
        {
            string source =
@"
class C
{
    scoped record A;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                            N(SyntaxKind.ScopedType);
                            {
                                N(SyntaxKind.ScopedKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "record");
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "A");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Field_11(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    scoped private R1 F1;
    scoped private ref int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,12): error CS1585: Member modifier 'private' must precede the member type and name
                //     scoped private R1 F1;
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "private").WithArguments("private").WithLocation(4, 12),
                // (5,12): error CS1585: Member modifier 'private' must precede the member type and name
                //     scoped private ref int F3;
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "private").WithArguments("private").WithLocation(5, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PrivateKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "R1");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "F1");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.PrivateKeyword);
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
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Event_01(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    scoped event int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,12): error CS1519: Invalid token 'event' in class, record, struct, or interface member declaration
                //     scoped event int F3;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "event").WithArguments("event").WithLocation(4, 12)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                    }
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Event_02(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    event scoped int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,18): error CS1001: Identifier expected
                //     event scoped int F3;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(4, 18),
                // (4,18): error CS1514: { expected
                //     event scoped int F3;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "int").WithLocation(4, 18),
                // (4,18): error CS1513: } expected
                //     event scoped int F3;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "int").WithLocation(4, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
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
                                N(SyntaxKind.IdentifierToken, "F3");
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

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersion.CSharp11)]
        public void Event_03(LanguageVersion langVersion)
        {
            string source =
@"
ref struct R2
{
    event scoped ref int F3;
}
";
            UsingTree(source, TestOptions.Regular.WithLanguageVersion(langVersion),
                // (4,18): error CS1001: Identifier expected
                //     event scoped ref int F3;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(4, 18),
                // (4,18): error CS1514: { expected
                //     event scoped ref int F3;
                Diagnostic(ErrorCode.ERR_LbraceExpected, "ref").WithLocation(4, 18),
                // (4,18): error CS1513: } expected
                //     event scoped ref int F3;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "ref").WithLocation(4, 18)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.StructDeclaration);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.StructKeyword);
                    N(SyntaxKind.IdentifierToken, "R2");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.FieldDeclaration);
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
                                N(SyntaxKind.IdentifierToken, "F3");
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
        public void Fixed_01()
        {
            string source =
@"
fixed (scoped int* a = b);
";
            UsingTree(source,
                // (2,15): error CS1001: Identifier expected
                // fixed (scoped int* a = b);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(2, 15),
                // (2,15): error CS1003: Syntax error, ',' expected
                // fixed (scoped int* a = b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(2, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.FixedStatement);
                    {
                        N(SyntaxKind.FixedKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Fixed_02()
        {
            string source =
@"
fixed (scoped ref int* a = b);
";
            UsingTree(source,
                // (2,15): error CS1001: Identifier expected
                // fixed (scoped ref int* a = b);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(2, 15),
                // (2,15): error CS1003: Syntax error, ',' expected
                // fixed (scoped ref int* a = b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.FixedStatement);
                    {
                        N(SyntaxKind.FixedKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Fixed_03()
        {
            string source =
@"
fixed (scoped ref readonly int* a = b);
";
            UsingTree(source,
                // (2,15): error CS1001: Identifier expected
                // fixed (scoped ref readonly int* a = b);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(2, 15),
                // (2,15): error CS1003: Syntax error, ',' expected
                // fixed (scoped ref readonly int* a = b);
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",").WithLocation(2, 15)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.FixedStatement);
                    {
                        N(SyntaxKind.FixedKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "scoped");
                            }
                            M(SyntaxKind.VariableDeclarator);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.EmptyStatement);
                        {
                            N(SyntaxKind.SemicolonToken);
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Catch_01()
        {
            string source =
@"
try {}
catch (scoped T a) {}
";
            UsingTree(source,
                // (3,17): error CS1026: ) expected
                // catch (scoped T a) {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "a").WithLocation(3, 17),
                // (3,17): error CS1514: { expected
                // catch (scoped T a) {}
                Diagnostic(ErrorCode.ERR_LbraceExpected, "a").WithLocation(3, 17),
                // (3,18): error CS1002: ; expected
                // catch (scoped T a) {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(3, 18),
                // (3,18): error CS1513: } expected
                // catch (scoped T a) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(3, 18),
                // (3,22): error CS1513: } expected
                // catch (scoped T a) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 22)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.TryStatement);
                    {
                        N(SyntaxKind.TryKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.CatchClause);
                        {
                            N(SyntaxKind.CatchKeyword);
                            N(SyntaxKind.CatchDeclaration);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                                N(SyntaxKind.IdentifierToken, "T");
                                M(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                M(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.ExpressionStatement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "a");
                                    }
                                    M(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Catch_02()
        {
            string source =
@"
try {}
catch (scoped ref T a) {}
";
            UsingTree(source,
                // (3,15): error CS1026: ) expected
                // catch (scoped ref T a) {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(3, 15),
                // (3,15): error CS1514: { expected
                // catch (scoped ref T a) {}
                Diagnostic(ErrorCode.ERR_LbraceExpected, "ref").WithLocation(3, 15),
                // (3,22): error CS1003: Syntax error, ',' expected
                // catch (scoped ref T a) {}
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(3, 22),
                // (3,24): error CS1002: ; expected
                // catch (scoped ref T a) {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(3, 24),
                // (3,26): error CS1513: } expected
                // catch (scoped ref T a) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 26));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.TryStatement);
                    {
                        N(SyntaxKind.TryKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.CatchClause);
                        {
                            N(SyntaxKind.CatchKeyword);
                            N(SyntaxKind.CatchDeclaration);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                                M(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                M(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.LocalDeclarationStatement);
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
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                    M(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Catch_03()
        {
            string source =
@"
try {}
catch (scoped ref readonly T a) {}
";
            UsingTree(source,
                // (3,15): error CS1026: ) expected
                // catch (scoped ref readonly T a) {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(3, 15),
                // (3,15): error CS1514: { expected
                // catch (scoped ref readonly T a) {}
                Diagnostic(ErrorCode.ERR_LbraceExpected, "ref").WithLocation(3, 15),
                // (3,31): error CS1003: Syntax error, ',' expected
                // catch (scoped ref readonly T a) {}
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(3, 31),
                // (3,33): error CS1002: ; expected
                // catch (scoped ref readonly T a) {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(3, 33),
                // (3,35): error CS1513: } expected
                // catch (scoped ref readonly T a) {}
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 35));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.TryStatement);
                    {
                        N(SyntaxKind.TryKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.CatchClause);
                        {
                            N(SyntaxKind.CatchKeyword);
                            N(SyntaxKind.CatchDeclaration);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "scoped");
                                }
                                M(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                M(SyntaxKind.OpenBraceToken);
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
                                                N(SyntaxKind.IdentifierToken, "T");
                                            }
                                        }
                                        N(SyntaxKind.VariableDeclarator);
                                        {
                                            N(SyntaxKind.IdentifierToken, "a");
                                        }
                                    }
                                    M(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }
    }
}
