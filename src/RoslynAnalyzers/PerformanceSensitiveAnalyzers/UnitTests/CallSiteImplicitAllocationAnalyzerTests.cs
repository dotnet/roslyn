﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.UnitTests.CSharpPerformanceCodeFixVerifier<
    Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers.CallSiteImplicitAllocationAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysisPerformanceSensitiveAnalyzers.UnitTests
{
    public class CallSiteImplicitAllocationAnalyzerTests
    {
        [Fact]
        public async Task CallSiteImplicitAllocation_ParamAsync()
        {
            var sampleProgram =
@"using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {

        Params(); //no allocation, because compiler will implicitly substitute Array.Empty<int>()
        Params(1, 2);
        Params(new [] { 1, 2}); // explicit, so no warning
        ParamsWithObjects(new [] { 1, 2}); // explicit, but converted to objects, so stil la warning?!

        // Only 4 args and above use the params overload of String.Format
        var test = String.Format(""Testing {0}, {1}, {2}, {3}"", 1, ""blah"", 2.0m, 'c');
    }

    public void Params(params int[] args)
    {
    }

    public void ParamsWithObjects(params object[] args)
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(11,9): warning HAA0101: This call site is calling into a function with a 'params' parameter. This results in an array allocation
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(CallSiteImplicitAllocationAnalyzer.ParamsParameterRule).WithLocation(11, 9),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(13,9): warning HAA0101: This call site is calling into a function with a 'params' parameter. This results in an array allocation
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(CallSiteImplicitAllocationAnalyzer.ParamsParameterRule).WithLocation(13, 9),
#pragma warning restore RS0030 // Do not use banned APIs
                // Test0.cs(16,20): warning HAA0101: This call site is calling into a function with a 'params' parameter. This results in an array allocation
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(CallSiteImplicitAllocationAnalyzer.ParamsParameterRule).WithLocation(16, 20));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact, WorkItem(3272, "https://github.com/dotnet/roslyn-analyzers/issues/3272")]
        public async Task EmptyParamsWithNetFramework45Async()
        {
            await new VerifyCS.Test
            {
                ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net45.Default,
                TestState =
                {
                    Sources =
                    {
                        @"
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        Params(); // allocation
    }

    public void Params(params int[] args)
    {
    }
}",
                        ("PerformanceSensitiveAttribute.cs", VerifyCS.PerformanceSensitiveAttributeSource)
                    },
                    ExpectedDiagnostics =
                    {
#pragma warning disable RS0030 // Do not use banned APIs
                        VerifyCS.Diagnostic(CallSiteImplicitAllocationAnalyzer.ParamsParameterRule).WithLocation(10, 9),
#pragma warning restore RS0030 // Do not use banned APIs
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task CallSiteImplicitAllocation_NonOverridenMethodOnStructAsync()
        {
            var sampleProgram = @"
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var normal = new Normal().GetHashCode();
        var overridden = new OverrideToHashCode().GetHashCode();
    }
}

public struct Normal
{
}

public struct OverrideToHashCode
{
    public override int GetHashCode()
    {
        return -1;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                // Test0.cs(10,22): warning HAA0102: Non-overridden virtual method call on a value type adds a boxing or constrained instruction
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(CallSiteImplicitAllocationAnalyzer.ValueTypeNonOverridenCallRule).WithLocation(10, 22));
#pragma warning restore RS0030 // Do not use banned APIs
        }

        [Fact]
        public async Task CallSiteImplicitAllocation_DoNotReportNonOverriddenMethodCallForStaticCallsAsync()
        {
            var sampleProgram = @"
using System;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        var t = System.Enum.GetUnderlyingType(typeof(System.StringComparison));
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task CallSiteImplicitAllocation_DoNotReportNonOverriddenMethodCallForNonVirtualCallsAsync()
        {
            var sampleProgram = @"
using System.IO;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void Testing()
    {
        FileAttributes attr = FileAttributes.System;
        attr.HasFlag (FileAttributes.Directory);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram);
        }

        [Fact]
        public async Task ParamsIsPrecededByOptionalParametersAsync()
        {
            var sampleProgram = @"
using System.IO;
using Roslyn.Utilities;

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    void Fun1()
    {
        Fun2();
        {|#0:Fun2(args: """", i: 5)|};
    }

    void Fun2(int i = 0, params object[] args)
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(sampleProgram,
                VerifyCS.Diagnostic(CallSiteImplicitAllocationAnalyzer.ParamsParameterRule).WithLocation(0));
        }

        [Fact]
        [WorkItem(7995606, "http://stackoverflow.com/questions/7995606/boxing-occurrence-in-c-sharp")]
        public async Task Calling_non_overridden_virtual_methods_on_value_typesAsync()
        {
            var source = @"
using System;
using Roslyn.Utilities;

enum E { A }

public class MyClass
{
    [PerformanceSensitive(""uri"")]
    public void SomeMethod()
    {
        E.A.GetHashCode();
    }
}";
            await VerifyCS.VerifyAnalyzerAsync(source,
                // Test0.cs(12,9): warning HAA0102: Non-overridden virtual method call on a value type adds a boxing or constrained instruction
#pragma warning disable RS0030 // Do not use banned APIs
                VerifyCS.Diagnostic(CallSiteImplicitAllocationAnalyzer.ValueTypeNonOverridenCallRule).WithLocation(12, 9));
#pragma warning restore RS0030 // Do not use banned APIs
        }
    }
}
