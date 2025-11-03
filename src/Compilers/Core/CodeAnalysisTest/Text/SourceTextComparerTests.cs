// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public class SourceTextComparerTests
    {
        [Fact]
        public void EqualityContract_SameContent_DifferentEncodings()
        {
            // Test case from the issue: two SourceText instances with same content but different encodings
            // should be equal and have the same hash code when using content-based comparison
            const string content = "Hello, World!";
            var utf8 = Encoding.UTF8;
            var unicode = Encoding.Unicode;

            var text1 = SourceText.From(content, utf8);
            var text2 = SourceText.From(content, unicode);

            var comparer = SourceTextComparer.Instance;

            // Both should be considered equal by the comparer (content-based)
            Assert.True(comparer.Equals(text1, text2));

            // They must have the same hash code to satisfy IEqualityComparer contract
            Assert.Equal(comparer.GetHashCode(text1), comparer.GetHashCode(text2));
        }

        [Fact]
        public void EqualityContract_SameContent_WithAndWithoutBOM()
        {
            // Test the case described in the issue comments:
            // Create source texts from byte arrays where one has a BOM and the other doesn't
            const string content = "Test content";

            var utf8WithBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var utf8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            // Create byte arrays with and without BOM
            var preamble = utf8WithBOM.GetPreamble();
            var contentBytes = utf8WithBOM.GetBytes(content);
            var bytesWithBOM = new byte[preamble.Length + contentBytes.Length];
            preamble.CopyTo(bytesWithBOM, 0);
            contentBytes.CopyTo(bytesWithBOM, preamble.Length);

            var bytesNoBOM = utf8NoBOM.GetBytes(content);

            // Both pass Encoding.UTF8 as the encoding parameter
            var textWithBOM = SourceText.From(bytesWithBOM, bytesWithBOM.Length, Encoding.UTF8);
            var textNoBOM = SourceText.From(bytesNoBOM, bytesNoBOM.Length, Encoding.UTF8);

            var comparer = SourceTextComparer.Instance;

            // They should be equal based on content
            Assert.True(comparer.Equals(textWithBOM, textNoBOM));

            // They must have the same hash code
            Assert.Equal(comparer.GetHashCode(textWithBOM), comparer.GetHashCode(textNoBOM));
        }

        [Fact]
        public void EqualityContract_DifferentContent()
        {
            var text1 = SourceText.From("content1");
            var text2 = SourceText.From("content2");

            var comparer = SourceTextComparer.Instance;

            // Different content should not be equal
            Assert.False(comparer.Equals(text1, text2));

            // Hash codes may or may not be equal (no requirement for unequal objects)
            // but we just verify they don't throw
            _ = comparer.GetHashCode(text1);
            _ = comparer.GetHashCode(text2);
        }

        [Fact]
        public void EqualityContract_NullHandling()
        {
            var text = SourceText.From("content");
            var comparer = SourceTextComparer.Instance;

            // null equals null
            Assert.True(comparer.Equals(null, null));

            // null does not equal non-null
            Assert.False(comparer.Equals(null, text));
            Assert.False(comparer.Equals(text, null));

            // null hash code
            Assert.Equal(0, comparer.GetHashCode(null));
        }

        [Fact]
        public void EqualityContract_SameContentFromStream()
        {
            const string content = "Stream content";
            var bytes = Encoding.UTF8.GetBytes(content);

            // Create from stream
            var stream1 = new MemoryStream(bytes);
            var text1 = SourceText.From(stream1, Encoding.UTF8);

            // Create from string
            var text2 = SourceText.From(content, Encoding.UTF8);

            var comparer = SourceTextComparer.Instance;

            // Should be equal based on content
            Assert.True(comparer.Equals(text1, text2));

            // Must have same hash code
            Assert.Equal(comparer.GetHashCode(text1), comparer.GetHashCode(text2));
        }

        [Fact]
        public void ContentEquals_MatchesComparerEquals()
        {
            // Verify that SourceTextComparer.Equals aligns with SourceText.ContentEquals
            const string content = "Test";

            var text1 = SourceText.From(content, Encoding.UTF8);
            var text2 = SourceText.From(content, Encoding.Unicode);

            var comparer = SourceTextComparer.Instance;

            // SourceTextComparer.Equals should match SourceText.ContentEquals
            Assert.Equal(text1.ContentEquals(text2), comparer.Equals(text1, text2));
        }

        [Fact]
        public void GetContentHash_MatchesComparerHashCode()
        {
            // Verify that SourceTextComparer uses content-based hashing
            const string content = "Hash test content";

            var text1 = SourceText.From(content, Encoding.UTF8);
            var text2 = SourceText.From(content, Encoding.Unicode);

            // Both should have the same content hash
            Assert.True(text1.GetContentHash().SequenceEqual(text2.GetContentHash()));

            var comparer = SourceTextComparer.Instance;

            // And therefore the same hash code from the comparer
            Assert.Equal(comparer.GetHashCode(text1), comparer.GetHashCode(text2));
        }
    }
}
