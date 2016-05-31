﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class LocalFunctionParsingTests : ParsingTests
    {
        [Fact]
        public void DiagnosticsWithoutExperimental()
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
    void m2()
    {
        int local() { return 0; }
    }
}");
            Assert.NotNull(file);
            Assert.False(file.DescendantNodes().Any(n => n.Kind() == SyntaxKind.LocalFunctionStatement && !n.ContainsDiagnostics));
            Assert.True(file.HasErrors);
            file.SyntaxTree.GetDiagnostics().Verify(
                // (6,9): error CS8058: Feature 'local functions' is experimental and unsupported; use '/features:localFunctions' to enable.
                //         int local() => 0;
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "int local() => 0;").WithArguments("local functions", "localFunctions").WithLocation(6, 9),
                // (10,9): error CS8058: Feature 'local functions' is experimental and unsupported; use '/features:localFunctions' to enable.
                //         int local() { return 0; }
                Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "int local() { return 0; }").WithArguments("local functions", "localFunctions").WithLocation(10, 9)
                );

            Assert.Equal(0, file.SyntaxTree.Options.Features.Count);
            var c = Assert.IsType<ClassDeclarationSyntax>(file.Members.Single());
            Assert.Equal(2, c.Members.Count);
            var m = Assert.IsType<MethodDeclarationSyntax>(c.Members[0]);
            var s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.True(s1.ContainsDiagnostics);

            var m2 = Assert.IsType<MethodDeclarationSyntax>(c.Members[1]);
            s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.True(s1.ContainsDiagnostics);
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
    void m2()
    {
        int local()
        {
            return 0;
        }
    }
}");
            Assert.NotNull(file);
            Assert.False(file.HasErrors);
            Assert.Equal(0, file.SyntaxTree.Options.Features.Count);
            var c = Assert.IsType<ClassDeclarationSyntax>(file.Members.Single());
            Assert.Equal(2, c.Members.Count);
            var m = Assert.IsType<MethodDeclarationSyntax>(c.Members[0]);
            var s1 = Assert.IsType<LocalFunctionStatementSyntax>(m.Body.Statements[0]);
            Assert.Equal(SyntaxKind.PredefinedType, s1.ReturnType.Kind());
            Assert.Equal("int", s1.ReturnType.ToString());
            Assert.Equal("local", s1.Identifier.ToString());
            Assert.NotNull(s1.ParameterList);
            Assert.Empty(s1.ParameterList.Parameters);
            Assert.NotNull(s1.ExpressionBody);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, s1.ExpressionBody.Expression.Kind());

            var m2 = Assert.IsType<MethodDeclarationSyntax>(c.Members[1]);
            s1 = Assert.IsType<LocalFunctionStatementSyntax>(m2.Body.Statements[0]);
            Assert.Equal(SyntaxKind.PredefinedType, s1.ReturnType.Kind());
            Assert.Equal("int", s1.ReturnType.ToString());
            Assert.Equal("local", s1.Identifier.ToString());
            Assert.NotNull(s1.ParameterList);
            Assert.Empty(s1.ParameterList.Parameters);
            Assert.Null(s1.ExpressionBody);
            Assert.NotNull(s1.Body);
            var s2 = Assert.IsType<ReturnStatementSyntax>(s1.Body.Statements.Single());
            Assert.Equal(SyntaxKind.NumericLiteralExpression, s2.Expression.Kind());
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
