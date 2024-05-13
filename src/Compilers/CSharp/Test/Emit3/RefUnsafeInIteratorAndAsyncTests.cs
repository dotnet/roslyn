﻿// Licensed to the .NET Foundation under one or more agreements.
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
            // (6,17): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "y").WithArguments("ref and unsafe in async and iterator methods").WithLocation(6, 17),
            // (7,26): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         ref readonly int z = ref y;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "z").WithArguments("ref and unsafe in async and iterator methods").WithLocation(7, 26));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();
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
            // (6,17): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "y").WithArguments("ref and unsafe in async and iterator methods").WithLocation(6, 17),
            // (7,26): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         ref readonly int z = ref y;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "z").WithArguments("ref and unsafe in async and iterator methods").WithLocation(7, 26));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();
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
            // (6,17): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         ref int y = ref x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "y").WithArguments("ref and unsafe in async and iterator methods").WithLocation(6, 17),
            // (7,26): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         ref readonly int z = ref y;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "z").WithArguments("ref and unsafe in async and iterator methods").WithLocation(7, 26));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();
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
            // (7,9): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         R y = default;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "R").WithArguments("ref and unsafe in async and iterator methods").WithLocation(7, 9),
            // (8,16): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         scoped R z = default;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "R").WithArguments("ref and unsafe in async and iterator methods").WithLocation(8, 16));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics(expectedDiagnostics);
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
            // (7,9): error CS8652: The feature 'ref and unsafe in async and iterator methods' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         System.TypedReference t = default;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "System.TypedReference").WithArguments("ref and unsafe in async and iterator methods").WithLocation(7, 9));

        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics();
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
        CreateCompilation(source, parseOptions: TestOptions.RegularNext).VerifyEmitDiagnostics(expectedDiagnostics);
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
}
