// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

public class ExtractToCssCodeActionResolverTest
{
    [Fact]
    public void GetLastLineNumberAndLength()
    {
        var input = """
            body {
                background-color: red;
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        ExtractToCssCodeActionResolver.TestAccessor.GetLastLineNumberAndLength(stream, bufferSize: 4096, out var lastLineNumber, out var lastLineLength);

        Assert.Equal(2, lastLineNumber);
        Assert.Equal(1, lastLineLength);
    }

    [Fact]
    public void GetLastLineNumberAndLength_LF()
    {
        var input = """
            body {
                background-color: red;
            }
            """.Replace(Environment.NewLine, "\n");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        ExtractToCssCodeActionResolver.TestAccessor.GetLastLineNumberAndLength(stream, bufferSize: 4096, out var lastLineNumber, out var lastLineLength);

        Assert.Equal(2, lastLineNumber);
        Assert.Equal(1, lastLineLength);
    }

    [Fact]
    public void GetLastLineNumberAndLength_LineExceedsBuffer()
    {
        var input = """
            body {
                background-color: red;
            }

            .a-really-long-class-name-that-exceeds-the-buffer-size {
                color: blue;
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        ExtractToCssCodeActionResolver.TestAccessor.GetLastLineNumberAndLength(stream, bufferSize: 4, out var lastLineNumber, out var lastLineLength);

        Assert.Equal(6, lastLineNumber);
        Assert.Equal(1, lastLineLength);
    }

    [Fact]
    public void GetLastLineNumberAndLength_LastLineExceedsBuffer()
    {
        var input = """
            body {
                background-color: red;
                a-really-long-class-name-that-exceeds-the-buffer-size: yes }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        ExtractToCssCodeActionResolver.TestAccessor.GetLastLineNumberAndLength(stream, bufferSize: 4, out var lastLineNumber, out var lastLineLength);

        Assert.Equal(2, lastLineNumber);
        Assert.Equal(64, lastLineLength);
    }

    [Fact]
    public void GetLastLineNumberAndLength_Empty()
    {
        var input = "";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        ExtractToCssCodeActionResolver.TestAccessor.GetLastLineNumberAndLength(stream, bufferSize: 4096, out var lastLineNumber, out var lastLineLength);

        Assert.Equal(0, lastLineNumber);
        Assert.Equal(0, lastLineLength);
    }

    [Fact]
    public void GetLastLineNumberAndLength_BlankLines()
    {
        var input = """


            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));

        ExtractToCssCodeActionResolver.TestAccessor.GetLastLineNumberAndLength(stream, bufferSize: 4096, out var lastLineNumber, out var lastLineLength);

        Assert.Equal(1, lastLineNumber);
        Assert.Equal(0, lastLineLength);
    }
}
