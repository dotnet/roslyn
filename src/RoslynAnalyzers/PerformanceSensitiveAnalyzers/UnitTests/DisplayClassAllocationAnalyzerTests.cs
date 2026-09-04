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

    [Fact]
    public Task DisplayClassAllocation_ReportForLambdaCapturingLocalAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    Action action = () => Console.WriteLine(local);
                }
            }
            """,
            // Test0.cs(9,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(9, 13),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(10,28): warning HAA0301: Heap allocation of closure Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(10, 28).WithArguments("local"));

    [Fact]
    public Task DisplayClassAllocation_DoNotReportForStaticLambdaAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    Action action = static () => Console.WriteLine("no capture");
                }
            }
            """);

    // Documents current behavior for a lambda that captures only 'this': HAA0301 names 'this' as
    // the captured symbol, and HAA0302 is reported on the enclosing method's identifier, because
    // the location of the implicit 'this' parameter symbol is the method declaration itself rather
    // than a variable declarator.
    [Fact]
    public Task DisplayClassAllocation_ReportForLambdaCapturingThisAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                private int _field;

                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    Action action = () => Console.WriteLine(_field);
                }
            }
            """,
            // Test0.cs(9,17): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithSpan(9, 17, 9, 27),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(11,28): warning HAA0301: Heap allocation of closure Captures: this
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(11, 28).WithArguments("this"));

    // Documents current behavior for nested lambdas capturing the same outer local: the analyzer
    // runs data flow analysis once per lambda, and the outer lambda's region also contains the
    // inner one, so the capture of 'local' is reported once per enclosing lambda.
    [Fact]
    public Task DisplayClassAllocation_ReportForNestedLambdasCapturingOuterLocalAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    Action outer = () =>
                    {
                        Action inner = () => Console.WriteLine(local);
                        inner();
                    };
                }
            }
            """,
            // Test0.cs(9,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(9, 13),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(9,13): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(9, 13),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(10,27): warning HAA0301: Heap allocation of closure Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(10, 27).WithArguments("local"),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(12,31): warning HAA0301: Heap allocation of closure Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(12, 31).WithArguments("local"));

    // A local function that is only ever invoked directly needs no heap allocation: the compiler
    // emits the captured state as a by-ref struct passed to the local function. Reporting a
    // closure allocation here would be a false positive, so this must never produce a diagnostic.
    [Fact]
    public Task DisplayClassAllocation_DoNotReportForDirectlyInvokedLocalFunctionAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    void LocalFunction() => Console.WriteLine(local);
                    LocalFunction();
                }
            }
            """);

    // Converting a capturing local function to a delegate forces the compiler to allocate a display
    // class, so HAA0304 is reported at the conversion. Closes the gap tracked by
    // dotnet/roslyn-analyzers#1438. HAA0603 also fires on the conversion and is asserted in
    // TypeConversionAllocationAnalyzerTests; the two rules coexist, as they already do for lambdas.
    [Fact]
    public Task DisplayClassAllocation_ReportForLocalFunctionConvertedToDelegate_InitializerAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    void LocalFunction() => Console.WriteLine(local);
                    Action action = LocalFunction;
                }
            }
            """,
            // Test0.cs(11,25): warning HAA0304: Heap allocation of closure for local function 'LocalFunction' Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LocalFunctionClosureRule).WithLocation(11, 25).WithArguments("LocalFunction", "local"));

    [Fact]
    public Task DisplayClassAllocation_ReportForLocalFunctionConvertedToDelegate_ArgumentAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                private static void Consume(Action action)
                {
                }

                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    void LocalFunction() => Console.WriteLine(local);
                    Consume(LocalFunction);
                }
            }
            """,
            // Test0.cs(15,17): warning HAA0304: Heap allocation of closure for local function 'LocalFunction' Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LocalFunctionClosureRule).WithLocation(15, 17).WithArguments("LocalFunction", "local"));

    // The return position is covered by HAA0304's own reference-site check. HAA0603 is never
    // reported here at all (pinned by
    // TypeConversionAllocationAnalyzerTests.TypeConversionAllocation_DoNotReportMethodGroupAllocationForReturnedLocalFunctionAsync),
    // so HAA0304 must not rely on any overlap with it.
    [Fact]
    public Task DisplayClassAllocation_ReportForLocalFunctionConvertedToDelegate_ReturnAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public Action SomeMethod()
                {
                    int local = 1;
                    void LocalFunction() => Console.WriteLine(local);
                    return LocalFunction;
                }
            }
            """,
            // Test0.cs(11,16): warning HAA0304: Heap allocation of closure for local function 'LocalFunction' Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LocalFunctionClosureRule).WithLocation(11, 16).WithArguments("LocalFunction", "local"));

    [Fact]
    public Task DisplayClassAllocation_ReportForLocalFunctionConvertedToDelegate_AssignmentAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    void LocalFunction() => Console.WriteLine(local);
                    Action action = null;
                    action = LocalFunction;
                }
            }
            """,
            // Test0.cs(12,18): warning HAA0304: Heap allocation of closure for local function 'LocalFunction' Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LocalFunctionClosureRule).WithLocation(12, 18).WithArguments("LocalFunction", "local"));

    // A local function that captures nothing allocates a delegate but no display class, so HAA0304
    // must not fire. HAA0603 still reports the delegate allocation.
    [Fact]
    public Task DisplayClassAllocation_DoNotReportForNonCapturingLocalFunctionConvertedToDelegateAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    void LocalFunction() => Console.WriteLine("no capture");
                    Action action = LocalFunction;
                }
            }
            """);

    // Returning a non-capturing local function reports nothing from either rule: HAA0304 has no
    // captures to report, and HAA0603 does not cover the return position at all.
    [Fact]
    public Task DisplayClassAllocation_DoNotReportForNonCapturingLocalFunctionReturnedAsDelegateAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public Action SomeMethod()
                {
                    void LocalFunction() => Console.WriteLine("no capture");
                    return LocalFunction;
                }
            }
            """);

    // Mirrors the pinned lambda behavior for a capture of only 'this': the capture is reported and
    // named 'this' in the message.
    [Fact]
    public Task DisplayClassAllocation_ReportForLocalFunctionCapturingThisConvertedToDelegateAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                private int _field;

                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    void LocalFunction() => Console.WriteLine(_field);
                    Action action = LocalFunction;
                }
            }
            """,
            // Test0.cs(12,25): warning HAA0304: Heap allocation of closure for local function 'LocalFunction' Captures: this
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LocalFunctionClosureRule).WithLocation(12, 25).WithArguments("LocalFunction", "this"));

    // A capture inside a loop is the costly case the rule exists for: the conversion runs on every
    // iteration, allocating a display class each time.
    [Fact]
    public Task DisplayClassAllocation_ReportForLocalFunctionConvertedToDelegateInLoopAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using System.Collections.Generic;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod(int[] values)
                {
                    var actions = new List<Action>();
                    foreach (int value in values)
                    {
                        void LocalFunction() => Console.WriteLine(value);
                        actions.Add(LocalFunction);
                    }
                }
            }
            """,
            // Test0.cs(14,25): warning HAA0304: Heap allocation of closure for local function 'LocalFunction' Captures: value
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LocalFunctionClosureRule).WithLocation(14, 25).WithArguments("LocalFunction", "value"));

    // A generic local function is referenced through a GenericNameSyntax rather than an
    // IdentifierNameSyntax, so both syntax shapes have to be handled.
    [Fact]
    public Task DisplayClassAllocation_ReportForGenericLocalFunctionConvertedToDelegateAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    void LocalFunction<T>() => Console.WriteLine(local);
                    Action action = LocalFunction<int>;
                }
            }
            """,
            // Test0.cs(11,25): warning HAA0304: Heap allocation of closure for local function 'LocalFunction' Captures: local
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.LocalFunctionClosureRule).WithLocation(11, 25).WithArguments("LocalFunction", "local"));

    // 'nameof' does not convert the method group to a delegate, so nothing is allocated.
    [Fact]
    public Task DisplayClassAllocation_DoNotReportForLocalFunctionInNameOfAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    int local = 1;
                    void LocalFunction() => Console.WriteLine(local);
                    LocalFunction();
                    Console.WriteLine(nameof(LocalFunction));
                }
            }
            """);

    // Only variables declared outside the local function contribute to the display class allocated
    // at the conversion. Here the nested lambda captures a local of the local function, which is
    // allocated when the local function runs regardless of the delegate conversion.
    [Fact]
    public Task DisplayClassAllocation_DoNotReportForLocalFunctionCapturingOnlyItsOwnLocalAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    void LocalFunction()
                    {
                        int inner = 1;
                        Action nested = () => Console.WriteLine(inner);
                        nested();
                    }

                    Action action = LocalFunction;
                }
            }
            """,
            // Test0.cs(11,17): warning HAA0302: The compiler will emit a class that will hold this as a field to allow capturing of this closure
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureCaptureRule).WithLocation(11, 17),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(12,32): warning HAA0301: Heap allocation of closure Captures: inner
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic(DisplayClassAllocationAnalyzer.ClosureDriverRule).WithLocation(12, 32).WithArguments("inner"));

    // The VerifyAnalyzerAsync helper always injects Roslyn.Utilities.PerformanceSensitiveAttribute
    // as a second source file, so a Test instance is constructed directly here instead. The
    // attribute applied below is an identically named type in a different namespace, so the
    // compilation is valid and the code would otherwise report HAA0304, but the analyzer finds no
    // Roslyn.Utilities.PerformanceSensitiveAttribute and bails out at compilation start.
    [Fact]
    public Task DisplayClassAllocation_DoNotReportWhenPerformanceSensitiveAttributeIsNotDefinedAsync()
        => new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    """
                    using System;
                    using Other.Utilities;

                    namespace Other.Utilities
                    {
                        internal sealed class PerformanceSensitiveAttribute : Attribute
                        {
                            public PerformanceSensitiveAttribute(string uri)
                            {
                            }
                        }
                    }

                    public class MyClass
                    {
                        [PerformanceSensitive("uri")]
                        public void SomeMethod()
                        {
                            int local = 1;
                            void LocalFunction() => Console.WriteLine(local);
                            Action action = LocalFunction;
                        }
                    }
                    """,
                },
            },
        }.RunAsync();
}
