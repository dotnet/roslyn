// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
public sealed class UseCollectionExpressionForStackAllocTests
{
    [Fact]
    public Task TestNotInCSharp11()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotInCSharp11_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotInCSharp12_Net70()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNotInCSharp12_Net70_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestInCSharp12()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestInCSharp12_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestInCSharp12_Span()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestInCSharp12_Span_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleArraySizes()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMismatchedSize1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMismatchedSize2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNonConstSize()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestConstSize()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithPointer()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithPointer_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithVar()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithVar_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithSpanArgument()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithSpanArgument_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestEmpty()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCast()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestIdentifierCast()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestEmptyWithSize()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestEmpty_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_ZeroSize()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_NotEnoughFollowingStatements()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_WrongFollowingStatement()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_NotLocalStatementInitializer()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_ExpressionStatementNotAssignment()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_AssignmentNotElementAccess()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_ElementAccessNotToIdentifier()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_IdentifierNotEqualToVariableName()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_ArgumentNotConstant()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_ConstantArgumentNotCorrect1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_ConstantArgumentNotCorrect2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_OneElement()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_OneElement_MultipleFollowingStatements()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_TwoElement2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_TwoElement2_Constant()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_TwoElement2_SecondWrongIndex()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_TwoElement2_SecondNonConstant()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_TwoElement2_SecondWrongDestination()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTrivia1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTrivia2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine3()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine4()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine1_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine2_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine3_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestExistingInitializer_MultiLine4_Implicit()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_MultiLine1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNoInitializer_MultiLine2()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestGlobalStatement1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestGlobalStatement2()
        => new VerifyCS.Test
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
