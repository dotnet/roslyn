// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Razor.Diagnostics.Analyzers.Test;

using VerifyCS = CSharpAnalyzerVerifier<ImmutableArrayBoxingAnalyzer>;

public class ImmutableArrayBoxingAnalyzerTests
{
    private const string ExtensionsSource = """
        namespace System.Collections.Immutable
        {
            public struct ImmutableArray<T> : IReadOnlyList<T>
            {
                public static readonly ImmutableArray<T> Empty = default;

                public T this[int index] => default!;
                public int Count => 0;

                public IEnumerator<T> GetEnumerator() => null!;
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null!;
            }
        }

        namespace System.Collections.Generic
        {
            using System.Collections.Immutable;

            internal static class ReadOnlyListExtensions
            {
                public static bool Any<T>(this IReadOnlyList<T> list) => false;
            }

            internal static class EnumerableExtensions
            {
                public static ImmutableArray<T> OrderAsArray<T>(this IEnumerable<T> sequence) => default;
            }
        }
        """;

    [Fact]
    public async Task TestReadOnlyListExtensions_CSharpAsync()
    {
        var code = $$"""
            using System.Collections.Generic;

            class C
            {
                void Method()
                {
                    System.Collections.Immutable.ImmutableArray<int> array = System.Collections.Immutable.ImmutableArray<int>.Empty;
                    _ = [|array|].Any();
                }
            }

            {{ExtensionsSource}}
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEnumerableExtensions_CSharpAsync()
    {
        var code = $$"""
            using System.Collections.Generic;

            class C
            {
                void Method()
                {
                    System.Collections.Immutable.ImmutableArray<int> array = System.Collections.Immutable.ImmutableArray<int>.Empty;
                    _ = [|array|].OrderAsArray();
                }
            }

            {{ExtensionsSource}}
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
        }.RunAsync();
    }
}
