﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding index/range expressions.
    /// </summary>
    public class IndexAndRangeTests : CompilingTestBase
    {
        [Fact]
        public void IndexExpression_TypeNotFound()
        {
            var compilation = CreateCompilation(@"
class Test
{
    void M(int arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (6,17): error CS0656: Missing compiler required member 'System.Index..ctor'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^arg").WithArguments("System.Index", ".ctor").WithLocation(6, 17));
        }

        [Fact]
        public void IndexExpression_SemanticModel()
        {
            var compilation = CreateCompilationWithIndex(@"
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
            var compilation = CreateCompilationWithIndex(@"
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
        public void IndexExpression_InvalidTypes()
        {
            var compilation = CreateCompilationWithIndex(@"
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
            var compilation = CreateCompilationWithIndex(@"
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
            var compilation = CreateCompilationWithIndex(@"
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
        
        [Fact]
        public void RangeExpression_RangeNotFound()
        {
            var compilation = CreateCompilationWithIndex(@"
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics(
                // (6,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1..2").WithArguments("System.Range").WithLocation(6, 17),
                // (7,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1..").WithArguments("System.Range").WithLocation(7, 17),
                // (8,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..2").WithArguments("System.Range").WithLocation(8, 17),
                // (9,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..").WithArguments("System.Range").WithLocation(9, 17));
        }

        [Fact]
        public void RangeExpression_WithoutRangeCreate()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        // public static Range Create(Index start, Index end) => default;
        public static Range FromStart(Index start) => default;
        public static Range ToEnd(Index end) => default;
        public static Range All() => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics(
                // (16,17): error CS0656: Missing compiler required member 'System.Range.Create'
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1..2").WithArguments("System.Range", "Create").WithLocation(16, 17));
        }

        [Fact]
        public void RangeExpression_WithoutRangeFromStart()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public static Range Create(Index start, Index end) => default;
        // public static Range FromStart(Index start) => default;
        public static Range ToEnd(Index end) => default;
        public static Range All() => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics(
                // (17,17): error CS0656: Missing compiler required member 'System.Range.FromStart'
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1..").WithArguments("System.Range", "FromStart").WithLocation(17, 17));
        }

        [Fact]
        public void RangeExpression_WithoutRangeToEnd()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public static Range Create(Index start, Index end) => default;
        public static Range FromStart(Index start) => default;
        // public static Range ToEnd(Index end) => default;
        public static Range All() => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics(
                // (18,17): error CS0656: Missing compiler required member 'System.Range.ToEnd'
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..2").WithArguments("System.Range", "ToEnd").WithLocation(18, 17));
        }

        [Fact]
        public void RangeExpression_WithoutRangeAll()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public static Range Create(Index start, Index end) => default;
        public static Range FromStart(Index start) => default;
        public static Range ToEnd(Index end) => default;
        // public static Range All() => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics(
                // (19,17): error CS0656: Missing compiler required member 'System.Range.All'
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..").WithArguments("System.Range", "All").WithLocation(19, 17));
        }

        [Fact]
        public void RangeExpression_SemanticModel()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
class Test
{
    void M(Index start, Index end)
    {
        var a = start..end;
        var b = start..;
        var c = ..end;
        var d = ..;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expressions = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ToArray();
            Assert.Equal(4, expressions.Length);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[0]).Type.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Null(expressions[3].RightOperand);
            Assert.Null(expressions[3].LeftOperand);
        }

        [Fact]
        public void RangeExpression_Nullable_SemanticModel()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
class Test
{
    void M(Index? start, Index? end)
    {
        var a = start..end;
        var b = start..;
        var c = ..end;
        var d = ..;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expressions = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ToArray();
            Assert.Equal(4, expressions.Length);

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[0]).Type.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Null(expressions[3].RightOperand);
            Assert.Null(expressions[3].LeftOperand);
        }

        [Fact]
        public void RangeExpression_InvalidTypes()
        {
            // PROTOTYPE: add more test for complex conversions

            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var x = 1..""string"";
        var y = 1.5..;
        var z = ..true;
    }
}").VerifyDiagnostics(
                // (6,20): error CS0029: Cannot implicitly convert type 'string' to 'System.Index'
                //         var x = 1.."string";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""string""").WithArguments("string", "System.Index").WithLocation(6, 20),
                // (7,17): error CS0029: Cannot implicitly convert type 'double' to 'System.Index'
                //         var y = 1.5..;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1.5").WithArguments("double", "System.Index").WithLocation(7, 17),
                // (8,19): error CS0029: Cannot implicitly convert type 'bool' to 'System.Index'
                //         var z = ..true;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "true").WithArguments("bool", "System.Index").WithLocation(8, 19));
        }

        [Fact]
        public void RangeExpression_NoOperatorOverloading()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
public class Test
{
    public static Test operator ..(Test value) => default;
    public static Test operator ..(Test value1, Test value2) => default;
}").VerifyDiagnostics(
                // (4,33): error CS1019: Overloadable unary operator expected
                //     public static Test operator ..(Test value) => default;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "..").WithLocation(4, 33),
                // (5,33): error CS1020: Overloadable binary operator expected
                //     public static Test operator ..(Test value1, Test value2) => default;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "..").WithLocation(5, 33));
        }

        [Fact]
        public void RangeExpression_OlderLanguageVersion()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}", parseOptions: new CSharpParseOptions(LanguageVersion.CSharp7_3)).VerifyDiagnostics(
                // (6,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8 or greater.
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "1..2").WithArguments("range operator", "8").WithLocation(6, 17),
                // (7,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8 or greater.
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "1..").WithArguments("range operator", "8").WithLocation(7, 17),
                // (8,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8 or greater.
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "..2").WithArguments("range operator", "8").WithLocation(8, 17),
                // (9,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8 or greater.
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "..").WithArguments("range operator", "8").WithLocation(9, 17));
        }
    }
}
