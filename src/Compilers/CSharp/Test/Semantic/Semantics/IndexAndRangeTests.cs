// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
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
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^arg").WithArguments("System.Index", ".ctor").WithLocation(6, 17),
                // (6,17): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^arg").WithArguments("System.Index").WithLocation(6, 17));
        }

        [Fact]
        public void IndexExpression_LiftedTypeIsNotNullable()
        {
            var compilation = CreateCompilation(@"
namespace System
{
    public class Index
    {
        public Index(int value, bool fromEnd) { }
    }
}
class Test
{
    void M(int? arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (13,17): error CS0453: The type 'Index' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "^arg").WithArguments("System.Nullable<T>", "T", "System.Index").WithLocation(13, 17));
        }

        [Fact]
        public void IndexExpression_NullableConstructorNotFound()
        {
            var compilation = CreateEmptyCompilation(@"
namespace System
{
    public struct Int32 { }
    public struct Boolean { }
    public class ValueType { }
    public class String { }
    public class Object { }
    public class Void { }
    public struct Nullable<T> where T : struct
    {
    }
    public struct Index
    {
        public Index(int value, bool fromEnd) { }
    }
}
class Test
{
    void M(int? arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (22,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(22, 17));
        }

        [Fact]
        public void IndexExpression_ConstructorNotFound()
        {
            var compilation = CreateCompilation(@"
namespace System
{
    public readonly struct Index
    {
    }
}
class Test
{
    void M(int arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (12,17): error CS0656: Missing compiler required member 'System.Index..ctor'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^arg").WithArguments("System.Index", ".ctor").WithLocation(12, 17));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal("System.Index", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(expression).Symbol);
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
            Assert.Equal("System.Index..ctor(System.Int32 value, System.Boolean fromEnd)", model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
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
            Assert.Equal("System.Index..ctor(System.Int32 value, System.Boolean fromEnd)", model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
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
                //(6,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         var x = ^"string";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"^""string""").WithArguments("string", "int").WithLocation(6, 17),
                //(7,17): error CS0029: Cannot implicitly convert type 'double' to 'int'
                //         var y = ^1.5;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "^1.5").WithArguments("double", "int").WithLocation(7, 17),
                //(8,17): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                //         var z = ^true;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "^true").WithArguments("bool", "int").WithLocation(8, 17));
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
                // (6,17): error CS8370: Feature 'index operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x = ^1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "^1").WithArguments("index operator", "8.0").WithLocation(6, 17));
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
        public void RangeExpression_LiftedRangeNotNullable()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public class Range
    {
        public static Range Create(Index start, Index end) => default;
    }
}
class Test
{
    void M(System.Index? index)
    {
        var a = index..index;
    }
}").VerifyDiagnostics(
                // (13,17): error CS0453: The type 'Range' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "index..index").WithArguments("System.Nullable<T>", "T", "System.Range").WithLocation(13, 17));
        }

        [Fact]
        public void RangeExpression_LiftedIndexNotNullable()
        {
            var compilation = CreateCompilation(@"
namespace System
{
    public class Index
    {
        public Index(int value, bool fromEnd) { }
        public static implicit operator Index(int value) => new Index(value, fromEnd: false);
    }
    public readonly struct Range
    {
        public static Range Create(Index start, Index end) => default;
    }
}
class Test
{
    void M(int? index)
    {
        var a = index..index;
    }
}").VerifyDiagnostics(
                // (18,17): error CS0453: The type 'Index' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "index").WithArguments("System.Nullable<T>", "T", "System.Index").WithLocation(18, 17),
                // (18,17): error CS0029: Cannot implicitly convert type 'int?' to 'System.Index?'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "index").WithArguments("int?", "System.Index?").WithLocation(18, 17),
                // (18,24): error CS0453: The type 'Index' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "index").WithArguments("System.Nullable<T>", "T", "System.Index").WithLocation(18, 24),
                // (18,24): error CS0029: Cannot implicitly convert type 'int?' to 'System.Index?'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "index").WithArguments("int?", "System.Index?").WithLocation(18, 24));
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(0);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(expression).Symbol);
        }

        [Fact]
        public void RangeExpression_NullableConstructorNotFound()
        {
            var compilation = CreateEmptyCompilation(@"
namespace System
{
    public struct Int32 { }
    public struct Boolean { }
    public class ValueType { }
    public class String { }
    public class Object { }
    public class Void { }
    public struct Nullable<T> where T : struct
    {
    }
    public struct Index
    {
        public Index(int value, bool fromEnd) { }
    }
    public readonly struct Range
    {
        public static Range Create(Index start, Index end) => default;
    }
}
class Test
{
    void M(System.Index? arg)
    {
        var x = arg..arg;
    }
}").VerifyDiagnostics(
                // (26,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(26, 17),
                // (26,22): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(26, 22),
                // (26,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg..arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(26, 17));
        }

        [Fact]
        public void RangeExpression_BooleanNotFound()
        {
            var compilation = CreateEmptyCompilation(@"
namespace System
{
    public struct Int32 { }
    public class ValueType { }
    public class String { }
    public class Object { }
    public class Void { }
    public struct Nullable<T> where T : struct
    {
        public Nullable(T value) { }
    }
    public struct Index
    {
    }
    public readonly struct Range
    {
        public static Range Create(Index start, Index end) => default;
    }
}
class Test
{
    void M(System.Index? arg)
    {
        var x = arg..arg;
    }
}").VerifyDiagnostics(
                // (25,17): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "arg..arg").WithArguments("System.Boolean").WithLocation(25, 17));
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(1);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(expression).Symbol);
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(2);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(expression).Symbol);
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

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(3);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(expression).Symbol);
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
            Assert.Equal("System.Range System.Range.Create(System.Index start, System.Index end)", model.GetSymbolInfo(expressions[0]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Equal("System.Range System.Range.FromStart(System.Index start)", model.GetSymbolInfo(expressions[1]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal("System.Range System.Range.ToEnd(System.Index end)", model.GetSymbolInfo(expressions[2]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Equal("System.Range System.Range.All()", model.GetSymbolInfo(expressions[3]).Symbol.ToTestDisplayString());
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
            Assert.Equal("System.Range System.Range.Create(System.Index start, System.Index end)", model.GetSymbolInfo(expressions[0]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Equal("System.Range System.Range.FromStart(System.Index start)", model.GetSymbolInfo(expressions[1]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal("System.Range System.Range.ToEnd(System.Index end)", model.GetSymbolInfo(expressions[2]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Equal("System.Range System.Range.All()", model.GetSymbolInfo(expressions[3]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[3].RightOperand);
            Assert.Null(expressions[3].LeftOperand);
        }

        [Fact]
        public void RangeExpression_InvalidTypes()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var a = 1..""string"";
        var b = 1.5..;
        var c = ..true;
        var d = ..M();
    }
}").VerifyDiagnostics(
                // (6,20): error CS0029: Cannot implicitly convert type 'string' to 'System.Index'
                //         var a = 1.."string";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""string""").WithArguments("string", "System.Index").WithLocation(6, 20),
                // (7,17): error CS0029: Cannot implicitly convert type 'double' to 'System.Index'
                //         var b = 1.5..;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1.5").WithArguments("double", "System.Index").WithLocation(7, 17),
                // (8,19): error CS0029: Cannot implicitly convert type 'bool' to 'System.Index'
                //         var c = ..true;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "true").WithArguments("bool", "System.Index").WithLocation(8, 19),
                // (9,19): error CS0029: Cannot implicitly convert type 'void' to 'System.Index'
                //         var d = ..M();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "M()").WithArguments("void", "System.Index").WithLocation(9, 19));
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
                // (6,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "1..2").WithArguments("range operator", "8.0").WithLocation(6, 17),
                // (7,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "1..").WithArguments("range operator", "8.0").WithLocation(7, 17),
                // (8,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "..2").WithArguments("range operator", "8.0").WithLocation(8, 17),
                // (9,17): error CS8370: Feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "..").WithArguments("range operator", "8.0").WithLocation(9, 17));
        }

        [Fact]
        public void IndexOnNonTypedNodes()
        {
            CreateCompilationWithIndex(@"
class Test
{
    void M()
    {
        var a = ^M;
        var b = ^null;
        var c = ^default;
    }
}").VerifyDiagnostics(
                // (6,17): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
                //         var a = ^M;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "^M").WithArguments("M", "int").WithLocation(6, 17),
                // (7,17): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         var b = ^null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "^null").WithArguments("int").WithLocation(7, 17));
        }

        [Fact]
        public void RangeOnNonTypedNodes()
        {
            CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var a = 0..M;
        var b = 0..null;
        var c = 0..default;

        var d = M..0;
        var e = null..0;
        var f = default..0;
    }
}").VerifyDiagnostics(
                // (6,20): error CS0428: Cannot convert method group 'M' to non-delegate type 'Index'. Did you intend to invoke the method?
                //         var a = 0..M;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "System.Index").WithLocation(6, 20),
                // (7,20): error CS0037: Cannot convert null to 'Index' because it is a non-nullable value type
                //         var b = 0..null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Index").WithLocation(7, 20),
                // (10,17): error CS0428: Cannot convert method group 'M' to non-delegate type 'Index'. Did you intend to invoke the method?
                //         var d = M..0;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "System.Index").WithLocation(10, 17),
                // (11,17): error CS0037: Cannot convert null to 'Index' because it is a non-nullable value type
                //         var e = null..0;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Index").WithLocation(11, 17));
        }

        [Fact]
        public void Range_OnVarOut_Error()
        {
            CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        var result = y..Create(out Index y);
    }
    static Index Create(out Index y)
    {
        y = ^2;
        return ^1;
    }
}").VerifyDiagnostics(
                // (7,22): error CS0841: Cannot use local variable 'y' before it is declared
                //         var result = y..Create(out Index y);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(7, 22));
        }
    }
}
