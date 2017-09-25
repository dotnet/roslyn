// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj1Value.Type).PointedAtType.SpecialType);
            Assert.Equal("Test", obj1Value.ConvertedType.Name);
            Assert.Equal(ConversionKind.ImplicitUserDefined, obj1Value.ImplicitConversion.Kind);

            var obj2 = variables.ElementAt(1);
            Assert.Equal("obj2", obj2.Identifier.Text);

            var obj2Value = model.GetSemanticInfoSummary(obj2.Initializer.Value);
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj2Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj2Value.ConvertedType).PointedAtType.SpecialType);
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
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj4Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj4Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.Identity, obj4Value.ImplicitConversion.Kind);

            var obj5 = variables.ElementAt(4);
            Assert.Equal("obj5", obj5.Identifier.Text);

            var obj5Value = model.GetSemanticInfoSummary(obj5.Initializer.Value);
            Assert.Null(obj5Value.Type);
            Assert.Equal(SpecialType.System_Double, ((PointerTypeSymbol)obj5Value.ConvertedType).PointedAtType.SpecialType);
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
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj2Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj2Value.ConvertedType).PointedAtType.SpecialType);
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
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj4Value.Type).PointedAtType.SpecialType);
            Assert.Equal(SpecialType.System_Int32, ((PointerTypeSymbol)obj4Value.ConvertedType).PointedAtType.SpecialType);
            Assert.Equal(ConversionKind.Identity, obj4Value.ImplicitConversion.Kind);

            var obj5 = variables.ElementAt(4);
            Assert.Equal("obj5", obj5.Identifier.Text);

            var obj5Value = model.GetSemanticInfoSummary(obj5.Initializer.Value);
            Assert.Null(obj5Value.Type);
            Assert.Equal(SpecialType.System_Double, ((PointerTypeSymbol)obj5Value.ConvertedType).PointedAtType.SpecialType);
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
            CreateStandardCompilation(@"
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
            CreateStandardCompilation(@"
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
}").VerifyDiagnostics(
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
                // (8,17): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'stackalloc int[10]' and 'Span<short>'
                //         var x = true ? stackalloc int [10] : a;
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? stackalloc int [10] : a").WithArguments("stackalloc int[10]", "System.Span<short>").WithLocation(8, 17));
        }

        [Fact]
        public void BooleanOperatorOnSpan_NoTargetTyping()
        {
            CreateCompilationWithMscorlibAndSpan(@"
class Test
{
    void M()
    {
        if(stackalloc int[10] == stackalloc int[10]) { }
    }
}", TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (6,12): error CS1525: Invalid expression term 'stackalloc'
                //         if(stackalloc int[10] == stackalloc int[10]) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc").WithLocation(6, 12),
                // (6,34): error CS1525: Invalid expression term 'stackalloc'
                //         if(stackalloc int[10] == stackalloc int[10]) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc").WithLocation(6, 34)
            );
        }

        [Fact]
        public void NewStackAllocSpanSyntaxProducesErrorsOnEarlierVersions()
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
                // (6,16): error CS1674: 'int*': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v = stackalloc int[1]").WithArguments("int*").WithLocation(6, 16));
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
            CreateStandardCompilation(test, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)).VerifyDiagnostics(
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
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (7,31): error CS1525: Invalid expression term 'stackalloc'
                //         ref Span<int> p = ref stackalloc int[1];
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc").WithLocation(7, 31)
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
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (7,11): error CS1525: Invalid expression term 'stackalloc'
                //         N(stackalloc int[1]);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc").WithLocation(7, 11)
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
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.ReleaseDll).VerifyDiagnostics(
                // (6,23): error CS1525: Invalid expression term 'stackalloc'
                //         int length = (stackalloc int [10]).Length;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc").WithLocation(6, 23)
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
            CreateCompilationWithMscorlibAndSpan(test, TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
                // (7,16): error CS1525: Invalid expression term 'stackalloc'
                //         Invoke(stackalloc int [10]);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "stackalloc").WithArguments("stackalloc").WithLocation(7, 16)
            );
        }
    }
}
