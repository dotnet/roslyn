// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) index/range expressions.
    /// </summary>
    public class IndexAndRangeTests : CompilingTestBase
    {
        [Fact]
        public void IndexExpression_SemanticModel()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M(int arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal("^", expression.OperatorToken.ToFullString());
            Assert.Equal("System.Index", model.GetTypeInfo(expression).Type.ToTestDisplayString());
        }

        [Fact]
        public void IndexExpression_Nullable_SemanticModel()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M(int? arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal("^", expression.OperatorToken.ToFullString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expression).Type.ToTestDisplayString());
        }

        [Fact]
        public void IndexExpression_Errors()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var x = ^""string"";
        var y = ^1.5;
        var z = ^true;
    }
}").VerifyDiagnostics(
                // (6,17): error CS0023: Operator '^' cannot be applied to operand of type 'string'
                //         var x = ^"string";
                Diagnostic(ErrorCode.ERR_BadUnaryOp, @"^""string""").WithArguments("^", "string").WithLocation(6, 17),
                // (7,17): error CS0023: Operator '^' cannot be applied to operand of type 'double'
                //         var y = ^1.5;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "^1.5").WithArguments("^", "double").WithLocation(7, 17),
                // (8,17): error CS0023: Operator '^' cannot be applied to operand of type 'bool'
                //         var z = ^true;
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "^true").WithArguments("^", "bool").WithLocation(8, 17));
        }

        [Fact]
        public void IndexExpression_NoOperatorOverloading()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
public class Test
{
    public static Test operator ^(Test value) => default;  
}").VerifyDiagnostics(
                // (4,33): error CS1019: Overloadable unary operator expected
                //     public static Test operator ^(Test value) => default;  
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "^").WithLocation(4, 33));
        }

        [Fact]
        public void IndexExpression_OlderLanguageVersion()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var x = ^1;
    }
}", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3)).VerifyDiagnostics(
                // (6,17): error CS8370: Feature 'index operator' is not available in C# 7.3. Please use language version 8 or greater.
                //         var x = ^1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "^1").WithArguments("index operator", "8").WithLocation(6, 17));
        }
    }
}
