// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AsyncParsingTests : ParsingTests
    {
        public AsyncParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: (options ?? TestOptions.Regular).WithLanguageVersion(LanguageVersion.CSharp5));
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options = null)
        {
            return SyntaxFactory.ParseExpression(text, options: (options ?? TestOptions.Regular).WithLanguageVersion(LanguageVersion.CSharp5));
        }

        private void TestVersions(Action<CSharpParseOptions> test)
        {
            foreach (LanguageVersion version in Enum.GetValues(typeof(LanguageVersion)))
            {
                test(new CSharpParseOptions(languageVersion: version));
            }
        }

        [Fact]
        public void SimpleAsyncMethod()
        {
            UsingTree(@"
class C
{
    async void M() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodCalledAsync()
        {
            UsingTree(@"
class C
{
    void async() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodReturningAsync()
        {
            UsingTree(@"
class C
{
    async M() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodAsyncAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async async() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void MethodAsyncAsyncAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async async async() { }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(13090, "https://github.com/dotnet/roslyn/issues/13090")]
        [Fact]
        public void MethodAsyncVarAsync()
        {
            UsingTree(
@"class C
{
    static async void M(object async)
    {
        async.F();
    }
}");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        N(SyntaxKind.VoidKeyword);
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.PredefinedType);
                                N(SyntaxKind.ObjectKeyword);
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        N(SyntaxKind.IdentifierToken);
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        N(SyntaxKind.IdentifierToken);
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void IncompleteAsync()
        {
            UsingTree(@"
class C
{
    async
",
                // (4,10): error CS1513: } expected
                //     async
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 10),
                // (5,1): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // 
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(5, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void IncompleteAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async
",
                // (4,16): error CS1513: } expected
                //     async async
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 16),
                // (5,1): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // 
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(5, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CompleteAsyncAsync1()
        {
            UsingTree(@"
class C
{
    async async;
",
                // (4,17): error CS1513: } expected
                //     async async;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 17));
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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CompleteAsyncAsync2()
        {
            UsingTree(@"
class C
{
    async async = 1;
",
                // (4,21): error CS1513: } expected
                //     async async = 1;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 21));
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
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void IncompleteAsyncAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async async
",
                // (4,22): error CS1513: } expected
                //     async async async
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 22),
                // (5,1): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // 
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(5, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CompleteAsyncAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async async;
",
                // (4,23): error CS1513: } expected
                //     async async async;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 23));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void IncompleteAsyncAsyncAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async async async
",
                // (4,28): error CS1513: } expected
                //     async async async async
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 28),
                // (5,1): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // 
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(5, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void CompleteAsyncAsyncAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async async async;
",
                // (4,29): error CS1513: } expected
                //     async async async async;
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 29));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteAsyncMember01()
        {
            // when parsing an incomplete member, treat 'async' as a modifier if it makes sense
            UsingTree(@"
class C
{
    async Task<
}",
                // (4,16): error CS1031: Type expected
                //     async Task<
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 16),
                // (4,16): error CS1003: Syntax error, '>' expected
                //     async Task<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">").WithLocation(4, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteAsyncMember02()
        {
            // when parsing an incomplete member, treat 'async' as a modifier if it makes sense
            UsingTree(@"
class C
{
    async Tasks.Task<
}",
                // (4,22): error CS1031: Type expected
                //     async Tasks.Task<
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 22),
                // (4,22): error CS1003: Syntax error, '>' expected
                //     async Tasks.Task<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">").WithLocation(4, 22));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteAsyncMember03()
        {
            // when parsing an incomplete member, treat 'async' as a modifier if it makes sense
            UsingTree(@"
class C
{
    static async Tasks.Task<
}",
                // (4,29): error CS1031: Type expected
                //     static async Tasks.Task<
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 29),
                // (4,29): error CS1003: Syntax error, '>' expected
                //     static async Tasks.Task<
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(">").WithLocation(4, 29));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.QualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                    M(SyntaxKind.GreaterThanToken);
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteAsyncMember04()
        {
            // when parsing an incomplete member, treat 'async' as a modifier if it makes sense
            // negative case
            UsingTree(@"
class C
{
    async operator+
}",
                // (4,19): error CS1534: Overloaded binary operator '+' takes two parameters
                //     async operator+
                Diagnostic(ErrorCode.ERR_BadBinOpArgs, "+").WithArguments("+").WithLocation(4, 19),
                // (4,20): error CS1003: Syntax error, '(' expected
                //     async operator+
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 20),
                // (4,20): error CS1026: ) expected
                //     async operator+
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 20),
                // (4,20): error CS1002: ; expected
                //     async operator+
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 20));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken); // async
                        }
                        N(SyntaxKind.OperatorKeyword);
                        N(SyntaxKind.PlusToken);
                        M(SyntaxKind.ParameterList);
                        {
                            M(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteAsyncMember05()
        {
            // when parsing an incomplete member, treat 'async' as a modifier if it makes sense
            // negative case
            UsingTree(@"
class C
{
    async Task<T>
}",
                // (5,1): error CS1519: Invalid token '}' in class, record, struct, or interface member declaration
                // }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "}").WithArguments("}").WithLocation(5, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken);
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(609912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/609912")]
        [Fact]
        public void IncompleteAsyncMember06()
        {
            // when parsing an incomplete member, treat 'async' as a modifier if it makes sense
            // negative case
            UsingTree(@"
class C
{
    async Task<T> f
}",
                // (4,20): error CS1002: ; expected
                //     async Task<T> f
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 20));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.GenericName);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.TypeArgumentList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            M(SyntaxKind.SemicolonToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void PropertyAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async { get; set; }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
        }

        [Fact]
        public void PropertyAsyncAsyncAsync()
        {
            UsingTree(@"
class C
{
    async async async { get; set; }
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.IdentifierToken);
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
        }

        [Fact]
        public void EventAsyncAsync()
        {
            UsingTree(@"
class C
{
    event async async;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void EventAsyncAsyncAsync1()
        {
            UsingTree(@"
class C
{
    event async async async;
}
",
                // (4,23): error CS1002: ; expected
                //     event async async async;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "async").WithLocation(4, 23),
                // (4,28): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     event async async async;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28),
                // (4,28): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
                //     event async async async;
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void EventAsyncAsyncAsync2()
        {
            UsingTree(@"
class C
{
    async event async async;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventFieldDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.EventKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void AsyncModifierOnDelegateDeclaration()
        {
            UsingTree(@"
class C
{
    public async delegate void Goo();
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.DelegateDeclaration);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken);
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
        }

        [Fact]
        public void NonAsyncLambda()
        {
            TestVersions(options =>
            {
                UsingNode("async => async", options);

                N(SyntaxKind.SimpleLambdaExpression);
                {
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            });
        }

        [Fact]
        public void AsyncAsyncSimpleLambda()
        {
            TestVersions(options =>
            {
                UsingNode("async async => async", options);

                N(SyntaxKind.SimpleLambdaExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            });
        }

        [Fact]
        public void AsyncAsyncAsyncAsyncAsyncSimpleLambda()
        {
            TestVersions(options =>
            {
                UsingNode("async async => async async => async", options);

                N(SyntaxKind.SimpleLambdaExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.SimpleLambdaExpression);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.EqualsGreaterThanToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
            });
        }

        [Fact]
        public void NonAsyncParenthesizedLambda()
        {
            TestVersions(options =>
            {
                UsingNode("(async) => async", options);

                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            });
        }

        [Fact]
        public void AsyncAsyncParenthesizedLambda()
        {
            TestVersions(options =>
            {
                UsingNode("async (async) => async", options);

                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            });
        }

        [Fact]
        public void AsyncSimpleDelegate()
        {
            TestVersions(options =>
            {
                UsingNode("async delegate { }", options);

                N(SyntaxKind.AnonymousMethodExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            });
        }

        [Fact]
        public void AsyncParenthesizedDelegate()
        {
            TestVersions(options =>
            {
                UsingNode("async delegate (int x) { }", options);

                N(SyntaxKind.AnonymousMethodExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.DelegateKeyword);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            });
        }

        // Comment directly from CParser::IsAsyncMethod.
        // ... 'async' [partial] <typedecl> ...
        // ... 'async' [partial] <event> ...
        // ... 'async' [partial] <implicit> <operator> ...
        // ... 'async' [partial] <explicit> <operator> ...
        // ... 'async' [partial] <typename> <operator> ...
        // ... 'async' [partial] <typename> <membername> ...
        // DEVNOTE: Although we parse async user defined conversions, operators, etc. here,
        // anything other than async methods are detected as erroneous later, during the define phase

        [Fact]
        public void AsyncInterface()
        {
            // ... 'async' <typedecl> ...
            UsingTree(@"
class C
{
    async interface
",
                // (4,20): error CS1001: Identifier expected
                //     async interface
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 20),
                // (4,20): error CS1514: { expected
                //     async interface
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 20),
                // (4,20): error CS1513: } expected
                //     async interface
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 20),
                // (4,20): error CS1513: } expected
                //     async interface
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 20));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.InterfaceDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.InterfaceKeyword);
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncPartialClass()
        {
            // ... 'async' 'partial' <typedecl> ...
            UsingTree(@"
class C
{
    async partial class
",
                // (4,24): error CS1001: Identifier expected
                //     async partial class
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 24),
                // (4,24): error CS1514: { expected
                //     async partial class
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 24),
                // (4,24): error CS1513: } expected
                //     async partial class
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 24),
                // (4,24): error CS1513: } expected
                //     async partial class
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 24));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncEvent()
        {
            // ... 'async' <event> ...
            UsingTree(@"
class C
{
    async event
",
                // (4,16): error CS1031: Type expected
                //     async event
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 16),
                // (4,16): error CS1514: { expected
                //     async event
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 16),
                // (4,16): error CS1513: } expected
                //     async event
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 16),
                // (4,16): error CS1513: } expected
                //     async event
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 16));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.EventKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncPartialEvent()
        {
            // ... 'async' 'partial' <event> ...
            UsingTree(@"
class C
{
    async partial event
",
                // (4,19): error CS1519: Invalid token 'event' in class, record, struct, or interface member declaration
                //     async partial event
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "event").WithArguments("event").WithLocation(4, 19),
                // (4,24): error CS1031: Type expected
                //     async partial event
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 24),
                // (4,24): error CS1514: { expected
                //     async partial event
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 24),
                // (4,24): error CS1513: } expected
                //     async partial event
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 24),
                // (4,24): error CS1513: } expected
                //     async partial event
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 24));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                    }
                    N(SyntaxKind.EventDeclaration);
                    {
                        N(SyntaxKind.EventKeyword);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncImplicitOperator()
        {
            // ... 'async' <implicit> <operator> ...
            UsingTree(@"
class C
{
    async implicit operator
",
                // (4,28): error CS1031: Type expected
                //     async implicit operator
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 28),
                // (4,28): error CS1003: Syntax error, '(' expected
                //     async implicit operator
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 28),
                // (4,28): error CS1026: ) expected
                //     async implicit operator
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 28),
                // (4,28): error CS1002: ; expected
                //     async implicit operator
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 28),
                // (4,28): error CS1513: } expected
                //     async implicit operator
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 28));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.ImplicitKeyword);
                        N(SyntaxKind.OperatorKeyword);
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
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncPartialImplicitOperator()
        {
            // ... 'async' 'partial' <implicit> <operator> ...
            UsingTree(@"
class C
{
    async partial implicit operator
",
                // (4,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "partial").WithArguments("+").WithLocation(4, 11),
                // (4,19): error CS1003: Syntax error, 'operator' expected
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(4, 19),
                // (4,19): error CS1037: Overloadable operator expected
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "implicit").WithLocation(4, 19),
                // (4,28): error CS1003: Syntax error, '(' expected
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(4, 28),
                // (4,28): error CS1041: Identifier expected; 'operator' is a keyword
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(4, 28),
                // (4,36): error CS1026: ) expected
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 36),
                // (4,36): error CS1002: ; expected
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 36),
                // (4,36): error CS1513: } expected
                //     async partial implicit operator
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 36));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        M(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
                        M(SyntaxKind.ParameterList);
                        {
                            M(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncExplicitOperator()
        {
            // ... 'async' <explicit> <operator> ...
            UsingTree(@"
class C
{
    async explicit operator
",
                // (4,28): error CS1031: Type expected
                //     async explicit operator
                Diagnostic(ErrorCode.ERR_TypeExpected, "").WithLocation(4, 28),
                // (4,28): error CS1003: Syntax error, '(' expected
                //     async explicit operator
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(4, 28),
                // (4,28): error CS1026: ) expected
                //     async explicit operator
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 28),
                // (4,28): error CS1002: ; expected
                //     async explicit operator
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 28),
                // (4,28): error CS1513: } expected
                //     async explicit operator
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 28));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ConversionOperatorDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.ExplicitKeyword);
                        N(SyntaxKind.OperatorKeyword);
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
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncPartialExplicitOperator()
        {
            // ... 'async' 'partial' <explicit> <operator> ...
            UsingTree(@"
class C
{
    async partial explicit operator
",
                // (4,11): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "partial").WithArguments("+").WithLocation(4, 11),
                // (4,19): error CS1003: Syntax error, 'operator' expected
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(4, 19),
                // (4,19): error CS1037: Overloadable operator expected
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "explicit").WithLocation(4, 19),
                // (4,28): error CS1003: Syntax error, '(' expected
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(4, 28),
                // (4,28): error CS1041: Identifier expected; 'operator' is a keyword
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(4, 28),
                // (4,36): error CS1026: ) expected
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(4, 36),
                // (4,36): error CS1002: ; expected
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 36),
                // (4,36): error CS1513: } expected
                //     async partial explicit operator
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 36));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                        M(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
                        M(SyntaxKind.ParameterList);
                        {
                            M(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncTypeOperator()
        {
            // ... 'async' <typename> <operator> ...
            UsingTree(@"
class C
{
    async C operator
",
                // (5,1): error CS1037: Overloadable operator expected
                // 
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "").WithLocation(5, 1),
                // (5,1): error CS1003: Syntax error, '(' expected
                // 
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(5, 1),
                // (5,1): error CS1026: ) expected
                // 
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 1),
                // (5,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 1),
                // (5,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
                        M(SyntaxKind.ParameterList);
                        {
                            M(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncPartialTypeOperator()
        {
            // ... 'async' 'partial' <typename> <operator> ...
            UsingTree(@"
class C
{
    async partial int operator
",
                // (4,19): error CS1519: Invalid token 'int' in class, record, struct, or interface member declaration
                //     async partial int operator
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "int").WithArguments("int").WithLocation(4, 19),
                // (5,1): error CS1037: Overloadable operator expected
                // 
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "").WithLocation(5, 1),
                // (5,1): error CS1003: Syntax error, '(' expected
                // 
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("(").WithLocation(5, 1),
                // (5,1): error CS1026: ) expected
                // 
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(5, 1),
                // (5,1): error CS1002: ; expected
                // 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(5, 1),
                // (5,1): error CS1513: } expected
                // 
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 1));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "partial");
                        }
                    }
                    N(SyntaxKind.OperatorDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                        N(SyntaxKind.OperatorKeyword);
                        M(SyntaxKind.PlusToken);
                        M(SyntaxKind.ParameterList);
                        {
                            M(SyntaxKind.OpenParenToken);
                            M(SyntaxKind.CloseParenToken);
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncField()
        {
            // ... 'async' <typename> <membername> ...
            UsingTree(@"
class C
{
    async C C
",
                // (4,14): error CS1002: ; expected
                //     async C C
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 14),
                // (4,14): error CS1513: } expected
                //     async C C
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 14));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "C");
                            }
                        }
                        M(SyntaxKind.SemicolonToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncPartialIndexer()
        {
            // ... 'async' 'partial' <typename> <membername> ...
            UsingTree(@"
class C
{
    async partial C this
",
                // (4,25): error CS1003: Syntax error, '[' expected
                //     async partial C this
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("[").WithLocation(4, 25),
                // (4,25): error CS1003: Syntax error, ']' expected
                //     async partial C this
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]").WithLocation(4, 25),
                // (4,25): error CS1514: { expected
                //     async partial C this
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(4, 25),
                // (4,25): error CS1513: } expected
                //     async partial C this
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 25),
                // (4,25): error CS1513: } expected
                //     async partial C this
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(4, 25));
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PartialKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "C");
                        }
                        N(SyntaxKind.ThisKeyword);
                        M(SyntaxKind.BracketedParameterList);
                        {
                            M(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.CloseBracketToken);
                        }
                        M(SyntaxKind.AccessorList);
                        {
                            M(SyntaxKind.OpenBraceToken);
                            M(SyntaxKind.CloseBraceToken);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncTypeEndOfFile()
        {
            UsingTree("class C { async T",
                // (1,18): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // class C { async T
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(1, 18),
                // (1,18): error CS1513: } expected
                // class C { async T
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(1, 18));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncTypeCloseCurly()
        {
            UsingTree("class C { async T }",
                // (1,19): error CS1519: Invalid token '}' in class, record, struct, or interface member declaration
                // class C { async T }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "}").WithArguments("}").WithLocation(1, 19));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncTypePredefinedType()
        {
            UsingTree(
@"class C {
    async T
    int",
                // (3,5): error CS1519: Invalid token 'int' in class, record, struct, or interface member declaration
                //     int
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "int").WithArguments("int").WithLocation(3, 5),
                // (3,8): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                //     int
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(3, 8),
                // (3,8): error CS1513: } expected
                //     int
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 8));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncTypeModifier()
        {
            UsingTree(
@"class C {
    async T
    public",
                // (3,5): error CS1585: Member modifier 'public' must precede the member type and name
                //     public
                Diagnostic(ErrorCode.ERR_BadModifierLocation, "public").WithArguments("public").WithLocation(3, 5),
                // (3,11): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                //     public
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(3, 11),
                // (3,11): error CS1513: } expected
                //     public
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 11));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.PublicKeyword);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncTypeFollowedByTypeDecl()
        {
            UsingTree(
@"class C {
    async T
class",
                // (3,1): error CS1519: Invalid token 'class' in class, record, struct, or interface member declaration
                // class
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "class").WithArguments("class").WithLocation(3, 1),
                // (3,6): error CS1001: Identifier expected
                // class
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 6),
                // (3,6): error CS1514: { expected
                // class
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(3, 6),
                // (3,6): error CS1513: } expected
                // class
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 6),
                // (3,6): error CS1513: } expected
                // class
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 6));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    N(SyntaxKind.ClassDeclaration);
                    {
                        N(SyntaxKind.ClassKeyword);
                        M(SyntaxKind.IdentifierToken);
                        M(SyntaxKind.OpenBraceToken);
                        M(SyntaxKind.CloseBraceToken);
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void AsyncTypeFollowedByNamespaceDecl()
        {
            UsingTree(
@"class C {
    async T
namespace",
                // (2,12): error CS1513: } expected
                //     async T
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(2, 12),
                // (3,1): error CS1519: Invalid token 'namespace' in class, record, struct, or interface member declaration
                // namespace
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "namespace").WithArguments("namespace").WithLocation(3, 1),
                // (3,10): error CS1001: Identifier expected
                // namespace
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(3, 10),
                // (3,10): error CS1514: { expected
                // namespace
                Diagnostic(ErrorCode.ERR_LbraceExpected, "").WithLocation(3, 10),
                // (3,10): error CS1513: } expected
                // namespace
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(3, 10));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.NamespaceDeclaration);
                {
                    N(SyntaxKind.NamespaceKeyword);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                    M(SyntaxKind.OpenBraceToken);
                    M(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(18621, "https://github.com/dotnet/roslyn/issues/18621")]
        public void AsyncGenericType()
        {
            UsingTree(
@"class Program
{
    public async Task<IReadOnlyCollection<ProjectConfiguration>>
}",
                // (4,1): error CS1519: Invalid token '}' in class, record, struct, or interface member declaration
                // }
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "}").WithArguments("}").WithLocation(4, 1));

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "Program");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IncompleteMember);
                    {
                        N(SyntaxKind.PublicKeyword);
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "Task");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.GenericName);
                                {
                                    N(SyntaxKind.IdentifierToken, "IReadOnlyCollection");
                                    N(SyntaxKind.TypeArgumentList);
                                    {
                                        N(SyntaxKind.LessThanToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "ProjectConfiguration");
                                        }
                                        N(SyntaxKind.GreaterThanToken);
                                    }
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(16044, "https://github.com/dotnet/roslyn/issues/16044")]
        public void AsyncAsType_Property_ExpressionBody()
        {
            var test = "class async { async async => null; }";

            CreateCompilation(test, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (1,7): warning CS8981: The type name 'async' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class async { async async => null; }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "async").WithArguments("async").WithLocation(1, 7),
                // (1,21): error CS0542: 'async': member names cannot be the same as their enclosing type
                // class async { async async => null; }
                Diagnostic(ErrorCode.ERR_MemberNameSameAsType, "async").WithArguments("async").WithLocation(1, 21),
                // (1,27): error CS8026: Feature 'expression-bodied property' is not available in C# 5. Please use language version 6 or greater.
                // class async { async async => null; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied property", "6").WithLocation(1, 27));

            UsingTree(test);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "async");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.IdentifierToken, "async");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
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
        [WorkItem(16044, "https://github.com/dotnet/roslyn/issues/16044")]
        public void AsyncAsType_Property()
        {
            UsingTree("class async { async async { get; } }").GetDiagnostics().Verify();
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "async");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.IdentifierToken, "async");
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(16044, "https://github.com/dotnet/roslyn/issues/16044")]
        public void AsyncAsType_Indexer_ExpressionBody_ErrorCase()
        {
            var text = "interface async { async this[async i] => null; }";

            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (1,11): warning CS8981: The type name 'async' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // interface async { async this[async i] => null; }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "async").WithArguments("async").WithLocation(1, 11),
                // (1,39): error CS8026: Feature 'expression-bodied indexer' is not available in C# 5. Please use language version 6 or greater.
                // interface async { async this[async i] => null; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied indexer", "6").WithLocation(1, 39),
                // (1,42): error CS8026: Feature 'default interface implementation' is not available in C# 5. Please use language version 8.0 or greater.
                // interface async { async this[async i] => null; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "null").WithArguments("default interface implementation", "8.0").WithLocation(1, 42),
                // (1,42): error CS8701: Target runtime doesn't support default interface implementation.
                // interface async { async this[async i] => null; }
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation, "null").WithLocation(1, 42));

            UsingTree(text);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "async");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.ThisKeyword);
                        N(SyntaxKind.BracketedParameterList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "async");
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
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
        [WorkItem(16044, "https://github.com/dotnet/roslyn/issues/16044")]
        public void AsyncAsType_Indexer()
        {
            UsingTree("interface async { async this[async i] { get; } }").GetDiagnostics().Verify();
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.InterfaceDeclaration);
                {
                    N(SyntaxKind.InterfaceKeyword);
                    N(SyntaxKind.IdentifierToken, "async");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.IndexerDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.ThisKeyword);
                        N(SyntaxKind.BracketedParameterList);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.Parameter);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "async");
                                }
                                N(SyntaxKind.IdentifierToken, "i");
                            }
                            N(SyntaxKind.CloseBracketToken);
                        }
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        [WorkItem(16044, "https://github.com/dotnet/roslyn/issues/16044")]
        public void AsyncAsType_Property_ExplicitInterface()
        {
            var test = "class async : async { async async.async => null; }";

            CreateCompilation(test, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (1,7): error CS0146: Circular base type dependency involving 'async' and 'async'
                // class async : async { async async.async => null; }
                Diagnostic(ErrorCode.ERR_CircularBase, "async").WithArguments("async", "async").WithLocation(1, 7),
                // (1,7): warning CS8981: The type name 'async' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class async : async { async async.async => null; }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "async").WithArguments("async").WithLocation(1, 7),
                // (1,29): error CS0538: 'async' in explicit interface declaration is not an interface
                // class async : async { async async.async => null; }
                Diagnostic(ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface, "async").WithArguments("async").WithLocation(1, 29),
                // (1,41): error CS8026: Feature 'expression-bodied property' is not available in C# 5. Please use language version 6 or greater.
                // class async : async { async async.async => null; }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied property", "6").WithLocation(1, 41));

            UsingTree(test);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "async");
                    N(SyntaxKind.BaseList);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.SimpleBaseType);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                        }
                    }
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.PropertyDeclaration);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "async");
                        }
                        N(SyntaxKind.ExplicitInterfaceSpecifier);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "async");
                            }
                            N(SyntaxKind.DotToken);
                        }
                        N(SyntaxKind.IdentifierToken, "async");
                        N(SyntaxKind.ArrowExpressionClause);
                        {
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.NullLiteralExpression);
                            {
                                N(SyntaxKind.NullKeyword);
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void AsyncLambdaInConditionalExpressionAfterPattern1()
        {
            UsingExpression("x is A ? async b => 0 : null");

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleLambdaExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void AsyncLambdaInConditionalExpressionAfterPattern2()
        {
            UsingExpression("x is A a ? async b => 0 : null");

            N(SyntaxKind.ConditionalExpression);
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.SimpleLambdaExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.Parameter);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void AsyncLambdaInConditionalExpressionAfterPattern3()
        {
            UsingExpression("x is A ? async (b) => 0 : null");

            N(SyntaxKind.ConditionalExpression);
            {
                N(SyntaxKind.IsExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.IsKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "A");
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72720")]
        public void AsyncLambdaInConditionalExpressionAfterPattern4()
        {
            UsingExpression("x is A a ? async (b) => 0 : null");

            N(SyntaxKind.ConditionalExpression);
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
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "A");
                        }
                        N(SyntaxKind.SingleVariableDesignation);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                }
                N(SyntaxKind.QuestionToken);
                N(SyntaxKind.ParenthesizedLambdaExpression);
                {
                    N(SyntaxKind.AsyncKeyword);
                    N(SyntaxKind.ParameterList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Parameter);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.EqualsGreaterThanToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "0");
                    }
                }
                N(SyntaxKind.ColonToken);
                N(SyntaxKind.NullLiteralExpression);
                {
                    N(SyntaxKind.NullKeyword);
                }
            }
            EOF();
        }
    }
}
