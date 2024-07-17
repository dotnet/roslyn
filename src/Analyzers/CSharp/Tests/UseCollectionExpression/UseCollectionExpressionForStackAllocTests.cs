// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
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
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp11_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = stackalloc[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp12_Net70()
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotInCSharp12_Net70_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = stackalloc[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> x = [|[|stackalloc|][]|] { 1, 2, 3 };
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Span()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Span<int> x = [|[|stackalloc|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Span<int> x = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Span_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Span<int> x = [|[|stackalloc|][]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Span<int> x = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithPointer_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    unsafe void M()
                    {
                        int* x = stackalloc[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithVar_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    unsafe void M()
                    {
                        var x = stackalloc[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithSpanArgument()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Goo([|[|stackalloc|] int[]|] { 1, 2, 3 });
                    }

                    void Goo(ReadOnlySpan<int> span) { }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Goo([1, 2, 3]);
                    }

                    void Goo(ReadOnlySpan<int> span) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithSpanArgument_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Goo([|[|stackalloc|][]|] { 1, 2, 3 });
                    }

                    void Goo(ReadOnlySpan<int> span) { }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Goo([1, 2, 3]);
                    }

                    void Goo(ReadOnlySpan<int> span) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEmpty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = [|[|stackalloc|] int[]|] { };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCast()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        var r = (ReadOnlySpan<int>)[|[|stackalloc|] int[]|] { };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        var r = (ReadOnlySpan<int>)[];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestIdentifierCast()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using X = System.ReadOnlySpan<int>;

                class C
                {
                    void M()
                    {
                        var r = (X)stackalloc int[] { };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEmptyWithSize()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = [|[|stackalloc|] int[0]|] { };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEmpty_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = {|CS8346:{|CS0826:stackalloc[] { }|}|};
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ZeroSize()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = [|[|stackalloc|] int[0]|];
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_NotEnoughFollowingStatements()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = stackalloc int[1];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_WrongFollowingStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> r = stackalloc int[1];
                        return;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_NotLocalStatementInitializer()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Span<int> r = Goo(stackalloc int[1]);
                        r[0] = 1;
                    }

                    Span<int> Goo(Span<int> input) => default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ExpressionStatementNotAssignment()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i)
                    {
                        Span<int> r = stackalloc int[1];
                        i++;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_AssignmentNotElementAccess()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = stackalloc int[1];
                        i = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ElementAccessNotToIdentifier()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    static int[] array;

                    void M(int i, int j)
                    {
                        Span<int> r = stackalloc int[1];
                        C.array[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_IdentifierNotEqualToVariableName()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    static int[] array;

                    void M(int i, int j)
                    {
                        Span<int> r = stackalloc int[1];
                        array[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ArgumentNotConstant()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = stackalloc int[1];
                        r[i] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ConstantArgumentNotCorrect1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = stackalloc int[1];
                        r[1] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ConstantArgumentNotCorrect2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = stackalloc int[2];
                        r[0] = i;
                        r[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_OneElement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[1]|];
                        r[0] = i;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [i];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_OneElement_MultipleFollowingStatements()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[1]|];
                        r[0] = i;
                        r[0] = j;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [i];
                        r[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[2]|];
                        r[0] = i;
                        r[1] = j;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [i, j];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_Constant()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        const int v = 1;
                        Span<int> r = [|[|stackalloc|] int[2]|];
                        r[0] = i;
                        r[v] = j;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        const int v = 1;
                        Span<int> r = [i, j];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_SecondWrongIndex()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = stackalloc int[2];
                        r[0] = i;
                        r[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_SecondNonConstant()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        var v = 1;
                        Span<int> r = stackalloc int[2];
                        r[0] = i;
                        r[v] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_SecondWrongDestination()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    static int[] array;

                    void M(int i, int j)
                    {
                        var v = 1;
                        Span<int> r = stackalloc int[2];
                        r[0] = i;
                        array[1] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[2]|];
                        // Leading
                        r[0] = i;
                        r[1] = j; // Trailing
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            // Leading
                            i,
                            j, // Trailing
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[2]|];
                        r[0] = i; // Trailing
                        // Leading
                        r[1] = j;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            i, // Trailing
                            // Leading
                            j,
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[]|]
                        {
                            1, 2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            1, 2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[]|]
                        {
                            1,
                            2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            1,
                            2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[]|] {
                            1, 2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [
                            1, 2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[]|] {
                            1,
                            2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [
                            1,
                            2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine1_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|][]|]
                        {
                            1, 2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            1, 2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine2_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|][]|]
                        {
                            1,
                            2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            1,
                            2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine3_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|][]|] {
                            1, 2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [
                            1, 2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExistingInitializer_MultiLine4_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|][]|] {
                            1,
                            2
                        };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [
                            1,
                            2
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_MultiLine1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[1]|];
                        r[0] = 1 +
                            2;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            1 +
                                2,
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_MultiLine2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r = [|[|stackalloc|] int[2]|];
                        r[0] = 1 +
                            2;
                        r[1] = 3 +
                            4;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        Span<int> r =
                        [
                            1 +
                                2,
                            3 +
                                4,
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGlobalStatement1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                ReadOnlySpan<int> x = [|[|stackalloc|] int[]|] { 1, 2, 3 };
                """,
            FixedCode = """
                using System;

                ReadOnlySpan<int> x = [1, 2, 3];
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestGlobalStatement2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                Span<int> r = [|[|stackalloc|] int[2]|];
                r[0] = 1 +
                    2;
                r[1] = 3 +
                    4;

                """,
            FixedCode = """
                using System;

                Span<int> r =
                [
                    1 +
                        2,
                    3 +
                        4,
                ];

                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();
    }
}
