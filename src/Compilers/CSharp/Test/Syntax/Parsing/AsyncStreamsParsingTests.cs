// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.AsyncStreams)]
    public class AsyncStreamsParsingTests : ParsingTests
    {
        public AsyncStreamsParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: (options ?? TestOptions.Regular).WithLanguageVersion(LanguageVersion.CSharp8));
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options = null)
        {
            return SyntaxFactory.ParseExpression(text, options: (options ?? TestOptions.Regular).WithLanguageVersion(LanguageVersion.CSharp8));
        }

        [Theory, WorkItem(32318, "https://github.com/dotnet/roslyn/issues/32318")]
        [InlineData(true)]
        [InlineData(false)]
        public void AwaitUsingDeclaration(bool useCSharp8)
        {
            UsingTree(@"
class C
{
    async void M()
    {
        await using (var x = this)
        {
        }
    }
}
", options: useCSharp8 ? TestOptions.Regular8 : TestOptions.Regular7_3);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                            N(SyntaxKind.UsingStatement);
                            {
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.ThisExpression);
                                            {
                                                N(SyntaxKind.ThisKeyword);
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
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

        [Fact]
        public void AwaitUsingWithExpression()
        {
            UsingTree(@"
class C
{
    async void M()
    {
        await using (this)
        {
        }
    }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                            N(SyntaxKind.UsingStatement);
                            {
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.ThisExpression);
                                {
                                    N(SyntaxKind.ThisKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(30565, "https://github.com/dotnet/roslyn/issues/30565")]
        public void AwaitUsingWithExpression_Reversed()
        {
            UsingTree(@"
class C
{
    async void M()
    {
        using await (this)
        {
        }
    }
}
",
                // (6,15): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         using await (this)
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(6, 15),
                // (6,21): error CS1001: Identifier expected
                //         using await (this)
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(6, 21),
                // (6,21): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                //         using await (this)
                Diagnostic(ErrorCode.ERR_BadVarDecl, "(this").WithLocation(6, 21),
                // (6,21): error CS1003: Syntax error, '[' expected
                //         using await (this)
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(6, 21),
                // (6,26): error CS1003: Syntax error, ']' expected
                //         using await (this)
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(6, 26),
                // (6,27): error CS1002: ; expected
                //         using await (this)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 27));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.UsingKeyword);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            M(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.ThisExpression);
                                                {
                                                    N(SyntaxKind.ThisKeyword);
                                                }
                                            }
                                            M(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
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
        [InlineData(true)]
        [InlineData(false)]
        public void AwaitForeach(bool useCSharp8)
        {
            UsingTree(@"
class C
{
    async void M()
    {
        await foreach (var i in collection)
        {
        }
    }
}
", options: useCSharp8 ? TestOptions.Regular8 : TestOptions.Regular7_3);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                            N(SyntaxKind.ForEachStatement);
                            {
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                                N(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "collection");
                                }
                                N(SyntaxKind.CloseParenToken);
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

        [Fact, WorkItem(30565, "https://github.com/dotnet/roslyn/issues/30565")]
        public void AwaitForeach_Reversed()
        {
            UsingTree(@"
class C
{
    async void M()
    {
        foreach await (var i in collection)
        {
        }
    }
}
",
                // (6,17): error CS1003: Syntax error, '(' expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_SyntaxError, "await").WithArguments("(").WithLocation(6, 17),
                // (6,28): error CS1026: ) expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "i").WithLocation(6, 28),
                // (6,28): error CS1515: 'in' expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_InExpected, "i").WithLocation(6, 28),
                // (6,28): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "i").WithLocation(6, 28),
                // (6,30): error CS1026: ) expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "in").WithLocation(6, 30),
                // (6,30): error CS1525: Invalid expression term 'in'
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "in").WithArguments("in").WithLocation(6, 30),
                // (6,30): error CS1002: ; expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(6, 30),
                // (6,30): error CS1513: } expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_RbraceExpected, "in").WithLocation(6, 30),
                // (6,43): error CS1002: ; expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 43),
                // (6,43): error CS1513: } expected
                //         foreach await (var i in collection)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 43));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                            N(SyntaxKind.ForEachVariableStatement);
                            {
                                N(SyntaxKind.ForEachKeyword);
                                M(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.ParenthesizedExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        M(SyntaxKind.CloseParenToken);
                                    }
                                }
                                M(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "i");
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "collection");
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.CloseBraceToken);
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
        [InlineData(true)]
        [InlineData(false)]
        public void DeconstructionAwaitForeach(bool useCSharp8)
        {
            UsingTree(@"
class C
{
    async void M()
    {
        await foreach (var (i, j) in collection)
        {
        }
    }
}
", options: useCSharp8 ? TestOptions.Regular8 : TestOptions.Regular7_3);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
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
                            N(SyntaxKind.ForEachVariableStatement);
                            {
                                N(SyntaxKind.AwaitKeyword);
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.DeclarationExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.ParenthesizedVariableDesignation);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "i");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "j");
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
    }
}
