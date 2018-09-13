// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    // Some examples of parsing subtleties:
    // `(T id, ...) = ...` is a deconstruction-assignment into a tuple expression with declaration expressions
    // `var (id, ...) = ...` is a deconstruction-assignment
    // `(T id, ...) id;` starts with a tuple type
    // `(T, ...) id;` starts with tuple type
    // `(T, ...)[] id;` starts with a tuple type array
    // `(E, ...) = ...;` is a deconstruction-assignment
    // `(E, ...).Goo();` starts with a tuple literal/expression
    // `(E, ...) + ...` also starts with a tuple literal/expression
    // `(T, ...)? id;` starts with a tuple type

    [CompilerTrait(CompilerFeature.Tuples)]
    public class DeconstructionTests : ParsingTests
    {
        public DeconstructionTests(ITestOutputHelper output) : base(output) { }

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
    void Goo()
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
    void Goo()
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
                                            N(SyntaxKind.IdentifierToken);
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.TupleElement);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken);
                                            }
                                            N(SyntaxKind.IdentifierToken);
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
    void Goo()
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
    void Goo()
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
    void Goo()
    {
        (Int32, Int64).Goo();
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
    void Goo()
    {
        (x, y) = goo;
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
    void Goo()
    {
        for(Int32 x = goo; ; ) { }
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                                N(SyntaxKind.IdentifierToken, "goo");
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
    void Goo()
    {
        (x, (y, z)) = goo;
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
    void Goo()
    {
        (Int32 a, Int64 b) = goo;
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                            N(SyntaxKind.DeclarationExpression);
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
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
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
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "goo");
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
    void Goo()
    {
        ((Int32 a, Int64 b), Int32 c) = goo;
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                            N(SyntaxKind.TupleExpression);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.DeclarationExpression);
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
                                                }
                                                N(SyntaxKind.CommaToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.DeclarationExpression);
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
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
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
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "goo");
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
    void Goo()
    {
        var (a, b) = goo;
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                        N(SyntaxKind.IdentifierToken, "goo");
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
            void Goo()
            {
                var ((a, b), c) = goo;
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                    N(SyntaxKind.DeclarationExpression);
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
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "goo");
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
    void Goo()
    {
        var(a, b);
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "b");
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
    void Goo()
    {
        (Int32 x, var (y, z)) = goo;
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                            N(SyntaxKind.DeclarationExpression);
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
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
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
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "goo");
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
            void Goo()
            {
                for ((Int32 x, Int64 y) = goo; ; ) { }
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
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
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
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
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "goo");
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
            void Goo()
            {
                for (var (x, y) = goo; ; ) { }
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
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
                                        N(SyntaxKind.IdentifierToken, "goo");
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
            void Goo()
            {
                foreach ((int x, var y) in goo) { }
            }
        }");
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.DeclarationExpression);
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
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.DeclarationExpression);
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
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.InKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "goo");
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
            void Goo()
            {
                foreach (var (x, y) in goo) { }
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                    N(SyntaxKind.IdentifierToken, "goo");
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
                    N(SyntaxKind.ExpressionStatement);
                    {
                        N(SyntaxKind.SimpleAssignmentExpression);
                        {
                            N(SyntaxKind.TupleExpression);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
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
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.DeclarationExpression);
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
                    N(SyntaxKind.ForEachVariableStatement);
                    {
                        N(SyntaxKind.ForEachKeyword);
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.TupleExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.DeclarationExpression);
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
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.DeclarationExpression);
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
        public void DeconstructionDeclarationWithDiscard()
        {
            var tree = UsingTree(@"
class C
{
    void Goo()
    {
        (int _, var _, var (_, _), _) = e;
    }
}");
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
                        N(SyntaxKind.IdentifierToken, "Goo");
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
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.DiscardDesignation);
                                                {
                                                    N(SyntaxKind.UnderscoreToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.DiscardDesignation);
                                                {
                                                    N(SyntaxKind.UnderscoreToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.ParenthesizedVariableDesignation);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.DiscardDesignation);
                                                    {
                                                        N(SyntaxKind.UnderscoreToken);
                                                    }
                                                    N(SyntaxKind.CommaToken);
                                                    N(SyntaxKind.DiscardDesignation);
                                                    {
                                                        N(SyntaxKind.UnderscoreToken);
                                                    }
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "_");
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "e");
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
            var text = "(((x, y))z).Goo();";
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
            var text = "for ((int x, var (y, z)) in goo) { }";
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
            CreateCompilation(source).VerifyDiagnostics(
                // (6,18): error CS8136: Deconstruction 'var (...)' form disallows a specific type for 'var'.
                //         int (x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType, "(x1, x2)").WithLocation(6, 13)
                );
        }

        [Fact, WorkItem(12803, "https://github.com/dotnet/roslyn/issues/12803")]
        public void BadTupleElementTypeInDeconstruction02()
        {
            var source =
@"
class C
{
    int x2, x3;
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
            CreateCompilation(source).VerifyDiagnostics(
                // (7,9): error CS8183: A deconstruction cannot mix declarations and expressions on the left-hand-side.
                //         (int x1, x2) = (1, 2);
                Diagnostic(ErrorCode.ERR_MixedDeconstructionUnsupported, "(int x1, x2)").WithLocation(7, 9),
                // (8,9): error CS8183: A deconstruction cannot mix declarations and expressions on the left-hand-side.
                //         (x3, int x4) = (1, 2);
                Diagnostic(ErrorCode.ERR_MixedDeconstructionUnsupported, "(x3, int x4)").WithLocation(8, 9)
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
            CreateCompilation(source).VerifyDiagnostics(
                );
        }

        [Fact]
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
    }
}";
            ParseAndValidate(source);
        }

        [Fact]
        public void NoDeconstructionAsLvalue_1()
        {
            var source =
@"
class C
{
    void M(string e)
    {
        var(x, y) += e;            // error 1
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var(x, y) += e;            // error 1
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(6, 9),
                // (6,9): error CS0103: The name 'var' does not exist in the current context
                //         var(x, y) += e;            // error 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 9),
                // (6,13): error CS0103: The name 'x' does not exist in the current context
                //         var(x, y) += e;            // error 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 13),
                // (6,16): error CS0103: The name 'y' does not exist in the current context
                //         var(x, y) += e;            // error 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 16));
        }

        [Fact]
        public void NoDeconstructionAsLvalue_2()
        {
            var source =
@"
class C
{
    void M(string e)
    {
        var(x, y)++;               // error 2
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var(x, y)++;               // error 2
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(6, 9),
                // (6,9): error CS0103: The name 'var' does not exist in the current context
                //         var(x, y)++;               // error 2
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 9),
                // (6,13): error CS0103: The name 'x' does not exist in the current context
                //         var(x, y)++;               // error 2
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 13),
                // (6,16): error CS0103: The name 'y' does not exist in the current context
                //         var(x, y)++;               // error 2
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 16));
        }

        [Fact]
        public void NoDeconstructionAsLvalue_3()
        {
            var source =
@"
class C
{
    void M(string e)
    {
        ++var(x, y);               // error 3
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,11): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         ++var(x, y);               // error 3
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(6, 11),
                // (6,11): error CS0103: The name 'var' does not exist in the current context
                //         ++var(x, y);               // error 3
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 11),
                // (6,15): error CS0103: The name 'x' does not exist in the current context
                //         ++var(x, y);               // error 3
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 15),
                // (6,18): error CS0103: The name 'y' does not exist in the current context
                //         ++var(x, y);               // error 3
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 18));
        }

        [Fact]
        public void NoDeconstructionAsLvalue_4()
        {
            var source =
@"
class C
{
    void M(string e)
    {
        X(out var(x, y));          // error 4
    }

    void X(out object x) { x = null; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0103: The name 'var' does not exist in the current context
                //         X(out var(x, y));          // error 4
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 15),
                // (6,19): error CS0103: The name 'x' does not exist in the current context
                //         X(out var(x, y));          // error 4
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 19),
                // (6,22): error CS0103: The name 'y' does not exist in the current context
                //         X(out var(x, y));          // error 4
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 22),
                // (6,15): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         X(out var(x, y));          // error 4
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(6, 15));
        }

        [Fact]
        public void NoDeconstructionAsLvalue_5()
        {
            var source =
@"
class C
{
    void M(string e)
    {
        X(ref var(x, y));          // error 5
    }

    void X(ref object x) { x = null; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0103: The name 'var' does not exist in the current context
                //         X(ref var(x, y));          // error 5
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 15),
                // (6,19): error CS0103: The name 'x' does not exist in the current context
                //         X(ref var(x, y));          // error 5
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 19),
                // (6,22): error CS0103: The name 'y' does not exist in the current context
                //         X(ref var(x, y));          // error 5
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 22),
                // (6,15): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         X(ref var(x, y));          // error 5
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(6, 15));
        }

        [Fact]
        public void NoDeconstructionAsLvalue_6()
        {
            var source =
@"
class C
{
    ref object M(string e)
    {
        return ref var(x, y);      // error 6
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,20): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         return ref var(x, y);      // error 6
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(6, 20),
                // (6,20): error CS0103: The name 'var' does not exist in the current context
                //         return ref var(x, y);      // error 6
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 20),
                // (6,24): error CS0103: The name 'x' does not exist in the current context
                //         return ref var(x, y);      // error 6
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 24),
                // (6,27): error CS0103: The name 'y' does not exist in the current context
                //         return ref var(x, y);      // error 6
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 27));
        }

        [Fact]
        public void NoDeconstructionAsLvalue_7()
        {
            var source =
@"
class C
{
    void M(string e)
    {
        ref int x = ref var(x, y); // error 7
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,25): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         ref int x = ref var(x, y); // error 7
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var(x, y)").WithLocation(6, 25),
                // (6,25): error CS0103: The name 'var' does not exist in the current context
                //         ref int x = ref var(x, y); // error 7
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 25),
                // (6,32): error CS0103: The name 'y' does not exist in the current context
                //         ref int x = ref var(x, y); // error 7
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y").WithArguments("y").WithLocation(6, 32),
                // (6,29): error CS0165: Use of unassigned local variable 'x'
                //         ref int x = ref var(x, y); // error 7
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(6, 29));
        }

        [Fact]
        public void NoDeconstructionAsLvalue_8()
        {
            var source =
@"
class C
{
    void object M(string e)
    {
        var (x, 1) = e;            // error 8
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,10): error CS1519: Invalid token 'object' in class, struct, or interface member declaration
                //     void object M(string e)
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "object").WithArguments("object").WithLocation(4, 10),
                // (6,9): error CS8199: The syntax 'var (...)' as an lvalue is reserved.
                //         var (x, 1) = e;            // error 8
                Diagnostic(ErrorCode.ERR_VarInvocationLvalueReserved, "var (x, 1)").WithLocation(6, 9),
                // (6,9): error CS0103: The name 'var' does not exist in the current context
                //         var (x, 1) = e;            // error 8
                Diagnostic(ErrorCode.ERR_NameNotInContext, "var").WithArguments("var").WithLocation(6, 9),
                // (6,14): error CS0103: The name 'x' does not exist in the current context
                //         var (x, 1) = e;            // error 8
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(6, 14),
                // (4,17): error CS0161: 'C.M(string)': not all code paths return a value
                //     void object M(string e)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "M").WithArguments("C.M(string)").WithLocation(4, 17));
        }

        [Fact]
        public void DiscardsInDeconstruction_01()
        {
            var tree = UsingTree(@"void M() { var (x, _) = e; }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
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
                                            N(SyntaxKind.IdentifierToken, "x");
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.DiscardDesignation);
                                        {
                                            N(SyntaxKind.UnderscoreToken);
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "e");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardsInDeconstruction_02()
        {
            var tree = UsingTree(@"void M() { (var x, var _) = e; }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.SimpleAssignmentExpression);
                            {
                                N(SyntaxKind.TupleExpression);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.DeclarationExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                            N(SyntaxKind.SingleVariableDesignation);
                                            {
                                                N(SyntaxKind.IdentifierToken, "x");
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.DeclarationExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                            N(SyntaxKind.DiscardDesignation);
                                            {
                                                N(SyntaxKind.UnderscoreToken);
                                            }
                                        }
                                    }
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.EqualsToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "e");
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardsInOut_01()
        {
            var tree = UsingTree(@"void M() { M(out var _); }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "M");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.OutKeyword);
                                        N(SyntaxKind.DeclarationExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "var");
                                            }
                                            N(SyntaxKind.DiscardDesignation);
                                            {
                                                N(SyntaxKind.UnderscoreToken);
                                            }
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardsInOut_02()
        {
            var tree = UsingTree(@"void M() { M(out int _); }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.ExpressionStatement);
                        {
                            N(SyntaxKind.InvocationExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "M");
                                }
                                N(SyntaxKind.ArgumentList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.Argument);
                                    {
                                        N(SyntaxKind.OutKeyword);
                                        N(SyntaxKind.DeclarationExpression);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                            N(SyntaxKind.DiscardDesignation);
                                            {
                                                N(SyntaxKind.UnderscoreToken);
                                            }
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardsInPattern_01()
        {
            var tree = UsingTree(@"void M() { if (e is int _) {} }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.IfStatement);
                        {
                            N(SyntaxKind.IfKeyword);
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "e");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.DeclarationPattern);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                    N(SyntaxKind.DiscardDesignation);
                                    {
                                        N(SyntaxKind.UnderscoreToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardsInPattern_02()
        {
            var tree = UsingTree(@"void M() { if (e is var _) {} }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.IfStatement);
                        {
                            N(SyntaxKind.IfKeyword);
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IsPatternExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "e");
                                }
                                N(SyntaxKind.IsKeyword);
                                N(SyntaxKind.VarPattern);
                                {
                                    N(SyntaxKind.VarKeyword, "var");
                                    N(SyntaxKind.DiscardDesignation);
                                    {
                                        N(SyntaxKind.UnderscoreToken);
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
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardsInPattern_03()
        {
            var tree = UsingTree(@"void M() { switch (e) { case int _: break; } }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.SwitchStatement);
                        {
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            N(SyntaxKind.CloseParenToken);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchSection);
                            {
                                N(SyntaxKind.CasePatternSwitchLabel);
                                {
                                    N(SyntaxKind.CaseKeyword);
                                    N(SyntaxKind.DeclarationPattern);
                                    {
                                        N(SyntaxKind.PredefinedType);
                                        {
                                            N(SyntaxKind.IntKeyword);
                                        }
                                        N(SyntaxKind.DiscardDesignation);
                                        {
                                            N(SyntaxKind.UnderscoreToken);
                                        }
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.BreakStatement);
                                {
                                    N(SyntaxKind.BreakKeyword);
                                    N(SyntaxKind.SemicolonToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void DiscardsInPattern_04()
        {
            var tree = UsingTree(@"void M() { switch (e) { case var _: break; } }");
            N(SyntaxKind.CompilationUnit);
            {
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
                        N(SyntaxKind.SwitchStatement);
                        {
                            N(SyntaxKind.SwitchKeyword);
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "e");
                            }
                            N(SyntaxKind.CloseParenToken);
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchSection);
                            {
                                N(SyntaxKind.CasePatternSwitchLabel);
                                {
                                    N(SyntaxKind.CaseKeyword);
                                    N(SyntaxKind.VarPattern);
                                    {
                                        N(SyntaxKind.VarKeyword, "var");
                                        N(SyntaxKind.DiscardDesignation);
                                        {
                                            N(SyntaxKind.UnderscoreToken);
                                        }
                                    }
                                    N(SyntaxKind.ColonToken);
                                }
                                N(SyntaxKind.BreakStatement);
                                {
                                    N(SyntaxKind.BreakKeyword);
                                    N(SyntaxKind.SemicolonToken);
                                }
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_00()
        {
            UsingStatement(@"var (x, y) = e;");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
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
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_01()
        {
            UsingStatement(@"var::var (x, y) = e;");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.AliasQualifiedName);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                            N(SyntaxKind.ColonColonToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_02()
        {
            UsingStatement(@"var.var (x, y) = e;");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.SimpleMemberAccessExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                            N(SyntaxKind.DotToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                        }
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_03()
        {
            UsingStatement(@"var<var> (x, y) = e;");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.GenericName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                            N(SyntaxKind.TypeArgumentList);
                            {
                                N(SyntaxKind.LessThanToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                                N(SyntaxKind.GreaterThanToken);
                            }
                        }
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_04()
        {
            UsingStatement(@"var[] (x, y) = e;",
                // (1,5): error CS0443: Syntax error; value expected
                // var[] (x, y) = e;
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(1, 5)
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.ElementAccessExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "var");
                            }
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
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
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_05()
        {
            UsingStatement(@"var* (x, y) = e;");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.MultiplyExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                        N(SyntaxKind.AsteriskToken);
                        N(SyntaxKind.TupleExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_06()
        {
            UsingStatement(@"var? (x, y) = e;",
                // (1,16): error CS1003: Syntax error, ':' expected
                // var? (x, y) = e;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":", ";").WithLocation(1, 16),
                // (1,16): error CS1525: Invalid expression term ';'
                // var? (x, y) = e;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(1, 16)
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.ConditionalExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.QuestionToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.TupleExpression);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "x");
                                }
                            }
                            N(SyntaxKind.CommaToken);
                            N(SyntaxKind.Argument);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "e");
                        }
                    }
                    M(SyntaxKind.ColonToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void BadTypeForDeconstruct_07()
        {
            UsingStatement(@"var?.var (x, y) = e;");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.ConditionalAccessExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.InvocationExpression);
                        {
                            N(SyntaxKind.MemberBindingExpression);
                            {
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "var");
                                }
                            }
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
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "y");
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(15934, "https://github.com/dotnet/roslyn/issues/15934")]
        public void PointerTypeInDeconstruction()
        {
            string source = @"
class C
{
    void M()
    {
        // syntax error: pointer types only permitted as an array element type in a tuple
        (int* x1, int y1) = e;

        // These are OK, because an array is a valid type in a tuple.
        (int*[] x2, int y2) = e;
        (var*[] x3, int y3) = e;

        // Multiplication in a tuple element is also OK
        (var* x4, int y4) = e;
        (var* x5, var* y5) = e;
        e = (var* x6, var* y6);
    }
}
";
            UsingTree(source).GetDiagnostics().Verify(
                // (7,10): error CS1525: Invalid expression term 'int'
                //         (int* x1, int y1) = e;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 10)
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
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.MultiplyExpression);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.AsteriskToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x1");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "y1");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "e");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.ArrayType);
                                                {
                                                    N(SyntaxKind.PointerType);
                                                    {
                                                        N(SyntaxKind.PredefinedType);
                                                        {
                                                            N(SyntaxKind.IntKeyword);
                                                        }
                                                        N(SyntaxKind.AsteriskToken);
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
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x2");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "y2");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "e");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.ArrayType);
                                                {
                                                    N(SyntaxKind.PointerType);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "var");
                                                        }
                                                        N(SyntaxKind.AsteriskToken);
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
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x3");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "y3");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "e");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.MultiplyExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.AsteriskToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x4");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.DeclarationExpression);
                                            {
                                                N(SyntaxKind.PredefinedType);
                                                {
                                                    N(SyntaxKind.IntKeyword);
                                                }
                                                N(SyntaxKind.SingleVariableDesignation);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "y4");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "e");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.MultiplyExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.AsteriskToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x5");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.MultiplyExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.AsteriskToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "y5");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "e");
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
                                        N(SyntaxKind.IdentifierToken, "e");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.TupleExpression);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.MultiplyExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.AsteriskToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "x6");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CommaToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.MultiplyExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "var");
                                                }
                                                N(SyntaxKind.AsteriskToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "y6");
                                                }
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
    }
}
