// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
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
public class UseCollectionExpressionForFluentTests
{
    [Fact]
    public async Task TestNotInCSharp11()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestInCSharp12_Net70()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestInCSharp12_Net80()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCast()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestIdentifierCast()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestOnlyOnOutermost()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestExplicitArrayCreation()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestExplicitArrayCreation_NoInitializer()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestArrayEmpty()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayEmpty()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayEmptyAndAdd()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayEmptyAndAddRange1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayEmptyAndAddRange2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayEmptyAndAddRange3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayCreate()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayCreateAndAdd()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayCreateAndAddRange1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayCreateAndAddRange2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayCreateAndAddRange3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestImmutableArrayCreateAndAddRange4()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotOnNonListEmpty()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotOnNonListCreate()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotOnNonListEmptyAndAdd()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestInitializerMultiLine1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestConcat1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotEndingWithConcat()
    {
        await new VerifyCS.Test
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
    }

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
    public async Task TestAppend1()
    {
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
    }

    [Fact]
    public async Task TestNotEndingWithAppend()
    {
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
                        IEnumerable<int> list = new int[] { 1, 2, 3 }.Append(x);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestObjectCreation1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestObjectCreation2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestObjectCreation3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestObjectCreation3_B()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestObjectCreation4()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestObjectCreation4_B()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotWithObjectCreationWithArg()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotWithNonListLikeObjectCreation()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotWithInnerCollectionInitializer()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotOnEmptyDictionary()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNotOnBuilder()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestEndsWithAdd1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestEndsWithAddRange1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestEndsWithAddRange2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestAsSpan1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestAsSpan2()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70101")]
    public async Task TestAsSpan3()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70101")]
    public async Task TestAsSpan4()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70101")]
    public async Task TestAsSpan5()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestStringToArray()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine1()
    {
        await new VerifyCS.Test
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
                        List<int> list =
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
    public async Task TestMultiLine2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine4()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine5()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine6()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine7()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestGlobalStatement1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestGlobalStatement2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNested1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNested2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection3_B()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection4()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection4_B()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection5()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReifyExistingCollection5_B()
    {
        await new VerifyCS.Test
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
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task TestWithDifferentNewLines(string endOfLine)
    {
        await new VerifyCS.Test
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
    }
}
