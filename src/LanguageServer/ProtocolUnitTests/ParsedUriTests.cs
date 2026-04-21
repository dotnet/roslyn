// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Tests for <see cref="ParsedUri"/>, ported from vscode-uri's uri.test.ts.
/// These tests verify that the C# implementation matches vscode-uri behavior exactly.
/// </summary>
public sealed class ParsedUriTests
{
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    [Theory]
    [MemberData(nameof(ParseCases))]
    public void Parse_Cases(UriCase testCase)
        => AssertCase(ParsedUri.Parse(testCase.Input), testCase);

    [Theory]
    [MemberData(nameof(FileCases))]
    public void File_Cases(UriCase testCase)
        => AssertCase(ParsedUri.File(testCase.Input), testCase);

    [ConditionalTheory(typeof(WindowsOnly))]
    [MemberData(nameof(FileWindowsCases))]
    public void File_Windows_Cases(UriCase testCase)
        => AssertCase(ParsedUri.File(testCase.Input), testCase);

    [ConditionalTheory(typeof(UnixLikeOnly))]
    [MemberData(nameof(FileUnixLikeCases))]
    public void File_UnixLike_Cases(UriCase testCase)
        => AssertCase(ParsedUri.File(testCase.Input), testCase);

    [Theory]
    [MemberData(nameof(FormattingCases))]
    public void Parse_Formatting_Cases(FormattingCase testCase)
    {
        var value = ParsedUri.Parse(testCase.UriString);

        Assert.Equal(testCase.ExpectedToString, value.ToString());

        if (testCase.ExpectedToStringSkipEncoding is not null)
        {
            Assert.Equal(testCase.ExpectedToStringSkipEncoding, value.ToString(skipEncoding: true));
        }
    }

    [Theory]
    [MemberData(nameof(EqualityCases))]
    public void Equality_Cases(EqualityCase testCase)
    {
        var left = ParsedUri.Parse(testCase.Left);
        var right = ParsedUri.Parse(testCase.Right);

        Assert.Equal(testCase.AreEqual, left.Equals(right));
        Assert.Equal(testCase.AreEqual, left == right);
        Assert.Equal(testCase.AreEqual, left != right == false);

        if (testCase.AreEqual)
        {
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
    }

    [Fact]
    public void FsPath_NoPathWhenAuthorityIsPath()
    {
        var value = ParsedUri.Parse("file://%2Fhome%2Fticino%2Fdesktop%2Fcpluscplus%2Ftest.cpp");
        Assert.Equal("/home/ticino/desktop/cpluscplus/test.cpp", value.Authority);
        Assert.Equal("/", value.Path);
        Assert.Equal(IsWindows ? @"\" : "/", value.FsPath);
    }

    [Theory]
    [InlineData("file:///c:/test/me", true)]
    [InlineData("FILE:///c:/test/me", true)]
    [InlineData("git:/test/me", false)]
    public void IsFile(string uriString, bool expected)
        => Assert.Equal(expected, ParsedUri.Parse(uriString).IsFile);

    [Fact]
    public void IsFile_UppercaseScheme_UsesFileFormattingAndFsPathRules()
    {
        var uri = ParsedUri.Parse("FILE://server/share/path");

        Assert.True(uri.IsFile);
        Assert.Equal("FILE://server/share/path", uri.ToString(skipEncoding: true));
        Assert.Equal(IsWindows ? @"\\server\share\path" : "//server/share/path", uri.FsPath);
    }

    [Fact]
    public void Http_ToString_SkipEncoding_RoundTripsParsedFileUri()
    {
        var value = ParsedUri.Parse("file://shares/pröjects/c%23/#l12");
        Assert.Equal("shares", value.Authority);
        Assert.Equal("/pröjects/c#/", value.Path);
        Assert.Equal("l12", value.Fragment);
        Assert.Equal("file://shares/pr%C3%B6jects/c%23/#l12", value.ToString());
        Assert.Equal("file://shares/pröjects/c%23/#l12", value.ToString(skipEncoding: true));

        var uri2 = ParsedUri.Parse(value.ToString(skipEncoding: true));
        var uri3 = ParsedUri.Parse(value.ToString());
        Assert.Equal(uri2.Authority, uri3.Authority);
        Assert.Equal(uri2.Path, uri3.Path);
        Assert.Equal(uri2.Query, uri3.Query);
        Assert.Equal(uri2.Fragment, uri3.Fragment);
    }

    [Fact]
    public void Parse_DisallowDoubleSlashPathWithNoAuthority()
        => Assert.Throws<UriFormatException>(() => ParsedUri.Parse("file:////shares/files/p.cs"));

    [Fact]
    public void DriveLetterPath_Regex()
    {
        var uri = ParsedUri.Parse("file:///_:/path");
        Assert.Equal(IsWindows ? @"\_:\path" : "/_:/path", uri.FsPath);
    }

    [Fact]
    public void File_NoPathIsUriCheck()
    {
        var value = ParsedUri.File("file://path/to/file");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/file://path/to/file", value.Path);
    }

    [Fact]
    public void RelativeFilePaths_ParsedFileUriRoundTrips()
    {
        var fileUri1 = ParsedUri.Parse("file:foo/bar");
        Assert.Equal("/foo/bar", fileUri1.Path);
        Assert.Equal("", fileUri1.Authority);

        var uriString = fileUri1.ToString();
        Assert.Equal("file:///foo/bar", uriString);

        var fileUri2 = ParsedUri.Parse(uriString);
        Assert.Equal("/foo/bar", fileUri2.Path);
        Assert.Equal("", fileUri2.Authority);
    }

    [Theory]
    [InlineData("http://localhost:3000/#/foo?bar=baz")]
    [InlineData("http://localhost:3000/foo?bar=baz")]
    public void HashQueryParamUrl(string input)
        => Assert.Equal(input, ParsedUri.Parse(input).ToString(skipEncoding: true));

    [Fact]
    public void PercentEncoding_MalformedUri_76506_RoundTripsThroughFileFactory()
    {
        var uri = ParsedUri.File("/foo/%A0.txt");
        var uri2 = ParsedUri.Parse(uri.ToString());
        Assert.Equal(uri.Scheme, uri2.Scheme);
        Assert.Equal(uri.Path, uri2.Path);

        uri = ParsedUri.File("/foo/%2e.txt");
        uri2 = ParsedUri.Parse(uri.ToString());
        Assert.Equal(uri.Scheme, uri2.Scheme);
        Assert.Equal(uri.Path, uri2.Path);
    }

    [Fact]
    public void Default_Struct()
    {
        var uri = default(ParsedUri);
        Assert.Null(uri.Scheme);
        Assert.Null(uri.Authority);
        Assert.Null(uri.Path);
        Assert.Null(uri.Query);
        Assert.Null(uri.Fragment);
    }

    public static TheoryData<UriCase> ParseCases => new()
    {
        new("http:/api/files/test.me?t=1234")
        {
            Scheme = "http",
            Path = "/api/files/test.me",
            Query = "t=1234",
            ExpectedToString = "http:/api/files/test.me?t%3D1234",
        },
        new("http://api/files/test.me?t=1234")
        {
            Scheme = "http",
            Authority = "api",
            Path = "/files/test.me",
            Query = "t=1234",
            ExpectedToString = "http://api/files/test.me?t%3D1234",
        },
        new("http:my/path")
        {
            Scheme = "http",
            Path = "/my/path",
            ExpectedToString = "http:/my/path",
        },
        new("file:///c:/test/me")
        {
            Scheme = "file",
            Path = "/c:/test/me",
            WindowsFsPath = @"c:\test\me",
            UnixFsPath = "c:/test/me",
            ExpectedToString = "file:///c%3A/test/me",
        },
        new("file:///c%3A/test/me")
        {
            // The encoded drive-letter colon (%3A) decodes to ':' in both Path and FsPath.
            Scheme = "file",
            Path = "/c:/test/me",
            WindowsFsPath = @"c:\test\me",
            UnixFsPath = "c:/test/me",
            ExpectedToString = "file:///c%3A/test/me",
        },
        new("file://shares/files/c%23/p.cs")
        {
            Scheme = "file",
            Authority = "shares",
            Path = "/files/c#/p.cs",
            WindowsFsPath = @"\\shares\files\c#\p.cs",
            UnixFsPath = "//shares/files/c#/p.cs",
            ExpectedToString = "file://shares/files/c%23/p.cs",
        },
        new("file:///c:/Source/Z%C3%BCrich%20or%20Zurich%20(%CB%88zj%CA%8A%C9%99r%C9%AAk,/Code/resources/app/plugins/c%23/plugin.json")
        {
            Scheme = "file",
            Path = "/c:/Source/Zürich or Zurich (ˈzjʊərɪk,/Code/resources/app/plugins/c#/plugin.json",
            WindowsFsPath = @"c:\Source\Zürich or Zurich (ˈzjʊərɪk,\Code\resources\app\plugins\c#\plugin.json",
            UnixFsPath = "c:/Source/Zürich or Zurich (ˈzjʊərɪk,/Code/resources/app/plugins/c#/plugin.json",
            ExpectedToString = "file:///c%3A/Source/Z%C3%BCrich%20or%20Zurich%20%28%CB%88zj%CA%8A%C9%99r%C9%AAk%2C/Code/resources/app/plugins/c%23/plugin.json",
            // Recreating from FsPath is lossy here; the decoded file-system form does not preserve this URI's exact canonical encoding.
            SkipFsPathRoundTrip = true,
        },
        new("file:///c:/test %25/path")
        {
            Scheme = "file",
            Path = "/c:/test %/path",
            WindowsFsPath = @"c:\test %\path",
            UnixFsPath = "c:/test %/path",
            ExpectedToString = "file:///c%3A/test%20%25/path",
        },
        new("inmemory:")
        {
            Scheme = "inmemory",
            ExpectedToString = "inmemory:",
        },
        new("foo:api/files/test")
        {
            Scheme = "foo",
            Path = "api/files/test",
            ExpectedToString = "foo:api/files/test",
        },
        new("file:?q")
        {
            Scheme = "file",
            Path = "/",
            Query = "q",
            WindowsFsPath = @"\",
            UnixFsPath = "/",
            ExpectedToString = "file:///?q",
            // FsPath only carries the file-system path, so it drops the query when rebuilt via ParsedUri.File(...).
            SkipFsPathRoundTrip = true,
        },
        new("file:#d")
        {
            Scheme = "file",
            Path = "/",
            Fragment = "d",
            WindowsFsPath = @"\",
            UnixFsPath = "/",
            ExpectedToString = "file:///#d",
            // FsPath only carries the file-system path, so it drops the fragment when rebuilt via ParsedUri.File(...).
            SkipFsPathRoundTrip = true,
        },
        new("f3ile:#d")
        {
            Scheme = "f3ile",
            Fragment = "d",
            ExpectedToString = "f3ile:#d",
        },
        new("foo+bar:path")
        {
            Scheme = "foo+bar",
            Path = "path",
            ExpectedToString = "foo+bar:path",
        },
        new("foo-bar:path")
        {
            Scheme = "foo-bar",
            Path = "path",
            ExpectedToString = "foo-bar:path",
        },
        new("foo.bar:path")
        {
            Scheme = "foo.bar",
            Path = "path",
            ExpectedToString = "foo.bar:path",
        },
        new("after:some/file/path")
        {
            Scheme = "after",
            Path = "some/file/path",
            ExpectedToString = "after:some/file/path",
        },
        new("scheme:/path")
        {
            Scheme = "scheme",
            Path = "/path",
            ExpectedToString = "scheme:/path",
        },
        new("scheme://authority")
        {
            Scheme = "scheme",
            Authority = "authority",
            ExpectedToString = "scheme://authority",
        },
        new("https:api/files/test.me?t=1234")
        {
            Scheme = "https",
            Path = "/api/files/test.me",
            Query = "t=1234",
            ExpectedToString = "https:/api/files/test.me?t%3D1234",
        },
        new("HTTP:/api/files/test.me?t=1234")
        {
            Scheme = "HTTP",
            Path = "/api/files/test.me",
            Query = "t=1234",
            ExpectedToString = "HTTP:/api/files/test.me?t%3D1234",
        },
        new("HTTPS:/api/files/test.me?t=1234")
        {
            Scheme = "HTTPS",
            Path = "/api/files/test.me",
            Query = "t=1234",
            ExpectedToString = "HTTPS:/api/files/test.me?t%3D1234",
        },
        new("boo:/api/files/test.me?t=1234")
        {
            Scheme = "boo",
            Path = "/api/files/test.me",
            Query = "t=1234",
            ExpectedToString = "boo:/api/files/test.me?t%3D1234",
        },
        new("http://a-test-site.com/?test=true")
        {
            Scheme = "http",
            Authority = "a-test-site.com",
            Path = "/",
            Query = "test=true",
            ExpectedToString = "http://a-test-site.com/?test%3Dtrue",
        },
        new("http://a-test-site.com/#test=true")
        {
            Scheme = "http",
            Authority = "a-test-site.com",
            Path = "/",
            Fragment = "test=true",
            ExpectedToString = "http://a-test-site.com/#test%3Dtrue",
        },
        new("https://go.microsoft.com/fwlink/?LinkId=518008")
        {
            Scheme = "https",
            Authority = "go.microsoft.com",
            Path = "/fwlink/",
            Query = "LinkId=518008",
            ExpectedToString = "https://go.microsoft.com/fwlink/?LinkId%3D518008",
        },
        new("https://go.microsoft.com/fwlink/?LinkId=518008&foö&ké¥=üü")
        {
            Scheme = "https",
            Authority = "go.microsoft.com",
            Path = "/fwlink/",
            Query = "LinkId=518008&foö&ké¥=üü",
            ExpectedToString = "https://go.microsoft.com/fwlink/?LinkId%3D518008%26fo%C3%B6%26k%C3%A9%C2%A5%3D%C3%BC%C3%BC",
        },
        new("git:/x:/%2525%EE%89%9B/%C2%89%EC%9E%BD?abc")
        {
            Scheme = "git",
            Path = "/x:/%25/잽",
            Query = "abc",
            ExpectedToString = "git:/x%3A/%2525%EE%89%9B/%C2%89%EC%9E%BD?abc",
        },
        new("git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")
        {
            Scheme = "git",
            Authority = "host",
            Path = "/%25/잽",
            ExpectedToString = "git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD",
        },
        new("xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")
        {
            Scheme = "xy",
            Authority = "host",
            Path = "/%25/잽",
            ExpectedToString = "xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD",
        },
        new("https://twitter.com/search?src=typd&q=%23tag")
        {
            Scheme = "https",
            Authority = "twitter.com",
            Path = "/search",
            Query = "src=typd&q=#tag",
        },
    };

    public static TheoryData<UriCase> FileCases => new()
    {
        new("c:/win/path")
        {
            Scheme = "file",
            Path = "/c:/win/path",
            ExpectedToString = "file:///c%3A/win/path",
            WindowsFsPath = @"c:\win\path",
            UnixFsPath = "c:/win/path",
        },
        new("C:/win/path")
        {
            Scheme = "file",
            Path = "/C:/win/path",
            ExpectedToString = "file:///c%3A/win/path",
            WindowsFsPath = @"c:\win\path",
            UnixFsPath = "c:/win/path",
        },
        new("c:/win/path/")
        {
            Scheme = "file",
            Path = "/c:/win/path/",
            ExpectedToString = "file:///c%3A/win/path/",
            WindowsFsPath = @"c:\win\path\",
            UnixFsPath = "c:/win/path/",
        },
        new("/c:/win/path")
        {
            Scheme = "file",
            Path = "/c:/win/path",
            ExpectedToString = "file:///c%3A/win/path",
            WindowsFsPath = @"c:\win\path",
            UnixFsPath = "c:/win/path",
        },
        new("/foo/bar")
        {
            Scheme = "file",
            Path = "/foo/bar",
            ExpectedToString = "file:///foo/bar",
            WindowsFsPath = @"\foo\bar",
            UnixFsPath = "/foo/bar",
        },
        new("foo/bar")
        {
            Scheme = "file",
            Path = "/foo/bar",
            ExpectedToString = "file:///foo/bar",
            WindowsFsPath = @"\foo\bar",
            UnixFsPath = "/foo/bar",
        },
        new("./foo/bar")
        {
            Scheme = "file",
            Path = "/./foo/bar",
            ExpectedToString = "file:///./foo/bar",
            WindowsFsPath = @"\.\foo\bar",
            UnixFsPath = "/./foo/bar",
        },
        new("a.file")
        {
            Scheme = "file",
            Path = "/a.file",
            ExpectedToString = "file:///a.file",
            WindowsFsPath = @"\a.file",
            UnixFsPath = "/a.file",
        },
        new("/Users/jrieken/Code/_samples/18500/Mödel + Other Thîngß/model.js")
        {
            Scheme = "file",
            Path = "/Users/jrieken/Code/_samples/18500/Mödel + Other Thîngß/model.js",
            ExpectedToString = "file:///Users/jrieken/Code/_samples/18500/M%C3%B6del%20%2B%20Other%20Th%C3%AEng%C3%9F/model.js",
            WindowsFsPath = @"\Users\jrieken\Code\_samples\18500\Mödel + Other Thîngß\model.js",
            UnixFsPath = "/Users/jrieken/Code/_samples/18500/Mödel + Other Thîngß/model.js",
        },
    };

    public static TheoryData<UriCase> FileWindowsCases => new()
    {
        new(@"c:\win\path")
        {
            Scheme = "file",
            Path = "/c:/win/path",
            ExpectedToString = "file:///c%3A/win/path",
            WindowsFsPath = @"c:\win\path",
        },
        new(@"c:\win/path")
        {
            Scheme = "file",
            Path = "/c:/win/path",
            ExpectedToString = "file:///c%3A/win/path",
            WindowsFsPath = @"c:\win\path",
        },
        new(@"\\shäres\path\c#\plugin.json")
        {
            Scheme = "file",
            Authority = "shäres",
            Path = "/path/c#/plugin.json",
            ExpectedToString = "file://sh%C3%A4res/path/c%23/plugin.json",
            WindowsFsPath = @"\\shäres\path\c#\plugin.json",
        },
        new(@"\\localhost\c$\GitDevelopment\express")
        {
            Scheme = "file",
            Authority = "localhost",
            Path = "/c$/GitDevelopment/express",
            ExpectedToString = "file://localhost/c%24/GitDevelopment/express",
            WindowsFsPath = @"\\localhost\c$\GitDevelopment\express",
        },
        new(@"c:\test with %\path")
        {
            Scheme = "file",
            Path = "/c:/test with %/path",
            ExpectedToString = "file:///c%3A/test%20with%20%25/path",
            WindowsFsPath = @"c:\test with %\path",
        },
        new(@"c:\test with %25\path")
        {
            Scheme = "file",
            Path = "/c:/test with %25/path",
            ExpectedToString = "file:///c%3A/test%20with%20%2525/path",
            WindowsFsPath = @"c:\test with %25\path",
        },
        new(@"c:\test with %25\c#code")
        {
            Scheme = "file",
            Path = "/c:/test with %25/c#code",
            ExpectedToString = "file:///c%3A/test%20with%20%2525/c%23code",
            WindowsFsPath = @"c:\test with %25\c#code",
        },
        new("C:/")
        {
            Scheme = "file",
            Path = "/C:/",
            ExpectedToString = "file:///c%3A/",
            WindowsFsPath = @"c:\",
        },
        new(@"C:\")
        {
            Scheme = "file",
            Path = "/C:/",
            ExpectedToString = "file:///c%3A/",
            WindowsFsPath = @"c:\",
        },
        new(@"C:\a\b")
        {
            Scheme = "file",
            Path = "/C:/a/b",
            ExpectedToString = "file:///c%3A/a/b",
            WindowsFsPath = @"c:\a\b",
        },
        new(@"C:\a\\b")
        {
            Scheme = "file",
            Path = "/C:/a//b",
            ExpectedToString = "file:///c%3A/a//b",
            WindowsFsPath = @"c:\a\\b",
        },
        new("C:\\%25\ue25b/a\\b")
        {
            Scheme = "file",
            Path = "/C:/%25/a/b",
            ExpectedToString = "file:///c%3A/%2525%EE%89%9B/a/b",
            WindowsFsPath = @"c:\%25\a\b",
        },
        new("C:\\%25\ue25b/a\\\\b")
        {
            Scheme = "file",
            Path = "/C:/%25/a//b",
            ExpectedToString = "file:///c%3A/%2525%EE%89%9B/a//b",
            WindowsFsPath = @"c:\%25\a\\b",
        },
        new("C:\\\u0089\uC7BD")
        {
            Scheme = "file",
            Path = "/C:/잽",
            ExpectedToString = "file:///c%3A/%C2%89%EC%9E%BD",
            WindowsFsPath = @"c:\잽",
        },
        new("/\\server\ue25b\\%25\ue25b\\b")
        {
            Scheme = "file",
            Authority = "server",
            Path = "/%25/b",
            ExpectedToString = "file://server%EE%89%9B/%2525%EE%89%9B/b",
            WindowsFsPath = @"\\server\%25\b",
        },
        new("\\\\server\ue25b\\%25\ue25b\\b")
        {
            Scheme = "file",
            Authority = "server",
            Path = "/%25/b",
            ExpectedToString = "file://server%EE%89%9B/%2525%EE%89%9B/b",
            WindowsFsPath = @"\\server\%25\b",
        },
        new("C:\\ !$&'()+,-;=@[]_~#")
        {
            Scheme = "file",
            Path = "/C:/ !$&'()+,-;=@[]_~#",
            ExpectedToString = "file:///c%3A/%20%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23",
            WindowsFsPath = @"c:\ !$&'()+,-;=@[]_~#",
        },
        new("C:\\ !$&'()+,-;=@[]_~#\ue25b")
        {
            Scheme = "file",
            Path = "/C:/ !$&'()+,-;=@[]_~#",
            ExpectedToString = "file:///c%3A/%20%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23%EE%89%9B",
            WindowsFsPath = @"c:\ !$&'()+,-;=@[]_~#",
        },
        new("C:\\\u0073\u0323\u0307")
        {
            Scheme = "file",
            Path = "/C:/ṩ",
            ExpectedToString = "file:///c%3A/s%CC%A3%CC%87",
            WindowsFsPath = @"c:\ṩ",
        },
        new("A:/\\\u200e//")
        {
            Scheme = "file",
            Path = "/A://‎//",
            ExpectedToString = "file:///a%3A//%E2%80%8E//",
            WindowsFsPath = @"a:\\‎\\",
        },
        new("B:\\/\u200e")
        {
            Scheme = "file",
            Path = "/B://‎",
            ExpectedToString = "file:///b%3A//%E2%80%8E",
            WindowsFsPath = @"b:\\‎",
        },
        new("C:/\\\\-Ā\r")
        {
            Scheme = "file",
            Path = "/C:///-Ā\r",
            ExpectedToString = "file:///c%3A///-%C4%80%0D",
            WindowsFsPath = @"c:\\\-Ā" + "\r",
        },
        new("D:\\\\\\\\\u200e")
        {
            Scheme = "file",
            Path = "/D:////‎",
            ExpectedToString = "file:///d%3A////%E2%80%8E",
            WindowsFsPath = @"d:\\\\‎",
        },
        new(@"\\shares")
        {
            Scheme = "file",
            Authority = "shares",
            Path = "/",
            ExpectedToString = "file://shares/",
            WindowsFsPath = @"\",
            // FsPath for a UNC share root collapses to "\", so rebuilding loses the original share authority.
            SkipFsPathRoundTrip = true,
        },
        new(@"\\shares\")
        {
            Scheme = "file",
            Authority = "shares",
            Path = "/",
            ExpectedToString = "file://shares/",
            WindowsFsPath = @"\",
            // FsPath for a UNC share root collapses to "\", so rebuilding loses the original share authority.
            SkipFsPathRoundTrip = true,
        },
    };

    public static TheoryData<UriCase> FileUnixLikeCases => new()
    {
        new(@"c:\win\path")
        {
            Scheme = "file",
            Path = @"/c:\win\path",
            ExpectedToString = "file:///c%3A%5Cwin%5Cpath",
            UnixFsPath = @"c:\win\path",
        },
        new(@"c:\win/path")
        {
            Scheme = "file",
            Path = @"/c:\win/path",
            ExpectedToString = "file:///c%3A%5Cwin/path",
            UnixFsPath = @"c:\win/path",
        },
        new("/")
        {
            Scheme = "file",
            Path = "/",
            ExpectedToString = "file:///",
            UnixFsPath = "/",
        },
        new("/u")
        {
            Scheme = "file",
            Path = "/u",
            ExpectedToString = "file:///u",
            UnixFsPath = "/u",
        },
        new("/unix/path")
        {
            Scheme = "file",
            Path = "/unix/path",
            ExpectedToString = "file:///unix/path",
            UnixFsPath = "/unix/path",
        },
        new("/%25\ue25b/\u0089\uC7BD")
        {
            Scheme = "file",
            Path = "/%25/잽",
            ExpectedToString = "file:///%2525%EE%89%9B/%C2%89%EC%9E%BD",
            UnixFsPath = "/%25/잽",
        },
        new("/!$&'()+,-;=@[]_~#")
        {
            Scheme = "file",
            Path = "/!$&'()+,-;=@[]_~#",
            ExpectedToString = "file:///%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23",
            UnixFsPath = "/!$&'()+,-;=@[]_~#",
        },
        new("/!$&'()+,-;=@[]_~#")
        {
            Scheme = "file",
            Path = "/!$&'()+,-;=@[]_~#",
            ExpectedToString = "file:///%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23%EE%89%9B",
            UnixFsPath = "/!$&'()+,-;=@[]_~#",
        },
        new("/\\\u200e//")
        {
            Scheme = "file",
            Path = "/\\‎//",
            ExpectedToString = "file:///%5C%E2%80%8E//",
            UnixFsPath = "/\\‎//",
        },
        new("/\\\\-Ā\r")
        {
            Scheme = "file",
            Path = "/\\\\-Ā\r",
            ExpectedToString = "file:///%5C%5C-%C4%80%0D",
            UnixFsPath = "/\\\\-Ā\r",
        },
    };

    public static TheoryData<FormattingCase> FormattingCases => new()
    {
        new("http:/api/files/test.me?t=1234", "http:/api/files/test.me?t%3D1234", "http:/api/files/test.me?t=1234"),
        new("http:my/path", "http:/my/path", "http:/my/path"),
        new("https:api/files/test.me?t=1234", "https:/api/files/test.me?t%3D1234", "https:/api/files/test.me?t=1234"),
        new("HTTP:/api/files/test.me?t=1234", "HTTP:/api/files/test.me?t%3D1234", "HTTP:/api/files/test.me?t=1234"),
        new("HTTPS:/api/files/test.me?t=1234", "HTTPS:/api/files/test.me?t%3D1234", "HTTPS:/api/files/test.me?t=1234"),
        new("http://a-test-site.com/?test=true", "http://a-test-site.com/?test%3Dtrue", "http://a-test-site.com/?test=true"),
        new("http://a-test-site.com/#test=true", "http://a-test-site.com/#test%3Dtrue", "http://a-test-site.com/#test=true"),
        new("https://go.microsoft.com/fwlink/?LinkId=518008", "https://go.microsoft.com/fwlink/?LinkId%3D518008", "https://go.microsoft.com/fwlink/?LinkId=518008"),
        new("https://go.microsoft.com/fwlink/?LinkId=518008&foö&ké¥=üü", "https://go.microsoft.com/fwlink/?LinkId%3D518008%26fo%C3%B6%26k%C3%A9%C2%A5%3D%C3%BC%C3%BC", "https://go.microsoft.com/fwlink/?LinkId=518008&foö&ké¥=üü"),
        new("https://twitter.com/search?src=typd&q=%23tag", "https://twitter.com/search?src%3Dtypd%26q%3D%23tag", "https://twitter.com/search?src=typd&q=%23tag"),
        new("http://www.MSFT.com/my/path", "http://www.msft.com/my/path"),
        new("untitled:c:/Users/jrieken/Code/abc.txt", "untitled:c%3A/Users/jrieken/Code/abc.txt"),
        new("untitled:C:/Users/jrieken/Code/abc.txt", "untitled:c%3A/Users/jrieken/Code/abc.txt"),
        new("http://localhost:8080/far", "http://localhost:8080/far"),
        new("http://löcalhost:8080/far", "http://l%C3%B6calhost:8080/far"),
        new("http://foo:bar@localhost/far", "http://foo:bar@localhost/far"),
        new("http://foo@localhost/far", "http://foo@localhost/far"),
        new("http://foo:bAr@localhost:8080/far", "http://foo:bAr@localhost:8080/far"),
        new("http://foo@localhost:8080/far", "http://foo@localhost:8080/far"),
        new("http://föö:bör@löcalhost:8080/far", "http://f%C3%B6%C3%B6:b%C3%B6r@l%C3%B6calhost:8080/far"),
        new("stuff:?qüery", "stuff:?q%C3%BCery"),
        new("file://sh%c3%a4res/path", "file://sh%C3%A4res/path"),
        new("file://some/%.txt", "file://some/%25.txt"),
        new("file://some/%A0.txt", "file://some/%25A0.txt"),
    };

    public static TheoryData<EqualityCase> EqualityCases => new()
    {
        new("file:///c:/test/me", "file:///c:/test/me", true),
        new("file:///c:/test/me", "file:///c:/test/other", false),
        // Under vscode-uri semantics, backslash vs forward-slash URIs parse to different components
        // (backslashes are only normalized in ParsedUri.File(), not in Parse()), so these are not equal.
        new("file://c:\\valid", "file:///c:/valid", false),
        // File URIs with UNC/DOS paths use case-insensitive comparison, matching System.Uri's IsUncOrDosPath behavior.
        new("file://c:\\valid", "file://c:\\valid", true),
        new("file://c:\\valid", "file://c:\\VALID", true),
        new("file://c:\\valid", "file://c:\\valid2", false),
        new("file:///C:/test/me", "file:///c:/test/me", true),
        new("FILE:///c:/Path/File.txt", "file:///c:/path/file.txt", true),
        new("file://server/Share/Path", "file://server/share/path", true),
        new("file:///c:/test%20file.txt", "file:///c:/test file.txt", true),
        // Hash character %23: unencoded # starts a fragment, so these parse to different components (not equal).
        new("file:///c:/code/c%23/project", "file:///c:/code/c#/project", false),
        // Unicode characters encoded as UTF-8 percent sequences.
        new("file:///c:/Source/Z%C3%BCrich", "file:///c:/Source/Zürich", true),
        // Mixed encoding in authority (UNC path).
        new("file://sh%C3%A4res/path", "file://shäres/path", true),
        new("http://example.com/path?q%3D1", "http://example.com/path?q=1", true),
        // Encoded vs unencoded in fragments.
        new("http://example.com/path#frag%20ment", "http://example.com/path#frag ment", true),
        // Double-encoded percent: %25 decodes to %, which is different from a literal %.
        new("file:///c:/test%2520file.txt", "file:///c:/test%20file.txt", false),
        // Encoded colon in path (vscode-uri encodes drive letter colons).
        new("file:///c%3A/test", "file:///c:/test", true),
        // Encoded slash %2F in query (not a path separator in query context).
        new("http://example.com/path?url%3Dhttp%3A%2F%2Fother", "http://example.com/path?url=http://other", true),
        new("git:/blah", "git:/Blah", false),
        new("file:///usr/Home", "file:///usr/home", false),
    };

    private static void AssertCase(ParsedUri value, UriCase testCase)
    {
        AssertUri(
            value,
            testCase.Scheme,
            testCase.Authority,
            testCase.Path,
            testCase.Query,
            testCase.Fragment);

        AssertFsPath(value, testCase.WindowsFsPath, testCase.UnixFsPath);

        if (testCase.ExpectedToString is not null)
        {
            Assert.Equal(testCase.ExpectedToString, value.ToString());
        }

        AssertParseRoundTrip(value);

        if (!testCase.SkipFsPathRoundTrip && string.Equals(testCase.Scheme, "file", StringComparison.OrdinalIgnoreCase))
        {
            AssertFsPathRoundTrip(value);
        }
    }

    private static void AssertUri(ParsedUri value, string expectedScheme, string expectedAuthority, string expectedPath, string expectedQuery, string expectedFragment)
    {
        Assert.Equal(expectedScheme, value.Scheme);
        Assert.Equal(expectedAuthority, value.Authority);
        Assert.Equal(expectedPath, value.Path);
        Assert.Equal(expectedQuery, value.Query);
        Assert.Equal(expectedFragment, value.Fragment);
    }

    private static void AssertFsPath(ParsedUri value, string? windowsExpected, string? unixExpected)
    {
        if (windowsExpected is null && unixExpected is null)
        {
            return;
        }

        Assert.Equal(IsWindows ? windowsExpected : unixExpected, value.FsPath);
    }

    private static void AssertParseRoundTrip(ParsedUri value)
    {
        var clone = ParsedUri.Parse(value.ToString());

        Assert.Equal(value.Scheme, clone.Scheme);
        Assert.Equal(value.Authority, clone.Authority);

        if (value.IsFile && value.Path.Length >= 3
            && value.Path[0] == '/'
            && ParsedUri.IsLetter(value.Path[1])
            && value.Path[2] == ':')
        {
            // vscode-uri stores the original case of the drive letter in Path, but normalizes it in toString or fsPath.
            // We'll allow the drive letter to be canonicalized to lowercase in the round-trip, but other parts of the path should round-trip exactly.
            var expectedPath = "/" + char.ToLowerInvariant(value.Path[1]) + value.Path.Substring(2);
            Assert.Equal(expectedPath, clone.Path);
        }
        else
        {
            Assert.Equal(value.Path, clone.Path);
        }
        Assert.Equal(value.Query, clone.Query);
        Assert.Equal(value.Fragment, clone.Fragment);
        Assert.Equal(value.FsPath, clone.FsPath);
        Assert.Equal(value.ToString(), clone.ToString());
    }

    private static void AssertFsPathRoundTrip(ParsedUri value)
    {
        var clone = ParsedUri.File(value.FsPath);
        Assert.Equal(value.FsPath, clone.FsPath);
        Assert.Equal(value.ToString(), clone.ToString());
    }

    public sealed class UriCase : IXunitSerializable
    {
        public UriCase()
        {
            Input = "";
        }

        public UriCase(string input)
        {
            Input = input;
        }

        public string Input { get; set; }
        public string Scheme { get; set; } = "";
        public string Authority { get; set; } = "";
        public string Path { get; set; } = "";
        public string Query { get; set; } = "";
        public string Fragment { get; set; } = "";
        public string? WindowsFsPath { get; set; }
        public string? UnixFsPath { get; set; }
        public string? ExpectedToString { get; set; }
        public bool SkipFsPathRoundTrip { get; set; }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Input), Input);
            info.AddValue(nameof(Scheme), Scheme);
            info.AddValue(nameof(Authority), Authority);
            info.AddValue(nameof(Path), Path);
            info.AddValue(nameof(Query), Query);
            info.AddValue(nameof(Fragment), Fragment);
            info.AddValue(nameof(WindowsFsPath), WindowsFsPath);
            info.AddValue(nameof(UnixFsPath), UnixFsPath);
            info.AddValue(nameof(ExpectedToString), ExpectedToString);
            info.AddValue(nameof(SkipFsPathRoundTrip), SkipFsPathRoundTrip);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Input = info.GetValue<string>(nameof(Input));
            Scheme = info.GetValue<string>(nameof(Scheme));
            Authority = info.GetValue<string>(nameof(Authority));
            Path = info.GetValue<string>(nameof(Path));
            Query = info.GetValue<string>(nameof(Query));
            Fragment = info.GetValue<string>(nameof(Fragment));
            WindowsFsPath = info.GetValue<string?>(nameof(WindowsFsPath));
            UnixFsPath = info.GetValue<string?>(nameof(UnixFsPath));
            ExpectedToString = info.GetValue<string?>(nameof(ExpectedToString));
            SkipFsPathRoundTrip = info.GetValue<bool>(nameof(SkipFsPathRoundTrip));
        }
    }

    public sealed class FormattingCase : IXunitSerializable
    {
        public FormattingCase()
        {
            UriString = "";
            ExpectedToString = "";
        }

        public FormattingCase(string uriString, string expectedToString, string? expectedToStringSkipEncoding = null)
        {
            UriString = uriString;
            ExpectedToString = expectedToString;
            ExpectedToStringSkipEncoding = expectedToStringSkipEncoding;
        }

        public string UriString { get; set; }
        public string ExpectedToString { get; set; }
        public string? ExpectedToStringSkipEncoding { get; set; }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(UriString), UriString);
            info.AddValue(nameof(ExpectedToString), ExpectedToString);
            info.AddValue(nameof(ExpectedToStringSkipEncoding), ExpectedToStringSkipEncoding);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            UriString = info.GetValue<string>(nameof(UriString));
            ExpectedToString = info.GetValue<string>(nameof(ExpectedToString));
            ExpectedToStringSkipEncoding = info.GetValue<string?>(nameof(ExpectedToStringSkipEncoding));
        }
    }

    public sealed class EqualityCase : IXunitSerializable
    {
        public EqualityCase()
        {
            Left = "";
            Right = "";
        }

        public EqualityCase(string left, string right, bool areEqual)
        {
            Left = left;
            Right = right;
            AreEqual = areEqual;
        }

        public string Left { get; set; }
        public string Right { get; set; }
        public bool AreEqual { get; set; }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(Left), Left);
            info.AddValue(nameof(Right), Right);
            info.AddValue(nameof(AreEqual), AreEqual);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Left = info.GetValue<string>(nameof(Left));
            Right = info.GetValue<string>(nameof(Right));
            AreEqual = info.GetValue<bool>(nameof(AreEqual));
        }
    }
}
