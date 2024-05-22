// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorAdapter;

public class SpanExtensionsTest
{
    [Fact]
    public void ConvertToTextSpan()
    {
        static void del(int start, int length)
        {
            var span = new Span(start, length);
            var textSpan = span.ToTextSpan();
            Assert.Equal(start, textSpan.Start);
            Assert.Equal(length, textSpan.Length);
        }

        del(0, 5);
        del(10, 15);
    }
}
