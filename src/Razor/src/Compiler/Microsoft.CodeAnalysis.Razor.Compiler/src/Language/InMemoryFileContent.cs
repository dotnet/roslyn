// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.CodeAnalysis.Razor.Language;

internal sealed class InMemoryFileContent(string content)
{
    private byte[]? s_contentBytes;

    private byte[] ContentBytes
        => s_contentBytes ?? InterlockedOperations.Initialize(ref s_contentBytes, ComputeContentBytes(content));

    private static byte[] ComputeContentBytes(string content)
    {
        var preamble = Encoding.UTF8.GetPreamble();
        var contentBytes = Encoding.UTF8.GetBytes(content);

        var bytes = new byte[preamble.Length + contentBytes.Length];
        preamble.CopyTo(bytes, 0);
        contentBytes.CopyTo(bytes, preamble.Length);

        return bytes;
    }

    public MemoryStream CreateStream()
        => new(ContentBytes, writable: false);
}
