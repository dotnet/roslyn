// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
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

        private static readonly CSharpParseOptions RequiredMembersOptions = TestOptions.Regular11;
        public static readonly IEnumerable<object[]> Regular10AndScriptAndRequiredMembersMinimum = new[] { new[] { TestOptions.Regular10 }, new[] { RequiredMembersOptions }, new[] { TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp10) } };
        public static readonly IEnumerable<object[]> Regular10AndScript = new[] { new[] { TestOptions.Regular10 }, new[] { TestOptions.Script.WithLanguageVersion(LanguageVersion.CSharp10) } };

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
        public void UnsignedRightShiftOperator_01()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingDeclaration("C operator >>>(C x, C y) => x;", options: options);

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.GreaterThanGreaterThanGreaterThanToken);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.IdentifierToken, "y");
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
        public void UnsignedRightShiftOperator_02()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingDeclaration("C operator > >>(C x, C y) => x;", options: options,
                    // (1,14): error CS1003: Syntax error, '(' expected
                    // C operator > >>(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ">").WithArguments("(").WithLocation(1, 14),
                    // (1,14): error CS1001: Identifier expected
                    // C operator > >>(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(1, 14),
                    // (1,27): error CS1001: Identifier expected
                    // C operator > >>(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(1, 27),
                    // (1,27): error CS1003: Syntax error, ',' expected
                    // C operator > >>(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(1, 27),
                    // (1,30): error CS1003: Syntax error, ',' expected
                    // C operator > >>(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(1, 30),
                    // (1,31): error CS1001: Identifier expected
                    // C operator > >>(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 31),
                    // (1,31): error CS1026: ) expected
                    // C operator > >>(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 31)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.GreaterThanToken);
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void UnsignedRightShiftOperator_03()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingDeclaration("C operator >> >(C x, C y) => x;", options: options,
                    // (1,15): error CS1003: Syntax error, '(' expected
                    // C operator >> >(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ">").WithArguments("(").WithLocation(1, 15),
                    // (1,15): error CS1001: Identifier expected
                    // C operator >> >(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(1, 15),
                    // (1,27): error CS1001: Identifier expected
                    // C operator >> >(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(1, 27),
                    // (1,27): error CS1003: Syntax error, ',' expected
                    // C operator >> >(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(1, 27),
                    // (1,30): error CS1003: Syntax error, ',' expected
                    // C operator >> >(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(1, 30),
                    // (1,31): error CS1001: Identifier expected
                    // C operator >> >(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 31),
                    // (1,31): error CS1026: ) expected
                    // C operator >> >(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 31)
                );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.GreaterThanGreaterThanToken);
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void UnsignedRightShiftOperator_04()
        {
            foreach (var options in new[] { TestOptions.RegularPreview, TestOptions.Regular10, TestOptions.Regular11 })
            {
                UsingDeclaration("C operator >>>=(C x, C y) => x;", options: options,
                    // (1,14): error CS1003: Syntax error, '(' expected
                    // C operator >>>=(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, ">=").WithArguments("(").WithLocation(1, 14),
                    // (1,14): error CS1001: Identifier expected
                    // C operator >>>=(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ">=").WithLocation(1, 14),
                    // (1,27): error CS1001: Identifier expected
                    // C operator >>>=(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(1, 27),
                    // (1,27): error CS1003: Syntax error, ',' expected
                    // C operator >>>=(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(1, 27),
                    // (1,30): error CS1003: Syntax error, ',' expected
                    // C operator >>>=(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "x").WithArguments(",").WithLocation(1, 30),
                    // (1,31): error CS1001: Identifier expected
                    // C operator >>>=(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ";").WithLocation(1, 31),
                    // (1,31): error CS1026: ) expected
                    // C operator >>>=(C x, C y) => x;
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 31)
                    );

                N(SyntaxKind.OperatorDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "C");
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.GreaterThanGreaterThanToken);
                    N(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "C");
                                    }
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.CloseParenToken);
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
                    Diagnostic(ErrorCode.ERR_SyntaxError, "Method").WithArguments(",").WithLocation(1, 35),
                    // (1,41): error CS1003: Syntax error, '>' expected
                    // async Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">").WithLocation(1, 41)
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
                    Diagnostic(ErrorCode.ERR_SyntaxError, "Method").WithArguments(",").WithLocation(1, 36),
                    // (1,42): error CS1003: Syntax error, '>' expected
                    // public Task<SomeNamespace.SomeType Method();
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">").WithLocation(1, 42)
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
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">").WithLocation(1, 33)
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
                    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments(">").WithLocation(1, 34)
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

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierProperty_01(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required string Prop { get; }", options: parseOptions);
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
                N(SyntaxKind.IdentifierToken, "Prop");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScript))]
        public void RequiredModifierProperty_02(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required Prop { get; }", options: parseOptions);
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "required");
                }
                N(SyntaxKind.IdentifierToken, "Prop");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierProperty_03()
        {
            UsingDeclaration("required Prop { get; }", options: RequiredMembersOptions,
                // (1,1): error CS1073: Unexpected token '{'
                // required Prop { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "required Prop").WithArguments("{").WithLocation(1, 1),
                // (1,15): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                // required Prop { get; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(1, 15)
                );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Prop");
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScript))]
        public void RequiredModifierProperty_04(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required required { get; }", options: parseOptions);
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "required");
                }
                N(SyntaxKind.IdentifierToken, "required");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierProperty_05()
        {
            UsingDeclaration("required required { get; }", options: RequiredMembersOptions,
                // (1,1): error CS1073: Unexpected token '{'
                // required required { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "required required").WithArguments("{").WithLocation(1, 1),
                // (1,19): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                // required required { get; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(1, 19)
                );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.RequiredKeyword);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierProperty_06()
        {
            UsingDeclaration("required required Prop { get; }", options: RequiredMembersOptions,
                // (1,1): error CS1073: Unexpected token '{'
                // required required Prop { get; }
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "required required Prop").WithArguments("{").WithLocation(1, 1),
                // (1,24): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                // required required Prop { get; }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(1, 24)
                );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Prop");
                }
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierProperty_07()
        {
            UsingDeclaration("required Type required { get; }", options: RequiredMembersOptions);
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Type");
                }
                N(SyntaxKind.IdentifierToken, "required");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierField_01(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required string Field;", options: parseOptions);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "Field");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScript))]
        public void RequiredModifierField_02(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required Field;", options: parseOptions);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "required");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "Field");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierField_03()
        {
            UsingDeclaration("required Field;", options: RequiredMembersOptions,
                // (1,1): error CS1073: Unexpected token ';'
                // required Field;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "required Field").WithArguments(";").WithLocation(1, 1),
                // (1,15): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                // required Field;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(1, 15)
                );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "Field");
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScript))]
        public void RequiredModifierField_04(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required required;", options: parseOptions);
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "required");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "required");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierField_05()
        {
            UsingDeclaration("required required;", options: RequiredMembersOptions,
                // (1,1): error CS1073: Unexpected token ';'
                // required required;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "required required").WithArguments(";").WithLocation(1, 1),
                // (1,18): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                // required required;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(1, 18)
                );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.RequiredKeyword);
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierMethod_01(CSharpParseOptions parseOptions)
        {
            // Note this is a semantic error, not a syntactic one
            UsingDeclaration("required string M() {}", options: parseOptions);
            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
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

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScript))]
        public void RequiredModifierMethod_02(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required M() {}", options: parseOptions);
            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "required");
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

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierMethod_03()
        {
            UsingDeclaration("required M() {}", options: RequiredMembersOptions);
            N(SyntaxKind.ConstructorDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
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

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierOperator(CSharpParseOptions parseOptions)
        {
            // Note this is a semantic error, not a syntactic one
            UsingDeclaration("static required C operator+(C c1, C c2) {}", options: parseOptions);
            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "c1");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "c2");
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

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierConversion_01(CSharpParseOptions parseOptions)
        {
            // Note this is a semantic error, not a syntactic one
            UsingDeclaration("static required implicit operator C(S s) {}", options: parseOptions);
            N(SyntaxKind.ConversionOperatorDeclaration);
            {
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "S");
                        }
                        N(SyntaxKind.IdentifierToken, "s");
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

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScript))]
        public void RequiredModifierConversion_02(CSharpParseOptions parseOptions)
        {
            var text = "static implicit required operator C(S s) {}";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: parseOptions).VerifyDiagnostics(
                // (1,27): error CS0246: The type or namespace name 'required' could not be found (are you missing a using directive or an assembly reference?)
                // class C { static implicit required operator C(S s) {} }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "required").WithArguments("required").WithLocation(1, 27),
                // (1,27): error CS8936: Feature 'static abstract members in interfaces' is not available in C# 10.0. Please use language version 11.0 or greater.
                // class C { static implicit required operator C(S s) {} }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion10, "required ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 27),
                // (1,27): error CS0538: 'required' in explicit interface declaration is not an interface
                // class C { static implicit required operator C(S s) {} }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "required").WithArguments("required").WithLocation(1, 27),
                // (1,36): error CS1003: Syntax error, '.' expected
                // class C { static implicit required operator C(S s) {} }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 36),
                // (1,45): error CS0161: 'C.implicit operator C(S)': not all code paths return a value
                // class C { static implicit required operator C(S s) {} }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "C").WithArguments("C.implicit operator C(S)").WithLocation(1, 45),
                // (1,47): error CS0246: The type or namespace name 'S' could not be found (are you missing a using directive or an assembly reference?)
                // class C { static implicit required operator C(S s) {} }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "S").WithArguments("S").WithLocation(1, 47));

            UsingDeclaration(text, options: parseOptions,
                // (1,26): error CS1003: Syntax error, '.' expected
                // static implicit required operator C(S s) {}
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 26));
            N(SyntaxKind.ConversionOperatorDeclaration);
            {
                N(SyntaxKind.StaticKeyword);
                N(SyntaxKind.ImplicitKeyword);
                N(SyntaxKind.ExplicitInterfaceSpecifier);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "required");
                    }
                    M(SyntaxKind.DotToken);
                }
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "S");
                        }
                        N(SyntaxKind.IdentifierToken, "s");
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

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierIncompleteProperty_01(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required string Prop { get;", options: parseOptions,
                // (1,28): error CS1513: } expected
                // required string Prop { get;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 28)
            );
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
                N(SyntaxKind.IdentifierToken, "Prop");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.GetAccessorDeclaration);
                    {
                        N(SyntaxKind.GetKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierIncompleteProperty_02(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required string Prop {", options: parseOptions,
                // (1,23): error CS1513: } expected
                // required string Prop {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 23)
            );
            N(SyntaxKind.PropertyDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
                N(SyntaxKind.IdentifierToken, "Prop");
                N(SyntaxKind.AccessorList);
                {
                    N(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierIncompleteMember_01(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required string Prop", options: parseOptions,
                // (1,21): error CS1002: ; expected
                // required string Prop
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 21)
            );
            N(SyntaxKind.FieldDeclaration);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.StringKeyword);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "Prop");
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierIncompleteMember_02(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required string", options: parseOptions,
                // (1,16): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // required string
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(1, 16)
            );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.StringKeyword);
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifierIncompleteMember_03(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required C", options: parseOptions,
                // (1,11): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // required C
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(1, 11)
            );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
            }
            EOF();
        }

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers)]
        [MemberData(nameof(Regular10AndScript))]
        public void RequiredModifierIncompleteMember_04(CSharpParseOptions parseOptions)
        {
            UsingDeclaration("required", options: parseOptions,
                // (1,9): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // required
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(1, 9)
            );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "required");
                }
            }
            EOF();
        }

        [Fact, CompilerTrait(CompilerFeature.RequiredMembers)]
        public void RequiredModifierIncompleteMember_05()
        {
            UsingDeclaration("required", options: RequiredMembersOptions,
                // (1,9): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // required
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(1, 9)
            );
            N(SyntaxKind.IncompleteMember);
            {
                N(SyntaxKind.RequiredKeyword);
            }
            EOF();
        }

        [Fact]
        public void TypeNamedRequired_CSharp10()
        {
            UsingNode($$"""
                class required { }

                class C
                {
                    required _required;
                    required[] _array;
                    required* _ptr;
                    required? _nullable;
                    delegate*<required, required> _funcPtr;
                    (required, required) _tuple;
                }
                """,
                options: TestOptions.Regular10);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "required");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "required");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_required");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "required");
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
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_array");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "required");
                                }
                                N(SyntaxKind.AsteriskToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_ptr");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "required");
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_nullable");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
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
                                            N(SyntaxKind.IdentifierToken, "required");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.FunctionPointerParameter);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "required");
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_funcPtr");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "required");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "required");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_tuple");
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
        public void TypeNamedRequired_CSharp11()
        {
            UsingNode($$"""
                class required { }

                class C
                {
                    required _required;
                    required[] _array;
                    required* _ptr;
                    required? _nullable;
                    delegate*<required, required> _funcPtr;
                    (required, required) _tuple;
                }
                """,
                options: TestOptions.Regular11,

                // (5,23): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     required _required;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 23),
                // (5,23): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     required _required;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(5, 23),
                // (6,13): error CS1031: Type expected
                //     required[] _array;
                Diagnostic(ErrorCode.ERR_TypeExpected, "[").WithLocation(6, 13),
                // (7,13): error CS1031: Type expected
                //     required* _ptr;
                Diagnostic(ErrorCode.ERR_TypeExpected, "*").WithLocation(7, 13),
                // (8,13): error CS1031: Type expected
                //     required? _nullable;
                Diagnostic(ErrorCode.ERR_TypeExpected, "?").WithLocation(8, 13)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "required");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.RequiredKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "_required");
                        }
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.RequiredKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.ArrayType);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_array");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.RequiredKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PointerType);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.AsteriskToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_ptr");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.RequiredKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.NullableType);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.QuestionToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_nullable");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
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
                                            N(SyntaxKind.IdentifierToken, "required");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.FunctionPointerParameter);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "required");
                                        }
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_funcPtr");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "required");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "required");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "_tuple");
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

        [Theory, CompilerTrait(CompilerFeature.RequiredMembers), WorkItem(61510, "https://github.com/dotnet/roslyn/issues/61510")]
        [MemberData(nameof(Regular10AndScriptAndRequiredMembersMinimum))]
        public void RequiredModifier_LocalNamedRequired_TopLevelStatements(CSharpParseOptions parseOptions)
        {
            bool isScript = parseOptions.Kind == SourceCodeKind.Script;

            UsingTree("""
                bool required;
                required = true;
                """, options: parseOptions);
            N(SyntaxKind.CompilationUnit);
            {
                if (isScript)
                {
                    N(SyntaxKind.FieldDeclaration);
                }
                else
                {
                    N(SyntaxKind.GlobalStatement);
                    N(SyntaxKind.LocalDeclarationStatement);
                }

                {
                    N(SyntaxKind.VariableDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.BoolKeyword);
                        }
                        N(SyntaxKind.VariableDeclarator);
                        {
                            N(SyntaxKind.IdentifierToken, "required");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "required");
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

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_01()
        {
            var text = "public int N.I.operator +(int x, int y) => x + y;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 35));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 35));

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version));

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
            var text = "public int N.I.implicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));

            var errors = new[]
            {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 16)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "public int N.I.explicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));

            var errors = new[] {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "public int N.I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, '.' expected
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 26),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 35));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, '.' expected
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 26),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 35));

            var errors = new[] {
                // (1,16): error CS1003: Syntax error, '.' expected
                // public int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "public int I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 22),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 33),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 33));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 22),
                // (1,22): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 22),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 33),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 33));

            var errors = new[] {
                // (1,14): error CS1003: Syntax error, '.' expected
                // public int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 14)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "public int N.I.operator +(int x, int y) => x + y;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 35));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { public int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 35));

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version));

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
            var text = "public int N.I.implicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));

            var errors = new[] {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "public int N.I.explicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,18): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 18),
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, 'operator' expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 26),
                // (1,26): error CS1019: Overloadable unary operator expected
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 26),
                // (1,26): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "").WithArguments("public").WithLocation(1, 26),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 26));

            var errors = new[] {
                // (1,8): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 8),
                // (1,16): error CS1003: Syntax error, 'operator' expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 16),
                // (1,16): error CS1019: Overloadable unary operator expected
                // public int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "public int N.I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, '.' expected
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 26),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 35));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 22),
                // (1,22): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 22),
                // (1,26): error CS1003: Syntax error, '.' expected
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 26),
                // (1,35): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 35),
                // (1,35): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 35));

            var errors = new[] {
                // (1,16): error CS1003: Syntax error, '.' expected
                // public int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 16)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "public int I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 22),
                // (1,22): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 22),
                // (1,22): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 22),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 33),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 33));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,22): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 22),
                // (1,22): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 22),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS0106: The modifier 'public' is not valid for this item
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "+").WithArguments("public").WithLocation(1, 33),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { public int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 33));

            var errors = new[] {
                // (1,14): error CS1003: Syntax error, '.' expected
                // public int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 14)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
                    // (1,14): error CS1001: Identifier expected
                    // public int I..operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".operato").WithLocation(1, 14)
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
            var text = "int N.I.operator +(int x, int y) => x + y;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 28));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 28));

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version));

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
            var text = "int N.I.implicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));

            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 9),
                // (1,9): error CS1019: Overloadable unary operator expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "int N.I.explicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));

            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 9),
                // (1,16): error CS1019: Overloadable unary operator expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "int N.I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, '.' expected
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 19),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 28));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, '.' expected
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 19),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 28));

            var errors = new[] {
                // (1,9): error CS1003: Syntax error, '.' expected
                // int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "int I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 15),
                // (1,17): error CS1003: Syntax error, '.' expected
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 17),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 26));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 15),
                // (1,15): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 15),
                // (1,17): error CS1003: Syntax error, '.' expected
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 17),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 26));

            var errors = new[] {
                // (1,7): error CS1003: Syntax error, '.' expected
                // int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 7)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "int N.I.operator +(int x, int y) => x + y;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 28));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int, int)' must be declared static
                // class C { int N.I.operator +(int x, int y) => x + y; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int, int)").WithLocation(1, 28));

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version));

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
            var text = "int N.I.implicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.implicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));

            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(1, 9),
                // (1,9): error CS1019: Overloadable unary operator expected
                // int N.I.implicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "int N.I.explicit (int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 11),
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, 'operator' expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 19),
                // (1,19): error CS1019: Overloadable unary operator expected
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 19),
                // (1,19): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I.explicit (int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "").WithArguments("C.operator +(int)").WithLocation(1, 19));

            var errors = new[] {
                // (1,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(1, 1),
                // (1,9): error CS1003: Syntax error, 'operator' expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(1, 9),
                // (1,9): error CS1019: Overloadable unary operator expected
                // int N.I.explicit (int x) => x;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "int N.I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, '.' expected
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 19),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 28));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 15),
                // (1,15): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 15),
                // (1,19): error CS1003: Syntax error, '.' expected
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 19),
                // (1,28): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int N.I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 28));

            var errors = new[] {
                // (1,9): error CS1003: Syntax error, '.' expected
                // int N.I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 9)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
            var text = "int I operator +(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 15),
                // (1,15): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 15),
                // (1,15): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 15),
                // (1,17): error CS1003: Syntax error, '.' expected
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 17),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 26));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,15): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 15),
                // (1,15): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 15),
                // (1,17): error CS1003: Syntax error, '.' expected
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 17),
                // (1,26): error CS8930: Explicit implementation of a user-defined operator 'C.operator +(int)' must be declared static
                // class C { int I operator +(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "+").WithArguments("C.operator +(int)").WithLocation(1, 26));

            var errors = new[] {
                // (1,7): error CS1003: Syntax error, '.' expected
                // int I operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 7)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

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
                    // (1,7): error CS1001: Identifier expected
                    // int I..operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".operato").WithLocation(1, 7)
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

        [Fact]
        public void OperatorDeclaration_ExplicitImplementation_45()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int N.I..operator +(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,9): error CS1001: Identifier expected
                    // int N.I..operator +(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".operato").WithLocation(1, 9)
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
        public void OperatorDeclaration_ExplicitImplementation_46()
        {
            var errors = new[] {
                // (1,5): error CS1001: Identifier expected
                // N.I.operator +(int x) => x;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "operator").WithLocation(1, 5)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("N.I.operator +(int x) => x;", options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.QualifiedName);
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
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
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
        public void OperatorDeclaration_ExplicitImplementation_47()
        {
            var errors = new[] {
                // (1,1): error CS1073: Unexpected token 'int'
                // N.I. int(int x) => x;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "N.I. ").WithArguments("int").WithLocation(1, 1),
                // (1,6): error CS1001: Identifier expected
                // N.I. int(int x) => x;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(1, 6)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("N.I. int(int x) => x;", options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.QualifiedName);
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
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_01()
        {
            var text = "implicit N.I.operator int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { implicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 33));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 33));

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version));

                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ImplicitKeyword);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_02()
        {
            var text = "N.I.operator int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,11): error CS1003: Syntax error, 'explicit' expected
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "N").WithArguments("explicit").WithLocation(1, 11),
                // (1,11): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 11),
                // (1,11): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 11),
                // (1,11): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 11),
                // (1,24): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 24));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,11): error CS1003: Syntax error, 'explicit' expected
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "N").WithArguments("explicit").WithLocation(1, 11),
                // (1,11): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 11),
                // (1,11): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 11),
                // (1,24): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 24));

            var errors = new[]
            {
                // (1,1): error CS1003: Syntax error, 'explicit' expected
                // N.I.operator int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "N").WithArguments("explicit").WithLocation(1, 1)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        M(SyntaxKind.ExplicitKeyword);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_03()
        {
            var errors = new[] {
                // (1,1): error CS1003: Syntax error, 'explicit' expected
                // operator int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("explicit").WithLocation(1, 1)
                };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration("operator int(int x) => x;", options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        M(SyntaxKind.ExplicitKeyword);
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_04()
        {
            var text = "implicit N.I operator int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 33));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 33));

            var errors = new[]
            {
                // (1,14): error CS1003: Syntax error, '.' expected
                // implicit N.I operator int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 14)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ImplicitKeyword);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_05()
        {
            var text = "explicit I operator int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 20),
                // (1,22): error CS1003: Syntax error, '.' expected
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 22),
                // (1,31): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 31));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 20),
                // (1,20): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 20),
                // (1,22): error CS1003: Syntax error, '.' expected
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 22),
                // (1,31): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 31));

            var errors = new[]
            {
                // (1,12): error CS1003: Syntax error, '.' expected
                // explicit I operator int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 12)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingDeclaration(text, options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ExplicitKeyword);
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_06()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("implicit N::I::operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,14): error CS7000: Unexpected use of an aliased name
                    // implicit N::I::operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 14)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ImplicitKeyword);
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
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_07()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I::operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,11): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                    // explicit I::operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 11)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        M(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_08()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("implicit I.operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ImplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_09()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I<T>.operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
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
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_10()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("implicit N1::N2::I.operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,16): error CS7000: Unexpected use of an aliased name
                    // implicit N1::N2::I.operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 16)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ImplicitKeyword);
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
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_11()
        {
            var text = "explicit N.I.operator int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { explicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { explicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 33));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { explicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit N.I.operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 33));

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version));

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.ConversionOperatorDeclaration);
                        {
                            N(SyntaxKind.ExplicitKeyword);
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
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
                        N(SyntaxKind.EndOfFileToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_12()
        {
            var text = "implicit N.I int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(".").WithLocation(1, 24),
                // (1,24): error CS1003: Syntax error, 'operator' expected
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("operator").WithLocation(1, 24),
                // (1,24): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 24));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(".").WithLocation(1, 24),
                // (1,24): error CS1003: Syntax error, 'operator' expected
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("operator").WithLocation(1, 24),
                // (1,24): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 24));

            var errors = new[]
            {
                // (1,14): error CS1003: Syntax error, '.' expected
                // implicit N.I int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(".").WithLocation(1, 14),
                // (1,14): error CS1003: Syntax error, 'operator' expected
                // implicit N.I int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("operator").WithLocation(1, 14)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.ConversionOperatorDeclaration);
                        {
                            N(SyntaxKind.ImplicitKeyword);
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
                            M(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
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
                        N(SyntaxKind.EndOfFileToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_13()
        {
            var text = "explicit N.I. int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I.").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,25): error CS1003: Syntax error, 'operator' expected
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("operator").WithLocation(1, 25),
                // (1,25): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 25));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,25): error CS1003: Syntax error, 'operator' expected
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("operator").WithLocation(1, 25),
                // (1,25): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit N.I. int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 25));

            var errors = new[]
            {
                // (1,15): error CS1003: Syntax error, 'operator' expected
                // explicit N.I. int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments("operator").WithLocation(1, 15)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.ConversionOperatorDeclaration);
                        {
                            N(SyntaxKind.ExplicitKeyword);
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
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
                        N(SyntaxKind.EndOfFileToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_14()
        {
            var text = "implicit N.I operator int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "N.I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 33));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'N' could not be found (are you missing a using directive or an assembly reference?)
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "N").WithArguments("N").WithLocation(1, 20),
                // (1,20): error CS0538: 'N.I' in explicit interface declaration is not an interface
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "N.I").WithArguments("N.I").WithLocation(1, 20),
                // (1,24): error CS1003: Syntax error, '.' expected
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 24),
                // (1,33): error CS8930: Explicit implementation of a user-defined operator 'C.implicit operator int(int)' must be declared static
                // class C { implicit N.I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.implicit operator int(int)").WithLocation(1, 33));

            var errors = new[]
            {
                // (1,14): error CS1003: Syntax error, '.' expected
                // implicit N.I operator int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 14)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.ConversionOperatorDeclaration);
                        {
                            N(SyntaxKind.ImplicitKeyword);
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
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
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
                        N(SyntaxKind.EndOfFileToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_15()
        {
            var text = "explicit I operator int(int x) => x;";
            var classWithText = $"class C {{ {text} }}";
            CreateCompilation(classWithText, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 20),
                // (1,20): error CS8773: Feature 'static abstract members in interfaces' is not available in C# 9.0. Please use language version 11.0 or greater.
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I ").WithArguments("static abstract members in interfaces", "11.0").WithLocation(1, 20),
                // (1,20): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 20),
                // (1,22): error CS1003: Syntax error, '.' expected
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 22),
                // (1,31): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 31));
            CreateCompilation(classWithText, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
                // (1,20): error CS0246: The type or namespace name 'I' could not be found (are you missing a using directive or an assembly reference?)
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "I").WithArguments("I").WithLocation(1, 20),
                // (1,20): error CS0538: 'I' in explicit interface declaration is not an interface
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "I").WithArguments("I").WithLocation(1, 20),
                // (1,22): error CS1003: Syntax error, '.' expected
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 22),
                // (1,31): error CS8930: Explicit implementation of a user-defined operator 'C.explicit operator int(int)' must be declared static
                // class C { explicit I operator int(int x) => x; }
                Diagnostic(ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic, "int").WithArguments("C.explicit operator int(int)").WithLocation(1, 31));

            var errors = new[]
            {
                // (1,12): error CS1003: Syntax error, '.' expected
                // explicit I operator int(int x) => x;
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments(".").WithLocation(1, 12)
            };

            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                foreach (var version in new[] { LanguageVersion.CSharp9, LanguageVersion.Preview })
                {
                    UsingTree(text, options: options.WithLanguageVersion(version), errors);

                    N(SyntaxKind.CompilationUnit);
                    {
                        N(SyntaxKind.ConversionOperatorDeclaration);
                        {
                            N(SyntaxKind.ExplicitKeyword);
                            N(SyntaxKind.ExplicitInterfaceSpecifier);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "I");
                                }
                                M(SyntaxKind.DotToken);
                            }
                            N(SyntaxKind.OperatorKeyword);
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
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
                        N(SyntaxKind.EndOfFileToken);
                    }
                    EOF();
                }
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_16()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("implicit N::I::operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,14): error CS7000: Unexpected use of an aliased name
                    // implicit N::I::operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 14)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ImplicitKeyword);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_17()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("explicit I::operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,11): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                    // explicit I::operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 11)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ExplicitKeyword);
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            M(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_18()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("implicit I.operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ImplicitKeyword);
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_19()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("explicit I<T>.operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview));

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ExplicitKeyword);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_20()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingTree("implicit N1::N2::I.operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,16): error CS7000: Unexpected use of an aliased name
                    // implicit N1::N2::I.operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 16)
                    );

                N(SyntaxKind.CompilationUnit);
                {
                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.ImplicitKeyword);
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
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
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
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_21()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I..operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,12): error CS1001: Identifier expected
                    // explicit I..operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".operato").WithLocation(1, 12)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
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
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_22()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("implicit I . . operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,14): error CS1001: Identifier expected
                    // implicit I . . operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 14)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ImplicitKeyword);
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
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_23()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I T(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,12): error CS1003: Syntax error, '.' expected
                    // explicit I T(int x) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "T").WithArguments(".").WithLocation(1, 12),
                    // (1,12): error CS1003: Syntax error, 'operator' expected
                    // explicit I T(int x) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "T").WithArguments("operator").WithLocation(1, 12)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        M(SyntaxKind.DotToken);
                    }
                    M(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
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
        public void ConversionDeclaration_ExplicitImplementation_24()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.T(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,12): error CS1003: Syntax error, 'operator' expected
                    // explicit I.T(int x) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "T").WithArguments("operator").WithLocation(1, 12)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    M(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T");
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
        public void ConversionDeclaration_ExplicitImplementation_25()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator (int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,21): error CS1001: Identifier expected
                    // explicit I.operator (int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 21)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
        public void ConversionDeclaration_ExplicitImplementation_26()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator (int x) { return x; }", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,21): error CS1001: Identifier expected
                    // explicit I.operator (int x) { return x; }
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 21)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.ReturnStatement);
                        {
                            N(SyntaxKind.ReturnKeyword);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_27()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator (int x);", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,21): error CS1001: Identifier expected
                    // explicit I.operator (int x);
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 21)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_28()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.T1 T2(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,15): error CS1003: Syntax error, '.' expected
                    // explicit I.T1 T2(int x) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "T2").WithArguments(".").WithLocation(1, 15),
                    // (1,15): error CS1003: Syntax error, 'operator' expected
                    // explicit I.T1 T2(int x) => x;
                    Diagnostic(ErrorCode.ERR_SyntaxError, "T2").WithArguments("operator").WithLocation(1, 15)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "T1");
                            }
                        }
                        M(SyntaxKind.DotToken);
                    }
                    M(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "T2");
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
        public void ConversionDeclaration_ExplicitImplementation_29()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator (int x)", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,21): error CS1001: Identifier expected
                    // explicit I.operator (int x)
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 21),
                    // (1,28): error CS1002: ; expected
                    // explicit I.operator (int x)
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 28)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                    M(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_30()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator (int x, );", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,29): error CS1031: Type expected
                    // explicit I.operator (int x, );
                    Diagnostic(ErrorCode.ERR_TypeExpected, ")").WithLocation(1, 29),
                    // (1,30): error CS1003: Syntax error, '(' expected
                    // explicit I.operator (int x, );
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("(").WithLocation(1, 30),
                    // (1,30): error CS1026: ) expected
                    // explicit I.operator (int x, );
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 30)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CommaToken);
                        M(SyntaxKind.TupleElement);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_31()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator (int x, int y);", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,35): error CS1003: Syntax error, '(' expected
                    // explicit I.operator (int x, int y);
                    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("(").WithLocation(1, 35),
                    // (1,35): error CS1026: ) expected
                    // explicit I.operator (int x, int y);
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(1, 35)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.TupleType);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "x");
                        }
                        N(SyntaxKind.CommaToken);
                        N(SyntaxKind.TupleElement);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken, "y");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_32()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator var(x);", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,26): error CS1001: Identifier expected
                    // explicit I.operator var(x);
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(1, 26)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "x");
                            }
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_33()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit I.operator (int x int y);", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,21): error CS1001: Identifier expected
                    // explicit I.operator (int x int y);
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(1, 21),
                    // (1,28): error CS1003: Syntax error, ',' expected
                    // explicit I.operator (int x int y);
                    Diagnostic(ErrorCode.ERR_SyntaxError, "int").WithArguments(",").WithLocation(1, 28)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "I");
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                        M(SyntaxKind.CommaToken);
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
                EOF();
            }
        }

        [Fact]
        public void ConversionDeclaration_ExplicitImplementation_34()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("explicit N.I..operator int(int x) => x;", options: options.WithLanguageVersion(LanguageVersion.Preview),
                    // (1,14): error CS1001: Identifier expected
                    // explicit N.I..operator int(int x) => x;
                    Diagnostic(ErrorCode.ERR_IdentifierExpected, ".operato").WithLocation(1, 14)
                    );

                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    N(SyntaxKind.ExplicitInterfaceSpecifier);
                    {
                        N(SyntaxKind.QualifiedName);
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
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.OperatorKeyword);
                    N(SyntaxKind.PredefinedType);
                    {
                        N(SyntaxKind.IntKeyword);
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
        public void ConversionDeclaration_ExplicitImplementation_35()
        {
            var error = new[] {
                // (2,9): error CS1003: Syntax error, 'operator' expected
                // explicit
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("operator").WithLocation(2, 9),
                // (2,9): error CS1001: Identifier expected
                // explicit
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(2, 9)
                };

            UsingTree(
@"
explicit
Func<int, int> f1 = (param1) => 10;
", options: TestOptions.Regular, error);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ConversionOperatorDeclaration);
                {
                    N(SyntaxKind.ExplicitKeyword);
                    M(SyntaxKind.OperatorKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.ParameterList);
                    {
                        M(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.CloseParenToken);
                    }
                    M(SyntaxKind.SemicolonToken);
                }
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken, "Func");
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "f1");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.ParenthesizedLambdaExpression);
                                    {
                                        N(SyntaxKind.ParameterList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Parameter);
                                            {
                                                N(SyntaxKind.IdentifierToken, "param1");
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.EqualsGreaterThanToken);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken, "10");
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
        public void DotDotRecovery_01()
        {
            UsingDeclaration("N1..N2 M(int x) => x;", options: TestOptions.Regular,
                // (1,4): error CS1001: Identifier expected
                // N1..N2 M(int x) => x;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 4)
                );

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.QualifiedName);
                {
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "N1");
                        }
                        N(SyntaxKind.DotToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.DotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "N2");
                    }
                }
                N(SyntaxKind.IdentifierToken, "M");
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

        [Fact]
        public void DotDotRecovery_02()
        {
            UsingDeclaration("int N1..M(int x) => x;", options: TestOptions.Regular,
                // (1,8): error CS1001: Identifier expected
                // int N1..M(int x) => x;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 8)
                );

            N(SyntaxKind.MethodDeclaration);
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
                            N(SyntaxKind.IdentifierToken, "N1");
                        }
                        N(SyntaxKind.DotToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.DotToken);
                }
                N(SyntaxKind.IdentifierToken, "M");
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

        [Fact]
        public void DotDotRecovery_03()
        {
            UsingDeclaration("int N1.N2..M(int x) => x;", options: TestOptions.Regular,
                // (1,11): error CS1001: Identifier expected
                // int N1.N2..M(int x) => x;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ".").WithLocation(1, 11)
                );

            N(SyntaxKind.MethodDeclaration);
            {
                N(SyntaxKind.PredefinedType);
                {
                    N(SyntaxKind.IntKeyword);
                }
                N(SyntaxKind.ExplicitInterfaceSpecifier);
                {
                    N(SyntaxKind.QualifiedName);
                    {
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "N1");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "N2");
                            }
                        }
                        N(SyntaxKind.DotToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.DotToken);
                }
                N(SyntaxKind.IdentifierToken, "M");
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

        [Fact]
        [WorkItem(53021, "https://github.com/dotnet/roslyn/issues/53021")]
        public void MisplacedColonColon_01()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int N::I::M1() => 0;", options: options,
                    // (1,9): error CS7000: Unexpected use of an aliased name
                    // int N::I::M1() => 0;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 9)
                    );

                N(SyntaxKind.MethodDeclaration);
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
                    N(SyntaxKind.IdentifierToken, "M1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(53021, "https://github.com/dotnet/roslyn/issues/53021")]
        public void MisplacedColonColon_02()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int N1::N2::I.M1() => 0;", options: options,
                    // (1,11): error CS7000: Unexpected use of an aliased name
                    // int N1::N2::I.M1() => 0;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 11)
                    );

                N(SyntaxKind.MethodDeclaration);
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
                    N(SyntaxKind.IdentifierToken, "M1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(53021, "https://github.com/dotnet/roslyn/issues/53021")]
        public void MisplacedColonColon_03()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int N1::N2.I::M1() => 0;", options: options,
                    // (1,13): error CS7000: Unexpected use of an aliased name
                    // int N1::N2.I::M1() => 0;
                    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(1, 13)
                    );

                N(SyntaxKind.MethodDeclaration);
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
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                        M(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "M1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(53021, "https://github.com/dotnet/roslyn/issues/53021")]
        public void MisplacedColonColon_04()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int I::M1() => 0;", options: options,
                    // (1,6): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                    // int I::M1() => 0;
                    Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::").WithLocation(1, 6)
                    );

                N(SyntaxKind.MethodDeclaration);
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
                    N(SyntaxKind.IdentifierToken, "M1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact]
        [WorkItem(53021, "https://github.com/dotnet/roslyn/issues/53021")]
        public void MisplacedColonColon_05()
        {
            foreach (var options in new[] { TestOptions.Script, TestOptions.Regular })
            {
                UsingDeclaration("int N1::I.M1() => 0;", options: options);

                N(SyntaxKind.MethodDeclaration);
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
                                N(SyntaxKind.IdentifierToken, "N1");
                            }
                            N(SyntaxKind.ColonColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "I");
                            }
                        }
                        N(SyntaxKind.DotToken);
                    }
                    N(SyntaxKind.IdentifierToken, "M1");
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.ArrowExpressionClause);
                    {
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.SemicolonToken);
                }
                EOF();
            }
        }

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void EnumConstraint_OnMethod()
        {
            UsingNode(@"
class C
{
    void M<T>() where T : /*comment*/ enum /*comment*/ { }
}
", options: TestOptions.Regular,
                // (4,39): error CS9010: Keyword 'enum' cannot be used as a constraint. Did you mean 'struct, System.Enum'?
                //     void M<T>() where T : /*comment*/ enum /*comment*/ { }
                Diagnostic(ErrorCode.ERR_NoEnumConstraint, "enum").WithLocation(4, 39)
                );

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
                            M(SyntaxKind.TypeConstraint);
                            {
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                            }
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void EnumConstraint_OnType()
        {
            UsingNode(@"
interface I<T> where T : /*comment*/ enum /*comment*/ { }
", options: TestOptions.Regular,
                // (2,38): error CS9010: Keyword 'enum' cannot be used as a constraint. Did you mean 'struct, System.Enum'?
                // interface I<T> where T : /*comment*/ enum /*comment*/ { }
                Diagnostic(ErrorCode.ERR_NoEnumConstraint, "enum").WithLocation(2, 38)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "I");
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

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void EnumConstraint_OnDelegate()
        {
            UsingNode(@"
class C
{
    delegate void D<T>() where T : /*comment*/ enum /*comment*/;
}
", options: TestOptions.Regular,
                // (4,48): error CS9010: Keyword 'enum' cannot be used as a constraint. Did you mean 'struct, System.Enum'?
                //     delegate void D<T>() where T : /*comment*/ enum /*comment*/;
                Diagnostic(ErrorCode.ERR_NoEnumConstraint, "enum").WithLocation(4, 48)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.DelegateDeclaration);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "D");
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void EnumConstraint_OnLocalFunction()
        {
            UsingNode(@"
class C
{
    void M()
    {
        void local<T>() where T : /*comment*/ enum /*comment*/ { }
    }
}
", options: TestOptions.Regular,
                // (6,47): error CS9010: Keyword 'enum' cannot be used as a constraint. Did you mean 'struct, System.Enum'?
                //         void local<T>() where T : /*comment*/ enum /*comment*/ { }
                Diagnostic(ErrorCode.ERR_NoEnumConstraint, "enum").WithLocation(6, 47)
                );

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
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.VoidKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "local");
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
                                    M(SyntaxKind.TypeConstraint);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
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

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void DelegateConstraint_OnType_First()
        {
            UsingNode(@"
class C<T> where T : /*comment*/ delegate /*comment*/ { }
", options: TestOptions.Regular,
                // (2,34): error CS9011: Keyword 'delegate' cannot be used as a constraint. Did you mean 'System.Delegate'?
                // class C<T> where T : /*comment*/ delegate /*comment*/ { }
                Diagnostic(ErrorCode.ERR_NoDelegateConstraint, "delegate").WithLocation(2, 34)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
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

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void DelegateConstraint_OnType_AfterClass()
        {
            UsingNode(@"
class C<T> where T : class, /*comment*/ delegate /*comment*/ { }
", options: TestOptions.Regular,
                // (2,41): error CS9011: Keyword 'delegate' cannot be used as a constraint. Did you mean 'System.Delegate'?
                // class C<T> where T : class, /*comment*/ delegate /*comment*/ { }
                Diagnostic(ErrorCode.ERR_NoDelegateConstraint, "delegate").WithLocation(2, 41)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
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
                        N(SyntaxKind.CommaToken);
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

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void DelegateConstraint_OnType_AfterType()
        {
            UsingNode(@"
class C<T> where T : Type, /*comment*/ delegate /*comment*/ { }
", options: TestOptions.Regular,
                // (2,40): error CS9011: Keyword 'delegate' cannot be used as a constraint. Did you mean 'System.Delegate'?
                // class C<T> where T : Type, /*comment*/ delegate /*comment*/ { }
                Diagnostic(ErrorCode.ERR_NoDelegateConstraint, "delegate").WithLocation(2, 40)
                );

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
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
                        N(SyntaxKind.TypeConstraint);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
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
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(59495, "https://github.com/dotnet/roslyn/issues/59495")]
        public void DelegateConstraint_OnType_AtEOF()
        {
            UsingNode(@"record R<T> where T : delegate", options: TestOptions.Regular,
                // (1,23): error CS9011: Keyword 'delegate' cannot be used as a constraint. Did you mean 'System.Delegate'?
                // record R<T> where T : delegate
                Diagnostic(ErrorCode.ERR_NoDelegateConstraint, "delegate").WithLocation(1, 23),
                // (1,31): error CS1514: { expected
                // record R<T> where T : delegate
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(1, 31),
                // (1,31): error CS1513: } expected
                // record R<T> where T : delegate
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 31)
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
                        M(SyntaxKind.TypeConstraint);
                        {
                            M(SyntaxKind.IdentifierName);
                            {
                                M(SyntaxKind.IdentifierToken);
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

        [Theory]
        [InlineData("+", SyntaxKind.PlusToken)]
        [InlineData("-", SyntaxKind.MinusToken)]
        [InlineData("!", SyntaxKind.ExclamationToken)]
        [InlineData("~", SyntaxKind.TildeToken)]
        [InlineData("++", SyntaxKind.PlusPlusToken)]
        [InlineData("--", SyntaxKind.MinusMinusToken)]
        [InlineData("true", SyntaxKind.TrueKeyword)]
        [InlineData("false", SyntaxKind.FalseKeyword)]
        public void CheckedOperatorDeclaration_01(string op, SyntaxKind opToken)
        {
            UsingDeclaration("C operator checked " + op + "(C x) => x;");

            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
                N(opToken);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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

        [Theory]
        [InlineData("+", SyntaxKind.PlusToken)]
        [InlineData("-", SyntaxKind.MinusToken)]
        [InlineData("!", SyntaxKind.ExclamationToken)]
        [InlineData("~", SyntaxKind.TildeToken)]
        [InlineData("++", SyntaxKind.PlusPlusToken)]
        [InlineData("--", SyntaxKind.MinusMinusToken)]
        [InlineData("true", SyntaxKind.TrueKeyword)]
        [InlineData("false", SyntaxKind.FalseKeyword)]
        public void CheckedOperatorDeclaration_02(string op, SyntaxKind opToken)
        {
            UsingDeclaration("C I.operator checked " + op + "(C x) => x;", options: TestOptions.RegularPreview);

            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
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
                N(SyntaxKind.CheckedKeyword);
                N(opToken);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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

        [Theory]
        [InlineData("+", SyntaxKind.PlusToken)]
        [InlineData("-", SyntaxKind.MinusToken)]
        [InlineData("*", SyntaxKind.AsteriskToken)]
        [InlineData("/", SyntaxKind.SlashToken)]
        [InlineData("%", SyntaxKind.PercentToken)]
        [InlineData("&", SyntaxKind.AmpersandToken)]
        [InlineData("|", SyntaxKind.BarToken)]
        [InlineData("^", SyntaxKind.CaretToken)]
        [InlineData("<<", SyntaxKind.LessThanLessThanToken)]
        [InlineData(">>", SyntaxKind.GreaterThanGreaterThanToken)]
        [InlineData(">>>", SyntaxKind.GreaterThanGreaterThanGreaterThanToken)]
        [InlineData("==", SyntaxKind.EqualsEqualsToken)]
        [InlineData("!=", SyntaxKind.ExclamationEqualsToken)]
        [InlineData(">", SyntaxKind.GreaterThanToken)]
        [InlineData("<", SyntaxKind.LessThanToken)]
        [InlineData(">=", SyntaxKind.GreaterThanEqualsToken)]
        [InlineData("<=", SyntaxKind.LessThanEqualsToken)]
        public void CheckedOperatorDeclaration_03(string op, SyntaxKind opToken)
        {
            UsingDeclaration("C operator checked " + op + "(C x, C y) => x;");

            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
                N(opToken);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "y");
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

        [Theory]
        [InlineData("+", SyntaxKind.PlusToken)]
        [InlineData("-", SyntaxKind.MinusToken)]
        [InlineData("*", SyntaxKind.AsteriskToken)]
        [InlineData("/", SyntaxKind.SlashToken)]
        [InlineData("%", SyntaxKind.PercentToken)]
        [InlineData("&", SyntaxKind.AmpersandToken)]
        [InlineData("|", SyntaxKind.BarToken)]
        [InlineData("^", SyntaxKind.CaretToken)]
        [InlineData("<<", SyntaxKind.LessThanLessThanToken)]
        [InlineData(">>", SyntaxKind.GreaterThanGreaterThanToken)]
        [InlineData(">>>", SyntaxKind.GreaterThanGreaterThanGreaterThanToken)]
        [InlineData("==", SyntaxKind.EqualsEqualsToken)]
        [InlineData("!=", SyntaxKind.ExclamationEqualsToken)]
        [InlineData(">", SyntaxKind.GreaterThanToken)]
        [InlineData("<", SyntaxKind.LessThanToken)]
        [InlineData(">=", SyntaxKind.GreaterThanEqualsToken)]
        [InlineData("<=", SyntaxKind.LessThanEqualsToken)]
        public void CheckedOperatorDeclaration_04(string op, SyntaxKind opToken)
        {
            UsingDeclaration("C I.operator checked " + op + "(C x, C y) => x;", options: TestOptions.RegularPreview);

            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
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
                N(SyntaxKind.CheckedKeyword);
                N(opToken);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "y");
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

        [Theory]
        [InlineData("implicit", SyntaxKind.ImplicitKeyword)]
        [InlineData("explicit", SyntaxKind.ExplicitKeyword)]
        public void CheckedOperatorDeclaration_05(string op, SyntaxKind opToken)
        {
            UsingDeclaration(op + " operator checked D(C x) => x;");

            N(SyntaxKind.ConversionOperatorDeclaration);
            {
                N(opToken);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "D");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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

        [Theory]
        [InlineData("implicit", SyntaxKind.ImplicitKeyword)]
        [InlineData("explicit", SyntaxKind.ExplicitKeyword)]
        public void CheckedOperatorDeclaration_06(string op, SyntaxKind opToken)
        {
            UsingDeclaration(op + " I.operator checked D(C x) => x;", options: TestOptions.RegularPreview);

            N(SyntaxKind.ConversionOperatorDeclaration);
            {
                N(opToken);
                N(SyntaxKind.ExplicitInterfaceSpecifier);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "I");
                    }
                    N(SyntaxKind.DotToken);
                }
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.CheckedKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "D");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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

        [Theory, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        [InlineData("+", SyntaxKind.PlusToken)]
        [InlineData("-", SyntaxKind.MinusToken)]
        [InlineData("!", SyntaxKind.ExclamationToken)]
        [InlineData("~", SyntaxKind.TildeToken)]
        [InlineData("++", SyntaxKind.PlusPlusToken)]
        [InlineData("--", SyntaxKind.MinusMinusToken)]
        [InlineData("true", SyntaxKind.TrueKeyword)]
        [InlineData("false", SyntaxKind.FalseKeyword)]
        public void UncheckedOperatorDeclaration_01(string op, SyntaxKind opToken)
        {
            UsingDeclaration("C operator unchecked " + op + "(C x) => x;", expectedErrors:
                // (1,12): error CS9027: Unexpected keyword 'unchecked'
                // C operator unchecked op(C x) => x;
                Diagnostic(ErrorCode.ERR_MisplacedUnchecked, "unchecked").WithLocation(1, 12));

            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.OperatorKeyword);
                N(opToken);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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

        [Theory, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        [InlineData("+", SyntaxKind.PlusToken)]
        [InlineData("-", SyntaxKind.MinusToken)]
        [InlineData("*", SyntaxKind.AsteriskToken)]
        [InlineData("/", SyntaxKind.SlashToken)]
        [InlineData("%", SyntaxKind.PercentToken)]
        [InlineData("&", SyntaxKind.AmpersandToken)]
        [InlineData("|", SyntaxKind.BarToken)]
        [InlineData("^", SyntaxKind.CaretToken)]
        [InlineData("<<", SyntaxKind.LessThanLessThanToken)]
        [InlineData(">>", SyntaxKind.GreaterThanGreaterThanToken)]
        [InlineData(">>>", SyntaxKind.GreaterThanGreaterThanGreaterThanToken)]
        [InlineData("==", SyntaxKind.EqualsEqualsToken)]
        [InlineData("!=", SyntaxKind.ExclamationEqualsToken)]
        [InlineData(">", SyntaxKind.GreaterThanToken)]
        [InlineData("<", SyntaxKind.LessThanToken)]
        [InlineData(">=", SyntaxKind.GreaterThanEqualsToken)]
        [InlineData("<=", SyntaxKind.LessThanEqualsToken)]
        public void UncheckedOperatorDeclaration_04(string op, SyntaxKind opToken)
        {
            UsingDeclaration("C I.operator unchecked " + op + "(C x, C y) => x;", options: TestOptions.RegularPreview,
                // (1,14): error CS9027: Unexpected keyword 'unchecked'
                // C I.operator unchecked op(C x, C y) => x;
                Diagnostic(ErrorCode.ERR_MisplacedUnchecked, "unchecked").WithLocation(1, 14));

            N(SyntaxKind.OperatorDeclaration);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
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
                N(opToken);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.IdentifierToken, "y");
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

        [Theory, WorkItem(60394, "https://github.com/dotnet/roslyn/issues/60394")]
        [InlineData("implicit", SyntaxKind.ImplicitKeyword)]
        [InlineData("explicit", SyntaxKind.ExplicitKeyword)]
        public void UnheckedOperatorDeclaration_05(string op, SyntaxKind opToken)
        {
            UsingDeclaration(op + " operator unchecked D(C x) => x;", expectedErrors:
                // (1,19): error CS9027: Unexpected keyword 'unchecked'
                // implicit operator unchecked op(C x) => x;
                Diagnostic(ErrorCode.ERR_MisplacedUnchecked, "unchecked").WithLocation(1, 19));

            N(SyntaxKind.ConversionOperatorDeclaration);
            {
                N(opToken);
                N(SyntaxKind.OperatorKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "D");
                }
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
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

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter1()
        {
            UsingTree(@"
public class Base {
    public virtual void M(ref int X) {
    }
}
public class Derived : Base {
    public override void M(ref readonly int X) {
    }
}");

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Base");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.VirtualKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Derived");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Base");
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.OverrideKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.ReadOnlyKeyword);
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "X");
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter2()
        {
            UsingExpression(@"
(readonly int i) => { }");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "i");
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

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter3()
        {
            UsingExpression(@"
(ref readonly int i) => { }");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "i");
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

        [Fact, WorkItem(63758, "https://github.com/dotnet/roslyn/issues/63758")]
        public void ReadonlyParameter4()
        {
            UsingExpression(@"
(readonly ref int i) => { }");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.ReadOnlyKeyword);
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
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem(52, "https://github.com/dotnet/roslyn/issues/52")]
        public void PropertyWithErrantSemicolon1()
        {
            var text = @"
public class Class
{
    public int MyProperty; { get; set; }

    // Pretty much anything here causes an error
}
";
            UsingTree(text,
                // (4,26): error CS1514: { expected
                //     public int MyProperty; { get; set; }
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(4, 26));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "MyProperty");
                        N(SyntaxKind.AccessorList);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.GetAccessorDeclaration);
                            {
                                N(SyntaxKind.GetKeyword);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(52, "https://github.com/dotnet/roslyn/issues/52")]
        public void PropertyWithErrantSemicolon2()
        {
            var text = @"
public class Class
{
    public int MyProperty; => 0;

    // Pretty much anything here causes an error
}
";
            UsingTree(text,
                // (4,26): error CS1514: { expected
                //     public int MyProperty; => 0;
                Diagnostic(ErrorCode.ERR_LbraceExpected, ";").WithLocation(4, 26));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.PublicKeyword);
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Class");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "MyProperty");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken, "0");
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
