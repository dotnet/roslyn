// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalFunctionParsingTests : ParsingTests
    {
        [Fact]
        public void NoNodesWithoutExperimental()
        {
            // Experimental nodes should only appear when experimental are
            // turned on in parse options
            var file = ParseFile(@"
class c
{
    void m()
    {
        int local() => 0;
    }
}");
            Assert.NotNull(file);
            Assert.True(file.HasErrors);
            file.SyntaxTree.GetDiagnostics().Verify(
                // (6,18): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                //         int local() => 0;
                Diagnostic(ErrorCode.ERR_BadVarDecl, "() => 0").WithLocation(6, 18),
                // (6,18): error CS1003: Syntax error, '[' expected
                //         int local() => 0;
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(6, 18),
                // (6,25): error CS1003: Syntax error, ']' expected
                //         int local() => 0;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(6, 25));

            Assert.Equal(0, file.SyntaxTree.Options.Features.Count);
            var c = Assert.IsType<ClassDeclarationSyntax>(file.Members.Single());
            var m = Assert.IsType<MethodDeclarationSyntax>(c.Members.Single());
            var s1 = Assert.IsType<LocalDeclarationStatementSyntax>(m.Body.Statements[0]);
            Assert.Equal(SyntaxKind.PredefinedType, s1.Declaration.Type.Kind());
            Assert.Equal("int", s1.Declaration.Type.ToString());
            var local = s1.Declaration.Variables.Single();
            Assert.Equal("local", local.Identifier.ToString());
            Assert.NotNull(local.ArgumentList);
            Assert.Null(local.Initializer);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, local.ArgumentList.Arguments.Single().Expression.Kind());
        }

        [Fact]
        public void NodesWithExperimental()
        {
            // Experimental nodes should only appear when experimental are
            // turned on in parse options
            var file = ParseFileExperimental(@"
class c
{
    void m()
    {
        int local() => 0;
    }
}");
            Assert.NotNull(file);
            Assert.False(file.HasErrors);
            Assert.Equal(0, file.SyntaxTree.Options.Features.Count);
            var c = Assert.IsType<ClassDeclarationSyntax>(file.Members.Single());
            var m = Assert.IsType<MethodDeclarationSyntax>(c.Members.Single());
            var s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.Equal(SyntaxKind.PredefinedType, s1.ReturnType.Kind());
            Assert.Equal("int", s1.ReturnType.ToString());
            Assert.Equal("local", s1.Identifier.ToString());
            Assert.NotNull(s1.ParameterList);
            Assert.Empty(s1.ParameterList.Parameters);
            Assert.NotNull(s1.ExpressionBody);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, s1.ExpressionBody.Expression.Kind());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/10388")]
        public void LocalFunctionsWithAwait()
        {
            var file = ParseFileExperimental(@"class c
{
    void m1() { await await() => new await(); }
    void m2() { await () => new await(); }
    async void m3() { await () => new await(); }
    void m4() { async await() => new await(); }
}");

            Assert.NotNull(file);
            var c = (ClassDeclarationSyntax)file.Members.Single();
            Assert.Equal(4, c.Members.Count);

            {
                Assert.Equal(SyntaxKind.MethodDeclaration, c.Members[0].Kind());
                var m1 = (MethodDeclarationSyntax)c.Members[0];
                Assert.Equal(0, m1.Modifiers.Count);
                Assert.Equal(1, m1.Body.Statements.Count);
                Assert.Equal(SyntaxKind.LocalFunctionStatement, m1.Body.Statements[0].Kind());
                var s1 = (LocalFunctionStatementSyntax)m1.Body.Statements[0];
                Assert.False(s1.HasErrors);
                Assert.Equal(0, s1.Modifiers.Count);
                Assert.Equal("await", s1.ReturnType.ToString());
                Assert.Equal("await", s1.Identifier.ToString());
                Assert.Null(s1.TypeParameterList);
                Assert.Equal(0, s1.ParameterList.ParameterCount);
                Assert.Null(s1.Body);
                Assert.NotNull(s1.ExpressionBody);
            }

            {
                Assert.Equal(SyntaxKind.MethodDeclaration, c.Members[1].Kind());
                var m2 = (MethodDeclarationSyntax)c.Members[1];
                Assert.Equal(0, m2.Modifiers.Count);
                Assert.Equal(2, m2.Body.Statements.Count);
                Assert.Equal(SyntaxKind.ExpressionStatement, m2.Body.Statements[0].Kind());
                var s1 = (ExpressionStatementSyntax)m2.Body.Statements[0];
                Assert.Equal(SyntaxKind.InvocationExpression, s1.Expression.Kind());
                var e1 = (InvocationExpressionSyntax)s1.Expression;
                Assert.Equal("await", e1.Expression.ToString());
                Assert.Equal(0, e1.ArgumentList.Arguments.Count);
                Assert.True(s1.SemicolonToken.IsMissing);
                Assert.Equal("=> ", s1.GetTrailingTrivia().ToFullString());
            }

            {
                Assert.Equal(SyntaxKind.MethodDeclaration, c.Members[2].Kind());
                var m3 = (MethodDeclarationSyntax)c.Members[2];
                Assert.Equal(1, m3.Modifiers.Count);
                Assert.Equal("async", m3.Modifiers.Single().ToString());
                Assert.Equal(2, m3.Body.Statements.Count);
                Assert.Equal(SyntaxKind.ExpressionStatement, m3.Body.Statements[0].Kind());
                var s1 = (ExpressionStatementSyntax)m3.Body.Statements[0];
                Assert.Equal(SyntaxKind.AwaitExpression, s1.Expression.Kind());
                var e1 = (AwaitExpressionSyntax)s1.Expression;
                Assert.Equal(SyntaxKind.SimpleLambdaExpression, e1.Expression.Kind());
                Assert.True(s1.SemicolonToken.IsMissing);
                Assert.Equal("=> ", s1.GetTrailingTrivia().ToFullString());
            }
        }
    }
}
