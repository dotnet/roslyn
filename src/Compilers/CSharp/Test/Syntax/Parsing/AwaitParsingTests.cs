// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AwaitParsingTests : ParsingTests
    {
        public AwaitParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text);
        }

        protected override CSharpSyntaxNode ParseNode(string text, CSharpParseOptions options = null)
        {
            return SyntaxFactory.ParseExpression(text);
        }

        [Fact]
        public void AwaitOnIdentifierInAsynchronousContext()
        {
            UsingTree(@"
class C
{
    async void f()
    {
        await goo();
    }
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
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
        }

        [Fact]
        public void AwaitOnIdentifierInSynchronousContext()
        {
            UsingTree(@"
class C
{
    void f()
    {
        await goo();
    }
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
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
        }

        [Fact]
        public void AwaitStatement()
        {
            UsingTree(@"
class C
{
    async void f()
    {
        await 1;
    }
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken);
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
        }

        [Fact]
        public void NestedLambdaAwait()
        {
            UsingTree(@"
class C
{
    void f()
    {
        async () => {
            await 1;
            () => {
                int await;
            };
        };
    }
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.ParenthesizedLambdaExpression);
                                {
                                    N(SyntaxKind.AsyncKeyword);
                                    N(SyntaxKind.ParameterList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.Block);
                                    {
                                        N(SyntaxKind.OpenBraceToken);
                                        N(SyntaxKind.ExpressionStatement);
                                        {
                                            N(SyntaxKind.AwaitExpression);
                                            {
                                                N(SyntaxKind.AwaitKeyword);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken);
                                                }
                                            }
                                            N(SyntaxKind.SemicolonToken);
                                        }
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
                                                N(SyntaxKind.Block);
                                                {
                                                    N(SyntaxKind.OpenBraceToken);
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
                                                                N(SyntaxKind.IdentifierToken);
                                                            }
                                                        }
                                                        N(SyntaxKind.SemicolonToken);
                                                    }
                                                    N(SyntaxKind.CloseBraceToken);
                                                }
                                            }
                                            N(SyntaxKind.SemicolonToken);
                                        }
                                        N(SyntaxKind.CloseBraceToken);
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
        }

        [Fact]
        public void AwaitExpr()
        {
            UsingTree(@"
class C
{
    async void f()
    {
        int c = await g() || await g();
    }
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
                                        N(SyntaxKind.IdentifierToken);
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LogicalOrExpression);
                                            {
                                                N(SyntaxKind.AwaitExpression);
                                                {
                                                    N(SyntaxKind.AwaitKeyword);
                                                    N(SyntaxKind.InvocationExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.ArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.BarBarToken);
                                                N(SyntaxKind.AwaitExpression);
                                                {
                                                    N(SyntaxKind.AwaitKeyword);
                                                    N(SyntaxKind.InvocationExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken);
                                                        }
                                                        N(SyntaxKind.ArgumentList);
                                                        {
                                                            N(SyntaxKind.OpenParenToken);
                                                            N(SyntaxKind.CloseParenToken);
                                                        }
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
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [Fact]
        public void AwaitMemberAccessExpression()
        {
            UsingNode(@"
async () => await base.g();
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                        }
                    }
                }
            }
        }

        [Fact]
        public void AwaitInvocationExpression()
        {
            UsingNode(@"
async () => await this.g();
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.InvocationExpression);
                    {
                    }
                }
            }
        }

        [Fact]
        public void AwaitDefaultExpression()
        {
            UsingNode(@"
async () => await default(Task);
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.DefaultExpression);
                    {
                    }
                }
            }
        }

        [Fact]
        public void AwaitIdentifierName()
        {
            UsingNode(@"
async () => await goo;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                    }
                }
            }
        }

        [Fact]
        public void AwaitCheckedExpression()
        {
            UsingNode(@"
async () => await checked { };
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.CheckedExpression);
                    {
                    }
                }
            }
        }

        [Fact]
        public void AwaitUncheckedExpression()
        {
            UsingNode(@"
async () => await unchecked { };
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.UncheckedExpression);
                    {
                    }
                }
            }
        }

        [Fact]
        public void AwaitParenthesizedExpression()
        {
            UsingNode(@"
async () => await (goo());
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitObjectCreationExpression()
        {
            UsingNode(@"
async () => await new Goo();
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.ObjectCreationExpression);
                    {
                    }
                }
            }
        }

        [Fact]
        public void AwaitAwaitExpression()
        {
            UsingNode(@"
async () => await await goo;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void PreIncrementAwait()
        {
            UsingNode(@"
async () => ++ await x;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.PreIncrementExpression);
                {
                    N(SyntaxKind.PlusPlusToken);
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void AwaitExpressionWithHigherPrecedence()
        {
            UsingNode(@"
async () => await x ++;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.PostIncrementExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.PlusPlusToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitExpressionWithLowerPrecedence()
        {
            UsingNode(@"
async () => await x * x;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.MultiplyExpression);
                {
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.AsteriskToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void PreIncrementNonAsyncAwait()
        {
            UsingNode(@"
() => ++ await x;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.PreIncrementExpression);
                {
                    N(SyntaxKind.PlusPlusToken);
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void AwaitTernary()
        {
            UsingNode(@"
async () => await a ? b : c;
");
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.ColonToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void ToggleIsAsyncOnOffTest()
        {
            UsingNode(@"
async () => {
    await 1;
    () => {
        int await;
    };
    await 1;
};
");
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AwaitExpression);
                        {
                            N(SyntaxKind.AwaitKeyword);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
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
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
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
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AwaitExpression);
                        {
                            N(SyntaxKind.AwaitKeyword);
                            N(SyntaxKind.NumericLiteralExpression);
                            {
                                N(SyntaxKind.NumericLiteralToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
        }

        [Fact]
        public void ToggleIsAsyncOffOnTest()
        {
            UsingNode(@"
() => {
    int await;
    async () => {
        await 1;
    };
    int await;
};
");
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
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
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.ParenthesizedLambdaExpression);
                        {
                            N(SyntaxKind.AsyncKeyword);
                            N(SyntaxKind.ParameterList);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsGreaterThanToken);
                            N(SyntaxKind.Block);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.ExpressionStatement);
                                {
                                    N(SyntaxKind.AwaitExpression);
                                    {
                                        N(SyntaxKind.AwaitKeyword);
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.SemicolonToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
        }

        [Fact]
        public void AwaitUsingTest()
        {
            UsingNode(@"
async () => {
    using (await goo())
    {
    }
};
");
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.AsyncKeyword);
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.AwaitExpression);
                        {
                            N(SyntaxKind.AwaitKeyword);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
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
        }

        #region AwaitExpressionInSyncContext

        [Fact]
        public void AwaitIdentifierExpressionInSyncContext()
        {
            UsingNode(@"
() => await goo;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitAwaitExpressionInSyncContext()
        {
            UsingNode(@"
() => await await goo;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.AwaitExpression);
                    {
                        N(SyntaxKind.AwaitKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void AwaitNewExpressionInSyncContext()
        {
            UsingNode(@"
() => await new int[];
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
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

        [Fact]
        public void AwaitThisExpressionInSyncContext()
        {
            UsingNode(@"
() => await this.goo();
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.ThisExpression);
                            {
                                N(SyntaxKind.ThisKeyword);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void AwaitBaseExpressionInSyncContext()
        {
            UsingNode(@"
() => await base.goo();
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.BaseExpression);
                            {
                                N(SyntaxKind.BaseKeyword);
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void AwaitDelegateExpressionInSyncContext()
        {
            UsingNode(@"
() => await delegate { };
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.AnonymousMethodExpression);
                    {
                        N(SyntaxKind.DelegateKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
            }
        }

        [Fact]
        public void AwaitCheckedExpressionInSyncContext()
        {
            UsingNode(@"
() => await checked ( );
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.CheckedExpression);
                    {
                        N(SyntaxKind.CheckedKeyword);
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitUncheckedExpressionInSyncContext()
        {
            UsingNode(@"
() => await unchecked ( );
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.UncheckedExpression);
                    {
                        N(SyntaxKind.UncheckedKeyword);
                        N(SyntaxKind.OpenParenToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitDefaultExpressionInSyncContext()
        {
            UsingNode(@"
() => await default(Goo);
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.DefaultExpression);
                    {
                        N(SyntaxKind.DefaultKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken);
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitTrueLiteralExpressionInSyncContext()
        {
            UsingNode(@"
() => await true;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.TrueLiteralExpression);
                    {
                        N(SyntaxKind.TrueKeyword);
                    }
                }
            }
        }

        [Fact]
        public void AwaitFalseLiteralExpressionInSyncContext()
        {
            UsingNode(@"
() => await false;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.FalseLiteralExpression);
                    {
                        N(SyntaxKind.FalseKeyword);
                    }
                }
            }
        }

        [Fact]
        public void AwaitStringLiteralExpressionInSyncContext()
        {
            UsingNode(@"
() => await ""goo"";
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.StringLiteralExpression);
                    {
                        N(SyntaxKind.StringLiteralToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitRealLiteralExpressionInSyncContext()
        {
            UsingNode(@"
() => await 3.14;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitIntegerLiteralExpressionInSyncContext()
        {
            UsingNode(@"
() => await 42;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitNullLiteralExpressionInSyncContext()
        {
            UsingNode(@"
() => await null;
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.NullLiteralExpression);
                    {
                        N(SyntaxKind.NullKeyword);
                    }
                }
            }
        }

        [Fact]
        public void AwaitCharacterLiteralExpressionInSyncContext()
        {
            UsingNode(@"
() => await 'a';
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.CharacterLiteralExpression);
                    {
                        N(SyntaxKind.CharacterLiteralToken);
                    }
                }
            }
        }

        [Fact]
        public void AwaitUsingExpressionInSyncContext()
        {
            UsingNode(@"
() => {
    using (await goo())
    {
    }
};
");
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.UsingStatement);
                    {
                        N(SyntaxKind.UsingKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.AwaitExpression);
                        {
                            N(SyntaxKind.AwaitKeyword);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
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
        }

        #endregion AwaitExpressionInSyncContext

        #region AwaitExpressionStatementInSyncContext

        [Fact]
        public void AwaitIdentifierExpressionStatementInSyncContext()
        {
            UsingNode(@"
() => {
    await goo;
}
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
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
            }
        }

        [Fact]
        public void AwaitInvocationExpressionStatementInSyncContext()
        {
            UsingNode(@"
() => {
    await goo();
}
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.AwaitExpression);
                        {
                            N(SyntaxKind.AwaitKeyword);
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken);
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
        }

        [Fact]
        public void BadConstAwaitInvocationExpressionStatementInSyncContext()
        {
            UsingNode(@"
() => {
    const await goo();
}
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.ConstKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "await");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "goo");
                                M(SyntaxKind.BracketedArgumentList);
                                {
                                    M(SyntaxKind.OpenBracketToken);
                                    M(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact]
        public void BadStaticAwaitInvocationExpressionStatementInSyncContext()
        {
            UsingNode(@"
() => {
    static await goo();
}
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.StaticKeyword);
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    M(SyntaxKind.OpenBracketToken);
                                    M(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
        }

        [Fact]
        public void BadLocalDeclarationAndAwaitExpressionInSyncContext()
        {
            UsingNode(@"
() => {
    await goo(];
}
");

            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "await");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "goo");
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    M(SyntaxKind.OpenBracketToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
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
            }
            EOF();
        }

        [Fact]
        public void BadStatementInSyncContext()
        {
            UsingNode(@"
() => {
    await goo(];
    int x = 2;
}
");
            N(SyntaxKind.ParenthesizedLambdaExpression);
            {
                N(SyntaxKind.ParameterList);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.EqualsGreaterThanToken);
                N(SyntaxKind.Block);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.LocalDeclarationStatement);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "await");
                            }
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken, "goo");
                                N(SyntaxKind.BracketedArgumentList);
                                {
                                    M(SyntaxKind.OpenBracketToken);
                                    M(SyntaxKind.Argument);
                                    {
                                        M(SyntaxKind.IdentifierName);
                                        {
                                            M(SyntaxKind.IdentifierToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseBracketToken);
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
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
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "2");
                                    }
                                }
                            }
                        }
                        N(SyntaxKind.SemicolonToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        #endregion AwaitExpressionStatementInSyncContext
    }
}
