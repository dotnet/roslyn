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
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
                                        N(SyntaxKind.OpenParenToken);
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
                                        N(SyntaxKind.CommaToken);
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
                                        N(SyntaxKind.CloseParenToken);
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken);
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeconstructionDeclarator);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.VariableDeclaration);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Int32");
                                                    }
                                                    N(SyntaxKind.VariableDeclarator);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "a");
                                                    }
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.VariableDeclaration);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Int64");
                                                    }
                                                    N(SyntaxKind.VariableDeclarator);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "b");
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int32");
                                            }
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeconstructionDeclarator);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.VariableDeclaration);
                                                {
                                                    N(SyntaxKind.VariableDeclarator);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "a");
                                                    }
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.VariableDeclaration);
                                                {
                                                    N(SyntaxKind.VariableDeclarator);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "b");
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "c");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
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
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
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
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                            N(SyntaxKind.VariableDeconstructionDeclarator);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.VariableDeclaration);
                                                {
                                                    N(SyntaxKind.VariableDeclarator);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "y");
                                                    }
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.VariableDeclaration);
                                                {
                                                    N(SyntaxKind.VariableDeclarator);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "z");
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
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
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
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
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Int64");
                                            }
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "foo");
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
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                        N(SyntaxKind.EqualsToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "foo");
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
                            N(SyntaxKind.ForEachStatement);
                            {
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
                                        N(SyntaxKind.OpenParenToken);
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
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
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
                            N(SyntaxKind.ForEachStatement);
                            {
                                N(SyntaxKind.ForEachKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeconstructionDeclarator);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.VariableDeclaration);
                                        {
                                            N(SyntaxKind.VariableDeclarator);
                                            {
                                                N(SyntaxKind.IdentifierToken, "y");
                                            }
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
    }
}
