// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public sealed class SourceTextStreamTests
    {
        private static readonly Encoding s_utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// In the case the destination buffer is of insufficient length to store the reading of a single 
        /// character we will throw.  Returning 0 is not correct here as that indicates end of stream
        /// not insufficient space in destination buffer.
        /// </summary>
        [Fact]
        public void MinimumLength()
        {
            var sourceText = SourceText.From("hello world", s_utf8NoBom);
            using (var stream = new SourceTextStream(sourceText))
            {
                var buffer = new byte[100];
                var max = s_utf8NoBom.GetMaxByteCount(charCount: 1);
                for (int i = 0; i < max; i++)
                {
                    var local = i;
                    Assert.Throws<ArgumentException>(() => stream.Read(buffer, 0, local));
                }
            }
        }

        /// <summary>
        /// In the case there is insufficient number of bytes to store the next character the read should
        /// complete with the already read data vs. throwing.
        /// </summary>
        [Fact]
        public void Issue1197()
        {
            var baseText = new string('a', SourceTextStream.BufferSize - 1);
            var text = string.Format("{0}{1}", baseText, '\u2019');
            var encoding = s_utf8NoBom;
            var sourceText = SourceText.From(text, encoding);
            using (var stream = new SourceTextStream(sourceText))
            {
                var buffer = new byte[baseText.Length + 1];
                Assert.Equal(baseText.Length, stream.Read(buffer, 0, buffer.Length));
                Assert.True(buffer.Take(baseText.Length).SequenceEqual(encoding.GetBytes(baseText)));

                Assert.Equal(3, stream.Read(buffer, 0, buffer.Length));
                Assert.True(buffer.Take(3).SequenceEqual(encoding.GetBytes(new[] { '\u2019' })));
            }
        }
    }
}
