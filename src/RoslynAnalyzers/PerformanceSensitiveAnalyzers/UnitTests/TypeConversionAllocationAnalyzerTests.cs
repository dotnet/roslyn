﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers;
using Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.TypeConversionAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class TypeConversionAllocationAnalyzerTests
    {
        [Fact]
        public async Task TypeConversionAllocation_ArgumentSyntaxAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Roslyn.Utilities;

public class MyObject
{
    public MyObject(object obj)
    {
    }

    private void ObjCall(object obj)
    {
    }

    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        ObjCall(10); // Allocation
        _ = new MyObject(10); // Allocation
    }
}",
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(18, 17),
#pragma warning restore RS0030 // Do not use banned APIs
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(19, 26)
#pragma warning restore RS0030 // Do not use banned APIs
            );
        }

        [Fact]
        public async Task TypeConversionAllocation_ArgumentSyntax_WithDelegatesAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @class = new MyClass();
        @class.ProcessFunc(someObjCall); // implicit, so Allocation
        @class.ProcessFunc(new Func<object, string>(someObjCall)); // Explicit, so NO Allocation
    }

    public void ProcessFunc(Func<object, string> func)
    {
    }

    private string someObjCall(object obj) => null;
}

public struct MyStruct
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @struct = new MyStruct();
        @struct.ProcessFunc(someObjCall); // implicit allocation + boxing
        @struct.ProcessFunc(new Func<object, string>(someObjCall)); // Explicit allocation + boxing
    }

    public void ProcessFunc(Func<object, string> func)
    {
    }

    private string someObjCall(object obj) => null;
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(10,28): warning HAA0603: This will allocate a delegate instance
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithSpan(10, 28, 10, 39),
                // Test0.cs(27,29): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithSpan(27, 29, 27, 40),
                // Test0.cs(27,29): warning HAA0603: This will allocate a delegate instance
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithSpan(27, 29, 27, 40),
                // Test0.cs(28,54): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithSpan(28, 54, 28, 65));
        }

        [Fact]
        public async Task TypeConversionAllocation_ReturnStatementSyntaxAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyObject
{
    public Object Obj1 
    { 
        [PerformanceSensitive(""uri"")]
        get { return 0; }
    }

    [PerformanceSensitive(""uri"")]
    public Object Obj2
    {
        get { return 0; }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,22): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(9, 22),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(15,22): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(15, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_ReturnStatementSyntax_NoAllocAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyObject
{
    [PerformanceSensitive(""uri"")]
    public Object ObjNoAllocation1 { get { return 0.ToString(); } }

    public Object ObjNoAllocation2
    {
        [PerformanceSensitive(""uri"")]
        get { return 0.ToString(); }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task TypeConversionAllocation_YieldStatementSyntaxAsync()
        {
            var sampleProgram =
@"using System;
using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    public void SomeMethod()
    {
        foreach (var item in GetItems())
        {
        }

        foreach (var item in GetItemsNoAllocation())
        {
        }
    }

    [PerformanceSensitive(""uri"")]
    public IEnumerable<object> GetItems()
    {
        yield return 0; // Allocation
        yield break;
    }

    [PerformanceSensitive(""uri"")]
    public IEnumerable<int> GetItemsNoAllocation()
    {
        yield return 0; // NO Allocation (IEnumerable<int>)
        yield break;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(21,22): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(21, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_BinaryExpressionSyntaxAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        object x = ""blah"";
        object a1 = x ?? 0; // Allocation
        object a2 = x ?? 0.ToString(); // No Allocation

        var b1 = 10 as object; // Allocation
        var b2 = 10.ToString() as object; // No Allocation
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(10,26): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(10, 26),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(13,18): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(13, 18));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_BinaryExpressionSyntax_WithDelegatesAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        Func<object, string> temp = null;
        var result1 = temp ?? someObjCall; // implicit, so Allocation
        var result2 = temp ?? new Func<object, string>(someObjCall); // Explicit, so NO Allocation
    }

    private string someObjCall(object obj)
    {
        return obj.ToString();
    }
}

public struct MyStruct
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        Func<object, string> temp = null;
        var result1 = temp ?? someObjCall; // implicit allocation + boxing
        var result2 = temp ?? new Func<object, string>(someObjCall); // Explicit allocation + boxing
    }

    private string someObjCall(object obj)
    {
        return obj.ToString();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(10,31): warning HAA0603: This will allocate a delegate instance
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithSpan(10, 31, 10, 42),
                // Test0.cs(26,31): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithSpan(26, 31, 26, 42),
                // Test0.cs(26,31): warning HAA0603: This will allocate a delegate instance
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithSpan(26, 31, 26, 42),
                // Test0.cs(27,56): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithSpan(27, 56, 27, 67));
        }

        [Fact]
        public async Task TypeConversionAllocation_EqualsValueClauseSyntaxAsync()
        {
            // for (object i = 0;;)
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        for (object i = 0;;) // Allocation
        {
        }

        for (int i = 0;;) // NO Allocation
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,25): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(9, 25));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_EqualsValueClauseSyntax_WithDelegatesAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        Func<object, string> func2 = someObjCall; // implicit, so Allocation
        Func<object, string> func1 = new Func<object, string>(someObjCall); // Explicit, so NO Allocation
    }

    private string someObjCall(object obj)
    {
        return obj.ToString();
    }
}

public struct MyStruct
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        Func<object, string> func2 = someObjCall; // implicit allocation + boxing
        Func<object, string> func1 = new Func<object, string>(someObjCall); // Explicit allocation + boxing
    }

    private string someObjCall(object obj)
    {
        return obj.ToString();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,38): warning HAA0603: This will allocate a delegate instance
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithSpan(9, 38, 9, 49),
                // Test0.cs(24,38): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithSpan(24, 38, 24, 49),
                // Test0.cs(24,38): warning HAA0603: This will allocate a delegate instance
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithSpan(24, 38, 24, 49),
                // Test0.cs(25,63): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithSpan(25, 63, 25, 74));
        }

        [Fact]
        [WorkItem(2, "https://github.com/mjsabby/RoslynClrHeapAllocationAnalyzer/issues/2")]
        public async Task TypeConversionAllocation_EqualsValueClause_ExplicitMethodGroupAllocation_BugAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        Action methodGroup = this.Method;
    }

    private void Method()
    {
    }
}

public struct MyStruct
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        Action methodGroup = this.Method;
    }

    private void Method()
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,30): warning HAA0603: This will allocate a delegate instance
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithLocation(9, 30),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(22,30): warning HAA0603: This will allocate a delegate instance
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithLocation(22, 30),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(22,30): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithLocation(22, 30));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_ConditionalExpressionSyntaxAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        object obj = ""test"";
        object test1 = true ? 0 : obj; // Allocation
        object test2 = true ? 0.ToString() : obj; // NO Allocation
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(10,31): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(10, 31));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_CastExpressionSyntaxAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var f1 = (object)5; // Allocation
        var f2 = (object)""5""; // NO Allocation
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,26): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(9, 26));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_ArgumentWithImplicitStringCastOperatorAsync()
        {
            const string programWithoutImplicitCastOperator = @"
using System;
using Roslyn.Utilities;

public struct AStruct
{
    [PerformanceSensitive(""uri"")]
    public static void Dump(AStruct astruct)
    {
        System.Console.WriteLine(astruct);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(programWithoutImplicitCastOperator,
                // Test0.cs(10,34): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(10, 34));
#pragma warning restore RS0030 // Do not use banned APIs

            const string programWithImplicitCastOperator = @"
using System;
using Roslyn.Utilities;

public struct AStruct
{
    public readonly string WrappedString;

    public AStruct(string s)
    {
        WrappedString = s ?? """";
    }

    [PerformanceSensitive(""uri"")]
    public static void Dump(AStruct astruct)
    {
        System.Console.WriteLine(astruct);
    }

    [PerformanceSensitive(""uri"")]
    public static implicit operator string(AStruct astruct)
    {
        return astruct.WrappedString;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(programWithImplicitCastOperator);
        }

        [Fact]
        public async Task TypeConversionAllocation_YieldReturnImplicitStringCastOperatorAsync()
        {
            const string programWithoutImplicitCastOperator = @"
using System;
using Roslyn.Utilities;

public struct AStruct
{
    [PerformanceSensitive(""uri"")]
    public System.Collections.Generic.IEnumerator<object> GetEnumerator()
    {
        yield return this;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(programWithoutImplicitCastOperator,
                // Test0.cs(10,22): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(10, 22));
#pragma warning restore RS0030 // Do not use banned APIs

            const string programWithImplicitCastOperator = @"
using System;
using Roslyn.Utilities;

public struct AStruct
{
    [PerformanceSensitive(""uri"")]
    public System.Collections.Generic.IEnumerator<string> GetEnumerator()
    {
        yield return this;
    }

    public static implicit operator string(AStruct astruct)
    {
        return """";
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(programWithImplicitCastOperator);
        }

        [Fact]
        public async Task TypeConversionAllocation_InterpolatedStringWithInt_BoxingWarningAsync()
        {
            var source = @"
using System;
using Roslyn.Utilities;

class Program
{
    [PerformanceSensitive(""uri"")]
    void SomeMethod()
    {
        string s = $""{1}"";
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(10,23): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(10, 23));
#pragma warning restore RS0030 // Do not use banned APIs
        }

#if false
        [Fact]
        public void TypeConversionAllocation_InterpolatedStringWithString_NoWarning()
        {
            var sampleProgram = @"string s = $""{1.ToString()}"";";

            var analyser = new TypeConversionAllocationAnalyzer();
            var info = ProcessCode(analyser, sampleProgram, ImmutableArray.Create(SyntaxKind.Interpolation));

            Assert.Empty(info.Allocations);
        }
#endif

        [Theory]
        [InlineData(@"private readonly System.Func<string, bool> fileExists =        System.IO.File.Exists;")]
        [InlineData(@"private System.Func<string, bool> fileExists { get; } =        System.IO.File.Exists;")]
        [InlineData(@"private static System.Func<string, bool> fileExists { get; } = System.IO.File.Exists;")]
        [InlineData(@"private static readonly System.Func<string, bool> fileExists = System.IO.File.Exists;")]
        public async Task TypeConversionAllocation_DelegateAssignmentToReadonly_DoNotWarnAsync(string snippet)
        {
            var source = $@"
using System;
using Roslyn.Utilities;

class Program
{{
    [PerformanceSensitive(""uri"")]
    {snippet}
}}";

            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(8,68): info HeapAnalyzerReadonlyMethodGroupAllocationRule: This will allocate a delegate instance
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ReadonlyMethodGroupAllocationRule).WithLocation(8, 68));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_ExpressionBodiedPropertyBoxing_WithBoxingAsync()
        {
            const string snippet = @"
using System;
using Roslyn.Utilities;

class Program
{
    [PerformanceSensitive(""uri"")]
    object Obj => 1;
}";

            await VerifyCS.VerifyAnalyzerAsync(snippet,
                // Test0.cs(8,19): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(8, 19));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_ExpressionBodiedPropertyBoxing_WithoutBoxingAsync()
        {
            const string snippet = @"
using System;
using Roslyn.Utilities;

class Program
{
    [PerformanceSensitive(""uri"")]
    object Obj => 1.ToString();
}";

            await VerifyCS.VerifyAnalyzerAsync(snippet);
        }

        [Fact]
        public async Task TypeConversionAllocation_ExpressionBodiedPropertyDelegateAsync()
        {
            const string snippet = @"
using System;
using Roslyn.Utilities;

class Program
{
    void Function(int i) { } 

    [PerformanceSensitive(""uri"")]
    Action<int> Obj => Function;
}";

            await VerifyCS.VerifyAnalyzerAsync(snippet,
                // Test0.cs(10,24): warning HAA0603: This will allocate a delegate instance
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithLocation(10, 24));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_ExpressionBodiedPropertyExplicitDelegate_NoWarningAsync()
        {
            // Tests that an explicit delegate creation does not trigger HAA0603. It should be handled by HAA0502.
            const string snippet = @"
using System;
using Roslyn.Utilities;

class Program
{
    void Function(int i) { } 

    [PerformanceSensitive(""uri"")]
    Action<int> Obj => new Action<int>(Function);
}";

            await VerifyCS.VerifyAnalyzerAsync(snippet);
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_enumeration_type_to_System_Enum_typeAsync()
        {
            var source = @"
using Roslyn.Utilities;

enum E { A }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        System.Enum box = E.A;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(11,27): warning HAA0601: Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.ValueTypeToReferenceTypeConversionRule).WithLocation(11, 27));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Creating_delegate_from_value_type_instance_methodAsync()
        {
            var source = @"
using System;
using Roslyn.Utilities;

struct S { public void M() {} }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        Action box = new S().M;
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(12,22): warning HAA0603: This will allocate a delegate instance
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.MethodGroupAllocationRule).WithLocation(12, 22),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(12,22): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithLocation(12, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task TypeConversionAllocation_NoDiagnosticWhenPassingDelegateAsArgumentAsync()
        {
            const string snippet = @"
using System;
using Roslyn.Utilities;

struct Foo
{
    [PerformanceSensitive(""uri"")]
    void Do(Action process)
    {
        DoMore(process);
    }

    void DoMore(Action process)
    {
        process();
    }
}
            ";
            await VerifyCS.VerifyAnalyzerAsync(snippet);
        }

        [Fact]
        public async Task TypeConversionAllocation_ReportBoxingAllocationForPassingStructInstanceMethodForDelegateConstructorAsync()
        {
            const string snippet = @"
using System;
using Roslyn.Utilities;

public struct MyStruct
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @struct = new MyStruct();
        @struct.ProcessFunc(new Func<object, string>(FooObjCall));
    }

    public void ProcessFunc(Func<object, string> func)
    {
    }

    private string FooObjCall(object obj)
    {
        return obj.ToString();
    }
}
            ";

            await VerifyCS.VerifyAnalyzerAsync(snippet,
                // Test0.cs(11,54): warning HAA0602: Struct instance method being used for delegate creation, this will result in a boxing instruction
                VerifyCS.Diagnostic(TypeConversionAllocationAnalyzer.DelegateOnStructInstanceRule).WithSpan(11, 54, 11, 64));
        }

        [Fact]
        public async Task TypeConversionAllocation_DoNotReportBoxingAllocationForPassingStructStaticMethodForDelegateConstructorAsync()
        {
            const string snippet = @"
using System;
using Roslyn.Utilities;

public struct MyStruct
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @struct = new MyStruct();
        @struct.ProcessFunc(new Func<object, string>(FooObjCall));
    }

    public void ProcessFunc(Func<object, string> func)
    {
    }

    private static string FooObjCall(object obj)
    {
        return obj.ToString();
    }
}
            ";

            await VerifyCS.VerifyAnalyzerAsync(snippet);
        }

        [Fact]
        public async Task TypeConversionAllocation_DoNotReportInlineDelegateAsStructInstanceMethodsAsync()
        {
            const string snippet = @"
using System;
using Roslyn.Utilities;

public struct MyStruct
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var ints = new[] { 5, 4, 3, 2, 1 };
        Array.Sort(ints, delegate(int x, int y) { return x - y; });
        Array.Sort(ints, (x, y) => x - y);
        DoSomething(() => throw new Exception());
        DoSomething(delegate() { throw new Exception(); });

        DoSomething2(x => throw new Exception());
    }

    private static void DoSomething(Action action)
    {
    }

    private static void DoSomething2(Action<int> action)
    {
    }
}
            ";

            await VerifyCS.VerifyAnalyzerAsync(snippet);
        }
    }
}
