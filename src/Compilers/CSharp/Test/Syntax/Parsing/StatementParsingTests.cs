// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class StatementParsingTests : ParsingTests
    {
        public StatementParsingTests(ITestOutputHelper output) : base(output) { }

        private StatementSyntax ParseStatement(string text, int offset = 0, ParseOptions options = null)
        {
            return SyntaxFactory.ParseStatement(text, offset, options);
        }

        [Fact]
        [WorkItem(17458, "https://github.com/dotnet/roslyn/issues/17458")]
        public void ParsePrivate()
        {
            UsingStatement("private",
                // (1,1): error CS1073: Unexpected token 'private'
                // private
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "").WithArguments("private").WithLocation(1, 1),
                // (1,1): error CS1525: Invalid expression term 'private'
                // private
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "private").WithArguments("private").WithLocation(1, 1),
                // (1,1): error CS1002: ; expected
                // private
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "private").WithLocation(1, 1)
                );
            M(SyntaxKind.ExpressionStatement);
            {
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestName()
        {
            var text = "a();";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var es = (ExpressionStatementSyntax)statement;
            Assert.NotNull(es.Expression);
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.IdentifierName, ((InvocationExpressionSyntax)es.Expression).Expression.Kind());
            Assert.Equal("a()", es.Expression.ToString());
            Assert.NotEqual(default, es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDottedName()
        {
            var text = "a.b();";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var es = (ExpressionStatementSyntax)statement;
            Assert.NotNull(es.Expression);
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, ((InvocationExpressionSyntax)es.Expression).Expression.Kind());
            Assert.Equal("a.b()", es.Expression.ToString());
            Assert.NotEqual(default, es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestGenericName()
        {
            var text = "a<b>();";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);
            var es = (ExpressionStatementSyntax)statement;
            Assert.NotNull(es.Expression);
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.GenericName, ((InvocationExpressionSyntax)es.Expression).Expression.Kind());
            Assert.Equal("a<b>()", es.Expression.ToString());
            Assert.NotEqual(default, es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestGenericDotName()
        {
            var text = "a<b>.c();";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var es = (ExpressionStatementSyntax)statement;
            Assert.NotNull(es.Expression);
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, ((InvocationExpressionSyntax)es.Expression).Expression.Kind());
            Assert.Equal("a<b>.c()", es.Expression.ToString());
            Assert.NotEqual(default, es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestDotGenericName()
        {
            var text = "a.b<c>();";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var es = (ExpressionStatementSyntax)statement;
            Assert.NotNull(es.Expression);
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.SimpleMemberAccessExpression, ((InvocationExpressionSyntax)es.Expression).Expression.Kind());
            Assert.Equal("a.b<c>()", es.Expression.ToString());
            Assert.NotEqual(default, es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);
        }

        private void TestPostfixUnaryOperator(SyntaxKind kind, ParseOptions options = null)
        {
            var text = "a" + SyntaxFacts.GetText(kind) + ";";
            var statement = this.ParseStatement(text, options: options);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var es = (ExpressionStatementSyntax)statement;
            Assert.NotNull(es.Expression);
            Assert.NotEqual(default, es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);

            var opKind = SyntaxFacts.GetPostfixUnaryExpression(kind);
            Assert.Equal(opKind, es.Expression.Kind());
            var us = (PostfixUnaryExpressionSyntax)es.Expression;
            Assert.Equal("a", us.Operand.ToString());
            Assert.Equal(kind, us.OperatorToken.Kind());
        }

        [Fact]
        public void TestPostfixUnaryOperators()
        {
            TestPostfixUnaryOperator(SyntaxKind.PlusPlusToken);
            TestPostfixUnaryOperator(SyntaxKind.MinusMinusToken);
            TestPostfixUnaryOperator(SyntaxKind.ExclamationToken, TestOptions.Regular8);
        }

        [Fact]
        public void TestLocalDeclarationStatement()
        {
            var text = "T a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithVar()
        {
            // note: semantically this would require an initializer, but we don't know 
            // about var being special until we bind.
            var text = "var a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("var", ds.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, ds.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)ds.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithTuple()
        {
            var text = "(int, int) a;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular);

            (text).ToString();

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("(int, int)", ds.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.TupleType, ds.Declaration.Type.Kind());

            var tt = (TupleTypeSyntax)ds.Declaration.Type;

            Assert.Equal(SyntaxKind.PredefinedType, tt.Elements[0].Type.Kind());
            Assert.Equal(SyntaxKind.None, tt.Elements[1].Identifier.Kind());
            Assert.Equal(2, tt.Elements.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithNamedTuple()
        {
            var text = "(T x, (U k, V l, W m) y) a;";
            var statement = this.ParseStatement(text);

            (text).ToString();

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("(T x, (U k, V l, W m) y)", ds.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.TupleType, ds.Declaration.Type.Kind());

            var tt = (TupleTypeSyntax)ds.Declaration.Type;

            Assert.Equal(SyntaxKind.IdentifierName, tt.Elements[0].Type.Kind());
            Assert.Equal("y", tt.Elements[1].Identifier.ToString());
            Assert.Equal(2, tt.Elements.Count);

            tt = (TupleTypeSyntax)tt.Elements[1].Type;

            Assert.Equal("(U k, V l, W m)", tt.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, tt.Elements[0].Type.Kind());
            Assert.Equal("l", tt.Elements[1].Identifier.ToString());
            Assert.Equal(3, tt.Elements.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithDynamic()
        {
            // note: semantically this would require an initializer, but we don't know 
            // about dynamic being special until we bind.
            var text = "dynamic a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("dynamic", ds.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, ds.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)ds.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithGenericType()
        {
            var text = "T<a> b;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T<a>", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("b", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithDottedType()
        {
            var text = "T.X.Y a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T.X.Y", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithMixedType()
        {
            var text = "T<t>.X<x>.Y<y> a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T<t>.X<x>.Y<y>", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithArrayType()
        {
            var text = "T[][,][,,] a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T[][,][,,]", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithPointerType()
        {
            var text = "T* a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T*", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithNullableType()
        {
            var text = "T? a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T?", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithMultipleVariables()
        {
            var text = "T a, b, c;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(3, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.Declaration.Variables[1].Identifier);
            Assert.Equal("b", ds.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[1].ArgumentList);
            Assert.Null(ds.Declaration.Variables[1].Initializer);

            Assert.NotEqual(default, ds.Declaration.Variables[2].Identifier);
            Assert.Equal("c", ds.Declaration.Variables[2].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[2].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithInitializer()
        {
            var text = "T a = b;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithMultipleVariablesAndInitializers()
        {
            var text = "T a = va, b = vb, c = vc;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(3, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotEqual(default, ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("va", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.Declaration.Variables[1].Identifier);
            Assert.Equal("b", ds.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[1].ArgumentList);
            Assert.NotEqual(default, ds.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[1].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("vb", ds.Declaration.Variables[1].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.Declaration.Variables[2].Identifier);
            Assert.Equal("c", ds.Declaration.Variables[2].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[2].ArgumentList);
            Assert.NotEqual(default, ds.Declaration.Variables[2].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[2].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[2].Initializer.Value);
            Assert.Equal("vc", ds.Declaration.Variables[2].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLocalDeclarationStatementWithArrayInitializer()
        {
            var text = "T a = {b, c};";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ArrayInitializerExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal("{b, c}", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestConstLocalDeclarationStatement()
        {
            var text = "const T a = b;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(1, ds.Modifiers.Count);
            Assert.Equal(SyntaxKind.ConstKeyword, ds.Modifiers[0].Kind());
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestStaticLocalDeclarationStatement()
        {
            var text = "static T a = b;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(1, statement.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_BadMemberFlag, statement.Errors()[0].Code);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(1, ds.Modifiers.Count);
            Assert.Equal(SyntaxKind.StaticKeyword, ds.Modifiers[0].Kind());
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestReadOnlyLocalDeclarationStatement()
        {
            var text = "readonly T a = b;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(1, statement.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_BadMemberFlag, statement.Errors()[0].Code);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(1, ds.Modifiers.Count);
            Assert.Equal(SyntaxKind.ReadOnlyKeyword, ds.Modifiers[0].Kind());
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestVolatileLocalDeclarationStatement()
        {
            var text = "volatile T a = b;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(1, statement.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_BadMemberFlag, statement.Errors()[0].Code);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(1, ds.Modifiers.Count);
            Assert.Equal(SyntaxKind.VolatileKeyword, ds.Modifiers[0].Kind());
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestRefLocalDeclarationStatement()
        {
            var text = "ref T a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("ref T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestRefLocalDeclarationStatementWithInitializer()
        {
            var text = "ref T a = ref b;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("ref T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            var initializer = ds.Declaration.Variables[0].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotEqual(default, initializer.EqualsToken);
            Assert.False(initializer.EqualsToken.IsMissing);
            Assert.Equal(SyntaxKind.RefExpression, initializer.Value.Kind());
            Assert.Equal("ref b", initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestRefLocalDeclarationStatementWithMultipleInitializers()
        {
            var text = "ref T a = ref b, c = ref d;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("ref T", ds.Declaration.Type.ToString());
            Assert.Equal(2, ds.Declaration.Variables.Count);

            Assert.NotEqual(default, ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            var initializer = ds.Declaration.Variables[0].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotEqual(default, initializer.EqualsToken);
            Assert.False(initializer.EqualsToken.IsMissing);
            Assert.Equal(SyntaxKind.RefExpression, initializer.Value.Kind());
            Assert.Equal("ref b", initializer.Value.ToString());

            Assert.NotEqual(default, ds.Declaration.Variables[1].Identifier);
            Assert.Equal("c", ds.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[1].ArgumentList);
            initializer = ds.Declaration.Variables[1].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotEqual(default, initializer.EqualsToken);
            Assert.False(initializer.EqualsToken.IsMissing);
            Assert.Equal(SyntaxKind.RefExpression, initializer.Value.Kind());
            Assert.Equal("ref d", initializer.Value.ToString());

            Assert.NotEqual(default, ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestFixedStatement()
        {
            var text = "fixed(T a = b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.FixedStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (FixedStatementSyntax)statement;
            Assert.NotEqual(default, fs.FixedKeyword);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotNull(fs.Declaration);
            Assert.Equal(SyntaxKind.VariableDeclaration, fs.Declaration.Kind());
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.Equal("a = b", fs.Declaration.Variables[0].ToString());
            Assert.NotNull(fs.Statement);
            Assert.Equal(SyntaxKind.Block, fs.Statement.Kind());
            Assert.Equal("{ }", fs.Statement.ToString());
        }

        [Fact]
        public void TestFixedVarStatement()
        {
            var text = "fixed(var a = b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.FixedStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (FixedStatementSyntax)statement;
            Assert.NotEqual(default, fs.FixedKeyword);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotNull(fs.Declaration);
            Assert.Equal(SyntaxKind.VariableDeclaration, fs.Declaration.Kind());
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("var", fs.Declaration.Type.ToString());
            Assert.True(fs.Declaration.Type.IsVar);
            Assert.Equal(SyntaxKind.IdentifierName, fs.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)fs.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.Equal("a = b", fs.Declaration.Variables[0].ToString());
            Assert.NotNull(fs.Statement);
            Assert.Equal(SyntaxKind.Block, fs.Statement.Kind());
            Assert.Equal("{ }", fs.Statement.ToString());
        }

        [Fact]
        public void TestFixedStatementWithMultipleVariables()
        {
            var text = "fixed(T a = b, c = d) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.FixedStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (FixedStatementSyntax)statement;
            Assert.NotEqual(default, fs.FixedKeyword);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotNull(fs.Declaration);
            Assert.Equal(SyntaxKind.VariableDeclaration, fs.Declaration.Kind());
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(2, fs.Declaration.Variables.Count);
            Assert.Equal("a = b", fs.Declaration.Variables[0].ToString());
            Assert.Equal("c = d", fs.Declaration.Variables[1].ToString());
            Assert.NotNull(fs.Statement);
            Assert.Equal(SyntaxKind.Block, fs.Statement.Kind());
            Assert.Equal("{ }", fs.Statement.ToString());
        }

        [Fact]
        public void TestEmptyStatement()
        {
            var text = ";";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.EmptyStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var es = (EmptyStatementSyntax)statement;
            Assert.NotEqual(default, es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestLabeledStatement()
        {
            var text = "label: ;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LabeledStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ls = (LabeledStatementSyntax)statement;
            Assert.NotEqual(default, ls.Identifier);
            Assert.Equal("label", ls.Identifier.ToString());
            Assert.NotEqual(default, ls.ColonToken);
            Assert.Equal(SyntaxKind.ColonToken, ls.ColonToken.Kind());
            Assert.NotNull(ls.Statement);
            Assert.Equal(SyntaxKind.EmptyStatement, ls.Statement.Kind());
            Assert.Equal(";", ls.Statement.ToString());
        }

        [Fact]
        public void TestBreakStatement()
        {
            var text = "break;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.BreakStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var b = (BreakStatementSyntax)statement;
            Assert.NotEqual(default, b.BreakKeyword);
            Assert.False(b.BreakKeyword.IsMissing);
            Assert.Equal(SyntaxKind.BreakKeyword, b.BreakKeyword.Kind());
            Assert.NotEqual(default, b.SemicolonToken);
            Assert.False(b.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestContinueStatement()
        {
            var text = "continue;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ContinueStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var cs = (ContinueStatementSyntax)statement;
            Assert.NotEqual(default, cs.ContinueKeyword);
            Assert.False(cs.ContinueKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ContinueKeyword, cs.ContinueKeyword.Kind());
            Assert.NotEqual(default, cs.SemicolonToken);
            Assert.False(cs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestGotoStatement()
        {
            var text = "goto label;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.GotoStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var gs = (GotoStatementSyntax)statement;
            Assert.NotEqual(default, gs.GotoKeyword);
            Assert.False(gs.GotoKeyword.IsMissing);
            Assert.Equal(SyntaxKind.GotoKeyword, gs.GotoKeyword.Kind());
            Assert.Equal(SyntaxKind.None, gs.CaseOrDefaultKeyword.Kind());
            Assert.NotNull(gs.Expression);
            Assert.Equal("label", gs.Expression.ToString());
            Assert.NotEqual(default, gs.SemicolonToken);
            Assert.False(gs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestGotoCaseStatement()
        {
            var text = "goto case label;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.GotoCaseStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var gs = (GotoStatementSyntax)statement;
            Assert.NotEqual(default, gs.GotoKeyword);
            Assert.False(gs.GotoKeyword.IsMissing);
            Assert.Equal(SyntaxKind.GotoKeyword, gs.GotoKeyword.Kind());
            Assert.NotEqual(default, gs.CaseOrDefaultKeyword);
            Assert.False(gs.CaseOrDefaultKeyword.IsMissing);
            Assert.Equal(SyntaxKind.CaseKeyword, gs.CaseOrDefaultKeyword.Kind());
            Assert.NotNull(gs.Expression);
            Assert.Equal("label", gs.Expression.ToString());
            Assert.NotEqual(default, gs.SemicolonToken);
            Assert.False(gs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestGotoDefault()
        {
            var text = "goto default;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.GotoDefaultStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var gs = (GotoStatementSyntax)statement;
            Assert.NotEqual(default, gs.GotoKeyword);
            Assert.False(gs.GotoKeyword.IsMissing);
            Assert.Equal(SyntaxKind.GotoKeyword, gs.GotoKeyword.Kind());
            Assert.NotEqual(default, gs.CaseOrDefaultKeyword);
            Assert.False(gs.CaseOrDefaultKeyword.IsMissing);
            Assert.Equal(SyntaxKind.DefaultKeyword, gs.CaseOrDefaultKeyword.Kind());
            Assert.Null(gs.Expression);
            Assert.NotEqual(default, gs.SemicolonToken);
            Assert.False(gs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestReturn()
        {
            var text = "return;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ReturnStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var rs = (ReturnStatementSyntax)statement;
            Assert.NotEqual(default, rs.ReturnKeyword);
            Assert.False(rs.ReturnKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ReturnKeyword, rs.ReturnKeyword.Kind());
            Assert.Null(rs.Expression);
            Assert.NotEqual(default, rs.SemicolonToken);
            Assert.False(rs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestReturnExpression()
        {
            var text = "return a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ReturnStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var rs = (ReturnStatementSyntax)statement;
            Assert.NotEqual(default, rs.ReturnKeyword);
            Assert.False(rs.ReturnKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ReturnKeyword, rs.ReturnKeyword.Kind());
            Assert.NotNull(rs.Expression);
            Assert.Equal("a", rs.Expression.ToString());
            Assert.NotEqual(default, rs.SemicolonToken);
            Assert.False(rs.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestYieldReturnExpression()
        {
            var text = "yield return a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.YieldReturnStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ys = (YieldStatementSyntax)statement;
            Assert.NotEqual(default, ys.YieldKeyword);
            Assert.False(ys.YieldKeyword.IsMissing);
            Assert.Equal(SyntaxKind.YieldKeyword, ys.YieldKeyword.Kind());
            Assert.NotEqual(default, ys.ReturnOrBreakKeyword);
            Assert.False(ys.ReturnOrBreakKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ReturnKeyword, ys.ReturnOrBreakKeyword.Kind());
            Assert.NotNull(ys.Expression);
            Assert.Equal("a", ys.Expression.ToString());
            Assert.NotEqual(default, ys.SemicolonToken);
            Assert.False(ys.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestYieldBreakExpression()
        {
            var text = "yield break;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.YieldBreakStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ys = (YieldStatementSyntax)statement;
            Assert.NotEqual(default, ys.YieldKeyword);
            Assert.False(ys.YieldKeyword.IsMissing);
            Assert.Equal(SyntaxKind.YieldKeyword, ys.YieldKeyword.Kind());
            Assert.NotEqual(default, ys.ReturnOrBreakKeyword);
            Assert.False(ys.ReturnOrBreakKeyword.IsMissing);
            Assert.Equal(SyntaxKind.BreakKeyword, ys.ReturnOrBreakKeyword.Kind());
            Assert.Null(ys.Expression);
            Assert.NotEqual(default, ys.SemicolonToken);
            Assert.False(ys.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestThrow()
        {
            var text = "throw;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ThrowStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (ThrowStatementSyntax)statement;
            Assert.NotEqual(default, ts.ThrowKeyword);
            Assert.False(ts.ThrowKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ThrowKeyword, ts.ThrowKeyword.ContextualKind());
            Assert.Null(ts.Expression);
            Assert.NotEqual(default, ts.SemicolonToken);
            Assert.False(ts.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestThrowExpression()
        {
            var text = "throw a;";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ThrowStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (ThrowStatementSyntax)statement;
            Assert.NotEqual(default, ts.ThrowKeyword);
            Assert.False(ts.ThrowKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ThrowKeyword, ts.ThrowKeyword.ContextualKind());
            Assert.NotNull(ts.Expression);
            Assert.Equal("a", ts.Expression.ToString());
            Assert.NotEqual(default, ts.SemicolonToken);
            Assert.False(ts.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestTryCatch()
        {
            var text = "try { } catch(T e) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.TryStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (TryStatementSyntax)statement;
            Assert.NotEqual(default, ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(1, ts.Catches.Count);
            Assert.NotEqual(default, ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotEqual(default, ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.NotEqual(default, ts.Catches[0].Declaration.Identifier);
            Assert.Equal("e", ts.Catches[0].Declaration.Identifier.ToString());
            Assert.NotEqual(default, ts.Catches[0].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[0].Block);

            Assert.Null(ts.Finally);
        }

        [Fact]
        public void TestTryCatchWithNoExceptionName()
        {
            var text = "try { } catch(T) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.TryStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (TryStatementSyntax)statement;
            Assert.NotEqual(default, ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(1, ts.Catches.Count);
            Assert.NotEqual(default, ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotEqual(default, ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.None, ts.Catches[0].Declaration.Identifier.Kind());
            Assert.NotEqual(default, ts.Catches[0].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[0].Block);

            Assert.Null(ts.Finally);
        }

        [Fact]
        public void TestTryCatchWithNoExceptionDeclaration()
        {
            var text = "try { } catch { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.TryStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (TryStatementSyntax)statement;
            Assert.NotEqual(default, ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(1, ts.Catches.Count);
            Assert.NotEqual(default, ts.Catches[0].CatchKeyword);
            Assert.Null(ts.Catches[0].Declaration);
            Assert.NotNull(ts.Catches[0].Block);

            Assert.Null(ts.Finally);
        }

        [Fact]
        public void TestTryCatchWithMultipleCatches()
        {
            var text = "try { } catch(T e) { } catch(T2) { } catch { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.TryStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (TryStatementSyntax)statement;
            Assert.NotEqual(default, ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(3, ts.Catches.Count);

            Assert.NotEqual(default, ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotEqual(default, ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.NotEqual(default, ts.Catches[0].Declaration.Identifier);
            Assert.Equal("e", ts.Catches[0].Declaration.Identifier.ToString());
            Assert.NotEqual(default, ts.Catches[0].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[0].Block);

            Assert.NotEqual(default, ts.Catches[1].CatchKeyword);
            Assert.NotNull(ts.Catches[1].Declaration);
            Assert.NotEqual(default, ts.Catches[1].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[1].Declaration.Type);
            Assert.Equal("T2", ts.Catches[1].Declaration.Type.ToString());
            Assert.NotEqual(default, ts.Catches[1].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[1].Block);

            Assert.NotEqual(default, ts.Catches[2].CatchKeyword);
            Assert.Null(ts.Catches[2].Declaration);
            Assert.NotNull(ts.Catches[2].Block);

            Assert.Null(ts.Finally);
        }

        [Fact]
        public void TestTryFinally()
        {
            var text = "try { } finally { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.TryStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (TryStatementSyntax)statement;
            Assert.NotEqual(default, ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(0, ts.Catches.Count);

            Assert.NotNull(ts.Finally);
            Assert.NotEqual(default, ts.Finally.FinallyKeyword);
            Assert.NotNull(ts.Finally.Block);
        }

        [Fact]
        public void TestTryCatchWithMultipleCatchesAndFinally()
        {
            var text = "try { } catch(T e) { } catch(T2) { } catch { } finally { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.TryStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ts = (TryStatementSyntax)statement;
            Assert.NotEqual(default, ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(3, ts.Catches.Count);

            Assert.NotEqual(default, ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotEqual(default, ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.NotEqual(default, ts.Catches[0].Declaration.Identifier);
            Assert.Equal("e", ts.Catches[0].Declaration.Identifier.ToString());
            Assert.NotEqual(default, ts.Catches[0].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[0].Block);

            Assert.NotEqual(default, ts.Catches[1].CatchKeyword);
            Assert.NotNull(ts.Catches[1].Declaration);
            Assert.NotEqual(default, ts.Catches[1].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[1].Declaration.Type);
            Assert.Equal("T2", ts.Catches[1].Declaration.Type.ToString());
            Assert.NotEqual(default, ts.Catches[1].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[1].Block);

            Assert.NotEqual(default, ts.Catches[2].CatchKeyword);
            Assert.Null(ts.Catches[2].Declaration);
            Assert.NotNull(ts.Catches[2].Block);

            Assert.NotNull(ts.Finally);
            Assert.NotEqual(default, ts.Finally.FinallyKeyword);
            Assert.NotNull(ts.Finally.Block);
        }

        [Fact]
        public void TestChecked()
        {
            var text = "checked { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.CheckedStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var cs = (CheckedStatementSyntax)statement;
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.CheckedKeyword, cs.Keyword.Kind());
            Assert.NotNull(cs.Block);
        }

        [Fact]
        public void TestUnchecked()
        {
            var text = "unchecked { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UncheckedStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var cs = (CheckedStatementSyntax)statement;
            Assert.NotEqual(default, cs.Keyword);
            Assert.Equal(SyntaxKind.UncheckedKeyword, cs.Keyword.Kind());
            Assert.NotNull(cs.Block);
        }

        [Fact]
        public void TestUnsafe()
        {
            var text = "unsafe { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UnsafeStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UnsafeStatementSyntax)statement;
            Assert.NotEqual(default, us.UnsafeKeyword);
            Assert.Equal(SyntaxKind.UnsafeKeyword, us.UnsafeKeyword.Kind());
            Assert.NotNull(us.Block);
        }

        [Fact]
        public void TestWhile()
        {
            var text = "while(a) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.WhileStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ws = (WhileStatementSyntax)statement;
            Assert.NotEqual(default, ws.WhileKeyword);
            Assert.Equal(SyntaxKind.WhileKeyword, ws.WhileKeyword.Kind());
            Assert.NotEqual(default, ws.OpenParenToken);
            Assert.NotNull(ws.Condition);
            Assert.NotEqual(default, ws.CloseParenToken);
            Assert.Equal("a", ws.Condition.ToString());
            Assert.NotNull(ws.Statement);
            Assert.Equal(SyntaxKind.Block, ws.Statement.Kind());
        }

        [Fact]
        public void TestDoWhile()
        {
            var text = "do { } while (a);";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.DoStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (DoStatementSyntax)statement;
            Assert.NotEqual(default, ds.DoKeyword);
            Assert.Equal(SyntaxKind.DoKeyword, ds.DoKeyword.Kind());
            Assert.NotNull(ds.Statement);
            Assert.NotEqual(default, ds.WhileKeyword);
            Assert.Equal(SyntaxKind.WhileKeyword, ds.WhileKeyword.Kind());
            Assert.Equal(SyntaxKind.Block, ds.Statement.Kind());
            Assert.NotEqual(default, ds.OpenParenToken);
            Assert.NotNull(ds.Condition);
            Assert.NotEqual(default, ds.CloseParenToken);
            Assert.Equal("a", ds.Condition.ToString());
            Assert.NotEqual(default, ds.SemicolonToken);
        }

        [Fact]
        public void TestFor()
        {
            var text = "for(;;) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithVariableDeclaration()
        {
            var text = "for(T a = 0;;) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithVarDeclaration()
        {
            var text = "for(var a = 0;;) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("var", fs.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, fs.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)fs.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithMultipleVariableDeclarations()
        {
            var text = "for(T a = 0, b = 1;;) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(2, fs.Declaration.Variables.Count);

            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, fs.Declaration.Variables[1].Identifier);
            Assert.Equal("b", fs.Declaration.Variables[1].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[1].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("1", fs.Declaration.Variables[1].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact, CompilerTrait(CompilerFeature.RefLocalsReturns)]
        public void TestForWithRefVariableDeclaration()
        {
            var text = "for(ref T a = ref b, c = ref d;;) { }";
            var statement = this.ParseStatement(text);

            UsingNode(statement);
            N(SyntaxKind.ForStatement);
            {
                N(SyntaxKind.ForKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "T");
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "d");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.SemicolonToken);
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.Block);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
        }

        [Fact]
        public void TestForWithVariableInitializer()
        {
            var text = "for(a = 0;;) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(1, fs.Initializers.Count);
            Assert.Equal("a = 0", fs.Initializers[0].ToString());

            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithMultipleVariableInitializers()
        {
            var text = "for(a = 0, b = 1;;) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(2, fs.Initializers.Count);
            Assert.Equal("a = 0", fs.Initializers[0].ToString());
            Assert.Equal("b = 1", fs.Initializers[1].ToString());

            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithCondition()
        {
            var text = "for(; a;) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotEqual(default, fs.FirstSemicolonToken);

            Assert.NotNull(fs.Condition);
            Assert.Equal("a", fs.Condition.ToString());

            Assert.NotEqual(default, fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithIncrementor()
        {
            var text = "for(; ; a++) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);

            Assert.Equal(1, fs.Incrementors.Count);
            Assert.Equal("a++", fs.Incrementors[0].ToString());

            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithMultipleIncrementors()
        {
            var text = "for(; ; a++, b++) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotEqual(default, fs.SecondSemicolonToken);

            Assert.Equal(2, fs.Incrementors.Count);
            Assert.Equal("a++", fs.Incrementors[0].ToString());
            Assert.Equal("b++", fs.Incrementors[1].ToString());

            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithDeclarationConditionAndIncrementor()
        {
            var text = "for(T a = 0; a < 10; a++) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);

            Assert.NotEqual(default, fs.FirstSemicolonToken);
            Assert.NotNull(fs.Condition);
            Assert.Equal("a < 10", fs.Condition.ToString());

            Assert.NotEqual(default, fs.SecondSemicolonToken);

            Assert.Equal(1, fs.Incrementors.Count);
            Assert.Equal("a++", fs.Incrementors[0].ToString());

            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForEach()
        {
            var text = "foreach(T a in b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForEachStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForEachStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());

            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.NotEqual(default, fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForAsForEach()
        {
            var text = "for(T a in b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForEachStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(1, statement.Errors().Length);

            var fs = (ForEachStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());
            Assert.True(fs.ForEachKeyword.IsMissing);
            Assert.Equal(1, fs.ForEachKeyword.TrailingTrivia.Count);
            Assert.Equal(SyntaxKind.SkippedTokensTrivia, fs.ForEachKeyword.TrailingTrivia[0].Kind());
            Assert.Equal("for", fs.ForEachKeyword.TrailingTrivia[0].ToString());

            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.NotEqual(default, fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForEachWithVar()
        {
            var text = "foreach(var a in b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForEachStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForEachStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());

            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("var", fs.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, fs.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)fs.Type).Identifier.Kind());
            Assert.NotEqual(default, fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestIf()
        {
            var text = "if (a) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.IfStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (IfStatementSyntax)statement;
            Assert.NotEqual(default, ss.IfKeyword);
            Assert.Equal(SyntaxKind.IfKeyword, ss.IfKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Condition);
            Assert.Equal("a", ss.Condition.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotNull(ss.Statement);

            Assert.Null(ss.Else);
        }

        [Fact]
        public void TestIfElse()
        {
            var text = "if (a) { } else { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.IfStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (IfStatementSyntax)statement;
            Assert.NotEqual(default, ss.IfKeyword);
            Assert.Equal(SyntaxKind.IfKeyword, ss.IfKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Condition);
            Assert.Equal("a", ss.Condition.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotNull(ss.Statement);

            Assert.NotNull(ss.Else);
            Assert.NotEqual(default, ss.Else.ElseKeyword);
            Assert.Equal(SyntaxKind.ElseKeyword, ss.Else.ElseKeyword.Kind());
            Assert.NotNull(ss.Else.Statement);
        }

        [Fact]
        public void TestIfElseIf()
        {
            var text = "if (a) { } else if (b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.IfStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (IfStatementSyntax)statement;
            Assert.NotEqual(default, ss.IfKeyword);
            Assert.Equal(SyntaxKind.IfKeyword, ss.IfKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Condition);
            Assert.Equal("a", ss.Condition.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotNull(ss.Statement);

            Assert.NotNull(ss.Else);
            Assert.NotEqual(default, ss.Else.ElseKeyword);
            Assert.Equal(SyntaxKind.ElseKeyword, ss.Else.ElseKeyword.Kind());
            Assert.NotNull(ss.Else.Statement);

            var subIf = (IfStatementSyntax)ss.Else.Statement;
            Assert.NotEqual(default, subIf.IfKeyword);
            Assert.Equal(SyntaxKind.IfKeyword, subIf.IfKeyword.Kind());
            Assert.NotNull(subIf.Condition);
            Assert.Equal("b", subIf.Condition.ToString());
            Assert.NotEqual(default, subIf.CloseParenToken);
            Assert.NotNull(subIf.Statement);
        }

        [Fact]
        public void TestLock()
        {
            var text = "lock (a) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LockStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ls = (LockStatementSyntax)statement;
            Assert.NotEqual(default, ls.LockKeyword);
            Assert.Equal(SyntaxKind.LockKeyword, ls.LockKeyword.Kind());
            Assert.NotEqual(default, ls.OpenParenToken);
            Assert.NotNull(ls.Expression);
            Assert.Equal("a", ls.Expression.ToString());
            Assert.NotEqual(default, ls.CloseParenToken);
            Assert.NotNull(ls.Statement);
        }

        [Fact]
        public void TestSwitch()
        {
            var text = "switch (a) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.SwitchStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);
            var diags = statement.ErrorsAndWarnings();
            Assert.Equal(0, diags.Length);

            var ss = (SwitchStatementSyntax)statement;
            Assert.NotEqual(default, ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotEqual(default, ss.OpenBraceToken);
            Assert.Equal(0, ss.Sections.Count);
            Assert.NotEqual(default, ss.CloseBraceToken);
        }

        [Fact]
        public void TestSwitchWithCase()
        {
            var text = "switch (a) { case b:; }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.SwitchStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (SwitchStatementSyntax)statement;
            Assert.NotEqual(default, ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotEqual(default, ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);
            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotEqual(default, ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.NotEqual(default, caseLabelSyntax.ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotEqual(default, ss.CloseBraceToken);
        }

        [Fact]
        public void TestSwitchWithMultipleCases()
        {
            var text = "switch (a) { case b:; case c:; }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.SwitchStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (SwitchStatementSyntax)statement;
            Assert.NotEqual(default, ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotEqual(default, ss.OpenBraceToken);

            Assert.Equal(2, ss.Sections.Count);

            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotEqual(default, ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.NotEqual(default, caseLabelSyntax.ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.Equal(1, ss.Sections[1].Labels.Count);
            Assert.NotEqual(default, ss.Sections[1].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[1].Labels[0].Keyword.Kind());
            var caseLabelSyntax2 = ss.Sections[1].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax2);
            Assert.NotNull(caseLabelSyntax2.Value);
            Assert.Equal("c", caseLabelSyntax2.Value.ToString());
            Assert.NotEqual(default, caseLabelSyntax2.ColonToken);
            Assert.Equal(1, ss.Sections[1].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotEqual(default, ss.CloseBraceToken);
        }

        [Fact]
        public void TestSwitchWithDefaultCase()
        {
            var text = "switch (a) { default:; }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.SwitchStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (SwitchStatementSyntax)statement;
            Assert.NotEqual(default, ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotEqual(default, ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);

            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotEqual(default, ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.DefaultKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            Assert.Equal(SyntaxKind.DefaultSwitchLabel, ss.Sections[0].Labels[0].Kind());
            Assert.NotEqual(default, ss.Sections[0].Labels[0].ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotEqual(default, ss.CloseBraceToken);
        }

        [Fact]
        public void TestSwitchWithMultipleLabelsOnOneCase()
        {
            var text = "switch (a) { case b: case c:; }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.SwitchStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (SwitchStatementSyntax)statement;
            Assert.NotEqual(default, ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotEqual(default, ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);

            Assert.Equal(2, ss.Sections[0].Labels.Count);
            Assert.NotEqual(default, ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.NotEqual(default, ss.Sections[0].Labels[1].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[1].Keyword.Kind());
            var caseLabelSyntax2 = ss.Sections[0].Labels[1] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax2);
            Assert.NotNull(caseLabelSyntax2.Value);
            Assert.Equal("c", caseLabelSyntax2.Value.ToString());
            Assert.NotEqual(default, ss.Sections[0].Labels[0].ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotEqual(default, ss.CloseBraceToken);
        }

        [Fact]
        public void TestSwitchWithMultipleStatementsOnOneCase()
        {
            var text = "switch (a) { case b: s1(); s2(); }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.SwitchStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ss = (SwitchStatementSyntax)statement;
            Assert.NotEqual(default, ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotEqual(default, ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotEqual(default, ss.CloseParenToken);
            Assert.NotEqual(default, ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);

            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotEqual(default, ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.Equal(2, ss.Sections[0].Statements.Count);
            Assert.Equal("s1();", ss.Sections[0].Statements[0].ToString());
            Assert.Equal("s2();", ss.Sections[0].Statements[1].ToString());

            Assert.NotEqual(default, ss.CloseBraceToken);
        }

        [Fact]
        public void TestUsingWithExpression()
        {
            var text = "using (a) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotEqual(default, us.OpenParenToken);
            Assert.Null(us.Declaration);
            Assert.NotNull(us.Expression);
            Assert.Equal("a", us.Expression.ToString());
            Assert.NotEqual(default, us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingWithDeclaration()
        {
            var text = "using (T a = b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotEqual(default, us.OpenParenToken);

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("T", us.Declaration.Type.ToString());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Null(us.Expression);

            Assert.NotEqual(default, us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingVarWithDeclaration()
        {
            var text = "using T a = b;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("T", us.Declaration.Type.ToString());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());
            Assert.NotEqual(default, us.SemicolonToken);
        }

        [Fact]
        public void TestUsingVarWithDeclarationTree()
        {
            UsingStatement(@"using T a = b;", options: TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName, "T");
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken);
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName, "b");
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestUsingWithVarDeclaration()
        {
            var text = "using (var a = b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotEqual(default, us.OpenParenToken);

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("var", us.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, us.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)us.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Null(us.Expression);

            Assert.NotEqual(default, us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingVarWithVarDeclaration()
        {
            var text = "using var a = b;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("var", us.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, us.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)us.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());
        }

        [Fact]
        [WorkItem(36413, "https://github.com/dotnet/roslyn/issues/36413")]
        public void TestUsingVarWithInvalidDeclaration()
        {
            var text = "using public readonly var a = b;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(2, statement.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_BadMemberFlag, statement.Errors()[0].Code);
            Assert.Equal("public", statement.Errors()[0].Arguments[0]);
            Assert.Equal((int)ErrorCode.ERR_BadMemberFlag, statement.Errors()[1].Code);
            Assert.Equal("readonly", statement.Errors()[1].Arguments[0]);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("var", us.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, us.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)us.Declaration.Type).Identifier.Kind());
            Assert.Equal(2, us.Modifiers.Count);
            Assert.Equal("public", us.Modifiers[0].ToString());
            Assert.Equal("readonly", us.Modifiers[1].ToString());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());
        }

        [Fact]
        public void TestUsingVarWithVarDeclarationTree()
        {
            UsingStatement(@"using var a = b;", options: TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName, "var");
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName, "b");
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestAwaitUsingVarWithDeclarationTree()
        {
            UsingStatement(@"await using T a = b;", TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName, "T");
                    {
                        N(SyntaxKind.IdentifierToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName, "b");
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestAwaitUsingWithVarDeclaration()
        {
            var text = "await using var a = b;";
            var statement = this.ParseStatement(text, 0, TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.AwaitKeyword);
            Assert.Equal(SyntaxKind.AwaitKeyword, us.AwaitKeyword.ContextualKind());
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("var", us.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, us.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)us.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());
        }

        [Fact]
        public void TestAwaitUsingVarWithVarDeclarationTree()
        {
            UsingStatement(@"await using var a = b;", TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.AwaitKeyword);
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName, "var");
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName, "b");
                            {
                                N(SyntaxKind.IdentifierToken);
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact, WorkItem(30565, "https://github.com/dotnet/roslyn/issues/30565")]
        public void AwaitUsingVarWithVarDecl_Reversed()
        {
            UsingTree(@"
class C
{
    async void M()
    {
        using await var x = null;
    }
}
",
                // (6,15): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         using await var x = null;
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(6, 15),
                // (6,25): error CS1002: ; expected
                //         using await var x = null;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "x").WithLocation(6, 25));
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
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleAssignmentExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "x");
                                    }
                                    N(SyntaxKind.EqualsToken);
                                    N(SyntaxKind.NullLiteralExpression);
                                    {
                                        N(SyntaxKind.NullKeyword);
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
        public void TestAwaitUsingVarWithVarAndNoUsingDeclarationTree()
        {
            UsingStatement(@"await var a = b;", TestOptions.Regular8,
                // (1,1): error CS1073: Unexpected token 'a'
                // await var a = b;
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "await var ").WithArguments("a").WithLocation(1, 1),
                // (1,11): error CS1002: ; expected
                // await var a = b;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "a").WithLocation(1, 11));

            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.AwaitExpression);
                {
                    N(SyntaxKind.AwaitKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void TestUsingWithDeclarationWithMultipleVariables()
        {
            var text = "using (T a = b, c = d) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotEqual(default, us.OpenParenToken);

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("T", us.Declaration.Type.ToString());

            Assert.Equal(2, us.Declaration.Variables.Count);

            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, us.Declaration.Variables[1].Identifier);
            Assert.Equal("c", us.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[1].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[1].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("d", us.Declaration.Variables[1].Initializer.Value.ToString());

            Assert.Null(us.Expression);

            Assert.NotEqual(default, us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingVarWithDeclarationWithMultipleVariables()
        {
            var text = "using T a = b, c = d;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("T", us.Declaration.Type.ToString());

            Assert.Equal(2, us.Declaration.Variables.Count);

            Assert.NotEqual(default, us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotEqual(default, us.Declaration.Variables[1].Identifier);
            Assert.Equal("c", us.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[1].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[1].Initializer);
            Assert.NotEqual(default, us.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("d", us.Declaration.Variables[1].Initializer.Value.ToString());
        }

        [Fact]
        public void TestUsingVarWithDeclarationMultipleVariablesTree()
        {
            UsingStatement(@"using T a = b, c = d;", options: TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName, "T");
                    {
                        N(SyntaxKind.IdentifierToken, "T");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName, "b");
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName, "d");
                            {
                                N(SyntaxKind.IdentifierToken, "d");
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestUsingSpecialCase1()
        {
            var text = "using (f ? x = a : x = b) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotEqual(default, us.OpenParenToken);
            Assert.Null(us.Declaration);
            Assert.NotNull(us.Expression);
            Assert.Equal("f ? x = a : x = b", us.Expression.ToString());
            Assert.NotEqual(default, us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingVarSpecialCase1()
        {
            var text = "using var x = f ? a : b;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.Declaration);
            Assert.Equal("var x = f ? a : b", us.Declaration.ToString());
        }

        [Fact]
        public void TestUsingVarSpecialCase1Tree()
        {
            UsingStatement(@"using var x = f ? a : b;", options: TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName, "var");
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.ConditionalExpression);
                            {
                                N(SyntaxKind.IdentifierName, "f");
                                {
                                    N(SyntaxKind.IdentifierToken, "f");
                                }
                                N(SyntaxKind.QuestionToken);
                                N(SyntaxKind.IdentifierName, "a");
                                {
                                    N(SyntaxKind.IdentifierToken, "a");
                                }
                                N(SyntaxKind.ColonToken);
                                N(SyntaxKind.IdentifierName, "b");
                                {
                                    N(SyntaxKind.IdentifierToken, "b");
                                }
                            }
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestUsingSpecialCase2()
        {
            var text = "using (f ? x = a) { }";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotEqual(default, us.OpenParenToken);
            Assert.NotNull(us.Declaration);
            Assert.Equal("f ? x = a", us.Declaration.ToString());
            Assert.Null(us.Expression);
            Assert.NotEqual(default, us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingVarSpecialCase2()
        {
            var text = "using f ? x = a;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.Declaration);
            Assert.Equal("f ? x = a", us.Declaration.ToString());
        }

        [Fact]
        public void TestUsingVarSpecialCase2Tree()
        {
            UsingStatement(@"using f ? x = a;", options: TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName, "f");
                        {
                            N(SyntaxKind.IdentifierToken, "f");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    N(SyntaxKind.IdentifierToken, "x");
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.IdentifierName, "a");
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
        }

        [Fact]
        public void TestUsingSpecialCase3()
        {
            var text = "using (f ? x, y) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotEqual(default, us.OpenParenToken);
            Assert.NotNull(us.Declaration);
            Assert.Equal("f ? x, y", us.Declaration.ToString());
            Assert.Null(us.Expression);
            Assert.NotEqual(default, us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingVarSpecialCase3()
        {
            var text = "using f ? x, y;";
            var statement = this.ParseStatement(text, options: TestOptions.Regular8);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (LocalDeclarationStatementSyntax)statement;
            Assert.NotEqual(default, us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.Declaration);
            Assert.Equal("f ? x, y", us.Declaration.ToString());
        }

        [Fact]
        public void TestUsingVarSpecialCase3Tree()
        {
            UsingStatement("using f? x, y;", options: TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.NullableType);
                    {
                        N(SyntaxKind.IdentifierName, "f");
                        {
                            N(SyntaxKind.IdentifierToken, "f");
                        }
                        N(SyntaxKind.QuestionToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void TestUsingVarRefTree()
        {
            UsingStatement("using ref int x = ref y;", TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName, "y");
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void TestUsingVarRefReadonlyTree()
        {
            UsingStatement("using ref readonly int x = ref y;", TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.ReadOnlyKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.IntKeyword);
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName, "y");
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void TestUsingVarRefVarTree()
        {
            UsingStatement("using ref var x = ref y;", TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName, "var");
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.RefExpression);
                            {
                                N(SyntaxKind.RefKeyword);
                                N(SyntaxKind.IdentifierName, "y");
                                {
                                    N(SyntaxKind.IdentifierToken, "y");
                                }
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void TestUsingVarRefVarIsYTree()
        {
            UsingStatement("using ref var x = y;", TestOptions.Regular8);
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.RefType);
                    {
                        N(SyntaxKind.RefKeyword);
                        N(SyntaxKind.IdentifierName, "var");
                        {
                            N(SyntaxKind.IdentifierToken, "var");
                        }
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                        N(SyntaxKind.EqualsValueClause);
                        {
                            N(SyntaxKind.EqualsToken);
                            N(SyntaxKind.IdentifierName, "y");
                            {
                                N(SyntaxKind.IdentifierToken, "y");
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void TestUsingVarReadonlyMultipleDeclarations()
        {
            UsingStatement("using readonly var x, y = ref z;", TestOptions.Regular8,
                // (1,7): error CS0106: The modifier 'readonly' is not valid for this item
                // using readonly var x, y = ref z;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(1, 7));
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.UsingKeyword);
                N(SyntaxKind.ReadOnlyKeyword);
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.IdentifierName, "var");
                    {
                        N(SyntaxKind.IdentifierToken, "var");
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "x");
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "y");
                    }
                    N(SyntaxKind.EqualsValueClause);
                    {
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.RefExpression);
                        {
                            N(SyntaxKind.RefKeyword);
                            N(SyntaxKind.IdentifierName, "z");
                            {
                                N(SyntaxKind.IdentifierToken, "z");
                            }
                        }
                    }
                }
            }
            N(SyntaxKind.SemicolonToken);
        }

        [Fact]
        public void TestContextualKeywordsAsLocalVariableTypes()
        {
            TestContextualKeywordAsLocalVariableType(SyntaxKind.PartialKeyword);
            TestContextualKeywordAsLocalVariableType(SyntaxKind.AsyncKeyword);
            TestContextualKeywordAsLocalVariableType(SyntaxKind.AwaitKeyword);
        }

        private void TestContextualKeywordAsLocalVariableType(SyntaxKind kind)
        {
            var keywordText = SyntaxFacts.GetText(kind);
            var text = keywordText + " o = null;";
            var statement = this.ParseStatement(text);
            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());

            var decl = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(keywordText, decl.Declaration.Type.ToString());
            Assert.IsType<IdentifierNameSyntax>(decl.Declaration.Type);
            var name = (IdentifierNameSyntax)decl.Declaration.Type;
            Assert.Equal(kind, name.Identifier.ContextualKind());
            Assert.Equal(SyntaxKind.IdentifierToken, name.Identifier.Kind());
        }

        [Fact]
        public void Bug862649()
        {
            var text = @"static char[] delimiter;";
            var tree = SyntaxFactory.ParseStatement(text);
            var toText = tree.ToFullString();
            Assert.Equal(text, toText);
        }

        [Fact]
        public void TestForEachAfterOffset()
        {
            const string prefix = "GARBAGE";
            var text = "foreach(T a in b) { }";
            var statement = this.ParseStatement(prefix + text, offset: prefix.Length);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForEachStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForEachStatementSyntax)statement;
            Assert.NotEqual(default, fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());

            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.NotEqual(default, fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [WorkItem(684860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/684860")]
        [Fact]
        public void Bug684860_SkippedTokens()
        {
            const int n = 100000;
            // 100000 instances of "0+" in:
            // #pragma warning disable 1 0+0+0+...
            var builder = new System.Text.StringBuilder();
            builder.Append("#pragma warning disable 1 ");
            for (int i = 0; i < n; i++)
            {
                builder.Append("0+");
            }
            builder.AppendLine();
            var text = builder.ToString();
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetRoot();
            var walker = new TokenAndTriviaWalker();
            walker.Visit(root);
            Assert.True(walker.Tokens > n);
            var tokens1 = root.DescendantTokens(descendIntoTrivia: false).ToArray();
            var tokens2 = root.DescendantTokens(descendIntoTrivia: true).ToArray();
            Assert.True((tokens2.Length - tokens1.Length) > n);
        }

        [WorkItem(684860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/684860")]
        [Fact]
        public void Bug684860_XmlText()
        {
            const int n = 100000;
            // 100000 instances of "&lt;" in:
            // /// <x a="&lt;&lt;&lt;..."/>
            // class { }
            var builder = new System.Text.StringBuilder();
            builder.Append("/// <x a=\"");
            for (int i = 0; i < n; i++)
            {
                builder.Append("&lt;");
            }
            builder.AppendLine("\"/>");
            builder.AppendLine("class C { }");
            var text = builder.ToString();
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: new CSharpParseOptions(documentationMode: DocumentationMode.Parse));
            var root = tree.GetRoot();
            var walker = new TokenAndTriviaWalker();
            walker.Visit(root);
            Assert.True(walker.Tokens > n);
            var tokens = root.DescendantTokens(descendIntoTrivia: true).ToArray();
            Assert.True(tokens.Length > n);
        }

        [Fact]
        public void ExceptionFilter_IfKeyword()
        {
            const string source = @"
class C
{
    void M()
    {
        try { }
        catch (System.Exception e) if (true) { }
    }
}
";

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var root = tree.GetRoot();
            tree.GetDiagnostics(root).Verify(
                // (7,36): error CS1003: Syntax error, 'when' expected
                //         catch (System.Exception e) if (true) { }
                CSharpTestBase.Diagnostic(ErrorCode.ERR_SyntaxError, "if").WithArguments("when").WithLocation(7, 36));

            var filterClause = root.DescendantNodes().OfType<CatchFilterClauseSyntax>().Single();
            Assert.Equal(SyntaxKind.WhenKeyword, filterClause.WhenKeyword.Kind());
            Assert.True(filterClause.WhenKeyword.HasStructuredTrivia);
        }

        [Fact]
        public void Tuple001()
        {
            var source = @"
class C1
{
    static void Test(int arg1, (byte, byte) arg2)
    {
        (int, int)? t1 = new(int, int)?();
        (int, int)? t1a = new(int, int)?((1,1));
        (int, int)? t1b = new(int, int)?[1];
        (int, int)? t1c = new(int, int)?[] {(1,1)};

        (int, int)? t2 = default((int a, int b));

        (int, int) t3 = (a: (int)arg1, b: (int)arg1);

        (int, int) t4 = ((int a, int b))(arg1, arg1);
        (int, int) t5 = ((int, int))arg2;

        List<(int, int)> l = new List<(int, int)>() { (a: arg1, b: arg1), (arg1, arg1) };

        Func<(int a, int b), (int a, int b)> f = ((int a, int b) t) => t;
        
        var x = from i in ""qq""
                from j in ""ee""
                select (i, j);

        foreach ((int, int) e in new (int, int)[10])
        {
        }
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular);
            tree.GetDiagnostics().Verify();
        }

        [Fact]
        [WorkItem(684860, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/266237")]
        public void DevDiv266237()
        {
            var source = @"
class Program
{
    static void Go()
    {
        using (var p = new P
        {

        }

    protected override void M()
    {

    }
}
";

            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular);
            tree.GetDiagnostics(tree.GetRoot()).Verify(
                // (9,10): error CS1026: ) expected
                //         }
                CSharpTestBase.Diagnostic(ErrorCode.ERR_CloseParenExpected, "").WithLocation(9, 10),
                // (9,10): error CS1002: ; expected
                //         }
                CSharpTestBase.Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(9, 10),
                // (9,10): error CS1513: } expected
                //         }
                CSharpTestBase.Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 10));
        }

        [WorkItem(6676, "https://github.com/dotnet/roslyn/issues/6676")]
        [Fact]
        public void TestRunEmbeddedStatementNotFollowedBySemicolon()
        {
            var text = @"if (true)
System.Console.WriteLine(true)";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.IfStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(1, statement.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, statement.Errors()[0].Code);
        }

        [WorkItem(266237, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=266237")]
        [Fact]
        public void NullExceptionInLabeledStatement()
        {
            UsingStatement(@"{ label: public",
                // (1,1): error CS1073: Unexpected token 'public'
                // { label: public
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "{ label: ").WithArguments("public").WithLocation(1, 1),
                // (1,10): error CS1002: ; expected
                // { label: public
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "public").WithLocation(1, 10),
                // (1,10): error CS1513: } expected
                // { label: public
                Diagnostic(ErrorCode.ERR_RbraceExpected, "public").WithLocation(1, 10)
                );

            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.LabeledStatement);
                {
                    N(SyntaxKind.IdentifierToken, "label");
                    N(SyntaxKind.ColonToken);
                    M(SyntaxKind.EmptyStatement);
                    {
                        M(SyntaxKind.SemicolonToken);
                    }
                }
                M(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [WorkItem(27866, "https://github.com/dotnet/roslyn/issues/27866")]
        [Fact]
        public void ParseElseWithoutPrecedingIfStatement()
        {
            UsingStatement("else {}",
                // (1,1): error CS8641: 'else' cannot start a statement.
                // else {}
                Diagnostic(ErrorCode.ERR_ElseCannotStartStatement, "else").WithLocation(1, 1),
                // (1,1): error CS1003: Syntax error, '(' expected
                // else {}
                Diagnostic(ErrorCode.ERR_SyntaxError, "else").WithArguments("(").WithLocation(1, 1),
                // (1,1): error CS1525: Invalid expression term 'else'
                // else {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 1),
                // (1,1): error CS1026: ) expected
                // else {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "else").WithLocation(1, 1),
                // (1,1): error CS1525: Invalid expression term 'else'
                // else {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 1),
                // (1,1): error CS1002: ; expected
                // else {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "else").WithLocation(1, 1)
                );
            N(SyntaxKind.IfStatement);
            {
                M(SyntaxKind.IfKeyword);
                M(SyntaxKind.OpenParenToken);
                M(SyntaxKind.IdentifierName);
                {
                    M(SyntaxKind.IdentifierToken);
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
                N(SyntaxKind.ElseClause);
                {
                    N(SyntaxKind.ElseKeyword);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                }
            }
            EOF();
        }

        [WorkItem(27866, "https://github.com/dotnet/roslyn/issues/27866")]
        [Fact]
        public void ParseElseAndElseWithoutPrecedingIfStatement()
        {
            UsingStatement("{ else {} else {} }",
                // (1,3): error CS8641: 'else' cannot start a statement.
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_ElseCannotStartStatement, "else").WithLocation(1, 3),
                // (1,3): error CS1003: Syntax error, '(' expected
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_SyntaxError, "else").WithArguments("(").WithLocation(1, 3),
                // (1,3): error CS1525: Invalid expression term 'else'
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 3),
                // (1,3): error CS1026: ) expected
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "else").WithLocation(1, 3),
                // (1,3): error CS1525: Invalid expression term 'else'
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 3),
                // (1,3): error CS1002: ; expected
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "else").WithLocation(1, 3),
                // (1,11): error CS8641: 'else' cannot start a statement.
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_ElseCannotStartStatement, "else").WithLocation(1, 11),
                // (1,11): error CS1003: Syntax error, '(' expected
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_SyntaxError, "else").WithArguments("(").WithLocation(1, 11),
                // (1,11): error CS1525: Invalid expression term 'else'
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 11),
                // (1,11): error CS1026: ) expected
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "else").WithLocation(1, 11),
                // (1,11): error CS1525: Invalid expression term 'else'
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 11),
                // (1,11): error CS1002: ; expected
                // { else {} else {} }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "else").WithLocation(1, 11)
                );
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IfStatement);
                {
                    M(SyntaxKind.IfKeyword);
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.ElseClause);
                    {
                        N(SyntaxKind.ElseKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.IfStatement);
                {
                    M(SyntaxKind.IfKeyword);
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.ElseClause);
                    {
                        N(SyntaxKind.ElseKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [WorkItem(27866, "https://github.com/dotnet/roslyn/issues/27866")]
        [Fact]
        public void ParseSubsequentElseWithoutPrecedingIfStatement()
        {
            UsingStatement("{ if (a) { } else { } else { } }",
                // (1,23): error CS8641: 'else' cannot start a statement.
                // { if (a) { } else { } else { } }
                Diagnostic(ErrorCode.ERR_ElseCannotStartStatement, "else").WithLocation(1, 23),
                // (1,23): error CS1003: Syntax error, '(' expected
                // { if (a) { } else { } else { } }
                Diagnostic(ErrorCode.ERR_SyntaxError, "else").WithArguments("(").WithLocation(1, 23),
                // (1,23): error CS1525: Invalid expression term 'else'
                // { if (a) { } else { } else { } }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 23),
                // (1,23): error CS1026: ) expected
                // { if (a) { } else { } else { } }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "else").WithLocation(1, 23),
                // (1,23): error CS1525: Invalid expression term 'else'
                // { if (a) { } else { } else { } }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 23),
                // (1,23): error CS1002: ; expected
                // { if (a) { } else { } else { } }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "else").WithLocation(1, 23)
                );
            N(SyntaxKind.Block);
            {
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.IfStatement);
                {
                    N(SyntaxKind.IfKeyword);
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CloseParenToken);
                    N(SyntaxKind.Block);
                    {
                        N(SyntaxKind.OpenBraceToken);
                        N(SyntaxKind.CloseBraceToken);
                    }
                    N(SyntaxKind.ElseClause);
                    {
                        N(SyntaxKind.ElseKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.IfStatement);
                {
                    M(SyntaxKind.IfKeyword);
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.ElseClause);
                    {
                        N(SyntaxKind.ElseKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [WorkItem(27866, "https://github.com/dotnet/roslyn/issues/27866")]
        [Fact]
        public void ParseElseKeywordPlacedAsIfEmbeddedStatement()
        {
            UsingStatement("if (a) else {}",
                // (1,8): error CS8641: 'else' cannot start a statement.
                // if (a) else {}
                Diagnostic(ErrorCode.ERR_ElseCannotStartStatement, "else").WithLocation(1, 8),
                // (1,8): error CS1003: Syntax error, '(' expected
                // if (a) else {}
                Diagnostic(ErrorCode.ERR_SyntaxError, "else").WithArguments("(").WithLocation(1, 8),
                // (1,8): error CS1525: Invalid expression term 'else'
                // if (a) else {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 8),
                // (1,8): error CS1026: ) expected
                // if (a) else {}
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "else").WithLocation(1, 8),
                // (1,8): error CS1525: Invalid expression term 'else'
                // if (a) else {}
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "else").WithArguments("else").WithLocation(1, 8),
                // (1,8): error CS1002: ; expected
                // if (a) else {}
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "else").WithLocation(1, 8)
                );
            N(SyntaxKind.IfStatement);
            {
                N(SyntaxKind.IfKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.IfStatement);
                {
                    M(SyntaxKind.IfKeyword);
                    M(SyntaxKind.OpenParenToken);
                    M(SyntaxKind.IdentifierName);
                    {
                        M(SyntaxKind.IdentifierToken);
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
                    N(SyntaxKind.ElseClause);
                    {
                        N(SyntaxKind.ElseKeyword);
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void ParseSwitch01()
        {
            UsingStatement("switch 1+2 {}",
                // (1,8): error CS8415: Parentheses are required around the switch governing expression.
                // switch 1+2 {}
                Diagnostic(ErrorCode.ERR_SwitchGoverningExpressionRequiresParens, "1+2").WithLocation(1, 8)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                M(SyntaxKind.OpenParenToken);
                N(SyntaxKind.AddExpression);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "2");
                    }
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParseSwitch02()
        {
            UsingStatement("switch (a: 0) {}",
                // (1,13): error CS8124: Tuple must contain at least two elements.
                // switch (a: 0) {}
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(1, 13)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NameColon);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    M(SyntaxKind.CommaToken);
                    M(SyntaxKind.Argument);
                    {
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParseSwitch03()
        {
            UsingStatement("switch (a: 0, b: 4) {}");
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.TupleExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NameColon);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "0");
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.Argument);
                    {
                        N(SyntaxKind.NameColon);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "b");
                            }
                            N(SyntaxKind.ColonToken);
                        }
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "4");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParseSwitch04()
        {
            UsingStatement("switch (1) + (2) {}",
                // (1,8): error CS8415: Parentheses are required around the switch governing expression.
                // switch (1) + (2) {}
                Diagnostic(ErrorCode.ERR_SwitchGoverningExpressionRequiresParens, "(1) + (2)").WithLocation(1, 8)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                M(SyntaxKind.OpenParenToken);
                N(SyntaxKind.AddExpression);
                {
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "1");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.ParenthesizedExpression);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                M(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact]
        public void ParseCreateNullableTuple_01()
        {
            UsingStatement("_ = new (int, int)? {};");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ObjectCreationExpression);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.ObjectInitializerExpression);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ParseCreateNullableTuple_02()
        {
            UsingStatement("_ = new (int, int) ? (x) : (y);",
                // (1,1): error CS1073: Unexpected token ':'
                // _ = new (int, int) ? (x) : (y);
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "_ = new (int, int) ? (x) ").WithArguments(":").WithLocation(1, 1),
                // (1,26): error CS1002: ; expected
                // _ = new (int, int) ? (x) : (y);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(1, 26)
                );
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ObjectCreationExpression);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.TupleType);
                            {
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CommaToken);
                                N(SyntaxKind.TupleElement);
                                {
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.IntKeyword);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                            }
                            N(SyntaxKind.QuestionToken);
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
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                M(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ParsePointerToArray()
        {
            UsingStatement("int []* p;");
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PointerType);
                    {
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
                        N(SyntaxKind.AsteriskToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "p");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ParsePointerToNullableType()
        {
            UsingStatement("int?* p;");
            N(SyntaxKind.LocalDeclarationStatement);
            {
                N(SyntaxKind.VariableDeclaration);
                {
                    N(SyntaxKind.PointerType);
                    {
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.AsteriskToken);
                    }
                    N(SyntaxKind.VariableDeclarator);
                    {
                        N(SyntaxKind.IdentifierToken, "p");
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact]
        public void ParseNewNullableWithInitializer()
        {
            UsingStatement("_ = new int? {};");
            N(SyntaxKind.ExpressionStatement);
            {
                N(SyntaxKind.SimpleAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "_");
                    }
                    N(SyntaxKind.EqualsToken);
                    N(SyntaxKind.ObjectCreationExpression);
                    {
                        N(SyntaxKind.NewKeyword);
                        N(SyntaxKind.NullableType);
                        {
                            N(SyntaxKind.PredefinedType);
                            {
                                N(SyntaxKind.IntKeyword);
                            }
                            N(SyntaxKind.QuestionToken);
                        }
                        N(SyntaxKind.ObjectInitializerExpression);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                }
                N(SyntaxKind.SemicolonToken);
            }
            EOF();
        }

        [Fact, WorkItem(66971, "https://github.com/dotnet/roslyn/issues/66971")]
        public void ParseCaseWithoutSwitch()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        case int when SomeTest():
                            Console.WriteLine("answer");
                            break;
                        }
                    }
                }
                """,
                // (4,6): error CS1003: Syntax error, 'switch' expected
                //     {
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("switch").WithLocation(4, 6));

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
                            N(SyntaxKind.SwitchStatement);
                            {
                                M(SyntaxKind.SwitchKeyword);
                                M(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.CloseParenToken);
                                M(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchSection);
                                {
                                    N(SyntaxKind.CasePatternSwitchLabel);
                                    {
                                        N(SyntaxKind.CaseKeyword);
                                        N(SyntaxKind.TypePattern);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.WhenClause);
                                        {
                                            N(SyntaxKind.WhenKeyword);
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "SomeTest");
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Console");
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "WriteLine");
                                                }
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.StringLiteralExpression);
                                                    {
                                                        N(SyntaxKind.StringLiteralToken, "\"answer\"");
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(66971, "https://github.com/dotnet/roslyn/issues/66971")]
        public void ParseErrantStatementInCase1()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        switch (expr)
                        {
                            int i;

                            case int when SomeTest():
                                Console.WriteLine("answer");
                                break;
                        }
                    }
                }
                """,
                // (6,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(6, 10),
                // (7,19): error CS1003: Syntax error, 'switch' expected
                //             int i;
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("switch").WithLocation(7, 19));

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
                            N(SyntaxKind.SwitchStatement);
                            {
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "expr");
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                M(SyntaxKind.CloseBraceToken);
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
                                        N(SyntaxKind.IdentifierToken, "i");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.SwitchStatement);
                            {
                                M(SyntaxKind.SwitchKeyword);
                                M(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.CloseParenToken);
                                M(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchSection);
                                {
                                    N(SyntaxKind.CasePatternSwitchLabel);
                                    {
                                        N(SyntaxKind.CaseKeyword);
                                        N(SyntaxKind.TypePattern);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.WhenClause);
                                        {
                                            N(SyntaxKind.WhenKeyword);
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "SomeTest");
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Console");
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "WriteLine");
                                                }
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.StringLiteralExpression);
                                                    {
                                                        N(SyntaxKind.StringLiteralToken, "\"answer\"");
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(66971, "https://github.com/dotnet/roslyn/issues/66971")]
        public void ParseErrantStatementInCase2()
        {
            UsingTree("""
                class C
                {
                    void M()
                    {
                        switch (new object())
                        {
                            bool SomeTest() => o is 42;

                            case int when SomeTest():
                                Console.WriteLine("answer");
                                break;
                        }
                    }
                }
                """,
                // (6,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(6, 10),
                // (7,40): error CS1003: Syntax error, 'switch' expected
                //             bool SomeTest() => o is 42;
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("switch").WithLocation(7, 40));

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
                            N(SyntaxKind.SwitchStatement);
                            {
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.ObjectCreationExpression);
                                {
                                    N(SyntaxKind.NewKeyword);
                                    N(SyntaxKind.PredefinedType);
                                    {
                                        N(SyntaxKind.ObjectKeyword);
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                M(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.PredefinedType);
                                {
                                    N(SyntaxKind.BoolKeyword);
                                }
                                N(SyntaxKind.IdentifierToken, "SomeTest");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.ArrowExpressionClause);
                                {
                                    N(SyntaxKind.EqualsGreaterThanToken);
                                    N(SyntaxKind.IsPatternExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "o");
                                        }
                                        N(SyntaxKind.IsKeyword);
                                        N(SyntaxKind.ConstantPattern);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "42");
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.SwitchStatement);
                            {
                                M(SyntaxKind.SwitchKeyword);
                                M(SyntaxKind.OpenParenToken);
                                M(SyntaxKind.IdentifierName);
                                {
                                    M(SyntaxKind.IdentifierToken);
                                }
                                M(SyntaxKind.CloseParenToken);
                                M(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchSection);
                                {
                                    N(SyntaxKind.CasePatternSwitchLabel);
                                    {
                                        N(SyntaxKind.CaseKeyword);
                                        N(SyntaxKind.TypePattern);
                                        {
                                            N(SyntaxKind.PredefinedType);
                                            {
                                                N(SyntaxKind.IntKeyword);
                                            }
                                        }
                                        N(SyntaxKind.WhenClause);
                                        {
                                            N(SyntaxKind.WhenKeyword);
                                            N(SyntaxKind.InvocationExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "SomeTest");
                                                }
                                                N(SyntaxKind.ArgumentList);
                                                {
                                                    N(SyntaxKind.OpenParenToken);
                                                    N(SyntaxKind.CloseParenToken);
                                                }
                                            }
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.ExpressionStatement);
                                    {
                                        N(SyntaxKind.InvocationExpression);
                                        {
                                            N(SyntaxKind.SimpleMemberAccessExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "Console");
                                                }
                                                N(SyntaxKind.DotToken);
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "WriteLine");
                                                }
                                            }
                                            N(SyntaxKind.ArgumentList);
                                            {
                                                N(SyntaxKind.OpenParenToken);
                                                N(SyntaxKind.Argument);
                                                {
                                                    N(SyntaxKind.StringLiteralExpression);
                                                    {
                                                        N(SyntaxKind.StringLiteralToken, "\"answer\"");
                                                    }
                                                }
                                                N(SyntaxKind.CloseParenToken);
                                            }
                                        }
                                        N(SyntaxKind.SemicolonToken);
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
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67757")]
        public void ParseSwitchStatementWithUnclosedRecursivePattern1()
        {
            UsingStatement("""
                switch (obj)
                {
                    case Type { Prop: Type { }:
                    case Type { Prop: Type { }:
                       break;
                }
                """,
                // (3,31): error CS1513: } expected
                //     case Type { Prop: Type { }:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 31),
                // (4,31): error CS1513: } expected
                //     case Type { Prop: Type { }:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 31)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "obj");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Prop");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.RecursivePattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Type");
                                        }
                                        N(SyntaxKind.PropertyPatternClause);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Prop");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.RecursivePattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Type");
                                        }
                                        N(SyntaxKind.PropertyPatternClause);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseBraceToken);
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
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67757")]
        public void ParseSwitchStatementWithUnclosedRecursivePattern2()
        {
            UsingStatement("""
                switch (obj)
                {
                    case Type { Prop: Type {:
                    case Type { Prop: Type {:
                       break;
                }
                """,
                // (3,29): error CS1513: } expected
                //     case Type { Prop: Type {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 29),
                // (3,29): error CS1513: } expected
                //     case Type { Prop: Type {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 29),
                // (4,29): error CS1513: } expected
                //     case Type { Prop: Type {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 29),
                // (4,29): error CS1513: } expected
                //     case Type { Prop: Type {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 29)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "obj");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Prop");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.RecursivePattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Type");
                                        }
                                        N(SyntaxKind.PropertyPatternClause);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            M(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Type");
                            }
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Prop");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.RecursivePattern);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Type");
                                        }
                                        N(SyntaxKind.PropertyPatternClause);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            M(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseBraceToken);
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
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67757")]
        public void ParseSwitchStatementWithUnclosedRecursivePattern3()
        {
            UsingStatement("""
                switch (obj)
                {
                    case { Prop: { Prop: {:
                    case { Prop: { Prop: {:
                       break;
                }
                """,
                // (3,27): error CS1513: } expected
                //     case { Prop: { Prop: {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 27),
                // (3,27): error CS1513: } expected
                //     case { Prop: { Prop: {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 27),
                // (3,27): error CS1513: } expected
                //     case { Prop: { Prop: {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(3, 27),
                // (4,27): error CS1513: } expected
                //     case { Prop: { Prop: {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 27),
                // (4,27): error CS1513: } expected
                //     case { Prop: { Prop: {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 27),
                // (4,27): error CS1513: } expected
                //     case { Prop: { Prop: {:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(4, 27)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "obj");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Prop");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.RecursivePattern);
                                    {
                                        N(SyntaxKind.PropertyPatternClause);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.Subpattern);
                                            {
                                                N(SyntaxKind.NameColon);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Prop");
                                                    }
                                                    N(SyntaxKind.ColonToken);
                                                }
                                                N(SyntaxKind.RecursivePattern);
                                                {
                                                    N(SyntaxKind.PropertyPatternClause);
                                                    {
                                                        N(SyntaxKind.OpenBraceToken);
                                                        M(SyntaxKind.CloseBraceToken);
                                                    }
                                                }
                                            }
                                            M(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.Subpattern);
                                {
                                    N(SyntaxKind.NameColon);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Prop");
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.RecursivePattern);
                                    {
                                        N(SyntaxKind.PropertyPatternClause);
                                        {
                                            N(SyntaxKind.OpenBraceToken);
                                            N(SyntaxKind.Subpattern);
                                            {
                                                N(SyntaxKind.NameColon);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "Prop");
                                                    }
                                                    N(SyntaxKind.ColonToken);
                                                }
                                                N(SyntaxKind.RecursivePattern);
                                                {
                                                    N(SyntaxKind.PropertyPatternClause);
                                                    {
                                                        N(SyntaxKind.OpenBraceToken);
                                                        M(SyntaxKind.CloseBraceToken);
                                                    }
                                                }
                                            }
                                            M(SyntaxKind.CloseBraceToken);
                                        }
                                    }
                                }
                                M(SyntaxKind.CloseBraceToken);
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
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67757")]
        public void ParseSwitchStatementWithUnclosedListPattern1()
        {
            UsingStatement("""
                switch (obj)
                {
                    case [:
                    case [:
                       break;
                }
                """,
                // (3,11): error CS1003: Syntax error, ']' expected
                //     case [:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 11),
                // (4,11): error CS1003: Syntax error, ']' expected
                //     case [:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 11)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "obj");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            M(SyntaxKind.CloseBracketToken);
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
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67757")]
        public void ParseSwitchStatementWithUnclosedListPattern2()
        {
            UsingStatement("""
                switch (obj)
                {
                    case [[:
                    case [[:
                       break;
                }
                """,
                // (3,12): error CS1003: Syntax error, ']' expected
                //     case [[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 12),
                // (3,12): error CS1003: Syntax error, ']' expected
                //     case [[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 12),
                // (4,12): error CS1003: Syntax error, ']' expected
                //     case [[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 12),
                // (4,12): error CS1003: Syntax error, ']' expected
                //     case [[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 12)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "obj");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ListPattern);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                M(SyntaxKind.CloseBracketToken);
                            }
                            M(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ListPattern);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                M(SyntaxKind.CloseBracketToken);
                            }
                            M(SyntaxKind.CloseBracketToken);
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
            EOF();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67757")]
        public void ParseSwitchStatementWithUnclosedListPattern3()
        {
            UsingStatement("""
                switch (obj)
                {
                    case [[[:
                    case [[[:
                       break;
                }
                """,
                // (3,13): error CS1003: Syntax error, ']' expected
                //     case [[[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 13),
                // (3,13): error CS1003: Syntax error, ']' expected
                //     case [[[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 13),
                // (3,13): error CS1003: Syntax error, ']' expected
                //     case [[[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(3, 13),
                // (4,13): error CS1003: Syntax error, ']' expected
                //     case [[[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 13),
                // (4,13): error CS1003: Syntax error, ']' expected
                //     case [[[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 13),
                // (4,13): error CS1003: Syntax error, ']' expected
                //     case [[[:
                Diagnostic(ErrorCode.ERR_SyntaxError, ":").WithArguments("]").WithLocation(4, 13)
                );
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "obj");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ListPattern);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ListPattern);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    M(SyntaxKind.CloseBracketToken);
                                }
                                M(SyntaxKind.CloseBracketToken);
                            }
                            M(SyntaxKind.CloseBracketToken);
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ListPattern);
                        {
                            N(SyntaxKind.OpenBracketToken);
                            N(SyntaxKind.ListPattern);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.ListPattern);
                                {
                                    N(SyntaxKind.OpenBracketToken);
                                    M(SyntaxKind.CloseBracketToken);
                                }
                                M(SyntaxKind.CloseBracketToken);
                            }
                            M(SyntaxKind.CloseBracketToken);
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
            EOF();
        }

        [Fact]
        public void ParseSwitchStatementWithUnclosedPatternAndArrow()
        {
            // No good error recovery strategy yet
            UsingStatement("""
                switch (obj)
                {
                    case { =>
                        break;
                    case { =>
                        break;
                }
                """,
                // (3,12): error CS1001: Identifier expected
                //     case { =>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(3, 12),
                // (4,14): error CS1513: } expected
                //         break;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(4, 14),
                // (4,14): error CS1003: Syntax error, ':' expected
                //         break;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(4, 14),
                // (5,12): error CS1001: Identifier expected
                //     case { =>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(5, 12),
                // (6,14): error CS1513: } expected
                //         break;
                Diagnostic(ErrorCode.ERR_RbraceExpected, ";").WithLocation(6, 14),
                // (6,14): error CS1003: Syntax error, ':' expected
                //         break;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(":").WithLocation(6, 14));

            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "obj");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                        M(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.RecursivePattern);
                        {
                            N(SyntaxKind.PropertyPatternClause);
                            {
                                N(SyntaxKind.OpenBraceToken);
                                M(SyntaxKind.CloseBraceToken);
                            }
                        }
                        M(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.EmptyStatement);
                    {
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        private sealed class TokenAndTriviaWalker : CSharpSyntaxWalker
        {
            public int Tokens;
            public TokenAndTriviaWalker()
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
            }
            public override void VisitToken(SyntaxToken token)
            {
                Tokens++;
                base.VisitToken(token);
            }
        }
    }
}
