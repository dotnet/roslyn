// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class RefUnsafeInIteratorAndAsyncTests : CSharpTestBase
{
    private static string? IfSpans(string expectedOutput)
        => ExecutionConditionUtil.IsDesktop ? null : expectedOutput;

    [Fact]
    public void LangVersion_RefLocalInAsync()
    {
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M(int x)
                {
                    ref int y = ref x;
                    ref readonly int z = ref y;
                    await Task.Yield();
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (6,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 17),
            // (7,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref readonly int z = ref y;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "z").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 26));

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics();
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void LangVersion_RefLocalInIterator()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    ref readonly int z = ref y;
                    yield return x;
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (6,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 17),
            // (7,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref readonly int z = ref y;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "z").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 26));

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics();
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void LangVersion_RefLocalInIterator_IEnumerator()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerator<int> M(int x)
                {
                    ref int y = ref x;
                    ref readonly int z = ref y;
                    yield return x;
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (6,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 17),
            // (7,26): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref readonly int z = ref y;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "z").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 26));

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics();
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void LangVersion_RefStructInAsync()
    {
        var source = """
            #pragma warning disable CS0219 // variable unused
            using System.Threading.Tasks;
            class C
            {
                async Task M()
                {
                    R y = default;
                    scoped R z = default;
                    await Task.Yield();
                }
            }
            ref struct R { }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (7,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         R y = default;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "R").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 9),
            // (8,16): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         scoped R z = default;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "R").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 16));

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics();
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void LangVersion_RefStructInIterator()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(R r)
                {
                    M(r);
                    yield return -1;        
                }
            }
            ref struct R { }
            """;

        var expectedDiagnostics = new[]
        {
            // (6,11): error CS4007: Instance of type 'R' cannot be preserved across 'await' or 'yield' boundary.
            //         M(r);
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "r").WithArguments("R").WithLocation(6, 11)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void LangVersion_RestrictedInAsync()
    {
        var source = """
            #pragma warning disable CS0219 // variable unused
            using System.Threading.Tasks;
            class C
            {
                async Task M()
                {
                    System.TypedReference t = default;
                    await Task.Yield();
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(
            // (7,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         System.TypedReference t = default;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "System.TypedReference").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 9));

        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics();
        CreateCompilation(source).VerifyEmitDiagnostics();
    }

    [Fact]
    public void LangVersion_RestrictedInIterator()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(System.TypedReference t)
                {
                    t.GetHashCode();
                    yield return -1;
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (6,9): error CS4007: Instance of type 'System.TypedReference' cannot be preserved across 'await' or 'yield' boundary.
            //         t.GetHashCode();
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "t").WithArguments("System.TypedReference").WithLocation(6, 9)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, parseOptions: TestOptions.Regular13).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Await_RefLocal_Across()
    {
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M(int x)
                {
                    ref int y = ref x;
                    await Task.Yield();
                    System.Console.Write(y);
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (8,30): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
            //         System.Console.Write(y);
            Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(8, 30));
    }

    [Fact]
    public void Await_RefLocal_Across_Unsafe_01()
    {
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M(int x)
                {
                    unsafe
                    {
                        ref int y = ref x;
                        await Task.Yield();
                        System.Console.Write(y);
                    }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (8,21): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //             ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 21),
            // (9,13): error CS4004: Cannot await in an unsafe context
            //             await Task.Yield();
            Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Yield()").WithLocation(9, 13));

        var expectedDiagnostics = new[]
        {
            // (9,13): error CS4004: Cannot await in an unsafe context
            //             await Task.Yield();
            Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Yield()").WithLocation(9, 13)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Await_RefLocal_Across_Unsafe_02()
    {
        var source = """
            using System.Threading.Tasks;
            unsafe class C
            {
                async Task M(int x)
                {
                    ref int y = ref x;
                    await Task.Yield();
                    System.Console.Write(y);
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (6,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 17),
            // (7,9): error CS4004: Cannot await in an unsafe context
            //         await Task.Yield();
            Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Yield()").WithLocation(7, 9));

        var expectedDiagnostics = new[]
        {
            // (7,9): error CS4004: Cannot await in an unsafe context
            //         await Task.Yield();
            Diagnostic(ErrorCode.ERR_AwaitInUnsafeContext, "await Task.Yield()").WithLocation(7, 9)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Await_RefLocal_Across_Unsafe_03()
    {
        var source = """
            using System.Threading.Tasks;
            class C
            {
                async Task M(int x)
                {
                    ref int y = ref x;
                    await Task.Yield();
                    unsafe
                    {
                        System.Console.Write(y);
                    }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (6,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 17));

        var expectedDiagnostics = new[]
        {
            // (10,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
            //             System.Console.Write(y);
            Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(10, 34)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Await_RefLocal_Across_Reassign()
    {
        var source = """
            using System.Threading.Tasks;
            class C
            {
                static Task Main() => M(123, 456);
                static async Task M(int x, int z)
                {
                    ref int y = ref x;
                    await Task.Yield();
                    y = ref z;
                    System.Console.Write(y);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "456").VerifyDiagnostics();
    }

    [Fact]
    public void Await_RefLocal_Between()
    {
        var source = """
            using System.Threading.Tasks;
            class C
            {
                static Task Main() => M(123);
                static async Task M(int x)
                {
                    ref int y = ref x;
                    System.Console.Write(y);
                    await Task.Yield();
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "123").VerifyDiagnostics();
    }

    [Fact]
    public void Await_RefStruct_Across()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                async Task M(int x)
                {
                    Span<int> y = new(ref x);
                    await Task.Yield();
                    Console.Write(y.ToString());
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (9,23): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
            //         Console.Write(y.ToString());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.Span<int>").WithLocation(9, 23));
    }

    [Fact]
    public void Await_RefStruct_Across_Unsafe()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                async Task M(int x)
                {
                    Span<int> y = new(ref x);
                    await Task.Yield();
                    unsafe { Console.Write(y.ToString()); }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
            // (7,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         Span<int> y = new(ref x);
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "Span<int>").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(7, 9));

        var expectedDiagnostics = new[]
        {
            // (9,32): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
            //         unsafe { Console.Write(y.ToString()); }
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.Span<int>").WithLocation(9, 32)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Await_RefStruct_Across_Reassign()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                static Task Main() => M(123, 456);
                static async Task M(int x, int z)
                {
                    Span<int> y = new(ref x);
                    await Task.Yield();
                    y = new(ref z);
                    Console.Write(y[0]);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("456"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void Await_RefStruct_Between()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                static Task Main() => M(123);
                static async Task M(int x)
                {
                    Span<int> y = new(ref x);
                    Console.Write(y[0]);
                    await Task.Yield();
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("123"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void Await_Restricted_Across()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                async Task M()
                {
                    TypedReference y = default;
                    await Task.Yield();
                    Console.Write(y.GetHashCode());
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (9,23): error CS4007: Instance of type 'System.TypedReference' cannot be preserved across 'await' or 'yield' boundary.
            //         Console.Write(y.GetHashCode());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.TypedReference").WithLocation(9, 23));
    }

    [Fact]
    public void Await_Restricted_Across_Reassign()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    TypedReference y = default;
                    await Task.Yield();
                    y = default;
                    Console.Write(y.GetHashCode());
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "0").VerifyDiagnostics();
    }

    [Fact]
    public void Await_Restricted_Between()
    {
        var source = """
            using System;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    TypedReference y = default;
                    Console.Write(y.GetHashCode());
                    await Task.Yield();
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "0").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83877")]
    public void Await_RefStruct_OutVar_01()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            await new Test().M();

            public class Test
            {
                public Task T() => Task.CompletedTask;

                public async Task M()
                {
                    await T();

                    if (!F(out var a) || !F(out var b))
                    {
                        return;
                    }

                    Console.Write(a.Value);
                    Console.Write(b.Value);
                }

                private int number;

                private bool F(out Ref<int> result)
                {
                    result = new Ref<int>(ref number);
                    return true;
                }
            }

            public ref struct Ref<T>
            {
                public ref T Value;

                public Ref(ref T value)
                {
                    Value = ref value;
                }
            }
            """;

        var expectedOutput = IfSpans("00");

        CompileAndVerify(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.Net70, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        CompileAndVerify(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83877")]
    public void Await_RefStruct_OutVar_02()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            await new Test().M();

            public class Test
            {
                public Task T() => Task.CompletedTask;

                public async Task M()
                {
                    await T();

                    if (F(out var a) && F(out var b))
                    {
                        Console.Write(a.Value);
                        Console.Write(b.Value);
                    }
                }

                private int number;

                private bool F(out Ref<int> result)
                {
                    result = new Ref<int>(ref number);
                    return true;
                }
            }

            public ref struct Ref<T>
            {
                public ref T Value;

                public Ref(ref T value)
                {
                    Value = ref value;
                }
            }
            """;

        var expectedOutput = IfSpans("00");

        CompileAndVerify(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.Net70, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        CompileAndVerify(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83877")]
    public void Await_RefStruct_OutVar_03()
    {
        var source = """
            using System;
            using System.Threading.Tasks;

            await new Test().M();

            public class Test
            {
                public Task T() => Task.CompletedTask;

                public async Task M()
                {
                    await T();

                    if (!F(out var a))
                    {
                        return;
                    }

                    if (!F(out var b))
                    {
                        return;
                    }

                    Console.Write(a.Value);
                    Console.Write(b.Value);
                }

                private int number;

                private bool F(out Ref<int> result)
                {
                    result = new Ref<int>(ref number);
                    return true;
                }
            }

            public ref struct Ref<T>
            {
                public ref T Value;

                public Ref(ref T value)
                {
                    Value = ref value;
                }
            }
            """;

        var expectedOutput = IfSpans("00");

        CompileAndVerify(source, options: TestOptions.DebugExe, targetFramework: TargetFramework.Net70, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();
        CompileAndVerify(source, options: TestOptions.ReleaseExe, targetFramework: TargetFramework.Net70, expectedOutput: expectedOutput, verify: Verification.FailsPEVerify).VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefLocal_Across()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    yield return 1;
                    System.Console.Write(y);
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (8,30): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
            //         System.Console.Write(y);
            Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(8, 30));
    }

    [Fact]
    public void YieldReturn_RefLocal_Across_Unsafe()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    yield return 1;
                    unsafe { System.Console.Write(y); }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (6,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "y").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(6, 17),
            // (8,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         unsafe { System.Console.Write(y); }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "unsafe").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(8, 9));

        var expectedDiagnostics = new[]
        {
            // (8,39): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
            //         unsafe { System.Console.Write(y); }
            Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(8, 39)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void YieldReturn_RefLocal_Across_Indexer()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> this[int x]
                {
                    get
                    {
                        ref int y = ref x;
                        yield return 1;
                        System.Console.Write(y);
                    }
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (10,34): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
            //             System.Console.Write(y);
            Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(10, 34));
    }

    [Fact]
    public void YieldReturn_RefLocal_Across_NestedBlock()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    if (x != 0) { yield return 1; }
                    System.Console.Write(y);
                }
            }
            """;
        CreateCompilation(source).VerifyEmitDiagnostics(
            // (8,30): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
            //         System.Console.Write(y);
            Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(8, 30));
    }

    [Fact]
    public void YieldReturn_RefLocal_Across_Async()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                async IAsyncEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    yield return 1; await Task.Yield();
                    System.Console.Write(y);
                }
            }
            """ + AsyncStreamsTypes;
        CreateCompilationWithTasksExtensions(source).VerifyEmitDiagnostics(
            // (9,30): error CS9217: A 'ref' local cannot be preserved across 'await' or 'yield' boundary.
            //         System.Console.Write(y);
            Diagnostic(ErrorCode.ERR_RefLocalAcrossAwait, "y").WithLocation(9, 30));
    }

    [Fact]
    public void YieldReturn_RefLocal_Across_Reassign()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in M(123, 456))
                    {
                        Console.Write(i + " ");
                    }
                }
                static IEnumerable<int> M(int x, int z)
                {
                    ref int y = ref x;
                    yield return -1;
                    y = ref z;
                    Console.Write(y);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "-1 456").VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefLocal_Across_Reassign_Indexer()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in new C()[123, 456])
                    {
                        Console.Write(i + " ");
                    }
                }
                IEnumerable<int> this[int x, int z]
                {
                    get
                    {
                        ref int y = ref x;
                        yield return -1;
                        y = ref z;
                        Console.Write(y);
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "-1 456").VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefLocal_Across_Reassign_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var i in M(123, 456))
                    {
                        Console.Write(i + " ");
                    }
                }
                static async IAsyncEnumerable<int> M(int x, int z)
                {
                    ref int y = ref x;
                    yield return -1; await Task.Yield();
                    y = ref z;
                    Console.Write(y);
                }
            }
            """ + AsyncStreamsTypes;
        var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "-1 456").VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefLocal_Between()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in M(123))
                    {
                        Console.Write(i + " ");
                    }
                }
                static IEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    Console.Write(y);
                    yield return -1;
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "123-1").VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefLocal_Between_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var i in M(123))
                    {
                        Console.Write(i + " ");
                    }
                }
                static async IAsyncEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    Console.Write(y);
                    yield return -1; await Task.Yield();
                }
            }
            """ + AsyncStreamsTypes;
        var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "123-1").VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefStruct_Across()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(int x)
                {
                    Span<int> y = new(ref x);
                    yield return -1;
                    Console.Write(y.ToString());
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (9,23): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
            //         Console.Write(y.ToString());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.Span<int>").WithLocation(9, 23));
    }

    [Fact]
    public void YieldReturn_RefStruct_Across_Unsafe()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(int x)
                {
                    Span<int> y = new(ref x);
                    yield return -1;
                    unsafe { Console.Write(y.ToString()); }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70).VerifyDiagnostics(
            // (9,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         unsafe { Console.Write(y.ToString()); }
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "unsafe").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(9, 9));

        var expectedDiagnostics = new[]
        {
            // (9,32): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
            //         unsafe { Console.Write(y.ToString()); }
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.Span<int>").WithLocation(9, 32)
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void YieldReturn_RefStruct_Across_Indexer()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> this[int x]
                {
                    get
                    {
                        Span<int> y = new(ref x);
                        yield return -1;
                        Console.Write(y.ToString());
                    }
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (11,27): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
            //             Console.Write(y.ToString());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.Span<int>").WithLocation(11, 27));
    }

    [Fact]
    public void YieldReturn_RefStruct_Across_NestedBlock()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M(int x)
                {
                    Span<int> y = new(ref x);
                    if (x != 0) { yield return -1; }
                    Console.Write(y.ToString());
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (9,23): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
            //         Console.Write(y.ToString());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.Span<int>").WithLocation(9, 23));
    }

    [Fact]
    public void YieldReturn_RefStruct_Across_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                async IAsyncEnumerable<int> M(int x)
                {
                    Span<int> y = new(ref x);
                    yield return -1; await Task.Yield();
                    Console.Write(y.ToString());
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (10,23): error CS4007: Instance of type 'System.Span<int>' cannot be preserved across 'await' or 'yield' boundary.
            //         Console.Write(y.ToString());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.Span<int>").WithLocation(10, 23));
    }

    [Fact]
    public void YieldReturn_RefStruct_Across_Reassign()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in M(123, 456))
                    {
                        Console.Write(i + " ");
                    }
                }
                static IEnumerable<int> M(int x, int z)
                {
                    Span<int> y = new(ref x);
                    yield return -1;
                    y = new(ref z);
                    Console.Write(y[0]);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("-1 456"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefStruct_Across_Reassign_Indexer()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in new C()[123, 456])
                    {
                        Console.Write(i + " ");
                    }
                }
                IEnumerable<int> this[int x, int z]
                {
                    get
                    {
                        Span<int> y = new(ref x);
                        yield return -1;
                        y = new(ref z);
                        Console.Write(y[0]);
                    }
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("-1 456"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefStruct_Across_Reassign_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var i in M(123, 456))
                    {
                        Console.Write(i + " ");
                    }
                }
                static async IAsyncEnumerable<int> M(int x, int z)
                {
                    Span<int> y = new(ref x);
                    yield return -1; await Task.Yield();
                    y = new(ref z);
                    Console.Write(y[0]);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("-1 456"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_RefStruct_Between()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in M(123))
                    {
                        Console.Write(i + " ");
                    }
                }
                static IEnumerable<int> M(int x)
                {
                    Span<int> y = new(ref x);
                    Console.Write(y[0]);
                    yield return -1;
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("123-1"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_Restricted_Across()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                IEnumerable<int> M()
                {
                    TypedReference y = default;
                    yield return -1;
                    Console.Write(y.GetHashCode());
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (9,23): error CS4007: Instance of type 'System.TypedReference' cannot be preserved across 'await' or 'yield' boundary.
            //         Console.Write(y.GetHashCode());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.TypedReference").WithLocation(9, 23));
    }

    [Fact]
    public void YieldReturn_Restricted_Across_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                async IAsyncEnumerable<int> M()
                {
                    TypedReference y = default;
                    yield return -1; await Task.Yield();
                    Console.Write(y.GetHashCode());
                }
            }
            """;
        CreateCompilation(source, targetFramework: TargetFramework.Net70).VerifyEmitDiagnostics(
            // (10,23): error CS4007: Instance of type 'System.TypedReference' cannot be preserved across 'await' or 'yield' boundary.
            //         Console.Write(y.GetHashCode());
            Diagnostic(ErrorCode.ERR_ByRefTypeAndAwait, "y").WithArguments("System.TypedReference").WithLocation(10, 23));
    }

    [Fact]
    public void YieldReturn_Restricted_Across_Reassign()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in M())
                    {
                        Console.Write(i + " ");
                    }
                }
                static IEnumerable<int> M()
                {
                    TypedReference y = default;
                    yield return -1;
                    y = default;
                    Console.Write(y.GetHashCode());
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "-1 0").VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_Restricted_Across_Reassign_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var i in M())
                    {
                        Console.Write(i + " ");
                    }
                }
                static async IAsyncEnumerable<int> M()
                {
                    TypedReference y = default;
                    yield return -1; await Task.Yield();
                    y = default;
                    Console.Write(y.GetHashCode());
                }
            }
            """ + AsyncStreamsTypes;
        var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "-1 0").VerifyDiagnostics();
    }

    [Fact]
    public void YieldReturn_Restricted_Between()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var i in M())
                    {
                        Console.Write(i + " ");
                    }
                }
                static IEnumerable<int> M()
                {
                    TypedReference y = default;
                    Console.Write(y.GetHashCode());
                    yield return -1;
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "0-1").VerifyDiagnostics();
    }

    [Fact]
    public void YieldBreak_RefLocal_Across()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var a in M(10)) { throw null; }
                    foreach (var b in M(123)) { throw null; }
                }
                static IEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    if (x < 100) yield break;
                    System.Console.Write(y);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "123").VerifyDiagnostics();
    }

    [Fact]
    public void YieldBreak_RefLocal_Across_Async()
    {
        var source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var a in M(10)) { throw null; }
                    await foreach (var b in M(123)) { throw null; }
                }
                static async IAsyncEnumerable<int> M(int x)
                {
                    ref int y = ref x;
                    if (x < 100) { await Task.Yield(); yield break; }
                    System.Console.Write(y);
                }
            }
            """ + AsyncStreamsTypes;
        var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "123").VerifyDiagnostics();
    }

    [Fact]
    public void YieldBreak_RefStruct_Across()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var a in M(10)) { throw null; }
                    foreach (var b in M(123)) { throw null; }
                }
                static IEnumerable<int> M(int x)
                {
                    Span<int> y = new(ref x);
                    if (x < 100) yield break;
                    Console.Write(y[0]);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("123"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void YieldBreak_RefStruct_Across_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var a in M(10)) { throw null; }
                    await foreach (var b in M(123)) { throw null; }
                }
                static async IAsyncEnumerable<int> M(int x)
                {
                    Span<int> y = new(ref x);
                    if (x < 100) { await Task.Yield(); yield break; }
                    Console.Write(y[0]);
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: IfSpans("123"), verify: Verification.FailsPEVerify, targetFramework: TargetFramework.Net70).VerifyDiagnostics();
    }

    [Fact]
    public void YieldBreak_Restricted_Across()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            class C
            {
                static void Main()
                {
                    foreach (var a in M(10)) { throw null; }
                    foreach (var b in M(123)) { throw null; }
                }
                static IEnumerable<int> M(int x)
                {
                    TypedReference t = default;
                    if (x < 100) yield break;
                    Console.Write(x + t.GetHashCode());
                }
            }
            """;
        CompileAndVerify(source, expectedOutput: "123").VerifyDiagnostics();
    }

    [Fact]
    public void YieldBreak_Restricted_Across_Async()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            class C
            {
                static async Task Main()
                {
                    await foreach (var a in M(10)) { throw null; }
                    await foreach (var b in M(123)) { throw null; }
                }
                static async IAsyncEnumerable<int> M(int x)
                {
                    TypedReference t = default;
                    if (x < 100) { await Task.Yield(); yield break; }
                    Console.Write(x + t.GetHashCode());
                }
            }
            """ + AsyncStreamsTypes;
        var comp = CreateCompilationWithTasksExtensions(source, options: TestOptions.ReleaseExe);
        CompileAndVerify(comp, expectedOutput: "123").VerifyDiagnostics();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83564")]
    public void RefAnalysis_Iterator()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                int field;

                unsafe void M1()
                {
                    ref int outside = ref field;
                    {
                        int local = 1;
                        outside = ref local; // 1
                    }
                }

                unsafe IEnumerable<int> M2()
                {
                    ref int outside = ref field;
                    {
                        int local = 1;
                        outside = ref local; // 2
                    }
                    yield return 1;
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (12,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(12, 13),
            // (16,29): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //     unsafe IEnumerable<int> M2()
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "M2").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(16, 29),
            // (18,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int outside = ref field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "outside").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(18, 17),
            // (21,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 2
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(21, 13));

        var expectedDiagnostics = new[]
        {
            // (12,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(12, 13),
            // (21,13): error CS8374: Cannot ref-assign 'local' to 'outside' because 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 2
            Diagnostic(ErrorCode.ERR_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(21, 13),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83564")]
    public void RefAnalysis_Iterator_UnsafeBlock()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                int field;

                void M1()
                {
                    ref int outside = ref field;
                    unsafe {
                        int local = 1;
                        outside = ref local; // 1
                    }
                }

                IEnumerable<int> M2()
                {
                    ref int outside = ref field;
                    unsafe {
                        int local = 1;
                        outside = ref local; // 2
                    }
                    yield return 1;
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (12,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(12, 13),
            // (18,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int outside = ref field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "outside").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(18, 17),
            // (19,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         unsafe {
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "unsafe").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(19, 9),
            // (21,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 2
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(21, 13));

        var expectedDiagnostics = new[]
        {
            // (12,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(12, 13),
            // (21,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local; // 2
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(21, 13),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83564")]
    public void RefAnalysis_Iterator_UnsafeBlockAndIterator()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                int field;

                IEnumerable<int> M()
                {
                    ref int outside = ref field;
                    unsafe
                    {
                        int local = 1;
                        outside = ref local;
                        yield return 1;
                    }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (9,17): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         ref int outside = ref field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "outside").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(9, 17),
            // (10,9): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         unsafe
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "unsafe").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(10, 9),
            // (13,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local;
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(13, 13));

        var expectedDiagnostics = new[]
        {
            // (13,13): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //             outside = ref local;
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(13, 13),
            // (14,13): error CS9238: Cannot use 'yield return' in an 'unsafe' block
            //             yield return 1;
            Diagnostic(ErrorCode.ERR_BadYieldInUnsafe, "yield").WithLocation(14, 13),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83564")]
    public void RefAnalysis_Iterator_LocalFunction()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                int field;

                void M1()
                {
                    F1();
                    F2();

                    unsafe void F1()
                    {
                        ref int outside = ref field;
                        {
                            int local = 1;
                            outside = ref local; // 1
                        }
                    }

                    unsafe IEnumerable<int> F2()
                    {
                        ref int outside = ref field;
                        {
                            int local = 1;
                            outside = ref local; // 2
                        }
                        yield return 1;
                    }
                }

                IEnumerable<int> M2()
                {
                    F1();
                    F2();

                    unsafe void F1()
                    {
                        ref int outside = ref field;
                        {
                            int local = 1;
                            outside = ref local; // 3
                        }
                    }

                    unsafe IEnumerable<int> F2()
                    {
                        ref int outside = ref field;
                        {
                            int local = 1;
                            outside = ref local; // 4
                        }
                        yield return 1;
                    }

                    yield return 1;
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (17,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(17, 17),
            // (21,33): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         unsafe IEnumerable<int> F2()
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "F2").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(21, 33),
            // (23,21): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //             ref int outside = ref field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "outside").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(23, 21),
            // (26,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 2
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(26, 17),
            // (42,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 3
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(42, 17),
            // (46,33): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //         unsafe IEnumerable<int> F2()
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "F2").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(46, 33),
            // (48,21): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //             ref int outside = ref field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "outside").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(48, 21),
            // (51,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 4
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(51, 17));

        var expectedDiagnostics = new[]
        {
            // (17,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(17, 17),
            // (26,17): error CS8374: Cannot ref-assign 'local' to 'outside' because 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 2
            Diagnostic(ErrorCode.ERR_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(26, 17),
            // (42,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 3
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(42, 17),
            // (51,17): error CS8374: Cannot ref-assign 'local' to 'outside' because 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 4
            Diagnostic(ErrorCode.ERR_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(51, 17),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83564")]
    public void RefAnalysis_Iterator_LocalFunction_UnsafeBlock()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                int field;

                void M1()
                {
                    unsafe
                    {
                        F1();
                        F2();

                        void F1()
                        {
                            ref int outside = ref field;
                            {
                                int local = 1;
                                outside = ref local; // 1
                            }
                        }

                        IEnumerable<int> F2()
                        {
                            ref int outside = ref field;
                            {
                                int local = 1;
                                outside = ref local; // 2
                            }
                            yield return 1;
                        }
                    }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (19,21): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(19, 21),
            // (25,25): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //                 ref int outside = ref field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "outside").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(25, 25),
            // (28,21): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 2
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(28, 21));

        var expectedDiagnostics = new[]
        {
            // (19,21): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(19, 21),
            // (28,21): error CS8374: Cannot ref-assign 'local' to 'outside' because 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 2
            Diagnostic(ErrorCode.ERR_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(28, 21),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/83564")]
    public void RefAnalysis_Iterator_LocalFunction_Nested()
    {
        var source = """
            using System.Collections.Generic;

            class C
            {
                int field;

                void M1()
                {
                    F1();

                    unsafe void F1()
                    {
                        ref int outside = ref field;
                        {
                            int local = 1;
                            outside = ref local; // 1
                        }

                        F11();
                        F12();

                        void F11()
                        {
                            ref int outside = ref field;
                            {
                                int local = 1;
                                outside = ref local; // 2
                            }
                        }

                        IEnumerable<int> F12()
                        {
                            ref int outside = ref field;
                            {
                                int local = 1;
                                outside = ref local; // 3
                            }
                            yield return 1;
                        }
                    }
                }
            }
            """;

        CreateCompilation(source, parseOptions: TestOptions.Regular12, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
            // (16,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(16, 17),
            // (27,21): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 2
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(27, 21),
            // (33,25): error CS9202: Feature 'ref and unsafe in async and iterator methods' is not available in C# 12.0. Please use language version 13.0 or greater.
            //                 ref int outside = ref field;
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion12, "outside").WithArguments("ref and unsafe in async and iterator methods", "13.0").WithLocation(33, 25),
            // (36,21): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 3
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(36, 21));

        var expectedDiagnostics = new[]
        {
            // (16,17): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                 outside = ref local; // 1
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(16, 17),
            // (27,21): warning CS9085: This ref-assigns 'local' to 'outside' but 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 2
            Diagnostic(ErrorCode.WRN_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(27, 21),
            // (36,21): error CS8374: Cannot ref-assign 'local' to 'outside' because 'local' has a narrower escape scope than 'outside'.
            //                     outside = ref local; // 3
            Diagnostic(ErrorCode.ERR_RefAssignNarrower, "outside = ref local").WithArguments("outside", "local").WithLocation(36, 21),
        };

        CreateCompilation(source, parseOptions: TestOptions.Regular13, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(expectedDiagnostics);
    }
}
