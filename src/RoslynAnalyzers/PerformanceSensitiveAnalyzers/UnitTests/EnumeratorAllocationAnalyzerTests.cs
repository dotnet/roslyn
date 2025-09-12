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
    EnumeratorAllocationAnalyzer,
    EmptyCodeFixProvider>;

public sealed class EnumeratorAllocationAnalyzerTests
{
    [Fact]
    public Task EnumeratorAllocation_BasicAsync()
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
                    int[] intData = new[] { 123, 32, 4 };
                    IList<int> iListData = new[] { 123, 32, 4 };
                    List<int> listData = new[] { 123, 32, 4 }.ToList();

                    foreach (var i in intData)
                    {
                        Console.WriteLine(i);
                    }

                    foreach (var i in listData)
                    {
                        Console.WriteLine(i);
                    }

                    foreach (var i in iListData) // Allocations (line 19)
                    {
                        Console.WriteLine(i);
                    }

                    foreach (var i in (IEnumerable<int>)intData) // Allocations (line 24)
                    {
                        Console.WriteLine(i);
                    }
                }
            }
            """,
            // Test0.cs(25,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic().WithLocation(25, 24),
#pragma warning restore RS0030 // Do not use banned APIs
            // Test0.cs(30,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic().WithLocation(30, 24));

    [Fact]
    public Task EnumeratorAllocation_AdvancedAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System.Collections.Generic;
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    // These next 3 are from the YouTube video
                    foreach (object a in new[] { 1, 2, 3}) // Allocations 'new [] { 1. 2, 3}'
                    {
                        Console.WriteLine(a.ToString());
                    }

                    IEnumerable<string> fx1 = default(IEnumerable<string>);
                    foreach (var f in fx1) // Allocations 'in'
                    {
                    }

                    List<string> fx2 = default(List<string>);
                    foreach (var f in fx2) // NO Allocations
                    {
                    }
                }
            }
            """,
            // Test0.cs(17,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic().WithLocation(17, 24));

    [Fact]
    public Task EnumeratorAllocation_Via_InvocationExpressionSyntaxAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System.Collections.Generic;
            using System.Collections;
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    var enumeratorRaw = GetIEnumerableRaw();
                    while (enumeratorRaw.MoveNext())
                    {
                        Console.WriteLine(enumeratorRaw.Current.ToString());
                    }

                    var enumeratorRawViaIEnumerable = GetIEnumeratorViaIEnumerable();
                    while (enumeratorRawViaIEnumerable.MoveNext())
                    {
                        Console.WriteLine(enumeratorRawViaIEnumerable.Current.ToString());
                    }
                }

                private IEnumerator GetIEnumerableRaw()
                {
                    return new[] { 123, 32, 4 }.GetEnumerator();
                }

                private IEnumerator<int> GetIEnumeratorViaIEnumerable()
                {
                    int[] intData = new[] { 123, 32, 4 };
                    return (IEnumerator<int>)intData.GetEnumerator();
                }
            }
            """,
            // Test0.cs(17,43): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
            VerifyCS.Diagnostic().WithLocation(17, 43));

    [Fact]
    public Task EnumeratorAllocation_IterateOverString_NoWarningAsync()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using Roslyn.Utilities;

            public class MyClass
            {
                [PerformanceSensitive("uri")]
                public void SomeMethod()
                {
                    foreach (char c in "aaa") { };
                }
            }
            """);
}
