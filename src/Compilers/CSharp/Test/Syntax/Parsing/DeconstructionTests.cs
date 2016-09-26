// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    // Some examples of parsing subtleties:
    // `(T id, ...) = ...;` is a deconstruction-declaration
    // `var (id, ...) = ...;` is a deconstruction-declaration
    // `(T id, ...) id;` starts with a tuple type
    // `(T, ...) id;` starts with tuple type
    // `(T, ...)[] id;` starts with a tuple type array
    // `(E, ...) = ...;` is a deconstruction-assignment, which starts with a tuple literal/expression
    // `(E, ...).Foo();` starts with a tuple literal/expression
    // `(E, ...) + ...` also starts with a tuple literal/expression
    // `(T, ...)? id;` starts with a tuple type

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
        public void SimpleDeclaration()
        {
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        for(Int32 x = foo; ; ) { }
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ForStatement);
                            {
                                N(SyntaxKind.ForKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Int32");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "foo");
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                                N(SyntaxKind.SemicolonToken);
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

        [Fact]
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.DeconstructionDeclarationStatement);
                            {
                                N(SyntaxKind.VariableComponentAssignment);
                                {
                                    N(SyntaxKind.ParenthesizedVariableComponent);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.TypedVariableComponent);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int32");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.TypedVariableComponent);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int64");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "foo");
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
        public void NestedDeconstructionDeclaration()
        {
            // `(T id, (...)) = ...` is a deconstruction-declaration
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        ((Int32 a, Int64 b), Int32 c) = foo;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.DeconstructionDeclarationStatement);
                            {
                                N(SyntaxKind.VariableComponentAssignment);
                                {
                                    N(SyntaxKind.ParenthesizedVariableComponent);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.ParenthesizedVariableComponent);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.TypedVariableComponent);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Int32");
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.TypedVariableComponent);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Int64");
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "b");
                                                }
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.TypedVariableComponent);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int32");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "foo");
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
        public void VarDeconstructionDeclaration()
        {
            // `var (id, ...) = ...` is a deconstruction-declaration
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        var (a, b) = foo;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.DeconstructionDeclarationStatement);
                            {
                                N(SyntaxKind.VariableComponentAssignment);
                                {
                                    N(SyntaxKind.TypedVariableComponent);
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
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "foo");
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
        public void VarNestedDeconstructionDeclaration()
        {
            // `var ((id, ...), ...) = ...` is a deconstruction-declaration
            var tree = UsingTree(@"
        class C
        {
            void Foo()
            {
                var ((a, b), c) = foo;
            }
        }", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.DeconstructionDeclarationStatement);
                            {
                                N(SyntaxKind.VariableComponentAssignment);
                                {
                                    N(SyntaxKind.TypedVariableComponent);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.ParenthesizedVariableDesignation);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.ParenthesizedVariableDesignation);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "b");
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "foo");
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
            EOF();
        }

        [Fact]
        public void VarMethodCall()
        {
            // `var(...);` is a method call
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        var(a, b);
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
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken);
                                    }
                                    N(SyntaxKind.ArgumentList);
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
        public void MixedDeconstructionVariables()
        {
            var tree = UsingTree(@"
class C
{
    void Foo()
    {
        (Int32 x, var (y, z)) = foo;
    }
}", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.DeconstructionDeclarationStatement);
                            {
                                N(SyntaxKind.VariableComponentAssignment);
                                {
                                    N(SyntaxKind.ParenthesizedVariableComponent);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.TypedVariableComponent);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int32");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.TypedVariableComponent);
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
                                                    N(SyntaxKind.IdentifierToken, "y");
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "z");
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "foo");
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
        public void DeconstructionFor()
        {
            var tree = UsingTree(@"
        class C
        {
            void Foo()
            {
                for ((Int32 x, Int64 y) = foo; ; ) { }
            }
        }", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ForStatement);
                            {
                                N(SyntaxKind.ForKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.VariableComponentAssignment);
                                {
                                    N(SyntaxKind.ParenthesizedVariableComponent);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.TypedVariableComponent);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int32");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.TypedVariableComponent);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int64");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "foo");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                                N(SyntaxKind.SemicolonToken);
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.EndOfFileToken);
                }
                EOF();
            }
        }

        [Fact]
        public void VarDeconstructionFor()
        {
            var tree = UsingTree(@"
        class C
        {
            void Foo()
            {
                for (var (x, y) = foo; ; ) { }
            }
        }", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ForStatement);
                            {
                                N(SyntaxKind.ForKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.VariableComponentAssignment);
                                {
                                    N(SyntaxKind.TypedVariableComponent);
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
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                            N(SyntaxKind.CommaToken);
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "foo");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                                N(SyntaxKind.SemicolonToken);
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.Block);
                                {
                                    N(SyntaxKind.OpenBraceToken);
                                    N(SyntaxKind.CloseBraceToken);
                                }
                            }
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DeconstructionForeach()
        {
            var tree = UsingTree(@"
        class C
        {
            void Foo()
            {
                foreach ((int x, var y) in foo) { }
            }
        }", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ForEachComponentStatement);
                            {
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.ParenthesizedVariableComponent);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.TypedVariableComponent);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.TypedVariableComponent);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "var");
                                        }
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "foo");
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
        public void VarDeconstructionForeach()
        {
            var tree = UsingTree(@"
        class C
        {
            void Foo()
            {
                foreach (var (x, y) in foo) { }
            }
        }", options: TestOptions.Regular.WithTuplesFeature());
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
                        N(SyntaxKind.IdentifierToken, "Foo");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ForEachComponentStatement);
                            {
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TypedVariableComponent);
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
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.SingleVariableDesignation);
                                        {
                                            N(SyntaxKind.IdentifierToken, "y");
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "foo");
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
        public void DeconstructionInScript()
        {
            var tree = UsingTree(@" (int x, int y) = (1, 2); ", options: TestOptions.Script);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.DeconstructionDeclarationStatement);
                    {
                        N(SyntaxKind.VariableComponentAssignment);
                        {
                            N(SyntaxKind.ParenthesizedVariableComponent);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TypedVariableComponent);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TypedVariableComponent);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.SingleVariableDesignation);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken);
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

        [Fact]
        public void DeconstructionForEachInScript()
        {
            var tree = UsingTree(@" foreach ((int x, int y) in new[] { (1, 2) }) { }; ", options: TestOptions.Script);

            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.GlobalStatement);
                {
                    N(SyntaxKind.ForEachComponentStatement);
                    {
                        N(SyntaxKind.ForEachKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.ParenthesizedVariableComponent);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.TypedVariableComponent);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.TypedVariableComponent);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.IntKeyword);
                                }
                                N(SyntaxKind.SingleVariableDesignation);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.InKeyword);
                        N(SyntaxKind.ImplicitArrayCreationExpression);
                        {
                            N(SyntaxKind.NewKeyword);
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.CloseBracketToken);
                            N(SyntaxKind.ArrayInitializerExpression);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.TupleExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.NumericLiteralExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralToken);
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
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
        public void TupleArray()
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
        public void ParenthesizedExpression()
        {
            var text = "(x).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.InvocationExpression, expression.Kind());
        }

        [Fact]
        public void TupleLiteralStatement()
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
        public void CastWithTupleType()
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
        public void NotACast()
        {
            var text = "((Int32.MaxValue, Int32.MaxValue)).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            var expression = ((ExpressionStatementSyntax)statement).Expression;
            var invocation = (InvocationExpressionSyntax)expression;
            var lhs = (MemberAccessExpressionSyntax)invocation.Expression;
            var paren = (ParenthesizedExpressionSyntax)lhs.Expression;
            Assert.Equal(SyntaxKind.TupleExpression, paren.Expression.Kind());
        }

        [Fact]
        public void AlsoNotACast()
        {
            var text = "((x, y)).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            var expression = ((ExpressionStatementSyntax)statement).Expression;
            var invocation = (InvocationExpressionSyntax)expression;
            var lhs = (MemberAccessExpressionSyntax)invocation.Expression;
            var paren = (ParenthesizedExpressionSyntax)lhs.Expression;
            Assert.Equal(SyntaxKind.TupleExpression, paren.Expression.Kind());
        }

        [Fact]
        public void StillNotACast()
        {
            var text = "((((x, y)))).ToString();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            var expression = ((ExpressionStatementSyntax)statement).Expression;
            var invocation = (InvocationExpressionSyntax)expression;
            var lhs = (MemberAccessExpressionSyntax)invocation.Expression;
            var paren1 = (ParenthesizedExpressionSyntax)lhs.Expression;
            var paren2 = (ParenthesizedExpressionSyntax)paren1.Expression;
            var paren3 = (ParenthesizedExpressionSyntax)paren2.Expression;
            Assert.Equal(SyntaxKind.TupleExpression, paren3.Expression.Kind());
        }

        [Fact]
        public void LambdaInExpressionStatement()
        {
            var text = "(a) => a;"; // syntax ok
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expression.Kind());
        }

        [Fact]
        public void LambdaWithBodyInExpressionStatement()
        {
            var text = "(a, b) => { };"; // syntax ok
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);

            var expression = ((ExpressionStatementSyntax)statement).Expression;
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expression.Kind());
        }

        [Fact]
        public void InvalidStatement()
        {
            var text = "(x, y)? = M();"; // error
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.True(statement.HasErrors);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/12402")]
        [WorkItem(12402, "https://github.com/dotnet/roslyn/issues/12402")]
        public void ConfusedForWithDeconstruction()
        {
            var text = "for ((int x, var (y, z)) in foo) { }";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());

            // This expectation is wrong. We should expect a foreach statement (because the 'in' keyword is there)
            Assert.True(statement.Kind() == SyntaxKind.ForStatement);
        }

        [Fact]
        public void NullableTuple()
        {
            var text = "(x, y)? z = M();";
            var statement = SyntaxFactory.ParseStatement(text, offset: 0, options: TestOptions.Regular.WithTuplesFeature());
            Assert.False(statement.HasErrors);
            var declaration = ((LocalDeclarationStatementSyntax)statement).Declaration;
            var nullable = (NullableTypeSyntax)declaration.Type;
            Assert.Equal(SyntaxKind.TupleType, nullable.ElementType.Kind());
        }

        [Fact, WorkItem(12803, "https://github.com/dotnet/roslyn/issues/12803")]
        public void BadTupleElementTypeInDeconstruction01()
        {
            var source =
@"
class C
{
    void M()
    {
        int (x1, x2) = (1, 2);
    }
}
namespace System
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) { this.Item1 = item1; this.Item2 = item2; }
    }
}";
            // the duplicate reporting of CS8136 below is due to open issue https://github.com/dotnet/roslyn/issues/12905
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,14): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                //         int (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x1").WithLocation(6, 14),
                // (6,18): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                //         int (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "x2").WithLocation(6, 18)
                );
        }

        [Fact, WorkItem(12803, "https://github.com/dotnet/roslyn/issues/12803")]
        public void BadTupleElementTypeInDeconstruction02()
        {
            var source =
@"
class C
{
    void M()
    {
        (int x1, x2) = (1, 2);
        (x3, int x4) = (1, 2);
    }
}
namespace System
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) { this.Item1 = item1; this.Item2 = item2; }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,18): error CS1031: Type expected
                //         (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeExpected, "x2").WithLocation(6, 18),
                // (7,10): error CS1031: Type expected
                //         (x3, int x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_TypeExpected, "x3").WithLocation(7, 10)
                );
        }

        [Fact, WorkItem(12803, "https://github.com/dotnet/roslyn/issues/12803")]
        public void SwapAssignmentShouldNotBeParsedAsDeconstructionDeclaration()
        {
            var source =
@"
class C
{
    void M(ref int x, ref int y)
    {
        (x, y) = (y, x);
    }
}
namespace System
{
    struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) { this.Item1 = item1; this.Item2 = item2; }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                );
        }

        public void NoDeconstructionAsLvalue()
        {
            var source =
@"
class C
{
    void M()
    {
        var (x, y) = e; // ok, deconstruction declaration
        var(x, y); // ok, invocation
        int x = var(x, y); // ok, invocation
        var(x, y) += e;            // error 1
        var(x, y)++;               // error 2
        ++var(x, y);               // error 3
        M(out var(x, y));          // error 4
        M(ref var(x, y));          // error 5
        return ref var(x, y);      // error 6
        ref int x = ref var(x, y); // error 7
        var (x, 1) = e;            // error 8
    }
}";
            ParseAndValidate(source,
                    // (9,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         var(x, y) += e;            // error 1
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(9, 9),
                    // (10,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         var(x, y)++;               // error 2
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(10, 9),
                    // (11,11): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         ++var(x, y);               // error 3
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(11, 11),
                    // (12,15): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         M(out var(x, y));          // error 4
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(12, 15),
                    // (13,15): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         M(ref var(x, y));          // error 5
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(13, 15),
                    // (14,20): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         return ref var(x, y);      // error 6
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(14, 20),
                    // (15,25): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         ref int x = ref var(x, y); // error 7
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(15, 25),
                    // (16,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                    //         var (x, 1) = e;            // error 8
                    Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var (x, 1)").WithLocation(16, 9)
                );
        }

        public static void ParseAndValidate(string text, params DiagnosticDescription[] expectedErrors)
        {
            var parsedTree = ParseWithRoundTripCheck(text);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
        }
    }
}
