// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class StackAllocSpanExpressionsTests : CompilingTestBase
    {
        [Fact]
        public void ConversionFromPointerStackAlloc_UserDefined_Implicit()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method()
    {
        Test obj1 = stackalloc int[10];
        var obj2 = stackalloc int[10];
        Span<int> obj3 = stackalloc int[10];
        int* obj4 = stackalloc int[10];
        double* obj5 = stackalloc int[10];
    }
    
    public static implicit operator Test(int* value) 
    {
        return default(Test);
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (11,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[10]").WithArguments("int", "double*").WithLocation(11, 24));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var variables = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
            Assert.Equal(5, variables.Count());

            var obj1 = variables.ElementAt(0);
            Assert.Equal("obj1", obj1.Identifier.Text);

            var obj1Value = model.GetSemanticInfoSummary(obj1.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj1Value.Type).PointedAtType.SpecialType);
            Assert.Equal("Test", obj1Value.ConvertedType.Name);
            Assert.Equal(ConversionKind.ImplicitUserDefined, obj1Value.ImplicitConversion.Kind);

            var obj2 = variables.ElementAt(1);
            Assert.Equal("obj2", obj2.Identifier.Text);

            var obj2Value = model.GetSemanticInfoSummary(obj2.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.Identity, obj2Value.ImplicitConversion.Kind);

            var obj3 = variables.ElementAt(2);
            Assert.Equal("obj3", obj3.Identifier.Text);

            var obj3Value = model.GetSemanticInfoSummary(obj3.Initializer.Value);
            Assert.Equal("Span", obj3Value.Type.Name);
            Assert.Equal("Span", obj3Value.ConvertedType.Name);
            Assert.Equal(ConversionKind.Identity, obj3Value.ImplicitConversion.Kind);

            var obj4 = variables.ElementAt(3);
            Assert.Equal("obj4", obj4.Identifier.Text);

            var obj4Value = model.GetSemanticInfoSummary(obj4.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.Identity, obj4Value.ImplicitConversion.Kind);

            var obj5 = variables.ElementAt(4);
            Assert.Equal("obj5", obj5.Identifier.Text);

            var obj5Value = model.GetSemanticInfoSummary(obj5.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj5Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Double, ((IPointerTypeSymbol)obj5Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.NoConversion, obj5Value.ImplicitConversion.Kind);
        }

        [Fact]
        public void ConversionFromPointerStackAlloc_UserDefined_Explicit()
        {
            var comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
unsafe class Test
{
    public void Method()
    {
        Test obj1 = (Test)stackalloc int[10];
        var obj2 = stackalloc int[10];
        Span<int> obj3 = stackalloc int[10];
        int* obj4 = stackalloc int[10];
        double* obj5 = stackalloc int[10];
    }
    
    public static explicit operator Test(Span<int> value) 
    {
        return default(Test);
    }
}", TestOptions.UnsafeReleaseDll);

            comp.VerifyDiagnostics(
                // (11,24): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double*' is not possible.
                //         double* obj5 = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[10]").WithArguments("int", "double*").WithLocation(11, 24));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var variables = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
            Assert.Equal(5, variables.Count());

            var obj1 = variables.ElementAt(0);
            Assert.Equal("obj1", obj1.Identifier.Text);
            Assert.Equal(SyntaxKind.CastExpression, obj1.Initializer.Value.Kind());

            var obj1Value = model.GetSemanticInfoSummary(((CastExpressionSyntax)obj1.Initializer.Value).Expression);
            Assert.Equal("Span", obj1Value.Type.Name);
            Assert.Equal("Span", obj1Value.ConvertedType.Name);
            Assert.Equal(ConversionKind.Identity, obj1Value.ImplicitConversion.Kind);

            var obj2 = variables.ElementAt(1);
            Assert.Equal("obj2", obj2.Identifier.Text);

            var obj2Value = model.GetSemanticInfoSummary(obj2.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj2Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.Identity, obj2Value.ImplicitConversion.Kind);

            var obj3 = variables.ElementAt(2);
            Assert.Equal("obj3", obj3.Identifier.Text);

            var obj3Value = model.GetSemanticInfoSummary(obj3.Initializer.Value);
            Assert.Equal("Span", obj3Value.Type.Name);
            Assert.Equal("Span", obj3Value.ConvertedType.Name);
            Assert.Equal(ConversionKind.Identity, obj3Value.ImplicitConversion.Kind);

            var obj4 = variables.ElementAt(3);
            Assert.Equal("obj4", obj4.Identifier.Text);

            var obj4Value = model.GetSemanticInfoSummary(obj4.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj4Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.Identity, obj4Value.ImplicitConversion.Kind);

            var obj5 = variables.ElementAt(4);
            Assert.Equal("obj5", obj5.Identifier.Text);

            var obj5Value = model.GetSemanticInfoSummary(obj5.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((IPointerTypeSymbol)obj5Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Double, ((IPointerTypeSymbol)obj5Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.NoConversion, obj5Value.ImplicitConversion.Kind);
        }

        [Fact]
        public void ConversionError()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    void M()
    {
        double x = stackalloc int[10];          // implicit
        short y = (short)stackalloc int[10];    // explicit
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,20): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'double' is not possible.
                //         double x = stackalloc int[10];          // implicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[10]").WithArguments("int", "double").WithLocation(6, 20),
                // (7,19): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'short' is not possible.
                //         short y = (short)stackalloc int[10];    // explicit
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(short)stackalloc int[10]").WithArguments("int", "short").WithLocation(7, 19));
        }

        [Fact]
        public void MissingSpanType()
        {
            CreateCompilation(@"
class Test
{
    void M()
    {
        Span<int> a = stackalloc int [10];
    }
}").VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'Span<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Span<int> a = stackalloc int [10];
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Span<int>").WithArguments("Span<>").WithLocation(6, 9),
                // (6,23): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //         Span<int> a = stackalloc int [10];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc int [10]").WithArguments("System.Span`1").WithLocation(6, 23));
        }

        [Fact]
        public void MissingSpanConstructor()
        {
            CreateCompilation(@"
namespace System
{
    ref struct Span<T>
    {
    }
    class Test
    {
        void M()
        {
            Span<int> a = stackalloc int [10];
        }
    }
}").VerifyEmitDiagnostics(
                // (11,27): error CS0656: Missing compiler required member 'System.Span`1..ctor'
                //             Span<int> a = stackalloc int [10];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "stackalloc int [10]").WithArguments("System.Span`1", ".ctor").WithLocation(11, 27));
        }

        [Fact]
        public void ConditionalExpressionOnSpan_BothStackallocSpans()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    void M()
    {
        var x = true ? stackalloc int [10] : stackalloc int [5];
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ConditionalExpressionOnSpan_Convertible()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        var x = true ? stackalloc int [10] : (Span<int>)stackalloc int [5];
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ConditionalExpressionOnSpan_NoCast()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        var x = true ? stackalloc int [10] : (Span<int>)stackalloc short [5];
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,46): error CS8346: Conversion of a stackalloc expression of type 'short' to type 'Span<int>' is not possible.
                //         var x = true ? stackalloc int [10] : (Span<int>)stackalloc short [5];
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "(Span<int>)stackalloc short [5]").WithArguments("short", "System.Span<int>").WithLocation(7, 46));
        }

        [Fact]
        public void ConditionalExpressionOnSpan_CompatibleTypes()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        Span<int> a = stackalloc int [10];
        var x = true ? stackalloc int [10] : a;
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void ConditionalExpressionOnSpan_IncompatibleTypes()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        Span<short> a = stackalloc short [10];
        var x = true ? stackalloc int [10] : a;
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (8,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'System.Span<int>' and 'System.Span<short>'
                //         var x = true ? stackalloc int [10] : a;
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? stackalloc int [10] : a").WithArguments("System.Span<int>", "System.Span<short>").WithLocation(8, 17)
            );
        }

        [Fact]
        public void ConditionalExpressionOnSpan_Nested()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    bool N() => true;

    void M()
    {
        var x = N()
            ? N()
                ? stackalloc int [1]
                : stackalloc int [2]
            : N()
                ? stackalloc int[3]
                : N()
                    ? stackalloc int[4]
                    : stackalloc int[5];
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void BooleanOperatorOnSpan_NoTargetTyping()
        {
            var source = @"
class Test
{
    void M()
    {
        if(stackalloc int[10] == stackalloc int[10]) { }
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeReleaseDll, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,12): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         if(stackalloc int[10] == stackalloc int[10]) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 12),
                // (6,34): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         if(stackalloc int[10] == stackalloc int[10]) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 34)
                );
            CreateCompilationWithMscorlibAndSpan(source, TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,12): error CS0019: Operator '==' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         if(stackalloc int[10] == stackalloc int[10]) { }
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc int[10] == stackalloc int[10]").WithArguments("==", "System.Span<int>", "System.Span<int>").WithLocation(6, 12)
            );
        }

        [Fact]
        public void NewStackAllocSpanSyntaxProducesErrorsOnEarlierVersions_Statements()
        {
            var parseOptions = new CSharpParseOptions().WithLanguageVersion(LanguageVersion.CSharp7);

            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    void M()
    {
        Span<int> x = stackalloc int[10];
    }
}", options: TestOptions.UnsafeReleaseDll, parseOptions: parseOptions).VerifyDiagnostics(
                // (7,23): error CS8107: Feature 'ref structs' is not available in C# 7. Please use language version 7.2 or greater.
                //         Span<int> x = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc int[10]").WithArguments("ref structs", "7.2").WithLocation(7, 23));
        }

        [Fact]
        public void NewStackAllocSpanSyntaxProducesErrorsOnEarlierVersions_Expressions()
        {
            var parseOptions = new CSharpParseOptions().WithLanguageVersion(LanguageVersion.CSharp7);

            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    void M(bool condition)
    {
        var x = condition
            ? stackalloc int[10]
            : stackalloc int[100];
    }
}", options: TestOptions.UnsafeReleaseDll, parseOptions: parseOptions).VerifyDiagnostics(
                // (7,15): error CS8107: Feature 'ref structs' is not available in C# 7.0. Please use language version 7.2 or greater.
                //             ? stackalloc int[10]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc int[10]").WithArguments("ref structs", "7.2").WithLocation(7, 15),
                // (8,15): error CS8107: Feature 'ref structs' is not available in C# 7.0. Please use language version 7.2 or greater.
                //             : stackalloc int[100];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "stackalloc int[100]").WithArguments("ref structs", "7.2").WithLocation(8, 15));
        }

        [Fact]
        public void StackAllocSyntaxProducesUnsafeErrorInSafeCode()
        {
            CreateCompilation(@"
class Test
{
    void M()
    {
        var x = stackalloc int[10];
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var x = stackalloc int[10];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[10]").WithLocation(6, 17));
        }

        [Fact]
        public void StackAllocInUsing1()
        {
            var test = @"
public class Test
{
    unsafe public static void Main()
    {
        using (var v = stackalloc int[1])
        {
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,16): error CS1674: 'Span<int>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v = stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(6, 16)
            );
        }

        [Fact]
        public void StackAllocInUsing2()
        {
            var test = @"
public class Test
{
    unsafe public static void Main()
    {
        using (System.IDisposable v = stackalloc int[1])
        {
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,39): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'IDisposable' is not possible.
                //         using (System.IDisposable v = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[1]").WithArguments("int", "System.IDisposable").WithLocation(6, 39));
        }

        [Fact]
        public void ConstStackAllocExpression()
        {
            var test = @"
unsafe public class Test
{
    void M()
    {
        const int* p = stackalloc int[1];
    }
}
";
            CreateCompilation(test, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (6,15): error CS0283: The type 'int*' cannot be declared const
                //         const int* p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_BadConstType, "int*").WithArguments("int*").WithLocation(6, 15));
        }

        [Fact]
        public void RefStackAllocAssignment_ValueToRef()
        {
            var test = @"
using System;
public class Test
{
    void M()
    {
        ref Span<int> p = stackalloc int[1];
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (7,23): error CS8172: Cannot initialize a by-reference variable with a value
                //         ref Span<int> p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "p = stackalloc int[1]").WithLocation(7, 23),
                // (7,27): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc int[1]").WithLocation(7, 27));
        }

        [Fact]
        public void RefStackAllocAssignment_RefToRef()
        {
            var test = @"
using System;
public class Test
{
    void M()
    {
        ref Span<int> p = ref stackalloc int[1];
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (7,31): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         ref Span<int> p = ref stackalloc int[1];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 31)
                );
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (7,31): error CS1510: A ref or out value must be an assignable variable
                //         ref Span<int> p = ref stackalloc int[1];
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "stackalloc int[1]").WithLocation(7, 31)
            );
        }

        [Fact]
        public void InvalidPositionForStackAllocSpan()
        {
            var test = @"
using System;
public class Test
{
    void M()
    {
        N(stackalloc int[1]);
    }
    void N(Span<int> span)
    {
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (7,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc int[1]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 11)
                );
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular8).VerifyDiagnostics(
            );
        }

        [Fact]
        public void CannotDotIntoStackAllocExpression()
        {
            var test = @"
public class Test
{
    void M()
    {
        int length = (stackalloc int [10]).Length;
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,23): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         int length = (stackalloc int [10]).Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 23)
                );
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true), parseOptions: TestOptions.Regular8).VerifyDiagnostics(
            );
        }

        [Fact]
        public void OverloadResolution_Fail()
        {
            var test = @"
using System;
unsafe public class Test
{
    static void Main()
    {
        Invoke(stackalloc int [10]);
    }

    static void Invoke(Span<short> shortSpan) => Console.WriteLine(""shortSpan"");
    static void Invoke(Span<bool> boolSpan) => Console.WriteLine(""boolSpan"");
    static void Invoke(int* intPointer) => Console.WriteLine(""intPointer"");
    static void Invoke(void* voidPointer) => Console.WriteLine(""voidPointer"");
}
";
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.UnsafeReleaseExe, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (7,16): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         Invoke(stackalloc int [10]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(7, 16)
                );
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
                // (7,16): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'System.Span<short>'
                //         Invoke(stackalloc int [10]);
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc int [10]").WithArguments("1", "System.Span<int>", "System.Span<short>").WithLocation(7, 16)
            );
        }

        [Fact]
        public void StackAllocWithDynamic()
        {
            CreateCompilation(@"
class Program
{
    static void Main()
    {
        var d = stackalloc dynamic[10];
    }
}").VerifyDiagnostics(
                // (6,28): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         var d = stackalloc dynamic[10];
                Diagnostic(ErrorCode.ERR_ManagedAddr, "dynamic").WithArguments("dynamic").WithLocation(6, 28)
                );
        }

        [Fact]
        public void StackAllocWithDynamicSpan()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Program
{
    static void Main()
    {
        Span<dynamic> d = stackalloc dynamic[10];
    }
}").VerifyDiagnostics(
                // (7,38): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('dynamic')
                //         Span<dynamic> d = stackalloc dynamic[10];
                Diagnostic(ErrorCode.ERR_ManagedAddr, "dynamic").WithArguments("dynamic").WithLocation(7, 38)
                );
        }

        [Fact]
        public void StackAllocAsArgument()
        {
            var source = @"
class Program
{
    static void N(object p) { }

    static void Main()
    {
        N(stackalloc int[10]);
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (8,11): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         N(stackalloc int[10]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(8, 11)
                );
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (8,11): error CS1503: Argument 1: cannot convert from 'System.Span<int>' to 'object'
                //         N(stackalloc int[10]);
                Diagnostic(ErrorCode.ERR_BadArgType, "stackalloc int[10]").WithArguments("1", "System.Span<int>", "object").WithLocation(8, 11)
            );
        }

        [Fact]
        public void StackAllocInParenthesis()
        {
            var source = @"
class Program
{
    static void Main()
    {
        var x = (stackalloc int[10]);
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,18): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x = (stackalloc int[10]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 18)
                );
            CreateCompilationWithMscorlibAndSpan(source).VerifyDiagnostics(
                );
        }

        [Fact]
        public void StackAllocInNullConditionalOperator()
        {
            var source = @"
class Program
{
    static void Main()
    {
        var x = stackalloc int[1] ?? stackalloc int[2];
    }
}";
            CreateCompilationWithMscorlibAndSpan(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(
                // (6,17): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x = stackalloc int[1] ?? stackalloc int[2];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 17),
                // (6,38): error CS8652: The feature 'stackalloc in nested expressions' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x = stackalloc int[1] ?? stackalloc int[2];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "stackalloc").WithArguments("stackalloc in nested expressions", "8.0").WithLocation(6, 38)
                );
            CreateCompilationWithMscorlibAndSpan(source).VerifyDiagnostics(
                // (6,17): error CS0019: Operator '??' cannot be applied to operands of type 'Span<int>' and 'Span<int>'
                //         var x = stackalloc int[1] ?? stackalloc int[2];
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "stackalloc int[1] ?? stackalloc int[2]").WithArguments("??", "System.Span<int>", "System.Span<int>").WithLocation(6, 17)
                );
        }

        [Fact]
        public void StackAllocInCastAndConditionalOperator()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
class Test
{
    public void Method()
    {
        Test value = true ? new Test() : (Test)stackalloc int[10];
    }
    
    public static explicit operator Test(Span<int> value) 
    {
        return new Test();
    }
}", TestOptions.ReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(25038, "https://github.com/dotnet/roslyn/issues/25038")]
        public void StackAllocToSpanWithRefStructType()
        {
            CreateCompilationWithMscorlibAndSpan(@"
using System;
ref struct S {}
class Test
{
    void M()
    {
        Span<S> explicitError = default;
        var implicitError = explicitError.Length > 0 ? stackalloc S[10] : stackalloc S[100];
    }
}").VerifyDiagnostics(
                // (8,14): error CS9244: The type 'S' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Span<T>'
                //         Span<S> explicitError = default;
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "S").WithArguments("System.Span<T>", "T", "S").WithLocation(8, 14),
                // (9,67): error CS9244: The type 'S' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Span<T>'
                //         var implicitError = explicitError.Length > 0 ? stackalloc S[10] : stackalloc S[100];
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "S[10]").WithArguments("System.Span<T>", "T", "S").WithLocation(9, 67),
                // (9,86): error CS9244: The type 'S' may not be a ref struct or a type parameter allowing ref structs in order to use it as parameter 'T' in the generic type or method 'Span<T>'
                //         var implicitError = explicitError.Length > 0 ? stackalloc S[10] : stackalloc S[100];
                Diagnostic(ErrorCode.ERR_NotRefStructConstraintNotSatisfied, "S[100]").WithArguments("System.Span<T>", "T", "S").WithLocation(9, 86)
                );
        }

        [Fact]
        [WorkItem(25086, "https://github.com/dotnet/roslyn/issues/25086")]
        public void StaackAllocToSpanWithCustomSpanAndConstraints()
        {
            var code = @"
using System;
namespace System
{
    public unsafe readonly ref struct Span<T> where T : IComparable
    {
        public Span(void* ptr, int length)
        {
            Length = length;
        }
        public int Length { get; }
    }
}
struct NonComparable { }
class Test
{
    void M()
    {
        Span<NonComparable> explicitError = default;
        var implicitError = explicitError.Length > 0 ? stackalloc NonComparable[10] : stackalloc NonComparable[100];
    }
}";

            var references = new List<MetadataReference>() { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef };

            CreateEmptyCompilation(code, references, TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (19,14): error CS0315: The type 'NonComparable' cannot be used as type parameter 'T' in the generic type or method 'Span<T>'. There is no boxing conversion from 'NonComparable' to 'System.IComparable'.
                //         Span<NonComparable> explicitError = default;
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "NonComparable").WithArguments("System.Span<T>", "System.IComparable", "T", "NonComparable").WithLocation(19, 14),
                // (20,67): error CS0315: The type 'NonComparable' cannot be used as type parameter 'T' in the generic type or method 'Span<T>'. There is no boxing conversion from 'NonComparable' to 'System.IComparable'.
                //         var implicitError = explicitError.Length > 0 ? stackalloc NonComparable[10] : stackalloc NonComparable[100];
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "NonComparable[10]").WithArguments("System.Span<T>", "System.IComparable", "T", "NonComparable").WithLocation(20, 67),
                // (20,98): error CS0315: The type 'NonComparable' cannot be used as type parameter 'T' in the generic type or method 'Span<T>'. There is no boxing conversion from 'NonComparable' to 'System.IComparable'.
                //         var implicitError = explicitError.Length > 0 ? stackalloc NonComparable[10] : stackalloc NonComparable[100];
                Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedValType, "NonComparable[100]").WithArguments("System.Span<T>", "System.IComparable", "T", "NonComparable").WithLocation(20, 98));
        }

        [Fact]
        [WorkItem(26195, "https://github.com/dotnet/roslyn/issues/26195")]
        public void StackAllocImplicitConversion_TwpStep_ToPointer()
        {
            var code = @"
class Test2
{
}
unsafe class Test
{
	public void Method()
	{
		Test obj1 = stackalloc int[2];
	}
	public static implicit operator Test2(int* value) => default;
}";

            CreateCompilationWithMscorlibAndSpan(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (11,34): error CS0556: User-defined conversion must convert to or from the enclosing type
                // 	public static implicit operator Test2(int* value) => default;
                Diagnostic(ErrorCode.ERR_ConversionNotInvolvingContainedType, "Test2").WithLocation(11, 34),
                // (9,15): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'Test' is not possible.
                // 		Test obj1 = stackalloc int[2];
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[2]").WithArguments("int", "Test").WithLocation(9, 15));
        }

        [Fact]
        [WorkItem(26195, "https://github.com/dotnet/roslyn/issues/26195")]
        public void StackAllocImplicitConversion_TwpStep_ToSpan()
        {
            var code = @"
class Test2
{
}
unsafe class Test
{
	public void Method()
	{
		Test obj1 = stackalloc int[2];
	}
	public static implicit operator Test2(System.Span<int> value) => default;
}";

            CreateCompilationWithMscorlibAndSpan(code, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (11,34): error CS0556: User-defined conversion must convert to or from the enclosing type
                // 	public static implicit operator Test2(System.Span<int> value) => default;
                Diagnostic(ErrorCode.ERR_ConversionNotInvolvingContainedType, "Test2").WithLocation(11, 34),
                // (9,15): error CS8346: Conversion of a stackalloc expression of type 'int' to type 'Test' is not possible.
                // 		Test obj1 = stackalloc int[2];
                Diagnostic(ErrorCode.ERR_StackAllocConversionNotPossible, "stackalloc int[2]").WithArguments("int", "Test").WithLocation(9, 15));
        }
    }
}
