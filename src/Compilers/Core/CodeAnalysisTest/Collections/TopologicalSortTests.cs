// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public class TopologicalSortTests
    {
        [Fact]
        public void Test01()
        {
            int[][] successors = new int[][]
            {
                /* 0 */ new int[] { }, // 0 has no successors
                /* 1 */ new int[] { },
                /* 2 */ new int[] { 3 },
                /* 3 */ new int[] { 1 },
                /* 4 */ new int[] { 0, 1, 0, 1 }, // tolerate duplicate edges
                /* 5 */ new int[] { 0, 2 },
            };

            Func<int, IEnumerable<int>> succF = x => successors[x];
            var sorted = TopologicalSort.IterativeSort<int>(new[] { 4, 5 }, i => succF(i).ToImmutableArray());
            AssertTopologicallySorted(sorted, succF, "Test01");
            Assert.Equal(6, sorted.Length);
            AssertEx.Equal(new[] { 4, 5, 2, 3, 1, 0 }, sorted);
        }

        [Fact]
        public void Test01b()
        {
            string[][] successors = new string[][]
            {
                /* 0 */ new string[] { }, // 0 has no successors
                /* 1 */ new string[] { },
                /* 2 */ new string[] { "3" },
                /* 3 */ new string[] { "1" },
                /* 4 */ new string[] { "0", "1" },
                /* 5 */ new string[] { "0", "2" },
            };

            Func<string, IEnumerable<string>> succF = x => successors[int.Parse(x)];
            var sorted = TopologicalSort.IterativeSort<string>(new[] { "4", "5" }, i => succF(i).ToImmutableArray());
            AssertTopologicallySorted(sorted, succF, "Test01");
            Assert.Equal(6, sorted.Length);
            AssertEx.Equal(new[] { "4", "5", "2", "3", "1", "0" }, sorted);
        }

        [Fact]
        public void Test02()
        {
            int[][] successors = new int[][]
            {
                /* 0 */ new int[] { },
                /* 1 */ new int[] { 2, 4 },
                /* 2 */ new int[] { },
                /* 3 */ new int[] { 2, 5 },
                /* 4 */ new int[] { 2, 3 },
                /* 5 */ new int[] { 2, },
                /* 6 */ new int[] { 2, 7 },
                /* 7 */ new int[] { }
            };

            Func<int, IEnumerable<int>> succF = x => successors[x];
            var sorted = TopologicalSort.IterativeSort<int>(new[] { 1, 6 }, i => succF(i).ToImmutableArray());
            AssertTopologicallySorted(sorted, succF, "Test02");
            Assert.Equal(7, sorted.Length);
            AssertEx.Equal(new[] { 1, 4, 3, 5, 6, 7, 2 }, sorted);
        }

        [Fact]
        public void TestCycle()
        {
            int[][] successors = new int[][]
            {
                /* 0 */ new int[] { },
                /* 1 */ new int[] { 2, 4 },
                /* 2 */ new int[] { },
                /* 3 */ new int[] { 2, 5 },
                /* 4 */ new int[] { 2, 3 },
                /* 5 */ new int[] { 2, 1 },
                /* 6 */ new int[] { 2, 7 },
                /* 7 */ new int[] { }
            };

            // 1 -> 4 -> 3 -> 5 -> 1
            Assert.Throws<ArgumentException>(() =>
            {
                var sorted = TopologicalSort.IterativeSort<int>(new[] { 1 }, x => successors[x].ToImmutableArray());
            });
        }

        [Theory]
        [InlineData(1984142830)]
        [InlineData(107329897)]
        [InlineData(136826316)]
        [InlineData(808774716)]
        [InlineData(729791148)]
        [InlineData(770911997)]
        [InlineData(1786285961)]
        [InlineData(321110113)]
        [InlineData(1686926633)]
        [InlineData(787934201)]
        [InlineData(745939035)]
        [InlineData(1075862430)]
        [InlineData(428872484)]
        [InlineData(489337268)]
        [InlineData(1976108951)]
        [InlineData(428397397)]
        [InlineData(1921108202)]
        [InlineData(926330127)]
        [InlineData(364136202)]
        [InlineData(1893034696)]
        public void TestRandom(int seed)
        {
            int numberOfNodes = 100;
            Random random = new Random(seed);

            // First, we produce a list of integers representing a possible (reversed)
            // topological sort of the graph we will construct
            var possibleSort = Enumerable.Range(0, numberOfNodes).ToArray();
            shuffle(possibleSort);

            // Then we produce a set of edges that is consistent with that possible topological sort
            int[][] successors = new int[numberOfNodes][];
            for (int i = numberOfNodes - 1; i >= 0; i--)
            {
                successors[possibleSort[i]] = randomSubset((int)Math.Sqrt(i), i);
            }

            // Perform a topological sort and check it.
            Func<int, IEnumerable<int>> succF = x => successors[x];
            var sorted = TopologicalSort.IterativeSort<int>(Enumerable.Range(0, numberOfNodes).ToArray(), i => succF(i).ToImmutableArray());
            Assert.Equal(numberOfNodes, sorted.Length);
            AssertTopologicallySorted(sorted, succF, $"TestRandom(seed: {seed})");

            // Now we modify the graph to add an edge from the last node to the first node, which
            // probably induces a cycle.  Since the graph is random, it is possible that this does
            // not induce a cycle. However, by the construction of the graph it is almost certain
            // that a cycle is induced. Nevertheless, to avoid flakiness in the tests, we do not
            // test with actual random graphs, but with graphs based on pseudo-random sequences using
            // random seeds hardcoded into the tests. That way we are testing on the same graphs each
            // time.
            successors[possibleSort[0]] = successors[possibleSort[0]].Concat(new int[] { possibleSort[numberOfNodes - 1] }).ToArray();

            Assert.Throws<ArgumentException>(() =>
            {
                TopologicalSort.IterativeSort<int>(Enumerable.Range(0, numberOfNodes).ToArray(), i => succF(i).ToImmutableArray());
            });

            // where
            void shuffle(int[] data)
            {
                int length = data.Length;
                for (int t = 0; t < length - 1; t++)
                {
                    int r = random.Next(t, length);
                    if (t != r)
                    {
                        var tmp = data[t];
                        data[t] = data[r];
                        data[r] = tmp;
                    }
                }
            }

            int[] randomSubset(int count, int limit)
            {
                // We don't worry about duplicate values. That's all part of the test,
                // as the topological sort should tolerate duplicate edges.
                var result = new int[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = possibleSort[random.Next(0, limit)];
                }

                return result;
            }
        }

        [Fact(Skip =
@"There is little additional coverage of this test over what is offered by TestRandom.
However, we are keeping it in the source as it may be useful to developers who change the topological sort algorithm in the future.")]
        public void TestLots()
        {
            Random random = new Random(1893034696);
            const int count = 100000;

            // Test lots more pseudo-random graphs using many different seeds.
            for (int i = 0; i < count; i++)
            {
                TestRandom(random.Next());
            }
        }

        private void AssertTopologicallySorted<T>(ImmutableArray<T> sorted, Func<T, IEnumerable<T>> successors, string message = null)
        {
            var seen = new HashSet<T>();
            for (int i = sorted.Length - 1; i >= 0; i--)
            {
                var n = sorted[i];
                foreach (var succ in successors(n))
                {
                    Assert.True(seen.Contains(succ), message);
                }

                seen.Add(n);
            }
        }
    }
}
