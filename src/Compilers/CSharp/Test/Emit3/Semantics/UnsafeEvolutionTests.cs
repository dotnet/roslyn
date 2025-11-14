// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

public sealed class UnsafeEvolutionTests : CompilingTestBase
{
    [Theory, CombinatorialData]
    public void Pointer_Variable_SafeContext(bool allowUnsafe)
    {
        var source = """
            int* x = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe)).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe)).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithEvolvedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithEvolvedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithEvolvedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'evolved memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("evolved memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_Variable_SafeContext_Var()
    {
        var source = """
            var x = GetPointer();
            int* GetPointer() => null;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithEvolvedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithEvolvedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,9): error CS8652: The feature 'evolved memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // var x = GetPointer();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "GetPointer()").WithArguments("evolved memory safety rules").WithLocation(1, 9),
            // (2,1): error CS8652: The feature 'evolved memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* GetPointer() => null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("evolved memory safety rules").WithLocation(2, 1));
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

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithEvolvedMemorySafetyRules()).VerifyDiagnostics(
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1));

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithEvolvedMemorySafetyRules()).VerifyEmitDiagnostics();
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

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithEvolvedMemorySafetyRules()).VerifyDiagnostics(
            // (2,9): error CS9500: This operation may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 9));
    }

    [Fact]
    public void Pointer_Dereference_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { int y = *x; }
            """;
        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithEvolvedMemorySafetyRules()).VerifyEmitDiagnostics();
    }
}
