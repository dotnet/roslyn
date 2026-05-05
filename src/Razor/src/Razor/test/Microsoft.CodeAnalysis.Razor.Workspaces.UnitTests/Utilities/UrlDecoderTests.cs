// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

public class UrlDecoderTests(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    public static IEnumerable<object[]> UrlDecodeData =>
        [
            ["http://127.0.0.1:8080/appDir/page.aspx?foo=bar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%61r"],
            ["http://127.0.0.1:8080/appDir/page.aspx?foo=b%ar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%%61r"],
            ["http://127.0.0.1:8080/app%Dir/page.aspx?foo=b%ar", "http://127.0.0.1:8080/app%Dir/page.aspx?foo=b%%61r"],
            ["http://127.0.0.1:8080/app%%Dir/page.aspx?foo=b%%r", "http://127.0.0.1:8080/app%%Dir/page.aspx?foo=b%%r"],
            ["http://127.0.0.1:8080/appDir/page.aspx?foo=ba%r", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%61%r"],
            ["http://127.0.0.1:8080/appDir/page.aspx?foo=bar baz", "http://127.0.0.1:8080/appDir/page.aspx?foo=bar+baz"],
            ["http://example.net/\U00010000", "http://example.net/\U00010000"],
            ["http://example.net/\uD800", "http://example.net/\uD800"],
            ["http://example.net/\uD800a", "http://example.net/\uD800a"],
            // The "Baz" portion of "http://example.net/Baz" has been double-encoded - one iteration of UrlDecode() should produce a once-encoded string.
            ["http://example.net/%6A%6B%6C", "http://example.net/%256A%256B%256C"],
            // The second iteration should return the original string
            ["http://example.net/jkl", "http://example.net/%6A%6B%6C"],
            // This example uses lowercase hex characters
            ["http://example.net/jkl", "http://example.net/%6a%6b%6c"]
        ];

    [Theory]
    [MemberData(nameof(UrlDecodeData))]
    public void Decode(string decoded, string encoded)
    {
        Assert.Equal(UrlDecoder.Decode(encoded), decoded);
    }
}
