﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.EnumeratorAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitive.Analyzers.UnitTests
{
    public class EnumeratorAllocationAnalyzerTests
    {
        [Fact]
        public async Task EnumeratorAllocation_BasicAsync()
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(25,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic().WithLocation(25, 24),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(30,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic().WithLocation(30, 24));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task EnumeratorAllocation_AdvancedAsync()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(17,24): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic().WithLocation(17, 24));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task EnumeratorAllocation_Via_InvocationExpressionSyntaxAsync()
        {
            var sampleProgram =
@"using System.Collections.Generic;
using System.Collections;
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
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
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(17,43): warning HAA0401: Non-ValueType enumerator may result in a heap allocation
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic().WithLocation(17, 43));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task EnumeratorAllocation_IterateOverString_NoWarningAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        foreach (char c in ""aaa"") { };
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }
    }
}
