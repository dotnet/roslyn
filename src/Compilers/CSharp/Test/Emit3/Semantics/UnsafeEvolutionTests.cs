// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Unsafe)]
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

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_Variable_SafeContext_Var()
    {
        var source = """
            var x = GetPointer();
            int* GetPointer() => null;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // var x = GetPointer();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "GetPointer()").WithArguments("updated memory safety rules").WithLocation(1, 9),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* GetPointer() => null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1));
    }

    [Fact]
    public void Pointer_Variable_UnsafeContext()
    {
        var source = """
            unsafe { int* x = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Pointer_Variable_UsingAlias_SafeContext()
    {
        var source = """
            using X = int*;
            X x = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using X = int*;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 11),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // X x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(2, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // using X = int*;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 11),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // X x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("updated memory safety rules").WithLocation(2, 1));
    }

    [Fact]
    public void Pointer_Variable_UsingAlias_UnsafeContext()
    {
        var source = """
            using unsafe X = int*;
            unsafe { X x = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,7): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // using unsafe X = int*;
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 7),
            // (2,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { X x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Theory, CombinatorialData]
    public void Pointer_Dereference_SafeContext(bool allowUnsafe)
    {
        var source = """
            int* x = null;
            int y = *x;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe))
            .VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 10));

        var expectedDiagnostics = new[]
        {
            // (2,9): error CS9500: This operation may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 9),
        };

        CreateCompilation(source,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithAllowUnsafe(allowUnsafe).WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = *x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 10),
            // (2,9): error CS9500: This operation may only be used in an unsafe context
            // int y = *x;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 9));
    }

    [Fact]
    public void Pointer_Dereference_SafeContext_Null()
    {
        var source = """
            int x = *null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,9): error CS0193: The * or -> operator must be applied to a pointer
            // int x = *null;
            Diagnostic(ErrorCode.ERR_PtrExpected, "*null").WithLocation(1, 9),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Pointer_Dereference_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { int y = *x; }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_MemberAccess_SafeContext()
    {
        var source = """
            int* x = null;
            string s = x->ToString();
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 12));

        var expectedDiagnostics = new[]
        {
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "->").WithLocation(2, 13),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 12),
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = x->ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "->").WithLocation(2, 13));
    }

    [Fact]
    public void Pointer_MemberAccess_SafeContext_Null()
    {
        var source = """
            string s = null->ToString();
            """;

        var expectedDiagnostics = new[]
        {
            // (1,12): error CS0193: The * or -> operator must be applied to a pointer
            // string s = null->ToString();
            Diagnostic(ErrorCode.ERR_PtrExpected, "null->ToString").WithLocation(1, 12),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Pointer_MemberAccess_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { string s = x->ToString(); }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_MemberAccessViaDereference_SafeContext()
    {
        var source = """
            int* x = null;
            string s = (*x).ToString();
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 14));

        var expectedDiagnostics = new[]
        {
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 13),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,14): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 14),
            // (2,13): error CS9500: This operation may only be used in an unsafe context
            // string s = (*x).ToString();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "*").WithLocation(2, 13));
    }

    [Fact]
    public void Pointer_MemberAccessViaDereference_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe { string s = (*x).ToString(); }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext()
    {
        var source = """
            int* x = null;
            x[0] = 1;
            int y = x[1];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (3,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 9));

        var expectedDiagnostics = new[]
        {
            // (2,2): error CS9500: This operation may only be used in an unsafe context
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(2, 2),
            // (3,10): error CS9500: This operation may only be used in an unsafe context
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(3, 10),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,2): error CS9500: This operation may only be used in an unsafe context
            // x[0] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(2, 2),
            // (3,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 9),
            // (3,10): error CS9500: This operation may only be used in an unsafe context
            // int y = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(3, 10));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext_MultipleIndices()
    {
        var source = """
            int* x = null;
            x[0, 1] = 1;
            int y = x[2, 3];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (2,1): error CS0196: A pointer must be indexed by only one value
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[0, 1]").WithLocation(2, 1),
            // (3,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 9),
            // (3,9): error CS0196: A pointer must be indexed by only one value
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[2, 3]").WithLocation(3, 9));

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0196: A pointer must be indexed by only one value
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[0, 1]").WithLocation(2, 1),
            // (3,9): error CS0196: A pointer must be indexed by only one value
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[2, 3]").WithLocation(3, 9),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS0196: A pointer must be indexed by only one value
            // x[0, 1] = 1;
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[0, 1]").WithLocation(2, 1),
            // (3,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 9),
            // (3,9): error CS0196: A pointer must be indexed by only one value
            // int y = x[2, 3];
            Diagnostic(ErrorCode.ERR_PtrIndexSingle, "x[2, 3]").WithLocation(3, 9));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext_ArrayOfPointers()
    {
        var source = """
            int*[] x = [];
            x[0] = null;
            _ = x[1];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int*[] x = [];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0]").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0] = null").WithLocation(2, 1),
            // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 5),
            // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[1]").WithLocation(3, 5),
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "_ = x[1]").WithLocation(3, 1));

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int*[] x = [];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[0]").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[0] = null").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 5),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[1]").WithArguments("updated memory safety rules").WithLocation(3, 5),
            // (3,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "_ = x[1]").WithArguments("updated memory safety rules").WithLocation(3, 1));
    }

    [Fact]
    public void Pointer_ElementAccess_SafeContext_FunctionPointer()
    {
        var source = """
            delegate*<void> x = null;
            x[0] = null;
            _ = x[1];
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // delegate*<void> x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(2, 1),
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[0]").WithArguments("delegate*<void>").WithLocation(2, 1),
            // (3,5): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(3, 5),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[1]").WithArguments("delegate*<void>").WithLocation(3, 5));

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[0]").WithArguments("delegate*<void>").WithLocation(2, 1),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[1]").WithArguments("delegate*<void>").WithLocation(3, 5),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // delegate*<void> x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // x[0] = null;
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[0]").WithArguments("delegate*<void>").WithLocation(2, 1),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x").WithArguments("updated memory safety rules").WithLocation(3, 5),
            // (3,5): error CS0021: Cannot apply indexing with [] to an expression of type 'delegate*<void>'
            // _ = x[1];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "x[1]").WithArguments("delegate*<void>").WithLocation(3, 5));
    }

    [Fact]
    public void Pointer_ElementAccess_UnsafeContext()
    {
        var source = """
            int* x = null;
            unsafe
            {
                x[0] = 1;
                int y = x[1];
            }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_Function_Variable_SafeContext()
    {
        var source = """
            delegate*<void> f = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // delegate*<void> f = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // delegate*<void> f = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }

    [Fact]
    public void Pointer_Function_Variable_UnsafeContext()
    {
        var source = """
            unsafe { delegate*<void> f = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { delegate*<void> f = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();
    }

    [Fact]
    public void Pointer_Function_Variable_UsingAlias_SafeContext()
    {
        var source = """
            using X = delegate*<void>;
            X x = null;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // using X = delegate*<void>;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 11),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // X x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(2, 1),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe).VerifyDiagnostics(expectedDiagnostics);

        // https://github.com/dotnet/roslyn/issues/77389
        expectedDiagnostics =
        [
            // error CS8911: Using a function pointer type in this context is not supported.
            Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported).WithLocation(1, 1),
        ];

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // using X = delegate*<void>;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 11),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // X x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "X").WithArguments("updated memory safety rules").WithLocation(2, 1));
    }

    [Fact]
    public void Pointer_Function_Variable_UsingAlias_UnsafeContext()
    {
        var source = """
            using unsafe X = delegate*<void>;
            unsafe { X x = null; }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,7): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // using unsafe X = delegate*<void>;
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(1, 7),
            // (2,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { X x = null; }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 1),
        };

        CreateCompilation(source).VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        // https://github.com/dotnet/roslyn/issues/77389
        expectedDiagnostics =
        [
            // error CS8911: Using a function pointer type in this context is not supported.
            Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported).WithLocation(1, 1),
        ];

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe)
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics()
            .VerifyEmitDiagnostics(expectedDiagnostics);
    }

    [Fact]
    public void Pointer_Function_Call_SafeContext()
    {
        var source = """
            delegate*<string> x = null;
            string s = x();
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // delegate*<string> x = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "delegate*").WithLocation(1, 1),
            // (2,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // string s = x();
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x()").WithLocation(2, 12));

        var expectedDiagnostics = new[]
        {
            // (2,12): error CS9500: This operation may only be used in an unsafe context
            // string s = x();
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "x()").WithLocation(2, 12),
        };

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // delegate*<string> x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,12): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // string s = x();
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x()").WithArguments("updated memory safety rules").WithLocation(2, 12));
    }

    [Fact]
    public void Pointer_Function_Call_UnsafeContext()
    {
        var source = """
            delegate*<string> x = null;
            unsafe { string s = x(); }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // delegate*<string> x = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "delegate*").WithArguments("updated memory safety rules").WithLocation(1, 1));
    }
}
