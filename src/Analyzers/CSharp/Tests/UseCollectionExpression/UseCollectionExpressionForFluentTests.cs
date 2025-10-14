// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForFluentCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public sealed class UseCollectionExpressionForFluentTests
{
    [Fact]
    public Task TestNotInCSharp11()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestInCSharp12_Net70()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestInCSharp12_Net80()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
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
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        var list = (List<int>)new[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        var list = (List<int>)[1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestIdentifierCast()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using X = System.Collections.Generic.List<int>;
                
                class C
                {
                    void M()
                    {
                        var list = (X)new[] { 1, 2, 3 }.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestOnlyOnOutermost()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new[] { 1, 2, 3 }.ToArray().[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestExplicitArrayCreation()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new int[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestExplicitArrayCreation_NoInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new int[3].ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestArrayEmpty()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = Array.Empty<int>().[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayEmpty()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayEmptyAndAdd()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.Add(1).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayEmptyAndAddRange1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.AddRange(1, 2).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayEmptyAndAddRange2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = ImmutableArray<int>.Empty.AddRange(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = [.. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayEmptyAndAddRange3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        List<int> list = ImmutableArray<int>.Empty.AddRange(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        List<int> list = [.. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayCreate()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray.Create(1, 2, 3).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayCreateAndAdd()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray.Create(1, 2, 3).Add(4).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3, 4];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayCreateAndAddRange1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray.Create(1, 2, 3).AddRange(4, 5).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2, 3, 4, 5];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayCreateAndAddRange2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = ImmutableArray.Create(1, 2, 3).AddRange(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = [1, 2, 3, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayCreateAndAddRange3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        List<int> list = ImmutableArray.Create(1, 2, 3).AddRange(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        List<int> list = [1, 2, 3, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestImmutableArrayCreateAndAddRange4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        List<int> list = ImmutableArray.Create(1, 2, 3).AddRange(4, 5).AddRange(6, 7).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        List<int> list = [1, 2, 3, 4, 5, 6, 7];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotOnNonListEmpty()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableHashSet<int>.Empty.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotOnNonListCreate()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableHashSet.Create(1, 1, 1).ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotOnNonListEmptyAndAdd()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableHashSet<int>.Empty.Add(1).ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestInitializerMultiLine1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = new[]
                        {
                            1, 2, 3
                        }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list =
                        [
                            1, 2, 3
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestConcat1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = new int[] { 1, 2, 3 }.Concat(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = [1, 2, 3, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotEndingWithConcat()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        IEnumerable<int> list = new int[] { 1, 2, 3 }.Concat(x);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public async Task TestSingleValueConcat()
    {
        var singleArgConcat = """

            static class Extensions
            {
                public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, TSource second) => null;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int x)
                    {
                        List<int> list = new int[] { 1, 2, 3 }.Concat(x).[|ToList|]();
                    }
                }
                """ + singleArgConcat,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int x)
                    {
                        List<int> list = [1, 2, 3, x];
                    }
                }
                """ + singleArgConcat,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public Task TestAppend1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int x)
                    {
                        List<int> list = new int[] { 1, 2, 3 }.Append(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int x)
                    {
                        List<int> list = [1, 2, 3, x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotEndingWithAppend()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int x)
                    {
                        IEnumerable<int> list = new int[] { 1, 2, 3 }.Append(x);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestObjectCreation1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>().Concat(x).[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = [.. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestObjectCreation2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>() { 1, 2, 3 }.Concat(x).[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = [1, 2, 3, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestObjectCreation3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>()
                        {
                            1, 2, 3
                        }.Concat(x).[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array =
                        [
                            1, 2, 3, .. x
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestObjectCreation3_B()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>()
                        {
                            1,
                            2,
                            3
                        }.Concat(x).[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array =
                        [
                            1,
                            2,
                            3,
                            .. x,
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestObjectCreation4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>
                        {
                            1, 2, 3
                        }.Concat(x).[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array =
                        [
                            1, 2, 3, .. x
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestObjectCreation4_B()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>
                        {
                            1,
                            2,
                            3
                        }.Concat(x).[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array =
                        [
                            1,
                            2,
                            3,
                            .. x,
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotWithObjectCreationWithArg()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>(1)
                        {
                            1, 2, 3
                        }.Concat(x).ToArray();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotWithNonListLikeObjectCreation()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new HashSet<int>()
                        {
                            1, 2, 3
                        }.Concat(x).ToArray();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotWithInnerCollectionInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        KeyValuePair<int, int>[] array = new Dictionary<int, int>()
                        {
                            { 1, 2 }
                        }.ToArray();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotOnEmptyDictionary()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        KeyValuePair<int, int>[] array = new Dictionary<int, int>()
                        {
                        }.ToArray();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotOnBuilder()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        builder.Add(0);
                        ImmutableArray<int> result = builder.ToImmutable();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestEndsWithAdd1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ImmutableArray<int> list = ImmutableArray.Create(1, 2, 3).[|Add|](4);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ImmutableArray<int> list = [1, 2, 3, 4];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestEndsWithAddRange1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ImmutableArray<int> list = ImmutableArray.Create(1, 2, 3).[|AddRange|](4);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ImmutableArray<int> list = [1, 2, 3, 4];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestEndsWithAddRange2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ImmutableArray<int> list = ImmutableArray.Create(1, 2, 3).[|AddRange|](4, 5);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ImmutableArray<int> list = [1, 2, 3, 4, 5];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestAsSpan1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        Span<int> span = new[] { 1, 2, 3 }.[|AsSpan|]();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        Span<int> span = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestAsSpan2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ReadOnlySpan<int> span = new[] { 1, 2, 3 }.[|AsSpan|]();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(IEnumerable<int> x)
                    {
                        ReadOnlySpan<int> span = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70101")]
    public Task TestAsSpan3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        ReadOnlySpan<char> span = "".AsSpan();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70101")]
    public Task TestAsSpan4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        char[] buffer = new char[4];
                        Span<char> span1 = buffer.AsSpan();
                        span1[0] = 'a';
                        Console.WriteLine(buffer[0]);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70101")]
    public Task TestAsSpan5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        ReadOnlySpan<char> span = "now is the time".ToArray().[|AsSpan|]();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        ReadOnlySpan<char> span = [.. "now is the time"];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestStringToArray()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        char[] span = "now is the time".[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        char[] span = [.. "now is the time"];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMultiLine1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.Add(1 +
                            2).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1 +
                            2];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMultiLine2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.AddRange(1 +
                            2, 3 +
                            4).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list =
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
    public Task TestMultiLine3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.Add(1 +
                            2).Add(3 +
                            4).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list =
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
    public Task TestMultiLine4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> list = ImmutableArray<int>.Empty.AddRange(1 +
                        2, 3 +
                        4).[|ToList|]();
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> list =
                    [
                        1 +
                            2,
                        3 +
                            4,
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMultiLine5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> list = ImmutableArray<int>.Empty.Add(1 +
                        2).Add(3 +
                        4).[|ToList|]();
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    List<int> list =
                    [
                        1 +
                            2,
                        3 +
                            4,
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMultiLine6()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.AddRange(
                            1 +
                                2,
                            3 +
                                4).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list =
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
    public Task TestMultiLine7()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.AddRange(
                            1 +
                                2, 3 +
                            4).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list =
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
    public Task TestTrivia1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = /*leading*/ new[] { 1, 2, 3 }.[|ToList|]() /*trailing*/;
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<int> list = /*leading*/ [1, 2, 3] /*trailing*/;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestGlobalStatement1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                List<int> list = new[] { 1, 2, 3 }.[|ToList|]();
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                List<int> list = [1, 2, 3];
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
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                List<int> list = ImmutableArray<int>.Empty.Add(1 +
                    2).Add(3 +
                    4).[|ToList|]();
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                List<int> list =
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

    [Fact]
    public Task TestNested1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<List<int>> list = new[] { new[] { 1, 2, 3 }.ToList() }.[|ToList|]();
                    }
                }
                """,
            FixedState =
            {
                Sources =
                { """
                    using System.Linq;
                    using System.Collections.Generic;
                
                    class C
                    {
                        void M()
                        {
                            List<List<int>> list = [new[] { 1, 2, 3 }.ToList()];
                        }
                    }
                    """,
                },
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(8,51): info IDE0305: Collection initialization can be simplified
                    VerifyCS.Diagnostic().WithSpan(8, 51, 8, 57).WithSpan(8, 33, 8, 59).WithSeverity(DiagnosticSeverity.Info),
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();

    [Fact]
    public Task TestNested2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<List<int>> list = [new[] { 1, 2, 3 }.[|ToList|]()];
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        List<List<int>> list = [[1, 2, 3]];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = x.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = [.. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] x, int[] y)
                    {
                        List<int> list = x.Concat(y).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] x, int[] y)
                    {
                        List<int> list = [.. x, .. y];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y)
                    {
                        ImmutableArray<int> list = x.Add(y);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection3_B()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y)
                    {
                        List<int> list = x.Add(y).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y)
                    {
                        List<int> list = [.. x, y];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int[] y)
                    {
                        ImmutableArray<int> list = x.AddRange(y);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection4_B()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int[] y)
                    {
                        List<int> list = x.AddRange(y).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int[] y)
                    {
                        List<int> list = [.. x, .. y];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y, int z)
                    {
                        ImmutableArray<int> list = x.AddRange(y, z);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReifyExistingCollection5_B()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y, int z)
                    {
                        List<int> list = x.AddRange(y, z).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y, int z)
                    {
                        List<int> list = [.. x, y, z];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public Task TestWithDifferentNewLines(string endOfLine)
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y, int z)
                    {
                        List<int> list = x.AddRange(y, z).[|ToList|]();
                    }
                }
                """.ReplaceLineEndings(endOfLine),
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(ImmutableArray<int> x, int y, int z)
                    {
                        List<int> list = [.. x, y, z];
                    }
                }
                """.ReplaceLineEndings(endOfLine),
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70833")]
    public Task SpreadFormatting1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;

                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = Enum.GetValues<T>()
                        .Order()
                        .[|ToImmutableArray|]();
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;
                
                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = [.. Enum.GetValues<T>().Order()];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70833")]
    public Task SpreadFormatting2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;

                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = Enum.GetValues<T>().
                        Order().
                        [|ToImmutableArray|]();
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;
                
                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = [.. Enum.GetValues<T>().Order()];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70833")]
    public Task SpreadFormatting3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;

                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = Enum.GetValues<T>() // comment
                        .Order()
                        .[|ToImmutableArray|]();
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;
                
                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = [.. Enum.GetValues<T>() // comment
                        .Order()];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70833")]
    public Task SpreadFormatting4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;

                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = Enum.GetValues<T>(). // comment
                        Order().
                        [|ToImmutableArray|]();
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;
                
                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = [.. Enum.GetValues<T>(). // comment
                        Order()];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70833")]
    public Task SpreadFormatting5()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;

                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = Enum.GetValues<T>()
                        .Order()
                        .Order()
                        .[|ToImmutableArray|]();
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;
                
                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = [.. Enum.GetValues<T>()
                        .Order()
                        .Order()];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70833")]
    public Task SpreadFormatting6()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;

                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = Enum.GetValues<T>().
                        Order().
                        Order().
                        [|ToImmutableArray|]();
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;
                using System.Linq;
                
                static class EnumValueCache<T>
                    where T : struct, Enum
                {
                    public static readonly ImmutableArray<T> SortedValues = [.. Enum.GetValues<T>().
                        Order().
                        Order()];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestInLambda()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<List<int>> f = () => new int[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<List<int>> f = () => [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestNotInLambda1()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        var f = () => new int[] { 1, 2, 3 }.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestNotInExpressionTree()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Expression<Func<List<int>>> f = () => new int[] { 1, 2, 3 }.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71145")]
    public Task TestNotInParallelEnumerable1()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        const bool shouldParallelize = false;

                        IEnumerable<int> sequence = null!;

                        var result = sequence.AsParallel().ToArray();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71145")]
    public Task TestNotInParallelEnumerable2()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        const bool shouldParallelize = false;

                        IEnumerable<int> sequence = null!;

                        var result = shouldParallelize
                            ? sequence.AsParallel().ToArray()
                            : sequence.[|ToArray|]();
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        const bool shouldParallelize = false;

                        IEnumerable<int> sequence = null!;

                        var result = shouldParallelize
                            ? sequence.AsParallel().ToArray()
                            : [.. sequence];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public Task TestInterfaceOn()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        IEnumerable<int> list = new[] { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        IEnumerable<int> list = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public Task TestInterfaceOff()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        IEnumerable<int> list = new[] { 1, 2, 3 }.ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71607")]
    public Task TestAddRangeOfCollectionExpression1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = ImmutableArray<int>.Empty.{|CS0121:AddRange|}([1, 2]).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> list = [1, 2];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71607")]
    public Task TestAddRangeOfCollectionExpression2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = ImmutableArray<int>.Empty.{|CS0121:AddRange|}([1, .. x]).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> list = [1, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71788")]
    public Task NotOnBannedType()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                public class C
                {
                    public int[] M(object o)
                    {
                        if (o is IIListProvider<int> pa) return pa.ToArray();
                        return null;
                    }
                }

                interface IIListProvider<T> : IEnumerable<T>
                {
                    T[] ToArray();
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestObjectCreation_PreservesTrivia()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array = new List<int>() //Some comment
                        {
                            1, 2, 3
                        }.Concat(x).[|ToArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        int[] array =
                        //Some comment
                        [
                            1, 2, 3, .. x
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = new List<int>(values).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [.. values];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = new List<int>(values) { 1, 2, 3 }.[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [.. values, 1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values, int[] x)
                    {
                        List<int> list = new List<int>(values).Concat(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values, int[] x)
                    {
                        List<int> list = [.. values, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values, int[] x)
                    {
                        List<int> list = new List<int>(values) { 1, 2, 3 }.Concat(x).[|ToList|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values, int[] x)
                    {
                        List<int> list = [.. values, 1, 2, 3, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75870")]
    public Task TestSelectToImmutableArray()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    ImmutableArray<string> GetFormattedNumbers(ImmutableArray<int> numbers)
                    {
                        return numbers.Select(n => $"Number: {n}").[|ToImmutableArray|]();
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    ImmutableArray<string> GetFormattedNumbers(ImmutableArray<int> numbers)
                    {
                        return [.. numbers.Select(n => $"Number: {n}")];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
}
