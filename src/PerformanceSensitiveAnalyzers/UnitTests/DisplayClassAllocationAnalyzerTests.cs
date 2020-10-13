// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.DisplayClassAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class DisplayClassAllocationAnalyzerTests
    {
        [Fact]
        public async Task DisplayClassAllocation_AnonymousMethodExpressionSyntax()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

class Test
{
    static void Main()
    {
        Action action = CreateAction<int>(5);
    }

    [PerformanceSensitive(""uri"")]
    static Action CreateAction<T>(T item)
    {
        T test = default(T);
        int counter = 0;
        return delegate
        {
            counter++;
            Console.WriteLine(""counter={0}"", counter);
        };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(15,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(15, 13),
                // Test0.cs(16,16): warning HAA0303: Considering moving this out of the generic method
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LambaOrAnonymousMethodInGenericMethodRule).WithLocation(16, 16),
                // Test0.cs(16,16): warning HAA0301: Heap allocation of closure Captures: counter
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(16, 16).WithArguments("counter"));
        }

        [Fact]
        public async Task DisplayClassAllocation_SimpleLambdaExpressionSyntax()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System;
using System.Linq;
using Roslyn.Utilities;

public class Testing<T>
{
    [PerformanceSensitive(""uri"")]
    public Testing()
    {
        int[] intData = new[] { 123, 32, 4 };
        int min = 31;
        var results = intData.Where(i => i > min).ToList();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(12,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(12, 13),
                // Test0.cs(13,39): warning HAA0301: Heap allocation of closure Captures: min
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(13, 39).WithArguments("min"));
        }

        [Fact]
        public async Task DisplayClassAllocation_ParenthesizedLambdaExpressionSyntax()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System;
using System.Linq;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        var words = new[] { ""aaaa"", ""bbbb"", ""cccc"", ""ddd"" };
        var actions = new List<Action>();
        foreach (string word in words) // <-- captured closure
        {
            actions.Add(() => Console.WriteLine(word)); // <-- reason for closure capture
        }
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(13,25): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(13, 25),
                // Test0.cs(15,28): warning HAA0301: Heap allocation of closure Captures: word
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(15, 28).WithArguments("word"));
        }

        [Fact]
        public async Task DisplayClassAllocation_DoNotReportForNonCapturingAnonymousMethod()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Sorter(int[] arr)
    {
        System.Array.Sort(arr, delegate(int x, int y) { return x - y; });
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task DisplayClassAllocation_DoNotReportForNonCapturingLambda()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Sorter(int[] arr)
    {
        System.Array.Sort(arr, (x, y) => x - y);
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task DisplayClassAllocation_ReportForCapturingAnonymousMethod()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Sorter(int[] arr)
    {
        int z = 2;
        System.Array.Sort(arr, delegate(int x, int y) { return x - z; });
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(9,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(9, 13),
                // Test0.cs(10,32): warning HAA0301: Heap allocation of closure Captures: z
                VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(10, 32).WithArguments("z"));
        }
    }
}
