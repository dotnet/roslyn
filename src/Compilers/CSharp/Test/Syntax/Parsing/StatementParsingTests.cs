// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class StatementParsingTests
    {
        private StatementSyntax ParseStatement(string text, int offset = 0, ParseOptions options = null)
        {
            return SyntaxFactory.ParseStatement(text, offset, options);
        }

        private StatementSyntax ParseStatementExperimental(string text)
        {
            return ParseStatement(text, offset: 0, options: TestOptions.ExperimentalParseOptions);
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
            Assert.NotNull(es.SemicolonToken);
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
            Assert.NotNull(es.SemicolonToken);
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
            Assert.NotNull(es.SemicolonToken);
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
            Assert.NotNull(es.SemicolonToken);
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
            Assert.NotNull(es.SemicolonToken);
            Assert.False(es.SemicolonToken.IsMissing);
        }

        private void TestPostfixUnaryOperator(SyntaxKind kind)
        {
            var text = "a" + SyntaxFacts.GetText(kind) + ";";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ExpressionStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var es = (ExpressionStatementSyntax)statement;
            Assert.NotNull(es.Expression);
            Assert.NotNull(es.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("b", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.Declaration.Variables[1].Identifier);
            Assert.Equal("b", ds.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[1].ArgumentList);
            Assert.Null(ds.Declaration.Variables[1].Initializer);

            Assert.NotNull(ds.Declaration.Variables[2].Identifier);
            Assert.Equal("c", ds.Declaration.Variables[2].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[2].Initializer);

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("va", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(ds.Declaration.Variables[1].Identifier);
            Assert.Equal("b", ds.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[1].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[1].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("vb", ds.Declaration.Variables[1].Initializer.Value.ToString());

            Assert.NotNull(ds.Declaration.Variables[2].Identifier);
            Assert.Equal("c", ds.Declaration.Variables[2].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[2].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[2].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[2].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[2].Initializer.Value);
            Assert.Equal("vc", ds.Declaration.Variables[2].Initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ArrayInitializerExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal("{b, c}", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
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

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.False(ds.Declaration.Variables[0].Initializer.EqualsToken.IsMissing);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", ds.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestRefLocalDeclarationStatement()
        {
            var text = "ref T a;";
            var statement = this.ParseStatementExperimental(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.RefKeyword);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            Assert.Null(ds.Declaration.Variables[0].Initializer);

            Assert.NotNull(ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestRefLocalDeclarationStatementWithInitializer()
        {
            var text = "ref T a = ref b;";
            var statement = this.ParseStatementExperimental(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.RefKeyword);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(1, ds.Declaration.Variables.Count);

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            var initializer = ds.Declaration.Variables[0].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotNull(initializer.EqualsToken);
            Assert.False(initializer.EqualsToken.IsMissing);
            Assert.NotNull(initializer.RefKeyword);
            Assert.NotNull(initializer.Value);
            Assert.Equal("b", initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
            Assert.False(ds.SemicolonToken.IsMissing);
        }

        [Fact]
        public void TestRefLocalDeclarationStatementWithMultipleInitializers()
        {
            var text = "ref T a = ref b, c = ref d;";
            var statement = this.ParseStatementExperimental(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var ds = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal(0, ds.Modifiers.Count);
            Assert.NotNull(ds.RefKeyword);
            Assert.NotNull(ds.Declaration.Type);
            Assert.Equal("T", ds.Declaration.Type.ToString());
            Assert.Equal(2, ds.Declaration.Variables.Count);

            Assert.NotNull(ds.Declaration.Variables[0].Identifier);
            Assert.Equal("a", ds.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[0].ArgumentList);
            var initializer = ds.Declaration.Variables[0].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotNull(initializer.EqualsToken);
            Assert.False(initializer.EqualsToken.IsMissing);
            Assert.NotNull(initializer.RefKeyword);
            Assert.NotNull(initializer.Value);
            Assert.Equal("b", initializer.Value.ToString());

            Assert.NotNull(ds.Declaration.Variables[1].Identifier);
            Assert.Equal("c", ds.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(ds.Declaration.Variables[1].ArgumentList);
            initializer = ds.Declaration.Variables[1].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotNull(initializer.EqualsToken);
            Assert.False(initializer.EqualsToken.IsMissing);
            Assert.NotNull(initializer.RefKeyword);
            Assert.NotNull(initializer.Value);
            Assert.Equal("d", initializer.Value.ToString());

            Assert.NotNull(ds.SemicolonToken);
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
            Assert.NotNull(fs.FixedKeyword);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotNull(fs.OpenParenToken);
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
            Assert.NotNull(fs.FixedKeyword);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotNull(fs.OpenParenToken);
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
            Assert.NotNull(fs.FixedKeyword);
            Assert.False(fs.FixedKeyword.IsMissing);
            Assert.NotNull(fs.OpenParenToken);
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
            Assert.NotNull(es.SemicolonToken);
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
            Assert.NotNull(ls.Identifier);
            Assert.Equal("label", ls.Identifier.ToString());
            Assert.NotNull(ls.ColonToken);
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
            Assert.NotNull(b.BreakKeyword);
            Assert.False(b.BreakKeyword.IsMissing);
            Assert.Equal(SyntaxKind.BreakKeyword, b.BreakKeyword.Kind());
            Assert.NotNull(b.SemicolonToken);
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
            Assert.NotNull(cs.ContinueKeyword);
            Assert.False(cs.ContinueKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ContinueKeyword, cs.ContinueKeyword.Kind());
            Assert.NotNull(cs.SemicolonToken);
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
            Assert.NotNull(gs.GotoKeyword);
            Assert.False(gs.GotoKeyword.IsMissing);
            Assert.Equal(SyntaxKind.GotoKeyword, gs.GotoKeyword.Kind());
            Assert.Equal(SyntaxKind.None, gs.CaseOrDefaultKeyword.Kind());
            Assert.NotNull(gs.Expression);
            Assert.Equal("label", gs.Expression.ToString());
            Assert.NotNull(gs.SemicolonToken);
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
            Assert.NotNull(gs.GotoKeyword);
            Assert.False(gs.GotoKeyword.IsMissing);
            Assert.Equal(SyntaxKind.GotoKeyword, gs.GotoKeyword.Kind());
            Assert.NotNull(gs.CaseOrDefaultKeyword);
            Assert.False(gs.CaseOrDefaultKeyword.IsMissing);
            Assert.Equal(SyntaxKind.CaseKeyword, gs.CaseOrDefaultKeyword.Kind());
            Assert.NotNull(gs.Expression);
            Assert.Equal("label", gs.Expression.ToString());
            Assert.NotNull(gs.SemicolonToken);
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
            Assert.NotNull(gs.GotoKeyword);
            Assert.False(gs.GotoKeyword.IsMissing);
            Assert.Equal(SyntaxKind.GotoKeyword, gs.GotoKeyword.Kind());
            Assert.NotNull(gs.CaseOrDefaultKeyword);
            Assert.False(gs.CaseOrDefaultKeyword.IsMissing);
            Assert.Equal(SyntaxKind.DefaultKeyword, gs.CaseOrDefaultKeyword.Kind());
            Assert.Null(gs.Expression);
            Assert.NotNull(gs.SemicolonToken);
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
            Assert.NotNull(rs.ReturnKeyword);
            Assert.False(rs.ReturnKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ReturnKeyword, rs.ReturnKeyword.Kind());
            Assert.Null(rs.Expression);
            Assert.NotNull(rs.SemicolonToken);
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
            Assert.NotNull(rs.ReturnKeyword);
            Assert.False(rs.ReturnKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ReturnKeyword, rs.ReturnKeyword.Kind());
            Assert.NotNull(rs.Expression);
            Assert.Equal("a", rs.Expression.ToString());
            Assert.NotNull(rs.SemicolonToken);
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
            Assert.NotNull(ys.YieldKeyword);
            Assert.False(ys.YieldKeyword.IsMissing);
            Assert.Equal(SyntaxKind.YieldKeyword, ys.YieldKeyword.Kind());
            Assert.NotNull(ys.ReturnOrBreakKeyword);
            Assert.False(ys.ReturnOrBreakKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ReturnKeyword, ys.ReturnOrBreakKeyword.Kind());
            Assert.NotNull(ys.Expression);
            Assert.Equal("a", ys.Expression.ToString());
            Assert.NotNull(ys.SemicolonToken);
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
            Assert.NotNull(ys.YieldKeyword);
            Assert.False(ys.YieldKeyword.IsMissing);
            Assert.Equal(SyntaxKind.YieldKeyword, ys.YieldKeyword.Kind());
            Assert.NotNull(ys.ReturnOrBreakKeyword);
            Assert.False(ys.ReturnOrBreakKeyword.IsMissing);
            Assert.Equal(SyntaxKind.BreakKeyword, ys.ReturnOrBreakKeyword.Kind());
            Assert.Null(ys.Expression);
            Assert.NotNull(ys.SemicolonToken);
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
            Assert.NotNull(ts.ThrowKeyword);
            Assert.False(ts.ThrowKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ThrowKeyword, ts.ThrowKeyword.ContextualKind());
            Assert.Null(ts.Expression);
            Assert.NotNull(ts.SemicolonToken);
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
            Assert.NotNull(ts.ThrowKeyword);
            Assert.False(ts.ThrowKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ThrowKeyword, ts.ThrowKeyword.ContextualKind());
            Assert.NotNull(ts.Expression);
            Assert.Equal("a", ts.Expression.ToString());
            Assert.NotNull(ts.SemicolonToken);
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
            Assert.NotNull(ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(1, ts.Catches.Count);
            Assert.NotNull(ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotNull(ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.NotNull(ts.Catches[0].Declaration.Identifier);
            Assert.Equal("e", ts.Catches[0].Declaration.Identifier.ToString());
            Assert.NotNull(ts.Catches[0].Declaration.CloseParenToken);
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
            Assert.NotNull(ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(1, ts.Catches.Count);
            Assert.NotNull(ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotNull(ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.Equal(ts.Catches[0].Declaration.Identifier.Kind(), SyntaxKind.None);
            Assert.NotNull(ts.Catches[0].Declaration.CloseParenToken);
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
            Assert.NotNull(ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(1, ts.Catches.Count);
            Assert.NotNull(ts.Catches[0].CatchKeyword);
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
            Assert.NotNull(ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(3, ts.Catches.Count);

            Assert.NotNull(ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotNull(ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.NotNull(ts.Catches[0].Declaration.Identifier);
            Assert.Equal("e", ts.Catches[0].Declaration.Identifier.ToString());
            Assert.NotNull(ts.Catches[0].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[0].Block);

            Assert.NotNull(ts.Catches[1].CatchKeyword);
            Assert.NotNull(ts.Catches[1].Declaration);
            Assert.NotNull(ts.Catches[1].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[1].Declaration.Type);
            Assert.Equal("T2", ts.Catches[1].Declaration.Type.ToString());
            Assert.NotNull(ts.Catches[1].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[1].Block);

            Assert.NotNull(ts.Catches[2].CatchKeyword);
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
            Assert.NotNull(ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(0, ts.Catches.Count);

            Assert.NotNull(ts.Finally);
            Assert.NotNull(ts.Finally.FinallyKeyword);
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
            Assert.NotNull(ts.TryKeyword);
            Assert.False(ts.TryKeyword.IsMissing);
            Assert.NotNull(ts.Block);

            Assert.Equal(3, ts.Catches.Count);

            Assert.NotNull(ts.Catches[0].CatchKeyword);
            Assert.NotNull(ts.Catches[0].Declaration);
            Assert.NotNull(ts.Catches[0].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[0].Declaration.Type);
            Assert.Equal("T", ts.Catches[0].Declaration.Type.ToString());
            Assert.NotNull(ts.Catches[0].Declaration.Identifier);
            Assert.Equal("e", ts.Catches[0].Declaration.Identifier.ToString());
            Assert.NotNull(ts.Catches[0].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[0].Block);

            Assert.NotNull(ts.Catches[1].CatchKeyword);
            Assert.NotNull(ts.Catches[1].Declaration);
            Assert.NotNull(ts.Catches[1].Declaration.OpenParenToken);
            Assert.NotNull(ts.Catches[1].Declaration.Type);
            Assert.Equal("T2", ts.Catches[1].Declaration.Type.ToString());
            Assert.NotNull(ts.Catches[1].Declaration.CloseParenToken);
            Assert.NotNull(ts.Catches[1].Block);

            Assert.NotNull(ts.Catches[2].CatchKeyword);
            Assert.Null(ts.Catches[2].Declaration);
            Assert.NotNull(ts.Catches[2].Block);

            Assert.NotNull(ts.Finally);
            Assert.NotNull(ts.Finally.FinallyKeyword);
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
            Assert.NotNull(cs.Keyword);
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
            Assert.NotNull(cs.Keyword);
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
            Assert.NotNull(us.UnsafeKeyword);
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
            Assert.NotNull(ws.WhileKeyword);
            Assert.Equal(SyntaxKind.WhileKeyword, ws.WhileKeyword.Kind());
            Assert.NotNull(ws.OpenParenToken);
            Assert.NotNull(ws.Condition);
            Assert.NotNull(ws.CloseParenToken);
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
            Assert.NotNull(ds.DoKeyword);
            Assert.Equal(SyntaxKind.DoKeyword, ds.DoKeyword.Kind());
            Assert.NotNull(ds.Statement);
            Assert.NotNull(ds.WhileKeyword);
            Assert.Equal(SyntaxKind.WhileKeyword, ds.WhileKeyword.Kind());
            Assert.Equal(SyntaxKind.Block, ds.Statement.Kind());
            Assert.NotNull(ds.OpenParenToken);
            Assert.NotNull(ds.Condition);
            Assert.NotNull(ds.CloseParenToken);
            Assert.Equal("a", ds.Condition.ToString());
            Assert.NotNull(ds.SemicolonToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);
            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotNull(fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("var", fs.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, fs.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)fs.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotNull(fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(2, fs.Declaration.Variables.Count);

            Assert.NotNull(fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(fs.Declaration.Variables[1].Identifier);
            Assert.Equal("b", fs.Declaration.Variables[1].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[1].Initializer);
            Assert.NotNull(fs.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("1", fs.Declaration.Variables[1].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
        }

        [Fact]
        public void TestForWithRefVariableDeclaration()
        {
            var text = "for(ref T a = ref b, c = ref d;;) { }";
            var statement = this.ParseStatementExperimental(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.ForStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var fs = (ForStatementSyntax)statement;
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.NotNull(fs.RefKeyword);
            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(2, fs.Declaration.Variables.Count);

            Assert.NotNull(fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            var initializer = fs.Declaration.Variables[0].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotNull(initializer.EqualsToken);
            Assert.NotNull(initializer.RefKeyword);
            Assert.NotNull(initializer.Value);
            Assert.Equal("b", initializer.Value.ToString());

            Assert.NotNull(fs.Declaration.Variables[1].Identifier);
            Assert.Equal("c", fs.Declaration.Variables[1].Identifier.ToString());
            initializer = fs.Declaration.Variables[1].Initializer as EqualsValueClauseSyntax;
            Assert.NotNull(initializer);
            Assert.NotNull(initializer.EqualsToken);
            Assert.NotNull(initializer.RefKeyword);
            Assert.NotNull(initializer.Value);
            Assert.Equal("d", initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
            Assert.NotNull(fs.Statement);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(1, fs.Initializers.Count);
            Assert.Equal("a = 0", fs.Initializers[0].ToString());

            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(2, fs.Initializers.Count);
            Assert.Equal("a = 0", fs.Initializers[0].ToString());
            Assert.Equal("b = 1", fs.Initializers[1].ToString());

            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);

            Assert.NotNull(fs.Condition);
            Assert.Equal("a", fs.Condition.ToString());

            Assert.NotNull(fs.SecondSemicolonToken);
            Assert.Equal(0, fs.Incrementors.Count);
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);

            Assert.Equal(1, fs.Incrementors.Count);
            Assert.Equal("a++", fs.Incrementors[0].ToString());

            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.Null(fs.Declaration);
            Assert.Equal(0, fs.Initializers.Count);
            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.Null(fs.Condition);
            Assert.NotNull(fs.SecondSemicolonToken);

            Assert.Equal(2, fs.Incrementors.Count);
            Assert.Equal("a++", fs.Incrementors[0].ToString());
            Assert.Equal("b++", fs.Incrementors[1].ToString());

            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForKeyword);
            Assert.False(fs.ForKeyword.IsMissing);
            Assert.Equal(SyntaxKind.ForKeyword, fs.ForKeyword.Kind());
            Assert.NotNull(fs.OpenParenToken);

            Assert.NotNull(fs.Declaration);
            Assert.NotNull(fs.Declaration.Type);
            Assert.Equal("T", fs.Declaration.Type.ToString());
            Assert.Equal(1, fs.Declaration.Variables.Count);
            Assert.NotNull(fs.Declaration.Variables[0].Identifier);
            Assert.Equal("a", fs.Declaration.Variables[0].Identifier.ToString());
            Assert.NotNull(fs.Declaration.Variables[0].Initializer);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(fs.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("0", fs.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Equal(0, fs.Initializers.Count);

            Assert.NotNull(fs.FirstSemicolonToken);
            Assert.NotNull(fs.Condition);
            Assert.Equal("a < 10", fs.Condition.ToString());

            Assert.NotNull(fs.SecondSemicolonToken);

            Assert.Equal(1, fs.Incrementors.Count);
            Assert.Equal("a++", fs.Incrementors[0].ToString());

            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());

            Assert.NotNull(fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.NotNull(fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());
            Assert.True(fs.ForEachKeyword.IsMissing);
            Assert.Equal(fs.ForEachKeyword.TrailingTrivia.Count, 1);
            Assert.Equal(fs.ForEachKeyword.TrailingTrivia[0].Kind(), SyntaxKind.SkippedTokensTrivia);
            Assert.Equal(fs.ForEachKeyword.TrailingTrivia[0].ToString(), "for");

            Assert.NotNull(fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.NotNull(fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());

            Assert.NotNull(fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("var", fs.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, fs.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)fs.Type).Identifier.Kind());
            Assert.NotNull(fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.NotNull(ss.IfKeyword);
            Assert.Equal(SyntaxKind.IfKeyword, ss.IfKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Condition);
            Assert.Equal("a", ss.Condition.ToString());
            Assert.NotNull(ss.CloseParenToken);
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
            Assert.NotNull(ss.IfKeyword);
            Assert.Equal(SyntaxKind.IfKeyword, ss.IfKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Condition);
            Assert.Equal("a", ss.Condition.ToString());
            Assert.NotNull(ss.CloseParenToken);
            Assert.NotNull(ss.Statement);

            Assert.NotNull(ss.Else);
            Assert.NotNull(ss.Else.ElseKeyword);
            Assert.Equal(SyntaxKind.ElseKeyword, ss.Else.ElseKeyword.Kind());
            Assert.NotNull(ss.Else.Statement);
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
            Assert.NotNull(ls.LockKeyword);
            Assert.Equal(SyntaxKind.LockKeyword, ls.LockKeyword.Kind());
            Assert.NotNull(ls.OpenParenToken);
            Assert.NotNull(ls.Expression);
            Assert.Equal("a", ls.Expression.ToString());
            Assert.NotNull(ls.CloseParenToken);
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
            Assert.Equal(1, diags.Length);
            Assert.Equal((int)ErrorCode.WRN_EmptySwitch, diags[0].Code);

            var ss = (SwitchStatementSyntax)statement;
            Assert.NotNull(ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotNull(ss.CloseParenToken);
            Assert.NotNull(ss.OpenBraceToken);
            Assert.Equal(0, ss.Sections.Count);
            Assert.NotNull(ss.CloseBraceToken);
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
            Assert.NotNull(ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotNull(ss.CloseParenToken);
            Assert.NotNull(ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);
            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotNull(ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.NotNull(caseLabelSyntax.ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotNull(ss.CloseBraceToken);
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
            Assert.NotNull(ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotNull(ss.CloseParenToken);
            Assert.NotNull(ss.OpenBraceToken);

            Assert.Equal(2, ss.Sections.Count);

            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotNull(ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.NotNull(caseLabelSyntax.ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.Equal(1, ss.Sections[1].Labels.Count);
            Assert.NotNull(ss.Sections[1].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[1].Labels[0].Keyword.Kind());
            var caseLabelSyntax2 = ss.Sections[1].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax2);
            Assert.NotNull(caseLabelSyntax2.Value);
            Assert.Equal("c", caseLabelSyntax2.Value.ToString());
            Assert.NotNull(caseLabelSyntax2.ColonToken);
            Assert.Equal(1, ss.Sections[1].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotNull(ss.CloseBraceToken);
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
            Assert.NotNull(ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotNull(ss.CloseParenToken);
            Assert.NotNull(ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);

            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotNull(ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.DefaultKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            Assert.Equal(SyntaxKind.DefaultSwitchLabel, ss.Sections[0].Labels[0].Kind());
            Assert.NotNull(ss.Sections[0].Labels[0].ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotNull(ss.CloseBraceToken);
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
            Assert.NotNull(ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotNull(ss.CloseParenToken);
            Assert.NotNull(ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);

            Assert.Equal(2, ss.Sections[0].Labels.Count);
            Assert.NotNull(ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.NotNull(ss.Sections[0].Labels[1].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[1].Keyword.Kind());
            var caseLabelSyntax2 = ss.Sections[0].Labels[1] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax2);
            Assert.NotNull(caseLabelSyntax2.Value);
            Assert.Equal("c", caseLabelSyntax2.Value.ToString());
            Assert.NotNull(ss.Sections[0].Labels[0].ColonToken);
            Assert.Equal(1, ss.Sections[0].Statements.Count);
            Assert.Equal(";", ss.Sections[0].Statements[0].ToString());

            Assert.NotNull(ss.CloseBraceToken);
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
            Assert.NotNull(ss.SwitchKeyword);
            Assert.Equal(SyntaxKind.SwitchKeyword, ss.SwitchKeyword.Kind());
            Assert.NotNull(ss.OpenParenToken);
            Assert.NotNull(ss.Expression);
            Assert.Equal("a", ss.Expression.ToString());
            Assert.NotNull(ss.CloseParenToken);
            Assert.NotNull(ss.OpenBraceToken);

            Assert.Equal(1, ss.Sections.Count);

            Assert.Equal(1, ss.Sections[0].Labels.Count);
            Assert.NotNull(ss.Sections[0].Labels[0].Keyword);
            Assert.Equal(SyntaxKind.CaseKeyword, ss.Sections[0].Labels[0].Keyword.Kind());
            var caseLabelSyntax = ss.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(caseLabelSyntax);
            Assert.NotNull(caseLabelSyntax.Value);
            Assert.Equal("b", caseLabelSyntax.Value.ToString());
            Assert.Equal(2, ss.Sections[0].Statements.Count);
            Assert.Equal("s1();", ss.Sections[0].Statements[0].ToString());
            Assert.Equal("s2();", ss.Sections[0].Statements[1].ToString());

            Assert.NotNull(ss.CloseBraceToken);
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
            Assert.NotNull(us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.OpenParenToken);
            Assert.Null(us.Declaration);
            Assert.NotNull(us.Expression);
            Assert.Equal("a", us.Expression.ToString());
            Assert.NotNull(us.CloseParenToken);
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
            Assert.NotNull(us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.OpenParenToken);

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("T", us.Declaration.Type.ToString());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotNull(us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Null(us.Expression);

            Assert.NotNull(us.CloseParenToken);
            Assert.NotNull(us.Statement);
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
            Assert.NotNull(us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.OpenParenToken);

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("var", us.Declaration.Type.ToString());
            Assert.Equal(SyntaxKind.IdentifierName, us.Declaration.Type.Kind());
            Assert.Equal(SyntaxKind.IdentifierToken, ((IdentifierNameSyntax)us.Declaration.Type).Identifier.Kind());
            Assert.Equal(1, us.Declaration.Variables.Count);
            Assert.NotNull(us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.Null(us.Expression);

            Assert.NotNull(us.CloseParenToken);
            Assert.NotNull(us.Statement);
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
            Assert.NotNull(us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.OpenParenToken);

            Assert.NotNull(us.Declaration);
            Assert.NotNull(us.Declaration.Type);
            Assert.Equal("T", us.Declaration.Type.ToString());

            Assert.Equal(2, us.Declaration.Variables.Count);

            Assert.NotNull(us.Declaration.Variables[0].Identifier);
            Assert.Equal("a", us.Declaration.Variables[0].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[0].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[0].Initializer);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[0].Initializer.Value);
            Assert.Equal("b", us.Declaration.Variables[0].Initializer.Value.ToString());

            Assert.NotNull(us.Declaration.Variables[1].Identifier);
            Assert.Equal("c", us.Declaration.Variables[1].Identifier.ToString());
            Assert.Null(us.Declaration.Variables[1].ArgumentList);
            Assert.NotNull(us.Declaration.Variables[1].Initializer);
            Assert.NotNull(us.Declaration.Variables[1].Initializer.EqualsToken);
            Assert.NotNull(us.Declaration.Variables[1].Initializer.Value);
            Assert.Equal("d", us.Declaration.Variables[1].Initializer.Value.ToString());

            Assert.Null(us.Expression);

            Assert.NotNull(us.CloseParenToken);
            Assert.NotNull(us.Statement);
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
            Assert.NotNull(us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.OpenParenToken);
            Assert.Null(us.Declaration);
            Assert.NotNull(us.Expression);
            Assert.Equal("f ? x = a : x = b", us.Expression.ToString());
            Assert.NotNull(us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestUsingSpecialCase2()
        {
            var text = "using (f ? x = a) { }";
            var statement = this.ParseStatement(text);

            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.UsingStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());
            Assert.Equal(0, statement.Errors().Length);

            var us = (UsingStatementSyntax)statement;
            Assert.NotNull(us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.OpenParenToken);
            Assert.NotNull(us.Declaration);
            Assert.Equal("f ? x = a", us.Declaration.ToString());
            Assert.Null(us.Expression);
            Assert.NotNull(us.CloseParenToken);
            Assert.NotNull(us.Statement);
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
            Assert.NotNull(us.UsingKeyword);
            Assert.Equal(SyntaxKind.UsingKeyword, us.UsingKeyword.Kind());
            Assert.NotNull(us.OpenParenToken);
            Assert.NotNull(us.Declaration);
            Assert.Equal("f ? x, y", us.Declaration.ToString());
            Assert.Null(us.Expression);
            Assert.NotNull(us.CloseParenToken);
            Assert.NotNull(us.Statement);
        }

        [Fact]
        public void TestPartialAsLocalVariableType1()
        {
            var text = "partial v1 = null;";
            var statement = this.ParseStatement(text);
            Assert.NotNull(statement);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, statement.Kind());
            Assert.Equal(text, statement.ToString());

            var decl = (LocalDeclarationStatementSyntax)statement;
            Assert.Equal("partial", decl.Declaration.Type.ToString());
            Assert.IsType(typeof(IdentifierNameSyntax), decl.Declaration.Type);
            var name = (IdentifierNameSyntax)decl.Declaration.Type;
            Assert.Equal(SyntaxKind.PartialKeyword, name.Identifier.ContextualKind());
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
            Assert.NotNull(fs.ForEachKeyword);
            Assert.Equal(SyntaxKind.ForEachKeyword, fs.ForEachKeyword.Kind());

            Assert.NotNull(fs.OpenParenToken);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.NotNull(fs.Identifier);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal(SyntaxKind.InKeyword, fs.InKeyword.Kind());
            Assert.NotNull(fs.Expression);
            Assert.Equal("b", fs.Expression.ToString());
            Assert.NotNull(fs.CloseParenToken);
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
                CSharpTestBaseBase.Diagnostic(ErrorCode.ERR_SyntaxError, "if").WithArguments("when", "if").WithLocation(7, 36));

            var filterClause = root.DescendantNodes().OfType<CatchFilterClauseSyntax>().Single();
            Assert.Equal(SyntaxKind.WhenKeyword, filterClause.WhenKeyword.Kind());
            Assert.True(filterClause.WhenKeyword.HasStructuredTrivia);
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
