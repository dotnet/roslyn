// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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

        [Fact]
        [WorkItem(367, "https://github.com/dotnet/roslyn/issues/367")]
        public void ParsePrivate()
        {
            UsingDeclaration("private", options: null,
                // (1,8): error CS1519: Invalid token '' in class, struct, or interface member declaration
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
                    // (1,3): error CS1519: Invalid token '=' in class, struct, or interface member declaration
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
    }
}
