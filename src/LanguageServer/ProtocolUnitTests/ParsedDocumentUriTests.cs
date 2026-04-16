// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Tests for <see cref="ParsedDocumentUri"/>, ported from vscode-uri's uri.test.ts.
/// These tests verify that the C# implementation matches vscode-uri behavior exactly.
/// </summary>
public sealed class ParsedDocumentUriTests
{
    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    #region file#toString

    [Fact]
    public void File_ToString()
    {
        Assert.Equal("file:///c%3A/win/path", ParsedDocumentUri.File("c:/win/path").ToString());
        Assert.Equal("file:///c%3A/win/path", ParsedDocumentUri.File("C:/win/path").ToString());
        Assert.Equal("file:///c%3A/win/path/", ParsedDocumentUri.File("c:/win/path/").ToString());
        Assert.Equal("file:///c%3A/win/path", ParsedDocumentUri.File("/c:/win/path").ToString());
    }

    #endregion

    #region URI.file (win-special)

    [ConditionalFact(typeof(WindowsOnly))]
    public void File_WinSpecial_Windows()
    {
        Assert.Equal("file:///c%3A/win/path", ParsedDocumentUri.File(@"c:\win\path").ToString());
        Assert.Equal("file:///c%3A/win/path", ParsedDocumentUri.File(@"c:\win/path").ToString());
    }

    [ConditionalFact(typeof(UnixLikeOnly))]
    public void File_WinSpecial_Unix()
    {
        Assert.Equal("file:///c%3A%5Cwin%5Cpath", ParsedDocumentUri.File(@"c:\win\path").ToString());
        Assert.Equal("file:///c%3A%5Cwin/path", ParsedDocumentUri.File(@"c:\win/path").ToString());
    }

    #endregion

    #region file#fsPath (win-special)

    [ConditionalFact(typeof(WindowsOnly))]
    public void File_FsPath_Windows()
    {
        Assert.Equal(@"c:\win\path", ParsedDocumentUri.File(@"c:\win\path").FsPath);
        Assert.Equal(@"c:\win\path", ParsedDocumentUri.File(@"c:\win/path").FsPath);
        Assert.Equal(@"c:\win\path", ParsedDocumentUri.File("c:/win/path").FsPath);
        Assert.Equal(@"c:\win\path\", ParsedDocumentUri.File("c:/win/path/").FsPath);
        Assert.Equal(@"c:\win\path", ParsedDocumentUri.File("C:/win/path").FsPath);
        Assert.Equal(@"c:\win\path", ParsedDocumentUri.File("/c:/win/path").FsPath);
        Assert.Equal(@"\.\c\win\path", ParsedDocumentUri.File("./c/win/path").FsPath);
    }

    [ConditionalFact(typeof(UnixLikeOnly))]
    public void File_FsPath_Unix()
    {
        Assert.Equal("c:/win/path", ParsedDocumentUri.File("c:/win/path").FsPath);
        Assert.Equal("c:/win/path/", ParsedDocumentUri.File("c:/win/path/").FsPath);
        Assert.Equal("c:/win/path", ParsedDocumentUri.File("C:/win/path").FsPath);
        Assert.Equal("c:/win/path", ParsedDocumentUri.File("/c:/win/path").FsPath);
        Assert.Equal("/./c/win/path", ParsedDocumentUri.File("./c/win/path").FsPath);
    }

    #endregion

    #region URI#fsPath - no `fsPath` when no `path`

    [Fact]
    public void FsPath_NoPathWhenAuthorityIsPath()
    {
        var value = ParsedDocumentUri.Parse("file://%2Fhome%2Fticino%2Fdesktop%2Fcpluscplus%2Ftest.cpp");
        Assert.Equal("/home/ticino/desktop/cpluscplus/test.cpp", value.Authority);
        Assert.Equal("/", value.Path);
        if (IsWindows)
        {
            Assert.Equal(@"\", value.FsPath);
        }
        else
        {
            Assert.Equal("/", value.FsPath);
        }
    }

    #endregion

    #region http#toString

    [Fact]
    public void Http_ToString()
    {
        Assert.Equal("http://www.msft.com/my/path", ParsedDocumentUri.From("http", authority: "www.msft.com", path: "/my/path").ToString());
        Assert.Equal("http://www.msft.com/my/path", ParsedDocumentUri.From("http", authority: "www.msft.com", path: "/my/path").ToString());
        Assert.Equal("http://www.msft.com/my/path", ParsedDocumentUri.From("http", authority: "www.MSFT.com", path: "/my/path").ToString());
        Assert.Equal("http:/my/path", ParsedDocumentUri.From("http", authority: "", path: "my/path").ToString());
        Assert.Equal("http:/my/path", ParsedDocumentUri.From("http", authority: "", path: "/my/path").ToString());
        // http://a-test-site.com/#test=true
        Assert.Equal("http://a-test-site.com/?test%3Dtrue", ParsedDocumentUri.From("http", authority: "a-test-site.com", path: "/", query: "test=true").ToString());
        Assert.Equal("http://a-test-site.com/#test%3Dtrue", ParsedDocumentUri.From("http", authority: "a-test-site.com", path: "/", query: "", fragment: "test=true").ToString());
    }

    #endregion

    #region http#toString, encode=FALSE

    [Fact]
    public void Http_ToString_SkipEncoding()
    {
        Assert.Equal("http://a-test-site.com/?test=true", ParsedDocumentUri.From("http", authority: "a-test-site.com", path: "/", query: "test=true").ToString(skipEncoding: true));
        Assert.Equal("http://a-test-site.com/#test=true", ParsedDocumentUri.From("http", authority: "a-test-site.com", path: "/", query: "", fragment: "test=true").ToString(skipEncoding: true));
        Assert.Equal("http:/api/files/test.me?t=1234", ParsedDocumentUri.From("http", path: "/api/files/test.me", query: "t=1234").ToString(skipEncoding: true));

        var value = ParsedDocumentUri.Parse("file://shares/pröjects/c%23/#l12");
        Assert.Equal("shares", value.Authority);
        Assert.Equal("/pröjects/c#/", value.Path);
        Assert.Equal("l12", value.Fragment);
        Assert.Equal("file://shares/pr%C3%B6jects/c%23/#l12", value.ToString());
        Assert.Equal("file://shares/pröjects/c%23/#l12", value.ToString(skipEncoding: true));

        var uri2 = ParsedDocumentUri.Parse(value.ToString(skipEncoding: true));
        var uri3 = ParsedDocumentUri.Parse(value.ToString());
        Assert.Equal(uri2.Authority, uri3.Authority);
        Assert.Equal(uri2.Path, uri3.Path);
        Assert.Equal(uri2.Query, uri3.Query);
        Assert.Equal(uri2.Fragment, uri3.Fragment);
    }

    #endregion

    #region From, identity

    [Fact]
    public void From_Identity()
    {
        var uri = ParsedDocumentUri.Parse("foo:bar/path");

        // Reconstructing with same components should produce equal struct
        var uri2 = ParsedDocumentUri.From(uri.Scheme, path: uri.Path);
        Assert.Equal(uri, uri2);
    }

    #endregion

    #region From, changes

    [Fact]
    public void From_Changes()
    {
        Assert.Equal("after:some/file/path", ParsedDocumentUri.From("after", path: "some/file/path").ToString());
        Assert.Equal("http:/api/files/test.me?t%3D1234", ParsedDocumentUri.From("http", path: "/api/files/test.me", query: "t=1234").ToString());
        Assert.Equal("http:/api/files/test.me?t%3D1234", ParsedDocumentUri.From("http", authority: "", path: "/api/files/test.me", query: "t=1234", fragment: "").ToString());
        Assert.Equal("https:/api/files/test.me?t%3D1234", ParsedDocumentUri.From("https", authority: "", path: "/api/files/test.me", query: "t=1234", fragment: "").ToString());
        Assert.Equal("HTTP:/api/files/test.me?t%3D1234", ParsedDocumentUri.From("HTTP", authority: "", path: "/api/files/test.me", query: "t=1234", fragment: "").ToString());
        Assert.Equal("HTTPS:/api/files/test.me?t%3D1234", ParsedDocumentUri.From("HTTPS", authority: "", path: "/api/files/test.me", query: "t=1234", fragment: "").ToString());
        Assert.Equal("boo:/api/files/test.me?t%3D1234", ParsedDocumentUri.From("boo", authority: "", path: "/api/files/test.me", query: "t=1234", fragment: "").ToString());
    }

    #endregion

    #region From, component combinations #8465

    [Fact]
    public void From_ComponentCombinations()
    {
        Assert.Equal("scheme:/path", ParsedDocumentUri.From("scheme", authority: "", path: "/path").ToString());
        Assert.Equal("scheme://authority", ParsedDocumentUri.From("scheme", authority: "authority", path: "").ToString());
        Assert.Equal("scheme:/path", ParsedDocumentUri.From("scheme", path: "/path").ToString());
    }

    #endregion

    #region From, validation

    [Fact]
    public void From_Validation()
    {
        Assert.Throws<UriFormatException>(() => ParsedDocumentUri.From("fai:l", path: "bar/path"));
        Assert.Throws<UriFormatException>(() => ParsedDocumentUri.From("fäil", path: "bar/path"));
        Assert.Throws<UriFormatException>(() => ParsedDocumentUri.From("foo", authority: "fail", path: "bar/path"));
        Assert.Throws<UriFormatException>(() => ParsedDocumentUri.From("foo", path: "//fail"));
    }

    #endregion

    #region parse

    [Fact]
    public void Parse()
    {
        var value = ParsedDocumentUri.Parse("http:/api/files/test.me?t=1234");
        Assert.Equal("http", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/api/files/test.me", value.Path);
        Assert.Equal("t=1234", value.Query);
        Assert.Equal("", value.Fragment);

        value = ParsedDocumentUri.Parse("http://api/files/test.me?t=1234");
        Assert.Equal("http", value.Scheme);
        Assert.Equal("api", value.Authority);
        Assert.Equal("/files/test.me", value.Path);
        Assert.Equal("t=1234", value.Query);
        Assert.Equal("", value.Fragment);

        value = ParsedDocumentUri.Parse("file:///c:/test/me");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/c:/test/me", value.Path);
        Assert.Equal("", value.Fragment);
        Assert.Equal("", value.Query);
        Assert.Equal(IsWindows ? @"c:\test\me" : "c:/test/me", value.FsPath);

        value = ParsedDocumentUri.Parse("file://shares/files/c%23/p.cs");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("shares", value.Authority);
        Assert.Equal("/files/c#/p.cs", value.Path);
        Assert.Equal("", value.Fragment);
        Assert.Equal("", value.Query);
        Assert.Equal(IsWindows ? @"\\shares\files\c#\p.cs" : "//shares/files/c#/p.cs", value.FsPath);

        value = ParsedDocumentUri.Parse("file:///c:/Source/Z%C3%BCrich%20or%20Zurich%20(%CB%88zj%CA%8A%C9%99r%C9%AAk,/Code/resources/app/plugins/c%23/plugin.json");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/c:/Source/Zürich or Zurich (ˈzjʊərɪk,/Code/resources/app/plugins/c#/plugin.json", value.Path);
        Assert.Equal("", value.Fragment);
        Assert.Equal("", value.Query);

        value = ParsedDocumentUri.Parse("file:///c:/test %25/path");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/c:/test %/path", value.Path);
        Assert.Equal("", value.Fragment);
        Assert.Equal("", value.Query);

        value = ParsedDocumentUri.Parse("inmemory:");
        Assert.Equal("inmemory", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("", value.Path);
        Assert.Equal("", value.Query);
        Assert.Equal("", value.Fragment);

        value = ParsedDocumentUri.Parse("foo:api/files/test");
        Assert.Equal("foo", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("api/files/test", value.Path);
        Assert.Equal("", value.Query);
        Assert.Equal("", value.Fragment);

        value = ParsedDocumentUri.Parse("file:?q");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/", value.Path);
        Assert.Equal("q", value.Query);
        Assert.Equal("", value.Fragment);

        value = ParsedDocumentUri.Parse("file:#d");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/", value.Path);
        Assert.Equal("", value.Query);
        Assert.Equal("d", value.Fragment);

        value = ParsedDocumentUri.Parse("f3ile:#d");
        Assert.Equal("f3ile", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("", value.Path);
        Assert.Equal("", value.Query);
        Assert.Equal("d", value.Fragment);

        value = ParsedDocumentUri.Parse("foo+bar:path");
        Assert.Equal("foo+bar", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("path", value.Path);
        Assert.Equal("", value.Query);
        Assert.Equal("", value.Fragment);

        value = ParsedDocumentUri.Parse("foo-bar:path");
        Assert.Equal("foo-bar", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("path", value.Path);
        Assert.Equal("", value.Query);
        Assert.Equal("", value.Fragment);

        value = ParsedDocumentUri.Parse("foo.bar:path");
        Assert.Equal("foo.bar", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("path", value.Path);
        Assert.Equal("", value.Query);
        Assert.Equal("", value.Fragment);
    }

    #endregion

    #region parse, disallow //path when no authority

    [Fact]
    public void Parse_DisallowDoubleSlashPathWithNoAuthority()
    {
        Assert.Throws<UriFormatException>(() => ParsedDocumentUri.Parse("file:////shares/files/p.cs"));
    }

    #endregion

    #region URI#file, win-special

    [ConditionalFact(typeof(WindowsOnly))]
    public void File_WinSpecial_DriveAndUNC()
    {
        var value = ParsedDocumentUri.File(@"c:\test\drive");
        Assert.Equal("/c:/test/drive", value.Path);
        Assert.Equal("file:///c%3A/test/drive", value.ToString());

        value = ParsedDocumentUri.File(@"\\shäres\path\c#\plugin.json");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("shäres", value.Authority);
        Assert.Equal("/path/c#/plugin.json", value.Path);
        Assert.Equal("", value.Fragment);
        Assert.Equal("", value.Query);
        Assert.Equal("file://sh%C3%A4res/path/c%23/plugin.json", value.ToString());

        value = ParsedDocumentUri.File(@"\\localhost\c$\GitDevelopment\express");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("/c$/GitDevelopment/express", value.Path);
        Assert.Equal(@"\\localhost\c$\GitDevelopment\express", value.FsPath);
        Assert.Equal("", value.Query);
        Assert.Equal("", value.Fragment);
        Assert.Equal("file://localhost/c%24/GitDevelopment/express", value.ToString());

        value = ParsedDocumentUri.File(@"c:\test with %\path");
        Assert.Equal("/c:/test with %/path", value.Path);
        Assert.Equal("file:///c%3A/test%20with%20%25/path", value.ToString());

        value = ParsedDocumentUri.File(@"c:\test with %25\path");
        Assert.Equal("/c:/test with %25/path", value.Path);
        Assert.Equal("file:///c%3A/test%20with%20%2525/path", value.ToString());

        value = ParsedDocumentUri.File(@"c:\test with %25\c#code");
        Assert.Equal("/c:/test with %25/c#code", value.Path);
        Assert.Equal("file:///c%3A/test%20with%20%2525/c%23code", value.ToString());

        value = ParsedDocumentUri.File(@"\\shares");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("shares", value.Authority);
        Assert.Equal("/", value.Path);

        value = ParsedDocumentUri.File(@"\\shares\");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("shares", value.Authority);
        Assert.Equal("/", value.Path);
    }

    #endregion

    #region DriveLetterPath regex #32961

    [Fact]
    public void DriveLetterPath_Regex()
    {
        var uri = ParsedDocumentUri.Parse("file:///_:/path");
        Assert.Equal(IsWindows ? @"\_:\path" : "/_:/path", uri.FsPath);
    }

    #endregion

    #region URI#file, no path-is-uri check

    [Fact]
    public void File_NoPathIsUriCheck()
    {
        // we don't complain here
        var value = ParsedDocumentUri.File("file://path/to/file");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/file://path/to/file", value.Path);
    }

    #endregion

    #region URI#file, always slash

    [Fact]
    public void File_AlwaysSlash()
    {
        var value = ParsedDocumentUri.File("a.file");
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/a.file", value.Path);
        Assert.Equal("file:///a.file", value.ToString());

        value = ParsedDocumentUri.Parse(value.ToString());
        Assert.Equal("file", value.Scheme);
        Assert.Equal("", value.Authority);
        Assert.Equal("/a.file", value.Path);
        Assert.Equal("file:///a.file", value.ToString());
    }

    #endregion

    #region URI.toString, only scheme and query

    [Fact]
    public void ToString_OnlySchemeAndQuery()
    {
        var value = ParsedDocumentUri.Parse("stuff:?qüery");
        Assert.Equal("stuff:?q%C3%BCery", value.ToString());
    }

    #endregion

    #region URI#toString, upper-case percent escapes

    [Fact]
    public void ToString_UpperCasePercentEscapes()
    {
        var value = ParsedDocumentUri.Parse("file://sh%c3%a4res/path");
        Assert.Equal("file://sh%C3%A4res/path", value.ToString());
    }

    #endregion

    #region URI#toString, lower-case windows drive letter

    [Fact]
    public void ToString_LowerCaseWindowsDriveLetter()
    {
        Assert.Equal("untitled:c%3A/Users/jrieken/Code/abc.txt", ParsedDocumentUri.Parse("untitled:c:/Users/jrieken/Code/abc.txt").ToString());
        Assert.Equal("untitled:c%3A/Users/jrieken/Code/abc.txt", ParsedDocumentUri.Parse("untitled:C:/Users/jrieken/Code/abc.txt").ToString());
    }

    #endregion

    #region URI#toString, escape all the bits

    [Fact]
    public void ToString_EscapeAllTheBits()
    {
        var value = ParsedDocumentUri.File("/Users/jrieken/Code/_samples/18500/Mödel + Other Thîngß/model.js");
        Assert.Equal("file:///Users/jrieken/Code/_samples/18500/M%C3%B6del%20%2B%20Other%20Th%C3%AEng%C3%9F/model.js", value.ToString());
    }

    #endregion

    #region URI#toString, don't encode port

    [Fact]
    public void ToString_DontEncodePort()
    {
        var value = ParsedDocumentUri.Parse("http://localhost:8080/far");
        Assert.Equal("http://localhost:8080/far", value.ToString());

        value = ParsedDocumentUri.From("http", authority: "löcalhost:8080", path: "/far");
        Assert.Equal("http://l%C3%B6calhost:8080/far", value.ToString());
    }

    #endregion

    #region URI#toString, user information in authority

    [Fact]
    public void ToString_UserInformationInAuthority()
    {
        var value = ParsedDocumentUri.Parse("http://foo:bar@localhost/far");
        Assert.Equal("http://foo:bar@localhost/far", value.ToString());

        value = ParsedDocumentUri.Parse("http://foo@localhost/far");
        Assert.Equal("http://foo@localhost/far", value.ToString());

        value = ParsedDocumentUri.Parse("http://foo:bAr@localhost:8080/far");
        Assert.Equal("http://foo:bAr@localhost:8080/far", value.ToString());

        value = ParsedDocumentUri.Parse("http://foo@localhost:8080/far");
        Assert.Equal("http://foo@localhost:8080/far", value.ToString());

        value = ParsedDocumentUri.From("http", authority: "föö:bör@löcalhost:8080", path: "/far");
        Assert.Equal("http://f%C3%B6%C3%B6:b%C3%B6r@l%C3%B6calhost:8080/far", value.ToString());
    }

    #endregion

    #region correctFileUriToFilePath2

    [Fact]
    public void CorrectFileUriToFilePath()
    {
        void AssertRoundTrip(string input, string expected)
        {
            var value = ParsedDocumentUri.Parse(input);
            Assert.Equal(expected, value.FsPath);
            var value2 = ParsedDocumentUri.File(value.FsPath);
            Assert.Equal(expected, value2.FsPath);
            Assert.Equal(value.ToString(), value2.ToString());
        }

        AssertRoundTrip("file:///c:/alex.txt", IsWindows ? @"c:\alex.txt" : "c:/alex.txt");
        AssertRoundTrip(
            "file:///c:/Source/Z%C3%BCrich%20or%20Zurich%20(%CB%88zj%CA%8A%C9%99r%C9%AAk,/Code/resources/app/plugins",
            IsWindows
                ? @"c:\Source\Zürich or Zurich (ˈzjʊərɪk,\Code\resources\app\plugins"
                : "c:/Source/Zürich or Zurich (ˈzjʊərɪk,/Code/resources/app/plugins");
        AssertRoundTrip(
            "file://monacotools/folder/isi.txt",
            IsWindows ? @"\\monacotools\folder\isi.txt" : "//monacotools/folder/isi.txt");
        AssertRoundTrip(
            "file://monacotools1/certificates/SSL/",
            IsWindows ? @"\\monacotools1\certificates\SSL\" : "//monacotools1/certificates/SSL/");
    }

    #endregion

    #region URI - http, query & toString

    [Fact]
    public void Http_QueryAndToString()
    {
        var uri = ParsedDocumentUri.Parse("https://go.microsoft.com/fwlink/?LinkId=518008");
        Assert.Equal("LinkId=518008", uri.Query);
        Assert.Equal("https://go.microsoft.com/fwlink/?LinkId=518008", uri.ToString(skipEncoding: true));
        Assert.Equal("https://go.microsoft.com/fwlink/?LinkId%3D518008", uri.ToString());

        var uri2 = ParsedDocumentUri.Parse(uri.ToString());
        Assert.Equal("LinkId=518008", uri2.Query);
        Assert.Equal(uri.Query, uri2.Query);

        uri = ParsedDocumentUri.Parse("https://go.microsoft.com/fwlink/?LinkId=518008&foö&ké¥=üü");
        Assert.Equal("LinkId=518008&foö&ké¥=üü", uri.Query);
        Assert.Equal("https://go.microsoft.com/fwlink/?LinkId=518008&foö&ké¥=üü", uri.ToString(skipEncoding: true));
        Assert.Equal("https://go.microsoft.com/fwlink/?LinkId%3D518008%26fo%C3%B6%26k%C3%A9%C2%A5%3D%C3%BC%C3%BC", uri.ToString());

        uri2 = ParsedDocumentUri.Parse(uri.ToString());
        Assert.Equal("LinkId=518008&foö&ké¥=üü", uri2.Query);
        Assert.Equal(uri.Query, uri2.Query);

        // #24849
        uri = ParsedDocumentUri.Parse("https://twitter.com/search?src=typd&q=%23tag");
        Assert.Equal("https://twitter.com/search?src=typd&q=%23tag", uri.ToString(skipEncoding: true));
    }

    #endregion

    #region class URI cannot represent relative file paths #34449

    [Fact]
    public void RelativeFilePaths()
    {
        var path = "/foo/bar";
        Assert.Equal(path, ParsedDocumentUri.File(path).Path);
        path = "foo/bar";
        Assert.Equal("/foo/bar", ParsedDocumentUri.File(path).Path);
        path = "./foo/bar";
        Assert.Equal("/./foo/bar", ParsedDocumentUri.File(path).Path); // missing normalization

        var fileUri1 = ParsedDocumentUri.Parse("file:foo/bar");
        Assert.Equal("/foo/bar", fileUri1.Path);
        Assert.Equal("", fileUri1.Authority);
        var uriStr = fileUri1.ToString();
        Assert.Equal("file:///foo/bar", uriStr);
        var fileUri2 = ParsedDocumentUri.Parse(uriStr);
        Assert.Equal("/foo/bar", fileUri2.Path);
        Assert.Equal("", fileUri2.Authority);
    }

    #endregion

    #region Ctrl click to follow hash query param url gets urlencoded #49628

    [Fact]
    public void HashQueryParamUrl()
    {
        var input = "http://localhost:3000/#/foo?bar=baz";
        var uri = ParsedDocumentUri.Parse(input);
        Assert.Equal(input, uri.ToString(skipEncoding: true));

        input = "http://localhost:3000/foo?bar=baz";
        uri = ParsedDocumentUri.Parse(input);
        Assert.Equal(input, uri.ToString(skipEncoding: true));
    }

    #endregion

    #region Unable to open '%A0.txt': URI malformed #76506

    [Fact]
    public void PercentEncoding_MalformedUri_76506()
    {
        var uri = ParsedDocumentUri.File("/foo/%A0.txt");
        var uri2 = ParsedDocumentUri.Parse(uri.ToString());
        Assert.Equal(uri.Scheme, uri2.Scheme);
        Assert.Equal(uri.Path, uri2.Path);

        uri = ParsedDocumentUri.File("/foo/%2e.txt");
        uri2 = ParsedDocumentUri.Parse(uri.ToString());
        Assert.Equal(uri.Scheme, uri2.Scheme);
        Assert.Equal(uri.Path, uri2.Path);
    }

    [Fact]
    public void PercentEncoding_MalformedUri_76506_ToString()
    {
        Assert.Equal("file://some/%25.txt", ParsedDocumentUri.Parse("file://some/%.txt").ToString());
        Assert.Equal("file://some/%25A0.txt", ParsedDocumentUri.Parse("file://some/%A0.txt").ToString());
    }

    #endregion

    #region URI - (de)serialize (round-trip via parse/toString)

    [Fact]
    public void RoundTrip_ParseToString()
    {
        var uris = new[]
        {
            ParsedDocumentUri.Parse("http://localhost:8080/far"),
            ParsedDocumentUri.File(@"c:\test with %25\c#code"),
            ParsedDocumentUri.File(@"\\shäres\path\c#\plugin.json"),
            ParsedDocumentUri.Parse("http://api/files/test.me?t=1234"),
            ParsedDocumentUri.Parse("http://api/files/test.me?t=1234#fff"),
            ParsedDocumentUri.Parse("http://api/files/test.me#fff"),
        };

        foreach (var value in uris)
        {
            // Round-trip through parse(toString())
            var clone = ParsedDocumentUri.Parse(value.ToString());

            Assert.Equal(value.Scheme, clone.Scheme);
            Assert.Equal(value.Authority, clone.Authority);
            Assert.Equal(value.Path, clone.Path);
            Assert.Equal(value.Query, clone.Query);
            Assert.Equal(value.Fragment, clone.Fragment);
            Assert.Equal(value.FsPath, clone.FsPath);
            Assert.Equal(value.ToString(), clone.ToString());
        }
    }

    #endregion

    #region Equality

    [Fact]
    public void Equality_SameComponents()
    {
        var uri1 = ParsedDocumentUri.Parse("file:///c:/test/me");
        var uri2 = ParsedDocumentUri.Parse("file:///c:/test/me");
        Assert.Equal(uri1, uri2);
        Assert.True(uri1 == uri2);
        Assert.Equal(uri1.GetHashCode(), uri2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentComponents()
    {
        var uri1 = ParsedDocumentUri.Parse("file:///c:/test/me");
        var uri2 = ParsedDocumentUri.Parse("file:///c:/test/other");
        Assert.NotEqual(uri1, uri2);
        Assert.True(uri1 != uri2);
    }

    #endregion

    #region Default struct

    [Fact]
    public void Default_Struct()
    {
        // Default struct should have null fields but not crash
        var uri = default(ParsedDocumentUri);
        Assert.Null(uri.Scheme);
        Assert.Null(uri.Authority);
        Assert.Null(uri.Path);
        Assert.Null(uri.Query);
        Assert.Null(uri.Fragment);
    }

    #endregion
}
