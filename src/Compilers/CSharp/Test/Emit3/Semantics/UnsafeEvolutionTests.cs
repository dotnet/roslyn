// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
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
    public void Pointer_Variable_SafeContext_InIterator()
    {
        var source = """
            unsafe
            {
                M();
                System.Collections.Generic.IEnumerable<int> M()
                {
                    int* p = null;
                    yield return 1;
                }
            }
            """;

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
            // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(6, 9));

        CreateCompilation(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (6,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         int* p = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(6, 9));
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
        expectedDiagnostics = PlatformInformation.IsWindows
            ? [
                // error CS8911: Using a function pointer type in this context is not supported.
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported).WithLocation(1, 1),
            ]
            : [];

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
        expectedDiagnostics = PlatformInformation.IsWindows
            ? [
                // error CS8911: Using a function pointer type in this context is not supported.
                Diagnostic(ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported).WithLocation(1, 1),
            ]
            : [];

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

    [Fact]
    public void Pointer_AddressOf_SafeContext()
    {
        var source = """
            int x;
            int* p = &x;
            """;

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 1),
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(2, 10),
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
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "&x").WithArguments("updated memory safety rules").WithLocation(2, 10));
    }

    [Fact]
    public void Pointer_AddressOf_SafeContext_Const()
    {
        var source = """
            const int x = 1;
            int* p = &x;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 1),
            // (2,11): error CS0211: Cannot take the address of the given expression
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x").WithLocation(2, 11));

        var expectedDiagnostics = new[]
        {
            // (2,11): error CS0211: Cannot take the address of the given expression
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x").WithLocation(2, 11),
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
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,11): error CS0211: Cannot take the address of the given expression
            // int* p = &x;
            Diagnostic(ErrorCode.ERR_InvalidAddrOp, "x").WithLocation(2, 11));
    }

    [Fact]
    public void Pointer_AddressOf_UnsafeContext()
    {
        var source = """
            int x;
            unsafe { int* p = &x; }
            """;

        var expectedDiagnostics = new[]
        {
            // (2,1): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // unsafe { int* p = &x; }
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

    [Fact]
    public void Pointer_Fixed_SafeContext()
    {
        var source = """
            class C
            {
                static int x;
                static void Main()
                {
                    fixed (int* p = &x) { }
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (6,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "fixed (int* p = &x) { }").WithLocation(6, 9),
            // (6,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(6, 16),
            // (6,25): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(6, 25),
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
            // (6,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "fixed (int* p = &x) { }").WithArguments("updated memory safety rules").WithLocation(6, 9),
            // (6,16): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(6, 16),
            // (6,25): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "&x").WithArguments("updated memory safety rules").WithLocation(6, 25));
    }

    [Fact]
    public void Pointer_Fixed_PatternBased()
    {
        var source = """
            class C
            {
                static void Main()
                {
                    fixed (int* p = new S()) { }
                }
            }

            struct S
            {
                public ref readonly int GetPinnableReference() => throw null;
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (5,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "fixed (int* p = new S()) { }").WithLocation(5, 9),
            // (5,16): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(5, 16),
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
            // (5,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "fixed (int* p = new S()) { }").WithArguments("updated memory safety rules").WithLocation(5, 9),
            // (5,16): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //         fixed (int* p = new S()) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(5, 16));
    }

    [Fact]
    public void Pointer_Fixed_SafeContext_AlreadyFixed()
    {
        var source = """
            int x;
            fixed (int* p = &x) { }
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "fixed (int* p = &x) { }").WithLocation(2, 1),
            // (2,8): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 8),
            // (2,17): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&x").WithLocation(2, 17));

        var expectedDiagnostics = new[]
        {
            // (2,17): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&x").WithLocation(2, 17),
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
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "fixed (int* p = &x) { }").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,8): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 8),
            // (2,17): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
            // fixed (int* p = &x) { }
            Diagnostic(ErrorCode.ERR_FixedNotNeeded, "&x").WithLocation(2, 17));
    }

    [Fact]
    public void Pointer_Fixed_UnsafeContext()
    {
        var source = """
            class C
            {
                static int x;
                static void Main()
                {
                    unsafe { fixed (int* p = &x) { } }
                }
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (6,9): error CS0227: Unsafe code may only appear if compiling with /unsafe
            //         unsafe { fixed (int* p = &x) { } }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(6, 9),
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
    public void Pointer_Arithmetic_SafeContext()
    {
        var source = """
            int* p = null;
            p++;
            int* p2 = p + 2;
            long x = p - p;
            bool b = p > p2;
            """;

        var expectedDiagnostics = new[]
        {
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = null;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // p++;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(2, 1),
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // p++;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p++").WithLocation(2, 1),
            // (3,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(3, 1),
            // (3,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(3, 11),
            // (3,11): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p + 2").WithLocation(3, 11),
            // (4,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(4, 10),
            // (4,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(4, 14),
            // (5,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p").WithLocation(5, 10),
            // (5,14): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "p2").WithLocation(5, 14),
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
            // int* p = null;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // p++;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // p++;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p++").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (3,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(3, 1),
            // (3,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(3, 11),
            // (3,11): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p2 = p + 2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p + 2").WithArguments("updated memory safety rules").WithLocation(3, 11),
            // (4,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(4, 10),
            // (4,14): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // long x = p - p;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(4, 14),
            // (5,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p").WithArguments("updated memory safety rules").WithLocation(5, 10),
            // (5,14): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // bool b = p > p2;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "p2").WithArguments("updated memory safety rules").WithLocation(5, 14));
    }

    [Fact]
    public void SizeOf_SafeContext()
    {
        var source = """
            _ = sizeof(int);
            _ = sizeof(nint);
            _ = sizeof(S);
            struct S;
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,5): error CS0233: 'nint' does not have a predefined size, therefore sizeof can only be used in an unsafe context
            // _ = sizeof(nint);
            Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(nint)").WithArguments("nint").WithLocation(2, 5),
            // (3,5): error CS0233: 'S' does not have a predefined size, therefore sizeof can only be used in an unsafe context
            // _ = sizeof(S);
            Diagnostic(ErrorCode.ERR_SizeofUnsafe, "sizeof(S)").WithArguments("S").WithLocation(3, 5));

        CreateCompilation(source, options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules()).VerifyEmitDiagnostics();

        CreateCompilation(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.ReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (2,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = sizeof(nint);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "sizeof(nint)").WithArguments("updated memory safety rules").WithLocation(2, 5),
            // (3,5): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // _ = sizeof(S);
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "sizeof(S)").WithArguments("updated memory safety rules").WithLocation(3, 5));
    }

    [Fact]
    public void FixedSizeBuffer_SafeContext()
    {
        var source = """
            var s = new S();
            int* p = s.y;
            int z = s.x[100];

            struct S
            {
                public fixed int x[5], y[10];
            }
            """;

        CreateCompilation(source, options: TestOptions.ReleaseExe).VerifyDiagnostics(
            // (2,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 1),
            // (2,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s.y").WithLocation(2, 10),
            // (3,9): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "s.x").WithLocation(3, 9),
            // (7,22): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            //     public fixed int x[5], y[10];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[5]").WithLocation(7, 22));

        var expectedDiagnostics = new[]
        {
            // (3,12): error CS9500: This operation may only be used in an unsafe context
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(3, 12),
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
            // (2,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(2, 1),
            // (2,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* p = s.y;
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.y").WithArguments("updated memory safety rules").WithLocation(2, 10),
            // (3,9): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "s.x").WithArguments("updated memory safety rules").WithLocation(3, 9),
            // (3,12): error CS9500: This operation may only be used in an unsafe context
            // int z = s.x[100];
            Diagnostic(ErrorCode.ERR_UnsafeOperation, "[").WithLocation(3, 12),
            // (7,22): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            //     public fixed int x[5], y[10];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "x[5]").WithArguments("updated memory safety rules").WithLocation(7, 22));
    }

    [Fact]
    public void SkipLocalsInit_NeedsUnsafe()
    {
        var source = """
            class C { [System.Runtime.CompilerServices.SkipLocalsInit] void M() { } }

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        var expectedDiagnostics = new[]
        {
            // (1,12): error CS0227: Unsafe code may only appear if compiling with /unsafe
            // class C { [System.Runtime.CompilerServices.SkipLocalsInit] void M() { } }
            Diagnostic(ErrorCode.ERR_IllegalUnsafe, "System.Runtime.CompilerServices.SkipLocalsInit").WithLocation(1, 12),
        };

        CreateCompilation(source, options: TestOptions.ReleaseDll)
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.ReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll)
            .VerifyEmitDiagnostics();

        CreateCompilation(source, options: TestOptions.UnsafeReleaseDll.WithUpdatedMemorySafetyRules())
            .VerifyEmitDiagnostics();
    }

    [Fact]
    public void StackAlloc_SafeContext()
    {
        var source = """
            int* x = stackalloc int[3];
            System.Span<int> y = stackalloc int[5];
            M();

            [System.Runtime.CompilerServices.SkipLocalsInit]
            void M()
            {
                System.Span<int> a = stackalloc int[5];
                System.Span<int> b = stackalloc int[] { 1 };
                System.Span<int> d = stackalloc int[2] { 1, 2 };
                System.Span<int> e = stackalloc int[3] { 1, 2 };
            }

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (1,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[3]").WithLocation(1, 10),
            // (11,26): error CS0847: An array initializer of length '3' is expected
            //     System.Span<int> e = stackalloc int[3] { 1, 2 };
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 26));

        var expectedDiagnostics = new[]
        {
            // (8,26): error CS9501: stackalloc expression without an initializer inside SkipLocalsInit may only be used in an unsafe context
            //     System.Span<int> a = stackalloc int[5];
            Diagnostic(ErrorCode.ERR_UnsafeUninitializedStackAlloc, "stackalloc int[5]").WithLocation(8, 26),
            // (11,26): error CS0847: An array initializer of length '3' is expected
            //     System.Span<int> e = stackalloc int[3] { 1, 2 };
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 26),
        };

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (1,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "stackalloc int[3]").WithArguments("updated memory safety rules").WithLocation(1, 10),
            // (8,26): error CS9501: stackalloc expression without an initializer inside SkipLocalsInit may only be used in an unsafe context
            //     System.Span<int> a = stackalloc int[5];
            Diagnostic(ErrorCode.ERR_UnsafeUninitializedStackAlloc, "stackalloc int[5]").WithLocation(8, 26),
            // (11,26): error CS0847: An array initializer of length '3' is expected
            //     System.Span<int> e = stackalloc int[3] { 1, 2 };
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 26));
    }

    [Fact]
    public void StackAlloc_UnsafeContext()
    {
        var source = $$"""
            int* x = stackalloc int[3];
            System.Span<int> y = stackalloc int[5];
            M();

            [System.Runtime.CompilerServices.SkipLocalsInit]
            void M()
            {
                unsafe { System.Span<int> a = stackalloc int[5]; }
                System.Span<int> b = stackalloc int[] { 1 };
                System.Span<int> d = stackalloc int[2] { 1, 2 };
                unsafe { System.Span<int> e = stackalloc int[3] { 1, 2 }; }
            }

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe).VerifyDiagnostics(
            // (1,1): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(1, 1),
            // (1,10): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_UnsafeNeeded, "stackalloc int[3]").WithLocation(1, 10),
            // (11,35): error CS0847: An array initializer of length '3' is expected
            //     unsafe { System.Span<int> e = stackalloc int[3] { 1, 2 }; }
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 35));

        var expectedDiagnostics = new[]
        {
            // (11,35): error CS0847: An array initializer of length '3' is expected
            //     unsafe { System.Span<int> e = stackalloc int[3] { 1, 2 }; }
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 35),
        };

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.RegularNext,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(expectedDiagnostics);

        CreateCompilationWithSpan(source,
            parseOptions: TestOptions.Regular14,
            options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (1,1): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "int*").WithArguments("updated memory safety rules").WithLocation(1, 1),
            // (1,10): error CS8652: The feature 'updated memory safety rules' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            // int* x = stackalloc int[3];
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "stackalloc int[3]").WithArguments("updated memory safety rules").WithLocation(1, 10),
            // (11,35): error CS0847: An array initializer of length '3' is expected
            //     unsafe { System.Span<int> e = stackalloc int[3] { 1, 2 }; }
            Diagnostic(ErrorCode.ERR_ArrayInitializerIncorrectLength, "stackalloc int[3] { 1, 2 }").WithArguments("3").WithLocation(11, 35));
    }

    [Fact]
    public void StackAlloc_Lambda()
    {
        var source = """
            var lam = [System.Runtime.CompilerServices.SkipLocalsInit] () =>
            {
                System.Span<int> a = stackalloc int[5];
                int* b = stackalloc int[3];
                unsafe { System.Span<int> c = stackalloc int[1]; }
            };

            namespace System.Runtime.CompilerServices
            {
                public class SkipLocalsInitAttribute : Attribute;
            }
            """;

        CreateCompilationWithSpan(source, options: TestOptions.UnsafeReleaseExe.WithUpdatedMemorySafetyRules())
            .VerifyDiagnostics(
            // (3,26): error CS9501: stackalloc expression without an initializer inside SkipLocalsInit may only be used in an unsafe context
            //     System.Span<int> a = stackalloc int[5];
            Diagnostic(ErrorCode.ERR_UnsafeUninitializedStackAlloc, "stackalloc int[5]").WithLocation(3, 26));
    }
}
