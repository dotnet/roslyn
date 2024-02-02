// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public sealed class StringBuilderTextTest
    {
        [Fact]
        public void Write()
        {
            var stringBuilder = new StringBuilder(6);

            // This way, the StringBuilder should have at least two chunks of length 6 at the beginning.
            stringBuilder.Append("chunk1");
            stringBuilder.Append("chunk2");
            stringBuilder.Append("chunk3");

#if NETCOREAPP
            // We are relying on implementation details of StringBuilder here because the method itself enumerates individual
            // chunks and we want to verify that it behaves well across chunk boundaries. If this ever fails, this test should
            // be updated based on the new chunk sizes so that it still tests behavior across chunks.
            int index = 0;
            foreach (var chunk in stringBuilder.GetChunks())
            {
                if (index <= 1)
                    Assert.Equal(6, chunk.Length);

                ++index;
            }

            Assert.Equal(3, index);
#endif

            var text = new StringBuilderText(stringBuilder, encodingOpt: null, SourceHashAlgorithm.Sha1);

            assertWriteEquals(0, text.Length, "chunk1chunk2chunk3");
            assertWriteEquals(0, 2, "ch");
            assertWriteEquals(0, 6, "chunk1");
            assertWriteEquals(0, 12, "chunk1chunk2");
            assertWriteEquals(5, 1, "1");
            assertWriteEquals(5, 2, "1c");
            assertWriteEquals(5, 9, "1chunk2ch");
            assertWriteEquals(6, 6, "chunk2");
            assertWriteEquals(7, 0, "");

            using (var textWriter = new StringWriter())
            {
                Assert.Throws<ArgumentOutOfRangeException>("span", () => text.Write(textWriter, new TextSpan(0, text.Length + 1)));
                Assert.Throws<ArgumentOutOfRangeException>("span", () => text.Write(textWriter, new TextSpan(text.Length + 1, 0)));
            }

            void assertWriteEquals(int start, int length, string expected)
            {
                using var textWriter = new StringWriter();
                text.Write(textWriter, new TextSpan(start, length));
                Assert.Equal(expected, textWriter.ToString());
            }
        }
    }
}
