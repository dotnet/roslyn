// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;
using Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.ExplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class ExplicitAllocationAnalyzerTests
    {
        [Fact]
        public async Task ExplicitAllocation_ObjectInitializer()
        {
            var sampleProgram =
    @"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var @class = new TestClass { Name = ""Bob"" };
    }
}

public class TestClass
{
    public string Name { get; set; }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 22));
        }


        [Fact]
        public async Task ExplicitAllocation_ObjectInitializerStruct_NoWarning()
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
    }
}

public struct TestStruct
{
    public string Name { get; set; }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitArrayCreation()
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
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
        }

        [Fact]
        public async Task ExplicitAllocation_AnonymousObjectCreation()
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
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.AnonymousObjectCreationRule).WithLocation(9, 20));
        }

        [Fact]
        public async Task ExplicitAllocation_ArrayCreation()
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
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(9, 25));
        }

        [Fact]
        public async Task ExplicitAllocation_ObjectCreation()
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
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(9, 26));
        }

        [Fact]
        public async Task ExplicitAllocation_LetClause()
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
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ArrayCreationRule).WithLocation(10, 25),
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.LetCauseRule).WithLocation(12, 23));
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
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 22));
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
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(11, 32));
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
                VerifyCS.Diagnostic(ExplicitAllocationAnalyzer.ObjectCreationRule).WithLocation(13, 17));
        }

        [Fact]
        public async Task ExplicitAllocation_StructCreation_NoWarning()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public struct S { }

public class MyClass
{

    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var noBox1 = new DateTime();
        S noBox2 = new S();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_PrimitiveTypeConversion_NoWarning()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        double x = new int();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ExplicitAllocation_ImplicitValueTypeConversion_NoWarning()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

struct A
{
    public static implicit operator A(B other)
    {
        return new A();
    }
}

struct B
{
}

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        A a = new B();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }
    }
}
