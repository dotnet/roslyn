// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Text
{
    public class SubTextTests
    {
        [Theory]
        [InlineData("abcdefghijkl")]
        [InlineData(["\r\r\r\r\r\r\r\r\r\r\r\r"])]
        [InlineData(["\n\n\n\n\n\n\n\n\n\n\n\n"])]
        [InlineData(["\r\n\r\n\r\n\r\n\r\n\r\n"])]
        [InlineData(["\n\r\n\r\n\r\n\r\n\r\n\r"])]
        [InlineData(["a\r\nb\r\nc\r\nd\r\n"])]
        [InlineData(["\ra\n\rb\n\rc\n\rd\n"])]
        [InlineData(["\na\r\nb\r\nc\r\nd\r"])]
        [InlineData(["ab\r\ncd\r\nef\r\n"])]
        [InlineData(["ab\r\r\ncd\r\r\nef"])]
        [InlineData(["ab\n\n\rcd\n\n\ref"])]
        [InlineData(["ab\u0085cdef\u2028ijkl\u2029op"])]
        [InlineData(["\u0085\u2028\u2029\u0085\u2028\u2029\u0085\u2028\u2029\u0085\u2028\u2029"])]
        public void SubTextTestAllPossibleSubstrings(string contents)
        {
            var fullStringText = SourceText.From(contents);
            for (var start = 0; start < contents.Length; start++)
            {
                for (var end = start + 1; end <= contents.Length; end++)
                {
                    var stringText = SourceText.From(contents[start..end]);
                    var subText = new SubText(fullStringText, new TextSpan(start, length: end - start));

                    Assert.Equal(stringText.Length, subText.Length);
                    for (var i = 0; i < stringText.Length; i++)
                    {
                        Assert.Equal(stringText.Lines.IndexOf(i), subText.Lines.IndexOf(i));
                    }

                    Assert.Equal(stringText.Lines.Count, subText.Lines.Count);
                    for (var i = 0; i < stringText.Lines.Count; i++)
                    {
                        Assert.Equal(stringText.Lines[i].ToString(), subText.Lines[i].ToString());
                        Assert.Equal(stringText.Lines[i].EndIncludingLineBreak, subText.Lines[i].EndIncludingLineBreak);
                    }
                }
            }
        }
    }
}
