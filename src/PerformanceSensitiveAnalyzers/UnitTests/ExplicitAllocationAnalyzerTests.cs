// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers;
using Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.ExplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class ExplicitAllocationAnalyzerTests
    {
        [Fact]
        public async Task ExplicitAllocation_InitializerExpressionSyntax()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @struct = new TestStruct { Name = ""Bob"" };
        var @class = new TestClass { Name = ""Bob"" };
    }
}

public struct TestStruct
{
    public string Name { get; set; }
}

public class TestClass
{
    public string Name { get; set; }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(10,13): info HAA0505: Initializer reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.InitializerCreationRule).WithLocation(10, 13),
                // Test0.cs(10,22): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(10, 22));
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitArrayCreationExpressionSyntax()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new[] { 123, 32, 4 };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,25): info HAA0504: Implicit new array creation allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ImplicitArrayCreationRule).WithLocation(9, 25));
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreationExpressionSyntax()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var temp = new { A = 123, Name = ""Test"", };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,20): info HAA0503: Explicit new anonymous object allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.AnonymousNewObjectRule).WithLocation(9, 20));
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreationExpressionSyntax()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new int[] { 123, 32, 4 };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,25): info HAA0501: Explicit new array type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewArrayRule).WithLocation(9, 25));
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreationExpressionSyntax()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var allocation = new String('a', 10);
        var noAllocation = new DateTime();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,26): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(9, 26));
        }

        [Fact]
        public async Task ExplicitAllocation_LetClauseSyntax()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        int[] intData = new[] { 123, 32, 4 };
        var result = (from a in intData
                      let b = a * 3
                      select b).ToList();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(10,25): info HAA0504: Implicit new array creation allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ImplicitArrayCreationRule).WithLocation(10, 25),
                // Test0.cs(12,23): info HAA0506: Let clause induced allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.LetCauseRule).WithLocation(12, 23));
        }

        [Fact]
        public async Task ExplicitAllocation_AllSyntax()
        {
            var sampleProgram =
@"using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @struct = new TestStruct { Name = ""Bob"" };
        var @class = new TestClass { Name = ""Bob"" };

        int[] intDataImplicit = new[] { 123, 32, 4 };

        var temp = new { A = 123, Name = ""Test"", };

        int[] intDataExplicit = new int[] { 123, 32, 4 };

        var allocation = new String('a', 10);
        var noAllocation = new DateTime();

        int[] intDataLinq = new int[] { 123, 32, 4 };
        var result = (from a in intDataLinq
                      let b = a * 3
                      select b).ToList();
    }
}

public struct TestStruct
{
    public string Name { get; set; }
}

public class TestClass
{
    public string Name { get; set; }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(12,13): info HAA0505: Initializer reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.InitializerCreationRule).WithLocation(12, 13),
                // Test0.cs(12,22): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(12, 22),
                // Test0.cs(14,33): info HAA0504: Implicit new array creation allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ImplicitArrayCreationRule).WithLocation(14, 33),
                // Test0.cs(16,20): info HAA0503: Explicit new anonymous object allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.AnonymousNewObjectRule).WithLocation(16, 20),
                // Test0.cs(18,33): info HAA0501: Explicit new array type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewArrayRule).WithLocation(18, 33),
                // Test0.cs(20,26): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(20, 26),
                // Test0.cs(23,29): info HAA0501: Explicit new array type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewArrayRule).WithLocation(23, 29),
                // Test0.cs(25,23): info HAA0506: Let clause induced allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.LetCauseRule).WithLocation(25, 23));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_Object_type()
        {
            var source = @"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        object box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(11,22): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(11, 22));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_to_System_ValueType_type()
        {
            var source = @"
using Roslyn.Utilities;

public struct S { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        System.ValueType box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(11,32): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(11, 32));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Converting_any_value_type_into_interface_reference()
        {
            var source = @"
using Roslyn.Utilities;

interface I { }

public struct S : I { }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Foo() 
    {
        I box = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(13,17): info HAA0502: Explicit new reference type allocation
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.NewObjectRule).WithLocation(13, 17));
        }
    }
}
