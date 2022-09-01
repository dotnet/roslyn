// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.InternalUtilities
{
    public class AdditionalTextComparerTests
    {
        [Fact]
        public void Compares_Equal_When_Path_And_Content_Are_The_Same()
        {
            AdditionalText text1 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "abc");
            AdditionalText text2 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "abc");

            Assert.NotEqual(text1, text2);
            Assert.Equal(text1, text2, AdditionalTextComparer.Instance);
        }

        [Fact]
        public void HashCodes_Match_When_Path_And_Content_Are_The_Same()
        {
            AdditionalText text1 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "abc");
            AdditionalText text2 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "abc");

            var hash1 = text1.GetHashCode();
            var hash2 = text2.GetHashCode();
            Assert.NotEqual(hash1, hash2);

            var comparerHash1 = AdditionalTextComparer.Instance.GetHashCode(text1);
            var comparerHash2 = AdditionalTextComparer.Instance.GetHashCode(text2);
            Assert.Equal(comparerHash1, comparerHash2);
        }

        [Fact]
        public void Not_Equal_When_Paths_Differ()
        {
            AdditionalText text1 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "abc");
            AdditionalText text2 = new InMemoryAdditionalText(@"c:\d\e\f.txt", "abc");

            Assert.NotEqual(text1, text2, AdditionalTextComparer.Instance);

            var comparerHash1 = AdditionalTextComparer.Instance.GetHashCode(text1);
            var comparerHash2 = AdditionalTextComparer.Instance.GetHashCode(text2);
            Assert.NotEqual(comparerHash1, comparerHash2);
        }

        [Fact]
        public void Not_Equal_When_Contents_Differ()
        {
            AdditionalText text1 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "abc");
            AdditionalText text2 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "def");

            Assert.NotEqual(text1, text2, AdditionalTextComparer.Instance);

            var comparerHash1 = AdditionalTextComparer.Instance.GetHashCode(text1);
            var comparerHash2 = AdditionalTextComparer.Instance.GetHashCode(text2);
            Assert.NotEqual(comparerHash1, comparerHash2);
        }

        [Fact]
        public void Comparison_With_Different_Path_Casing()
        {
            AdditionalText text1 = new InMemoryAdditionalText(@"c:\a\b\c.txt", "abc");
            AdditionalText text2 = new InMemoryAdditionalText(@"c:\a\B\c.txt", "abc");

            // hash codes are path case insensitive
            var comparerHash1 = AdditionalTextComparer.Instance.GetHashCode(text1);
            var comparerHash2 = AdditionalTextComparer.Instance.GetHashCode(text2);
            Assert.Equal(comparerHash1, comparerHash2);

            // but we correctly compare
            if (PathUtilities.IsUnixLikePlatform)
            {
                Assert.NotEqual(text1, text2, AdditionalTextComparer.Instance);
            }
            else
            {
                Assert.Equal(text1, text2, AdditionalTextComparer.Instance);
            }
        }
    }
}
