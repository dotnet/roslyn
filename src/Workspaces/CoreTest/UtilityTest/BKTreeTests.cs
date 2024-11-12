// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest
{
    public class BKTreeTests
    {
        private static ImmutableArray<string> Find(BKTree tree, string value, int? threshold)
        {
            using var results = TemporaryArray<string>.Empty;
            tree.Find(ref results.AsRef(), value, threshold);
            return results.ToImmutableAndClear();
        }

        [Fact]
        public void SimpleTests()
        {
            string[] testValues = ["cook", "book", "books", "cake", "what", "water", "Cape", "Boon", "Cook", "Cart"];
            var tree = BKTree.Create(testValues);

            var results1 = Find(tree, "wat", threshold: 1);
            Assert.Single(results1, "what");

            var results2 = Find(tree, "wat", threshold: 2);
            Assert.True(results2.SetEquals(Expected("cart", "what", "water")));

            var results3 = Find(tree, "caqe", threshold: 1);
            Assert.True(results3.SetEquals(Expected("cake", "cape")));
        }

        [Fact]
        public void PermutationTests()
        {
            string[] testValues = ["cook", "book", "books", "cake", "what", "water", "Cape", "Boon", "Cook", "Cart"];
            TestTreeInvariants(testValues);
        }

        private static void TestTreeInvariants(string[] testValues)
        {
            var tree = BKTree.Create(testValues);

            foreach (var value in testValues)
            {
                // With a threshold of 0, we should only find exactly the item we're searching for.
                Assert.Single(Find(tree, value, threshold: 0), value.ToLower());
            }

            foreach (var value in testValues)
            {
                // With a threshold of 1, we should always at least find the item we're looking for.
                // But we may also find additional items along with it.
                var items = Find(tree, value, threshold: 1);
                Assert.Contains(value.ToLower(), items);

                // We better not be finding all items.
                Assert.NotEqual(testValues.Length, items.Length);
            }

            foreach (var value in testValues)
            {
                // If we delete each individual character in each search string, we should still
                // find the value in the tree.
                for (var i = 0; i < value.Length; i++)
                {
                    var items = Find(tree, Delete(value, i), threshold: null);
                    Assert.Contains(value.ToLower(), items);

                    // We better not be finding all items.
                    Assert.NotEqual(testValues.Length, items.Length);
                }
            }

            foreach (var value in testValues)
            {
                // If we add a random character at any location in a string, we should still 
                // be able to find it.
                for (var i = 0; i <= value.Length; i++)
                {
                    var items = Find(tree, Insert(value, i, 'Z'), threshold: null);
                    Assert.Contains(value.ToLower(), items);

                    // We better not be finding all items.
                    Assert.NotEqual(testValues.Length, items.Length);
                }
            }

            foreach (var value in testValues)
            {
                // If we transpose any characters in a string, we should still 
                // be able to find it.
                for (var i = 0; i < value.Length - 1; i++)
                {
                    var items = Find(tree, Transpose(value, i), threshold: null);
                    Assert.Contains(value.ToLower(), items);
                }
            }
        }

        private static string Transpose(string value, int i)
            => value[..i] + value[i + 1] + value[i] + value[(i + 2)..];

        private static string Insert(string value, int i, char v)
            => value[..i] + v + value[i..];

        private static string Delete(string value, int i)
            => value[..i] + value[(i + 1)..];

        [Fact]
        public void Test2()
        {
            string[] testValues = ["Leeds", "York", "Bristol", "Leicester", "Hull", "Durham"];
            var tree = BKTree.Create(testValues);

            var results = Find(tree, "hill", threshold: null);
            Assert.True(results.SetEquals(Expected("hull")));

            results = Find(tree, "liecester", threshold: null);
            Assert.True(results.SetEquals(Expected("leicester")));

            results = Find(tree, "leicestre", threshold: null);
            Assert.True(results.SetEquals(Expected("leicester")));

            results = Find(tree, "lecester", threshold: null);
            Assert.True(results.SetEquals(Expected("leicester")));
        }

        [Fact]
        public void TestSpillover()
        {
#pragma warning disable format // https://github.com/dotnet/roslyn/issues/70711 tracks removing this suppression.
            string[] testValues = [
                /*root:*/ "Four",
                /*d=1*/ "Fou", "For", "Fur", "Our", "FourA", "FouAr", "FoAur", "FAour", "AFour", "Tour",
                /*d=2*/ "Fo", "Fu", "Fr", "or", "ur", "ou", "FourAb", "FouAbr", "FoAbur", "FAbour", "AbFour", "oFour", "Fuor", "Foru", "ours",
                /*d=3*/ "F", "o", "u", "r", "Fob", "Fox", "bur", "urn", "hur", "foraa", "found"
            ];
#pragma warning restore format

            TestTreeInvariants(testValues);
        }

        [Fact]
        public void Top1000()
            => TestTreeInvariants(EditDistanceTests.Top1000);

        private static IEnumerable<string> Expected(params string[] values)
            => values;
    }
}
