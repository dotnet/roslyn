using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.UtilityTest
{
    public class BKTreeTests
    {
        [Fact]
        public void Test1()
        {
            string[] testValues = { "cook", "book", "books", "cake", "what", "water", "Cape", "Boon", "Cook", "Cart" };
            var tree = BKTree.Create(testValues);

            foreach (var value in testValues)
            {
                // With a threshold of 0, we should only find exactly the item we're searching for.
                var items = tree.Find(value, threshold: 0);
                Assert.Single(tree.Find(value, threshold: 0), value.ToLower());
            }

            foreach (var value in testValues)
            {
                // With a threshold of 1, we should always at least find the item we're looking for.
                // But we may also find additional items along with it.
                var items = tree.Find(value, threshold: 1);
                Assert.Contains(value.ToLower(), items);
            }

            var results1 = tree.Find("wat", threshold: 1);
            Assert.Single(results1, "what");

            var results2 = tree.Find("wat", threshold: 2);
            Assert.True(results2.SetEquals(Expected("cart", "what", "water")));

            var results3 = tree.Find("caqe", threshold: 1);
            Assert.True(results3.SetEquals(Expected("cake", "cape")));
        }

        [Fact]
        public void Test2()
        {
            string[] testValues = { "Leeds", "York", "Bristol", "Leicester", "Hull", "Durham" };
            var tree = BKTree.Create(testValues);

            var results1 = tree.Find("hill");
            Assert.True(results1.SetEquals(Expected("hull")));

            var results2 = tree.Find("liecester");
            Assert.True(results2.SetEquals(Expected("leicester")));

            var results3 = tree.Find("leicestre");
            Assert.True(results3.SetEquals(Expected("leicester")));

            var results4 = tree.Find("lecester");
            Assert.True(results4.SetEquals(Expected("leicester")));
        }

        private IEnumerable<string> Expected(params string[] values)
        {
            return values;
        }
    }
}
