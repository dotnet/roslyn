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

            // This way, the StringBuilder should have at least two chunks of length 6 at the beginning. We want to test writes
            // across their boundaries.
            stringBuilder.Append("chunk1");
            stringBuilder.Append("chunk2");
            stringBuilder.Append("chunk3");

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
                Assert.Throws<ArgumentOutOfRangeException>("span", () => text.Write(textWriter, new TextSpan(0, text.Length + 1)));

            void assertWriteEquals(int start, int length, string expected)
            {
                using var textWriter = new StringWriter();
                text.Write(textWriter, new TextSpan(start, length));
                Assert.Equal(expected, textWriter.ToString());
            }
        }
    }
}
