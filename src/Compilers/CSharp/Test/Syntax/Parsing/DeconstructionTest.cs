// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class DeconstructionTests : ParsingTests
    {
        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void ParenExpression()
        {
            // `(id) .` starts with a parenthesized expression
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (x).ToString();
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.ParenthesizedExpression);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.CloseParenToken);
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
        public void TupleTypeWithElementNames()
        {
            // `(T id, ...) id` starts with a tuple type

            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (Int32 a, Int64 b) x;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                            N(SyntaxKind.LocalDeclarationStatement);
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
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.TupleElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TupleType()
        {
            // `(T, ...) id` starts with a type

            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (Int32, Int64) x;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                            N(SyntaxKind.LocalDeclarationStatement);
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
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.TupleElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TupleTypeArray()
        {
            // (T, ...) [] is a type

            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (Int32, Int64)[] x;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.ArrayType);
                                    {
                                        N(SyntaxKind.TupleType);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.TupleElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.TupleElement);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
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
                                        N(SyntaxKind.IdentifierToken);
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
        public void TupleLiteral()
        {
            // (E, ...) followed by ., +, -, etc. starts with a tuple literal/expression

            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (Int32, Int64).Foo();
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.SimpleMemberAccessExpression);
                                    {
                                        N(SyntaxKind.TupleExpression);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken);
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
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
        public void DeconstructionAssignment()
        {
            // (E, ...) = is a deconstruction-assignment
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (x, y) = foo;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
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
        public void NestedDeconstructionAssignment()
        {
            // (E, ...) = is a deconstruction-assignment
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (x, (y, z)) = foo;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.TupleExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken);
                                                    }
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken);
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
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

        [Fact(Skip = "PROTOTYPE(tuples)")]
        public void DeconstructionDeclaration()
        {
            // `(T id, ...) = ...` is a deconstruction-declaration
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (Int32 a, Int64 b) = foo;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
            EOF();
        }

        [Fact]
        public void Statement1()
        {
            var text = "(T, T)[] id;";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            var declaration = ((LocalDeclarationStatementSyntax)statement).Declaration;
            Assert.Equal("(T, T)[]", declaration.Type.ToString());
            Assert.Equal("id", declaration.Variables.ToString());
        }

        [Fact]
        public void Statement2()
        {
            var text = "(x).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.InvocationExpression, expression.Kind());
        }

        [Fact]
        public void Statement3()
        {
            var text = "(x, x).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.InvocationExpression, expression.Kind());
        }

        [Fact]
        public void Statement4()
        {
            var text = "((x)).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.InvocationExpression, expression.Kind());
        }

        [Fact]
        public void Statement5()
        {
            var text = "((x, y) = M()).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.InvocationExpression, expression.Kind());

            var invocation = (InvocationExpressionSyntax)expression;
            var lhs = (MemberAccessExpressionSyntax)invocation.Expression;
            var lhsContent = (ParenthesizedExpressionSyntax)lhs.Expression;
            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, lhsContent.Expression.Kind());
        }

        [Fact]
        public void Statement6()
        {
            var text = "(((x, y))z).Foo();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.InvocationExpression, expression.Kind());

            var invocation = (InvocationExpressionSyntax)expression;
            var lhs = (MemberAccessExpressionSyntax)invocation.Expression;
            var lhsContent = (ParenthesizedExpressionSyntax)lhs.Expression;
            Assert.Equal(SyntaxKind.CastExpression, lhsContent.Expression.Kind());
        }

        [Fact]
        public void Statement7()
        {
            var text = "(a) => a;";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expression.Kind());
        }

        [Fact]
        public void Statement8()
        {
            var text = "(a, b) => { };";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expression.Kind());
        }
    }
}
