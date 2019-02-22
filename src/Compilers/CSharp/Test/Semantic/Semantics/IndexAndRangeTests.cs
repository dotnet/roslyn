// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IndexAndRangeTests : CompilingTestBase
    {
        private const string RangeCtorSignature = "System.Range..ctor(System.Index start, System.Index end)";

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void ArrayRangeIllegalRef()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
public class C {
    public ref int[] M(int[] arr) {
        ref int[] x = ref arr[0..2];
        M(in arr[0..2]);
        M(arr[0..2]);
        return ref arr[0..2];
    }
    void M(in int[] arr) { }
}");
            comp.VerifyDiagnostics(
                // (4,27): error CS1510: A ref or out value must be an assignable variable
                //         ref int[] x = ref arr[0..2];
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "arr[0..2]").WithLocation(4, 27),
                // (5,14): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         M(in arr[0..2]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "arr[0..2]").WithLocation(5, 14),
                // (7,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref arr[0..2];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "arr[0..2]").WithLocation(7, 20));
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void ArrayRangeIllegalRefNoRange()
        {
            var comp = CreateCompilationWithIndex(@"
public class C {
    public void M(int[] arr) {
        ref int[] x = ref arr[0..2];
    }
}");
            comp.VerifyDiagnostics(
                // (4,31): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         ref int[] x = ref arr[0..2];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "0..2").WithArguments("System.Range").WithLocation(4, 31));
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void FromEndIllegalRef()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
public class C {
    public ref Index M() {
        ref Index x = ref ^0;
        M(in ^0);
        M(^0);
        return ref ^0;
    }
    void M(in int[] arr) { }
}");
            comp.VerifyDiagnostics(
                // (5,27): error CS1510: A ref or out value must be an assignable variable
                //         ref Index x = ref ^0;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "^0").WithLocation(5, 27),
                // (6,14): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         M(in ^0);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "^0").WithLocation(6, 14),
                // (7,11): error CS1503: Argument 1: cannot convert from 'System.Index' to 'in int[]'
                //         M(^0);
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "in int[]").WithLocation(7, 11),
                // (8,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref ^0;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "^0").WithLocation(8, 20));
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void FromEndIllegalRefNoIndex()
        {
            var comp = CreateCompilationWithIndex(@"
public class C {
    public void M() {
        ref var x = ref ^0;
    }
}");
            comp.VerifyDiagnostics(
                // (4,25): error CS1510: A ref or out value must be an assignable variable
                //         ref var x = ref ^0;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "^0").WithLocation(4, 25));
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void StringIndexIllegalRef()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
public class C {
    public ref char M(string s) {
        ref readonly char x = ref s[^2];
        M(in s[^2]);
        M(s[^2]);
        return ref s[^2];
    }
    void M(in char c) { }
}");
            comp.VerifyDiagnostics(
                // (4,35): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         ref readonly char x = ref s[^2];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "s[^2]").WithArguments("string.this[int]").WithLocation(4, 35),
                // (5,14): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         M(in s[^2]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "s[^2]").WithArguments("string.this[int]").WithLocation(5, 14),
                // (7,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref s[^2];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "s[^2]").WithArguments("string.this[int]").WithLocation(7, 20));
        }

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
            Assert.Equal("System.Index..ctor(System.Int32 value, [System.Boolean fromEnd = false])", model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
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
            Assert.Equal("System.Index..ctor(System.Int32 value, [System.Boolean fromEnd = false])", model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
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
            var expected = new[]
            {
                // (6,17): error CS8652: The feature 'index operator' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var x = ^1;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "^1").WithArguments("index operator").WithLocation(6, 17)
            };
            const string source = @"
class Test
{
    void M()
    {
        var x = ^1;
    }
}";
            var compilation = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(expected);
            compilation = CreateCompilationWithIndex(source, parseOptions: TestOptions.RegularDefault).VerifyDiagnostics(expected);
            compilation = CreateCompilationWithIndex(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
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
        public Range(Index start, Index end) { }
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
        public Range(Index start, Index end) { }
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
                // (16,17): error CS0656: Missing compiler required member 'System.Range..ctor'
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1..2").WithArguments("System.Range", ".ctor").WithLocation(16, 17),
                // (17,17): error CS0656: Missing compiler required member 'System.Range..ctor'
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1..").WithArguments("System.Range", ".ctor").WithLocation(17, 17),
                // (18,17): error CS0656: Missing compiler required member 'System.Range..ctor'
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..2").WithArguments("System.Range", ".ctor").WithLocation(18, 17),
                // (19,17): error CS0656: Missing compiler required member 'System.Range..ctor'
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "..").WithArguments("System.Range", ".ctor").WithLocation(19, 17));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            foreach (var node in tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>())
            {
                Assert.Equal("System.Range", model.GetTypeInfo(node).Type.ToTestDisplayString());
                Assert.Null(model.GetSymbolInfo(node).Symbol);
            }
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
        public Range(Index start, Index end) { }
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
        public Range(Index start, Index end) { }
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
        public Range(Index start, Index end) { }
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
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(1);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void RangeExpression_WithoutRangeToEnd()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
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
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(2);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void RangeExpression_WithoutRangeAll()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
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
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(3);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
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
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[0]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[1]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[2]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[3]).Symbol.ToTestDisplayString());
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
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[0]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[1]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[2]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[3]).Symbol.ToTestDisplayString());
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
            const string source = @"
class Test
{
    void M()
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}";
            var expected = new[]
            {
                // (6,17): error CS8652: The feature 'range operator' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "1..2").WithArguments("range operator").WithLocation(6, 17),
                // (7,17): error CS8652: The feature 'range operator' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "1..").WithArguments("range operator").WithLocation(7, 17),
                // (8,17): error CS8652: The feature 'range operator' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "..2").WithArguments("range operator").WithLocation(8, 17),
                // (9,17): error CS8652: The feature 'range operator' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "..").WithArguments("range operator").WithLocation(9, 17)
            };
            CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(expected);
            CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularDefault).VerifyDiagnostics(expected);
            CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
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
