// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryUnsafeModifier;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryUnsafeModifier;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryUnsafeModifierDiagnosticAnalyzer,
    CSharpRemoveUnnecessaryUnsafeModifierCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)]
[WorkItem("https://github.com/dotnet/roslyn/issues/48031")]
public sealed class RemoveUnnecessaryUnsafeModifierTests
{
    [Fact]
    public Task RemoveWhenNotNeeded_Method()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    [|unsafe|] void M()
                    {
                        int a = 0;
                        int b = a + 1;
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        int a = 0;
                        int b = a + 1;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task RemoveWhenNotNeeded_LocalFunction()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Outer()
                    {
                        [|unsafe|] void M()
                        {
                            int a = 0;
                            int b = a + 1;
                        }
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Outer()
                    {
                        void M()
                        {
                            int a = 0;
                            int b = a + 1;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task RemoveWhenNotNeeded_Type1()
        => new VerifyCS.Test
        {
            TestCode = """
                [|unsafe|] class C
                {
                }
                """,
            FixedCode = """
                class C
                {
                }
                """,
        }.RunAsync();

    [Fact]
    public Task RemoveWhenNotNeeded_Type2()
        => new VerifyCS.Test
        {
            TestCode = """
                [|unsafe|] class C
                {
                    [|unsafe|] void M(int* i) { }
                }
                """,
            FixedCode = """
                class C
                {
                    unsafe void M(int* i) { }
                }
                """,
            BatchFixedCode = """
                unsafe class C
                {
                    void M(int* i) { }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task RemoveWhenNotNeeded_FixAll()
        => new VerifyCS.Test
        {
            TestCode = """
                [|unsafe|] class C
                {
                    [|unsafe|] void M() { }
                }
                """,
            FixedCode = """
                class C
                {
                    void M() { }
                }
                """,
            NumberOfFixAllIterations = 2,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/84564")]
    public Task KeepWhenRequiredForExplicitLayoutField()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Runtime.InteropServices;

                [StructLayout(LayoutKind.Explicit)]
                struct S
                {
                    [FieldOffset(0)] public unsafe int F1;
                    [FieldOffset(4)] public unsafe int F2;
                }
                """,
            LanguageVersion = LanguageVersion.Preview,
            SolutionTransforms = { EnableUpdatedMemorySafetyRules },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/84564")]
    public Task KeepWhenRequiredForExternMethod()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public unsafe extern void M();
                }
                """,
            LanguageVersion = LanguageVersion.Preview,
            SolutionTransforms = { EnableUpdatedMemorySafetyRules },
        }.RunAsync();

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85732")]
    public Task KeepWhenItMarksCallerUnsafe(
        [CombinatorialValues(
            "public unsafe void M() { }",
            "public unsafe C() { }",
            "public unsafe int F;",
            "public unsafe int P => 0;",
            "public unsafe int this[int i] => 0;",
            "public unsafe event System.Action E { add { } remove { } }",
            "public static unsafe C operator +(C left, C right) => left;",
            "public static unsafe explicit operator int(C value) => 0;")] string member)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    {{member}}
                }
                """,
            LanguageVersion = LanguageVersion.Preview,
            SolutionTransforms = { EnableUpdatedMemorySafetyRules },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/85732")]
    public Task KeepWhenItMarksLocalFunctionCallerUnsafe()
        => new VerifyCS.Test
        {
            TestCode = """
                static unsafe void Local() { }
                """,
            LanguageVersion = LanguageVersion.Preview,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            SolutionTransforms = { EnableUpdatedMemorySafetyRules },
        }.RunAsync();

    [Theory, CombinatorialData]
    public Task RemoveWhenNotNeededDueToSafePointers(
        [CombinatorialValues(LanguageVersionExtensions.CSharpNext, LanguageVersion.Preview)] LanguageVersion languageVersion)
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public [|unsafe|] void M(int* p) { }
                }
                """,
            FixedCode = """
                class C
                {
                    public void M(int* p) { }
                }
                """,
            LanguageVersion = languageVersion,
        }.RunAsync();

    [Fact]
    public Task KeepWhenNeeded_Method()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    unsafe void M(int* p)
                    {
                        int a = 0;
                        int b = a + 1;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task KeepWhenNeeded_LocalFunction()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void Outer()
                    {
                        unsafe void M(int* p)
                        {
                            int a = 0;
                            int b = a + 1;
                        }
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task KeepWhenNeeded_Type1()
        => new VerifyCS.Test
        {
            TestCode = """
                unsafe class C
                {
                    int* p;
                }
                """,
        }.RunAsync();

    private static Solution EnableUpdatedMemorySafetyRules(Solution solution, ProjectId projectId)
    {
        var compilationOptions = (CSharpCompilationOptions)solution.GetRequiredProject(projectId).CompilationOptions!;
        return solution.WithProjectCompilationOptions(
                projectId, compilationOptions.WithMemorySafetyRulesVersion(MemorySafetyRulesVersion.Version2));
    }
}
