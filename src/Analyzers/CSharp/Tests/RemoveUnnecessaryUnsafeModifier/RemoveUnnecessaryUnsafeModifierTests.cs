// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryUnsafeModifier;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
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
}
