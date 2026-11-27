// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ImmutableArrayBoxingAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class ImmutableArrayBoxingAnalyzerTests
    {
        private const string ExtensionsSource = """
            namespace System.Collections.Immutable
            {
                public struct ImmutableArray<T> : IReadOnlyList<T>
                {
                    public static readonly ImmutableArray<T> Empty = default;

                    public T this[int index] => default;
                    public int Count => 0;

                    public IEnumerator<T> GetEnumerator() => null;
                    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
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
        public Task TestReadOnlyListExtensions_CSharpAsync()
            => new VerifyCS.Test
            {
                TestCode = $$"""
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
                    """,
            }.RunAsync();

        [Fact]
        public Task TestEnumerableExtensions_CSharpAsync()
            => new VerifyCS.Test
            {
                TestCode = $$"""
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
                    """,
            }.RunAsync();
    }
}
