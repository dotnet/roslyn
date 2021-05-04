// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MemberDeclarationParsingTests : ParsingTests
    {
        public MemberDeclarationParsingTests(ITestOutputHelper output) : base(output) { }

        private MemberDeclarationSyntax ParseDeclaration(string text, int offset = 0, ParseOptions options = null)
        {
            return SyntaxFactory.ParseMemberDeclaration(text, offset, options);
        }

        private SyntaxTree UsingTree(string text, CSharpParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            var tree = base.UsingTree(text, options);

            var actualErrors = tree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);

            return tree;
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void ParsePrivate()
        {
            UsingDeclaration("private", options: null,
                // (1,8): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // private
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(1, 8)
                );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.PrivateKeyword);
            }
            EOF();
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void ParseEmpty()
        {
            Assert.Null(ParseDeclaration(""));
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void ParseTrash()
        {
            Assert.Null(ParseDeclaration("+-!@#$%^&*()"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void ParseOverflow()
        {
            const int n = 10000;
            var sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append("class A{\n");
            }
            for (int i = 0; i < n; i++)
            {
                sb.Append("}\n");
            }

            var d = SyntaxFactory.ParseMemberDeclaration(sb.ToString());
            if (d.GetDiagnostics().Any()) // some platforms have extra deep stacks and can parse this
            {
                d.GetDiagnostics().Verify(
                    // error CS8078: An expression is too long or complex to compile
                    Diagnostic(ErrorCode.ERR_InsufficientStack, "")
                    );
            }
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void ParseOverflow2()
        {
            const int n = 10000;
            var sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append("namespace ns {\n");
            }
            for (int i = 0; i < n; i++)
            {
                sb.Append("}\n");
            }

            // SyntaxFactory.ParseCompilationUnit has been hardened to be resilient to stack overflow at the same time.
            var cu = SyntaxFactory.ParseCompilationUnit(sb.ToString());
            if (cu.GetDiagnostics().Any()) // some platforms have extra deep stacks and can parse this
            {
                cu.GetDiagnostics().Verify(
                    // error CS8078: An expression is too long or complex to compile
                    Diagnostic(ErrorCode.ERR_InsufficientStack, "")
                );
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void Statement()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("x = x + 1;", offset: 0, options: options, consumeFullText: true,
                    // (1,3): error CS1519: Invalid token '=' in class, record, struct, or interface member declaration
                    // x = x + 1;
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(1, 3),
                    // (1,1): error CS1073: Unexpected token '='
                    // x = x + 1;
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "x").WithArguments("=").WithLocation(1, 1)
                    );
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void Namespace()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                var d = SyntaxFactory.ParseMemberDeclaration("namespace ns {}", options: options);
                Assert.Null(d);
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void TypeDeclaration()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("class C { }", options: options);
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void MethodDeclaration()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("void M() { }", options: options);
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
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void FieldDeclaration()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("static int F1 = a, F2 = b;", options: options);
                N(SyntaxKind.FieldDeclaration);
                {
                    N(SyntaxKind.StaticKeyword);
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F1");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                            }
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "F2");
                            N(SyntaxKind.EqualsValueClause);
                            {
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void CtorDeclaration()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public ThisClassName(int x) : base(x) { }", options: options);
                N(SyntaxKind.ConstructorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.IdentifierToken, "ThisClassName");
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.BaseConstructorInitializer);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.BaseKeyword);
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void DtorDeclaration()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public ~ThisClassName() { }", options: options);
                N(SyntaxKind.DestructorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.TildeToken);
                    N(SyntaxKind.IdentifierToken, "ThisClassName");
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
                EOF();
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void ConversionDeclaration()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public implicit operator long(int x) => x;", options: options);
                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ImplicitKeyword);
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.LongKeyword);
                    }
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void OperatorDeclaration()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int operator +(int x, int y) => x + y;", options: options);
                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.AddExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.PlusToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void TrashAfterDeclaration()
        {
            UsingDeclaration("public int x; public int y", offset: 0, options: null, consumeFullText: true,
                // (1,1): error CS1073: Unexpected token 'public'
                // public int x; public int y
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "public int x;").WithArguments("public").WithLocation(1, 1)
                );
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
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();

            UsingDeclaration("public int x; public int y", offset: 0, options: null, consumeFullText: false);
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
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(11959, "https://github.com/dotnet/roslyn/issues/11959")]
        public void GenericAsyncTask_01()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("async Task<SomeNamespace.SomeType Method();", options: options,
                    // (1,1): error CS1073: Unexpected token '('
                    // async Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "async Task<SomeNamespace.SomeType Method").WithArguments("(").WithLocation(1, 1),
                    // (1,35): error CS1003: Syntax error, ',' expected
                    // async Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "Method").WithArguments(",", "").WithLocation(1, 35),
                    // (1,41): error CS1003: Syntax error, '>' expected
                    // async Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">", "(").WithLocation(1, 41)
                    );
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Task");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeNamespace");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeType");
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Method");
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(11959, "https://github.com/dotnet/roslyn/issues/11959")]
        public void GenericPublicTask_01()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public Task<SomeNamespace.SomeType Method();", options: options,
                    // (1,1): error CS1073: Unexpected token '('
                    // public Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "public Task<SomeNamespace.SomeType Method").WithArguments("(").WithLocation(1, 1),
                    // (1,36): error CS1003: Syntax error, ',' expected
                    // public Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "Method").WithArguments(",", "").WithLocation(1, 36),
                    // (1,42): error CS1003: Syntax error, '>' expected
                    // public Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">", "(").WithLocation(1, 42)
                    );
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Task");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeNamespace");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeType");
                                }
                            }
                            M(SyntaxKind.CommaToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Method");
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(11959, "https://github.com/dotnet/roslyn/issues/11959")]
        public void GenericAsyncTask_02()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("async Task<SomeNamespace. Method();", options: options,
                    // (1,1): error CS1073: Unexpected token '('
                    // async Task<SomeNamespace. Method();
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "async Task<SomeNamespace. Method").WithArguments("(").WithLocation(1, 1),
                    // (1,33): error CS1003: Syntax error, '>' expected
                    // async Task<SomeNamespace. Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">", "(").WithLocation(1, 33)
                    );
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Task");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeNamespace");
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Method");
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(11959, "https://github.com/dotnet/roslyn/issues/11959")]
        public void GenericPublicTask_02()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public Task<SomeNamespace. Method();", options: options,
                    // (1,1): error CS1073: Unexpected token '('
                    // public Task<SomeNamespace. Method();
                    Diagnostic(ErrorCode.ERR_UnexpectedToken, "public Task<SomeNamespace. Method").WithArguments("(").WithLocation(1, 1),
                    // (1,34): error CS1003: Syntax error, '>' expected
                    // public Task<SomeNamespace. Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">", "(").WithLocation(1, 34)
                    );
                N(SyntaxKind.IncompleteMember);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Task");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeNamespace");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Method");
                                }
                            }
                            M(SyntaxKind.GreaterThanToken);
                        }
                    }
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(11959, "https://github.com/dotnet/roslyn/issues/11959")]
        public void GenericAsyncTask_03()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("async Task<SomeNamespace.> Method();", options: options,
                    // (1,26): error CS1001: Identifier expected
                    // async Task<SomeNamespace.> Method();
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(1, 26)
                    );
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Task");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeNamespace");
                                }
                                N(SyntaxKind.DotToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "Method");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(11959, "https://github.com/dotnet/roslyn/issues/11959")]
        public void GenericPublicTask_03()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public Task<SomeNamespace.> Method();", options: options,
                    // (1,27): error CS1001: Identifier expected
                    // public Task<SomeNamespace.> Method();
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(1, 27)
                    );
                N(SyntaxKind.MethodDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.GenericName);
                    {
                        N(SyntaxKind.IdentifierToken, "Task");
                        N(SyntaxKind.TypeArgumentList);
                        {
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "SomeNamespace");
                                }
                                N(SyntaxKind.DotToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                            N(SyntaxKind.GreaterThanToken);
                        }
                    }
                    N(SyntaxKind.IdentifierToken, "Method");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [CompilerTrait(CompilerFeature.InitOnlySetters)]
        public void InitAccessor()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("string Property { get; init; }", options: options);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Property");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.GetAccessorDeclaration);
                        {
                            N(SyntaxKind.GetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.InitKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        [CompilerTrait(CompilerFeature.InitOnlySetters)]
        public void InitSetAccessor()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("string Property { init set; }", options: options,
                    // (1,24): error CS8180: { or ; or => expected
                    // string Property { init set; }
                    Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "set").WithLocation(1, 24),
                    // (1,30): error CS1513: } expected
                    // string Property { init set; }
                    Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 30)
                    );
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Property");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.InitKeyword);
                            N(SyntaxKind.Block);
                            {
                                M(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.ExpressionStatement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "set");
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        M(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        [CompilerTrait(CompilerFeature.InitOnlySetters)]
        public void InitAndSetAccessor()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("string Property { init; set; }", options: options);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Property");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.InitKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        [CompilerTrait(CompilerFeature.InitOnlySetters)]
        public void SetAndInitAccessor()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("string Property { set; init; }", options: options);
                N(SyntaxKind.PropertyDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.IdentifierToken, "Property");
                    N(SyntaxKind.AccessorList);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.SetAccessorDeclaration);
                        {
                            N(SyntaxKind.SetKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.InitAccessorDeclaration);
                        {
                            N(SyntaxKind.InitKeyword);
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_01()
        {
            var error =
                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // public int N.I.operator +(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 12);

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("public int N.I.operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ? new[] { error } : new DiagnosticDescription[] { });

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_02()
        {
            var errors = new[] {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator", "implicit").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("public int N.I.implicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int N.I.implicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        M(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_03()
        {
            var errors = new[] {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator", "explicit").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("public int N.I.explicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int N.I.explicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        M(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_04()
        {
            var errors = new[] {
                // (1,16): error CS1003: Syntax error, '.' expected
                // public int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("public int N.I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int N.I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I ").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_05()
        {
            var errors = new[] {
                // (1,14): error CS1003: Syntax error, '.' expected
                // public int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 14)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("public int I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "I ").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_06()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int N::I::operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,16): error CS7000: Unexpected use of an aliased name
                    // public int N::I::operator +(int x, int y) => x + y;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 16)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.AliasQualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "N");
                            }
                            N(SyntaxKind.ColonColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                        M(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.AddExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.PlusToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_07()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int I::operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,13): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                    // public int I::operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 13)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        M(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_08()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_09()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int I<T>.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
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
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_10()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int N1::N2::I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,18): error CS7000: Unexpected use of an aliased name
                    // public int N1::N2::I.operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 18)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.AliasQualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N1");
                                }
                                N(SyntaxKind.ColonColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N2");
                                }
                            }
                            M(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_11()
        {
            var error =
                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // public int N.I.operator +(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 12);

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("public int N.I.operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ? new[] { error } : new DiagnosticDescription[] { });

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PublicKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                            }
                            N(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.AddExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.PlusToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
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

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_12()
        {
            var errors = new[] {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator", "implicit").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("public int N.I.implicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int N.I.implicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PublicKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                            }
                            M(SyntaxKind.OperatorKeyword);
                            M(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_13()
        {
            var errors = new[] {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator", "explicit").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("public int N.I.explicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int N.I.explicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PublicKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                            }
                            M(SyntaxKind.OperatorKeyword);
                            M(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_14()
        {
            var errors = new[] {
                // (1,16): error CS1003: Syntax error, '.' expected
                // public int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("public int N.I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int N.I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I ").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PublicKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                M(SyntaxKind.DotToken);
                            }
                            N(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_15()
        {
            var errors = new[] {
                // (1,14): error CS1003: Syntax error, '.' expected
                // public int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 14)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("public int I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,12): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // public int I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "I ").WithArguments("static abstract members in interfaces").WithLocation(1, 12)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PublicKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                                M(SyntaxKind.DotToken);
                            }
                            N(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_16()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("public int N::I::operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,16): error CS7000: Unexpected use of an aliased name
                    // public int N::I::operator +(int x, int y) => x + y;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 16)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.AliasQualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.ColonColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_17()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("public int I::operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,13): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                    // public int I::operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 13)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_18()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("public int I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_19()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("public int I<T>.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
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
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_20()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("public int N1::N2::I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,18): error CS7000: Unexpected use of an aliased name
                    // public int N1::N2::I.operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 18)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.AliasQualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N1");
                                    }
                                    N(SyntaxKind.ColonColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N2");
                                    }
                                }
                                M(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_21()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int I..operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,13): error CS1003: Syntax error, ',' expected
                    // public int I..operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "..").WithArguments(",", "..").WithLocation(1, 13)
                    );

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
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_22()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("public int I . . operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,16): error CS1001: Identifier expected
                    // public int I . . operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 16)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            N(SyntaxKind.DotToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_23()
        {
            var error =
                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // int N.I.operator +(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 5);

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("int N.I.operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ? new[] { error } : new DiagnosticDescription[] { });

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_24()
        {
            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator", "implicit").WithLocation(1, 9),
                // (1,9): error CS1019: Overloadable unary operator expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("int N.I.implicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int N.I.implicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        M(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_25()
        {
            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator", "explicit").WithLocation(1, 9),
                // (1,16): error CS1019: Overloadable unary operator expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("int N.I.explicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int N.I.explicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        M(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_26()
        {
            var errors = new[] {
                // (1,9): error CS1003: Syntax error, '.' expected
                // int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("int N.I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int N.I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I ").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_27()
        {
            var errors = new[] {
                // (1,7): error CS1003: Syntax error, '.' expected
                // int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 7)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("int I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "I ").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_28()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int N::I::operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,9): error CS7000: Unexpected use of an aliased name
                    // int N::I::operator +(int x, int y) => x + y;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 9)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.AliasQualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "N");
                            }
                            N(SyntaxKind.ColonColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                        M(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.AddExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.PlusToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_29()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int I::operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,6): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                    // int I::operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 6)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        M(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_30()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_31()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int I<T>.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
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
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_32()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int N1::N2::I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,11): error CS7000: Unexpected use of an aliased name
                    // int N1::N2::I.operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 11)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.AliasQualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N1");
                                }
                                N(SyntaxKind.ColonColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N2");
                                }
                            }
                            M(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_33()
        {
            var error =
                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                // int N.I.operator +(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 5);

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("int N.I.operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ? new[] { error } : new DiagnosticDescription[] { });

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                            }
                            N(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.AddExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.PlusToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
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

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_34()
        {
            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator", "implicit").WithLocation(1, 9),
                // (1,9): error CS1019: Overloadable unary operator expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("int N.I.implicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int N.I.implicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                            }
                            M(SyntaxKind.OperatorKeyword);
                            M(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_35()
        {
            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator", "explicit").WithLocation(1, 9),
                // (1,9): error CS1019: Overloadable unary operator expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("int N.I.explicit (int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int N.I.explicit (int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I.").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                N(SyntaxKind.DotToken);
                            }
                            M(SyntaxKind.OperatorKeyword);
                            M(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_36()
        {
            var errors = new[] {
                // (1,9): error CS1003: Syntax error, '.' expected
                // int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("int N.I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int N.I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "N.I ").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "I");
                                    }
                                }
                                M(SyntaxKind.DotToken);
                            }
                            N(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_37()
        {
            var errors = new[] {
                // (1,7): error CS1003: Syntax error, '.' expected
                // int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".", "operator").WithLocation(1, 7)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree("int I operator +(int x) => x;", options: options.WithLanguageVersion(version),
                        version == LanguageVersion.CSharp9 ?
                            errors.Append(
                                // (1,5): error CS8652: The feature 'static abstract members in interfaces' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                                // int I operator +(int x) => x;
                                Diagnostic(ErrorCode.ERR_FeatureInPreview, "I ").WithArguments("static abstract members in interfaces").WithLocation(1, 5)
                                ).ToArray() :
                            errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.OperatorDeclaration);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                                M(SyntaxKind.DotToken);
                            }
                            N(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PlusToken);
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
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.ArrowExpressionClause);
                            {
                                N(SyntaxKind.EqualsGreaterThanToken);
                                N(SyntaxKind.IdentifierName);
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
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_38()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("int N::I::operator +(int x, int y) => x + y;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,9): error CS7000: Unexpected use of an aliased name
                    // int N::I::operator +(int x, int y) => x + y;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 9)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.AliasQualifiedName);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "N");
                                }
                                N(SyntaxKind.ColonColonToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.AddExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.PlusToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_39()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("int I::operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,6): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                    // int I::operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 6)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_40()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("int I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_41()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("int I<T>.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
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
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_42()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("int N1::N2::I.operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,11): error CS7000: Unexpected use of an aliased name
                    // int N1::N2::I.operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 11)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.QualifiedName);
                            {
                                N(SyntaxKind.AliasQualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N1");
                                    }
                                    N(SyntaxKind.ColonColonToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "N2");
                                    }
                                }
                                M(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.IdentifierName);
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
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_43()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int I..operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,6): error CS1003: Syntax error, ',' expected
                    // int I..operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "..").WithArguments(",", "..").WithLocation(1, 6)
                    );

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
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_44()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int I . . operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,9): error CS1001: Identifier expected
                    // int I . . operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 9)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
                    }
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            N(SyntaxKind.DotToken);
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PlusToken);
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
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }
    }
}
