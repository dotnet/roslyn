// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers;
using Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.ConcatenationAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests
{
    public class ConcatenationAllocationAnalyzerTests
    {
        [Fact]
        public async Task ConcatenationAllocation_Basic1Async()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        string s0 = ""hello"" + 0.ToString() + ""world"" + 1.ToString();
    }
}");
        }

        [Fact]
        public async Task ConcatenationAllocation_Basic2Async()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        string s2 = ""ohell"" + 2.ToString() + ""world"" + 3.ToString() + 4.ToString();
    }
}",
                // Test0.cs(9,21): warning HAA0201: Considering using StringBuilder
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ConcatenationAllocationAnalyzer.StringConcatenationAllocationRule).WithLocation(9, 21));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Theory]
        [InlineData("string s0 = nameof(System.String) + '-';")]
        [InlineData("string s0 = nameof(System.String) + true;")]
        [InlineData("string s0 = nameof(System.String) + new System.IntPtr();")]
        [InlineData("string s0 = nameof(System.String) + new System.UIntPtr();")]
        public async Task ConcatenationAllocation_DoNotWarnForOptimizedValueTypesAsync(string statement)
        {
            await VerifyCS.VerifyAnalyzerAsync($@"using System;
using Roslyn.Utilities;

public class MyClass
{{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {{
        {statement}
    }}
}}");
        }

        [Theory]
        [InlineData(@"const string s0 = nameof(System.String) + ""."" + nameof(System.String);")]
        [InlineData(@"const string s0 = nameof(System.String) + ""."";")]
        [InlineData(@"string s0 = nameof(System.String) + ""."" + nameof(System.String);")]
        [InlineData(@"string s0 = nameof(System.String) + ""."";")]
        public async Task ConcatenationAllocation_DoNotWarnForConstAsync(string statement)
        {
            await VerifyCS.VerifyAnalyzerAsync($@"using System;
using Roslyn.Utilities;

public class MyClass
{{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {{
        {statement}
    }}
}}");
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Non_constant_value_types_in_CSharp_string_concatenationAsync()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        System.DateTime c = System.DateTime.Now;
        string s1 = ""char value will box"" + c;
    }
}",
                // Test0.cs(11,45): warning HAA0202: Value type (System.DateTime) is being boxed to a reference type for a string concatenation.
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(ConcatenationAllocationAnalyzer.ValueTypeToReferenceTypeInAStringConcatenationRule).WithLocation(11, 45).WithArguments("System.DateTime"));
#pragma warning restore RS0030 // Do not use banned APIs
        }
    }
}
