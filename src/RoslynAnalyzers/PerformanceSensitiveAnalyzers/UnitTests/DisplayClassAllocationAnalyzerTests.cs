// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests;

using VerifyCS = CSharpPerformanceCodeFixVerifier<
    DisplayClassAllocationAnalyzer,
    EmptyCodeFixProvider>;

public sealed class DisplayClassAllocationAnalyzerTests
{
    [Fact]
    public Task DisplayClassAllocation_AnonymousMethodExpressionSyntaxAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            class Test
            {
                static void Main()
                {
                    Action action = CreateAction<int>(5);
                }

                [PerformanceSensitive("uri")]
                static Action CreateAction<T>(T item)
                {
                    T test = default(T);
                    int counter = 0;
                    return delegate
                    {
                        counter++;
                        Console.WriteLine("counter={0}", counter);
                    };
                }
            }
            """,
            // Test0.cs(15,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(15, 13),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(16,16): warning HAA0303: Considering moving this out of the generic method
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LambaOrAnonymousMethodInGenericMethodRule).WithLocation(16, 16),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(16,16): warning HAA0301: Heap allocation of closure Captures: counter
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(16, 16).WithArguments("counter"));

    [Fact]
    public Task DisplayClassAllocation_SimpleLambdaExpressionSyntaxAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System.Collections.Generic;
            using System;
            using System.Linq;
            using Roslyn.Utilities;

            public class Testing<T>
            {
                [PerformanceSensitive("uri")]
                public Testing()
                {
                    int[] intData = new[] { 123, 32, 4 };
                    int min = 31;
                    var results = intData.Where(i => i > min).ToList();
                }
            }
            """,
            // Test0.cs(12,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(12, 13),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(13,39): warning HAA0301: Heap allocation of closure Captures: min
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(13, 39).WithArguments("min"));

    [Fact]
    public Task DisplayClassAllocation_ParenthesizedLambdaExpressionSyntaxAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System.Collections.Generic;
            using System;
            using System.Linq;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    var words = new[] { "aaaa", "bbbb", "cccc", "ddd" };
                    var actions = new List<Action>();
                    foreach (string word in words) // <-- captured closure
                    {
                        actions.Add(() => Console.WriteLine(word)); // <-- reason for closure capture
                    }
                }
            }
            """,
            // Test0.cs(13,25): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(13, 25),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(15,28): warning HAA0301: Heap allocation of closure Captures: word
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(15, 28).WithArguments("word"));

    [Fact]
    public Task DisplayClassAllocation_DoNotReportForNonCapturingAnonymousMethodAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void Sorter(int[] arr)
                {
                    System.Array.Sort(arr, delegate(int x, int y) { return x - y; });
                }
            }
            """);

    [Fact]
    public Task DisplayClassAllocation_DoNotReportForNonCapturingLambdaAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void Sorter(int[] arr)
                {
                    System.Array.Sort(arr, (x, y) => x - y);
                }
            }
            """);

    [Fact]
    public Task DisplayClassAllocation_ReportForCapturingAnonymousMethodAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void Sorter(int[] arr)
                {
                    int z = 2;
                    System.Array.Sort(arr, delegate(int x, int y) { return x - z; });
                }
            }
            """,
            // Test0.cs(9,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(9, 13),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(10,32): warning HAA0301: Heap allocation of closure Captures: z
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(10, 32).WithArguments("z"));
}
