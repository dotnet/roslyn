// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
        [InlineData(LanguageVersionFacts.CSharpNext)]
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

        [Fact]
        public void Method_02()
        {
            string source = "void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }";
            UsingDeclaration(source, TestOptions.RegularNext);

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

        // PROTOTYPE: These syntax errors are not helpful. Ideally, we should
        // report a "-langversion" error for the 'scoped' keyword.
        [Fact]
        public void Method_02_CSharp10()
        {
            string source = "void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }";
            UsingDeclaration(source, TestOptions.Regular10,
                // (1,15): error CS1001: Identifier expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 15),
                // (1,15): error CS1003: Syntax error, ',' expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int").WithLocation(1, 15),
                // (1,29): error CS1001: Identifier expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "ref").WithLocation(1, 29),
                // (1,29): error CS1003: Syntax error, ',' expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",", "ref").WithLocation(1, 29),
                // (1,47): error CS1001: Identifier expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "in").WithLocation(1, 47),
                // (1,47): error CS1003: Syntax error, ',' expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(",", "in").WithLocation(1, 47),
                // (1,64): error CS1001: Identifier expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "out").WithLocation(1, 64),
                // (1,64): error CS1003: Syntax error, ',' expected
                // void F(scoped int a, scoped ref int b, scoped in int c, scoped out int d) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",", "out").WithLocation(1, 64));

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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CommaToken);
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "scoped");
                        }
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
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

        [Fact]
        public void Method_03()
        {
            string source = "void F(ref scoped int b, in scoped int c, out scoped int d) { }";
            UsingDeclaration(source, TestOptions.RegularNext);

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
        public void Method_03_CSharp10()
        {
            string source = "void F(ref scoped int b, in scoped int c, out scoped int d) { }";
            UsingDeclaration(source, TestOptions.Regular10,
                // (1,19): error CS1001: Identifier expected
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 19),
                // (1,19): error CS1003: Syntax error, ',' expected
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int").WithLocation(1, 19),
                // (1,36): error CS1001: Identifier expected
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 36),
                // (1,36): error CS1003: Syntax error, ',' expected
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int").WithLocation(1, 36),
                // (1,54): error CS1001: Identifier expected
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 54),
                // (1,54): error CS1003: Syntax error, ',' expected
                // void F(ref scoped int b, in scoped int c, out scoped int d) { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int").WithLocation(1, 54));

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
            UsingDeclaration(source, TestOptions.RegularNext);

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
            UsingDeclaration(source, TestOptions.RegularNext);

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.RefType);
                {
                    N(SyntaxKind.RefKeyword);
                    N(SyntaxKind.ScopedKeyword);
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

        [Fact]
        public void Method_06()
        {
            string source = "void F(scoped scoped ref int i) { }";
            UsingDeclaration(source, TestOptions.RegularNext);

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
        public void Method_07()
        {
            string source = "void F(ref scoped scoped R r) { }";
            UsingDeclaration(source, TestOptions.RegularNext);

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

        [Fact]
        public void Method_08()
        {
            string source = "void F(scoped scoped x, ref scoped y, ref scoped scoped z, scoped ref scoped w) { }";
            UsingDeclaration(source, TestOptions.RegularNext);

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
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void Method_09(LanguageVersion langVersion)
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
        [InlineData(LanguageVersionFacts.CSharpNext)]
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

        [Fact]
        public void Lambda_02()
        {
            string source = "(scoped int a, scoped ref int b, scoped in int c, scoped out int d) => null";
            UsingExpression(source, TestOptions.RegularNext);

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

        [Fact]
        public void Lambda_02_CSharp10()
        {
            string source = "(scoped int a, scoped ref int b, scoped in int c, scoped out int d) => null";
            UsingExpression(source, TestOptions.Regular10,
                // (1,1): error CS1073: Unexpected token 'int'
                // (scoped int a, scoped ref int b, scoped in int c, scoped out int d) => null
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(scoped ").WithArguments("int").WithLocation(1, 1),
                // (1,9): error CS1026: ) expected
                // (scoped int a, scoped ref int b, scoped in int c, scoped out int d) => null
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(1, 9));

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
        public void Lambda_03()
        {
            string source = "(ref scoped int a, out scoped int b, in scoped int c) => null";
            UsingExpression(source, TestOptions.RegularNext);

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

        [Fact]
        public void Lambda_03_CSharp10()
        {
            string source = "(ref scoped int a, out scoped int b, in scoped int c) => null";
            UsingExpression(source, TestOptions.Regular10,
                // (1,1): error CS1073: Unexpected token 'int'
                // (ref scoped int a, out scoped int b, in scoped int c) => null
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(ref scoped ").WithArguments("int").WithLocation(1, 1),
                // (1,2): error CS1525: Invalid expression term 'ref'
                // (ref scoped int a, out scoped int b, in scoped int c) => null
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(1, 2),
                // (1,13): error CS1026: ) expected
                // (ref scoped int a, out scoped int b, in scoped int c) => null
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "int").WithLocation(1, 13));

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

        [Fact]
        public void Lambda_04()
        {
            string source = "(scoped R a, scoped ref R b, ref scoped R c) => null";
            UsingExpression(source, TestOptions.RegularNext);

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
            UsingExpression(source, TestOptions.RegularNext);

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
            UsingExpression(source, TestOptions.RegularNext,
                // (1,1): error CS1073: Unexpected token 'scoped'
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "(ref scoped ").WithArguments("scoped").WithLocation(1, 1),
                // (1,2): error CS1525: Invalid expression term 'ref'
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref scoped").WithArguments("ref").WithLocation(1, 2),
                // (1,13): error CS1026: ) expected
                // (ref scoped scoped R r) => { }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "scoped").WithLocation(1, 13));

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

        [Fact]
        public void Lambda_07()
        {
            string source = "([A] scoped R a, [B] scoped ref R b, [C] ref scoped R c) => null";
            UsingExpression(source, TestOptions.RegularNext);

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
        [InlineData(LanguageVersionFacts.CSharpNext)]
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
            UsingDeclaration(source, TestOptions.RegularNext);

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
            UsingDeclaration(source, TestOptions.RegularNext);

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

        [Fact]
        public void Local_01()
        {
            string source =
@$"class Program
{{
    static void Main()
    {{
        scoped int x;
    }}
}}
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void Local_01_CSharp10()
        {
            string source =
@$"class Program
{{
    static void Main()
    {{
        scoped int x;
    }}
}}
";
            UsingCompilationRoot(source, TestOptions.Regular10,
                // (5,16): error CS1002: ; expected
                //         scoped int x;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(5, 16));

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
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
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
        public void Local_02()
        {
            string source =
@"scoped int a;
scoped ref int b;
ref scoped int c;
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
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

        [Fact]
        public void Local_02_CSharp10()
        {
            string source =
@"scoped int a;
scoped ref int b;
ref scoped int c;
";
            UsingCompilationRoot(source, TestOptions.Regular10,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // scoped int a;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "scoped").WithLocation(1, 1),
                // (2,8): error CS1585: Member modifier 'ref' must precede the member type and name
                // scoped ref int b;
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(2, 8),
                // (3,5): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // ref scoped int c;
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "scoped").WithLocation(3, 5));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
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
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
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

        [Fact]
        public void Local_03()
        {
            string source =
@"scoped ref readonly S a;
ref readonly scoped S b;
scoped ref readonly scoped S c;
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
                                N(SyntaxKind.ScopedKeyword);
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
                                N(SyntaxKind.ScopedKeyword);
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
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void Local_04(LanguageVersion langVersion)
        {
            string source =
@"scoped a;
ref scoped b;
";
            UsingCompilationRoot(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void Local_05(LanguageVersion langVersion)
        {
            string source =
@"scoped.nested a;
ref scoped.nested b;
";
            UsingCompilationRoot(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp10)]
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void Local_06(LanguageVersion langVersion)
        {
            string source =
@"class Program
{
    static void Main()
    {
        scoped a;
        ref readonly scoped b;
    }
}";
            UsingCompilationRoot(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
                                        N(SyntaxKind.ReadOnlyKeyword);
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
        public void Local_07()
        {
            string source =
@"scoped scoped a;
scoped ref scoped b;
ref scoped scoped c;
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ScopedKeyword);
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
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void Local_08()
        {
            string source =
@"scoped var a;
scoped ref var b;
ref scoped var c;
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.RefType);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ScopedKeyword);
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

        [Fact]
        public void Local_09()
        {
            string source =
@"scoped ref readonly var a;
ref readonly scoped var b;
scoped ref readonly scoped var c;
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
                                N(SyntaxKind.ScopedKeyword);
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

        [Fact]
        public void Local_10()
        {
            string source =
@"ref scoped readonly S a;
";
            UsingCompilationRoot(source, TestOptions.RegularNext,
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
        public void Local_11()
        {
            string source =
@"scoped scoped int a;
scoped scoped var b;
";
            UsingCompilationRoot(source, TestOptions.RegularNext,
                // (1,15): error CS1003: Syntax error, ',' expected
                // scoped scoped int a;
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",", "int").WithLocation(1, 15),
                // (2,8): error CS1031: Type expected
                // scoped scoped var b;
                Diagnostic(ErrorCode.ERR_TypeExpected, "scoped").WithArguments("scoped").WithLocation(2, 8));

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
        [InlineData(LanguageVersionFacts.CSharpNext)]
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

        [Fact]
        public void FunctionPointer_02()
        {
            string source = @"delegate*<scoped R, ref scoped R, scoped ref int, void> f;";
            UsingStatement(source, TestOptions.RegularPreview);

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

        [Fact]
        public void FunctionPointer_03()
        {
            string source = @"delegate*<scoped R, ref scoped R, scoped ref int, void> f;";
            UsingStatement(source, TestOptions.Regular10,
                // (1,18): error CS1003: Syntax error, ',' expected
                // delegate*<scoped R, ref scoped R, scoped ref int, void> f;
                Diagnostic(ErrorCode.ERR_SyntaxError, "R").WithArguments(",", "").WithLocation(1, 18),
                // (1,32): error CS1003: Syntax error, ',' expected
                // delegate*<scoped R, ref scoped R, scoped ref int, void> f;
                Diagnostic(ErrorCode.ERR_SyntaxError, "R").WithArguments(",", "").WithLocation(1, 32),
                // (1,42): error CS1003: Syntax error, ',' expected
                // delegate*<scoped R, ref scoped R, scoped ref int, void> f;
                Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",", "ref").WithLocation(1, 42));

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
                            N(SyntaxKind.CommaToken);
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

        [Fact]
        public void Type_01()
        {
            string source =
@"scoped struct A { }
scoped ref struct B { }
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
        public void Type_01_CSharp10()
        {
            string source =
@"scoped struct A { }
scoped ref struct B { }
";
            UsingCompilationRoot(source, TestOptions.Regular10,
                // (1,1): error CS0116: A namespace cannot directly contain members such as fields, methods or statements
                // scoped struct A { }
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "scoped").WithLocation(1, 1),
                // (2,8): error CS1585: Member modifier 'ref' must precede the member type and name
                // scoped ref struct B { }
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "ref").WithArguments("ref").WithLocation(2, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.IncompleteMember);
                {
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
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "scoped");
                    }
                }
                N(SyntaxKind.StructDeclaration);
                {
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

        // PROTOTYPE: 'scoped' before type declaration should be an error.
        [Fact]
        public void Type_02()
        {
            string source =
@"scoped record A { }
scoped readonly record struct B;
readonly scoped record struct C();
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
            UsingCompilationRoot(source, TestOptions.RegularNext,
                // (1,17): error CS1001: Identifier expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, '(' expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("(", "int").WithLocation(1, 17),
                // (1,22): error CS1003: Syntax error, ',' expected
                // delegate scoped int A();
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",", "(").WithLocation(1, 22),
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
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void Type_04(LanguageVersion langVersion)
        {
            string source =
@"delegate scoped A();
";
            UsingCompilationRoot(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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

        // PROTOTYPE: Should `ref scoped` be a parse error in a return value? Or should plain `scoped` be parsed as a modifier as well?
        [Fact]
        public void Type_05()
        {
            string source =
@"delegate ref scoped int B();
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.DelegateDeclaration);
                {
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ScopedKeyword);
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

        [Fact]
        public void Type_05_CSharp10()
        {
            string source =
@"delegate ref scoped int B();
";
            UsingCompilationRoot(source, TestOptions.Regular10,
                // (1,21): error CS1001: Identifier expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 21),
                // (1,21): error CS1003: Syntax error, '(' expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("(", "int").WithLocation(1, 21),
                // (1,26): error CS1003: Syntax error, ',' expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(",", "(").WithLocation(1, 26),
                // (1,27): error CS8124: Tuple must contain at least two elements.
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, 27),
                // (1,28): error CS1001: Identifier expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 28),
                // (1,28): error CS1026: ) expected
                // delegate ref scoped int B();
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 28));

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
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void Type_06(LanguageVersion langVersion)
        {
            string source =
@"delegate ref scoped B();
";
            UsingCompilationRoot(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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

        [Fact]
        public void Type_07()
        {
            string source =
@"[A] scoped struct A { }
[A, B] scoped ref struct B { }
";
            UsingCompilationRoot(source, TestOptions.RegularNext);

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
        [InlineData(LanguageVersionFacts.CSharpNext)]
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
            UsingCompilationRoot(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
        [InlineData(LanguageVersionFacts.CSharpNext)]
        public void LocalAssignment_02(LanguageVersion langVersion)
        {
            string source =
@"bool scoped;
scoped = true;
";
            UsingCompilationRoot(source, TestOptions.Regular.WithLanguageVersion(langVersion));

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
    }
}
