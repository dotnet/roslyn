// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class UnsafeEvolutionTests : CompilingTestBase
{
    private const int MemorySafetyRulesVersion = 2;

    [Theory, CombinatorialData]
    public void Pointer_Variable_SafeContext(bool allowUnsafe)
    {
        var source = """
            int* x = null;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe)).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1));

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithMemorySafetyRules(MemorySafetyRulesVersion)).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Pointer_Variable_UnsafeContext()
    {
        var source = """
            unsafe { int* x = null; }
            """;

        CreateCompilation(source).VerifyDiagnostics(
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1));

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithMemorySafetyRules(MemorySafetyRulesVersion)).VerifyDiagnostics(
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1));

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithMemorySafetyRules(MemorySafetyRulesVersion)).VerifyEmitDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Pointer_Dereference_SafeContext(bool allowUnsafe)
    {
        var source = """
            int* x = null;
            int y = *x;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe)).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 10));

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithMemorySafetyRules(MemorySafetyRulesVersion)).VerifyDiagnostics(
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 10));
    }

    [Fact]
    public void Pointer_Dereference_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { int y = *x; }
            """;
        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithMemorySafetyRules(MemorySafetyRulesVersion)).VerifyEmitDiagnostics();
    }
}
