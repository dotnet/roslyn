// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForStackAllocCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public class UseCollectionExpressionForStackAllocTests
{
    [Fact]
    public async Task TestNotInCSharp11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = stackalloc int[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = [|[|stackalloc|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleArraySizes()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = stackalloc {|CS1575:int[0, 0]|};
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMismatchedSize1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = {|CS0847:stackalloc int[1] { }|};
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMismatchedSize2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = {|CS0847:stackalloc int[0] { 1 }|};
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNonConstSize()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int size)
                    {
                        ReadOnlySpan<int> x = stackalloc int[{|CS0150:size|}] { 1 };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestConstSize()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        const int size = 1;
                        ReadOnlySpan<int> x = [|[|stackalloc|] int[size]|] { 2 };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        const int size = 1;
                        ReadOnlySpan<int> x = [2];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithPointer()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    unsafe void M()
                    {
                        int* x = stackalloc int[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithVar()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    unsafe void M()
                    {
                        var x = stackalloc int[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }
}
