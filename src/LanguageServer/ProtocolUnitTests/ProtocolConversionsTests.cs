// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using Range = Roslyn.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class ProtocolConversionsTests : AbstractLanguageServerProtocolTests
{
    public ProtocolConversionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void CreateAbsoluteUri_LocalPaths_AllAscii()
    {
        var invalidFileNameChars = Path.GetInvalidFileNameChars();

        // System.Uri leaves more reserved characters unescaped in path segments than our ParsedUri does.
        var systemUriUnescaped = "!$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~";
        var parsedUriUnescaped = "-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~";

        for (var c = '\0'; c < '\u0080'; c++)
        {
            if (invalidFileNameChars.Contains(c))
            {
                // no need to validate escaping for characters that can't appear in a file/directory name
                continue;
            }

            // ? is a valid filename char on Unix but is a URI query delimiter, causing
            // inconsistent encoding between GetAbsoluteUriString and CreateAbsoluteDocumentUri
            if (c == '?' && PathUtilities.IsUnixLikePlatform)
                continue;

            var filePath = PathUtilities.IsUnixLikePlatform ? $"/_{c}/" : $"C:\\_{c}\\";

            var systemUriPrefix = PathUtilities.IsUnixLikePlatform ? "_" : "C:/_";
            var expectedAbsoluteSystemUri = "file:///" + systemUriPrefix + (systemUriUnescaped.Contains(c) ? c : "%" + ((int)c).ToString("X2")) + "/";

            var uriPrefix = PathUtilities.IsUnixLikePlatform ? "_" : "c%3A/_";
            var expectedAbsoluteUri = "file:///" + uriPrefix + (parsedUriUnescaped.Contains(c) ? c : "%" + ((int)c).ToString("X2")) + "/";

#pragma warning disable RS0030 // Do not use banned APIs - testing behavior of System.Uri which is still used in some cases.
            Assert.Equal(expectedAbsoluteSystemUri, ProtocolConversions.CreateAbsoluteUri(filePath).AbsoluteUri);
#pragma warning restore RS0030 // Do not use banned APIs

            var uri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
            Assert.Equal(expectedAbsoluteUri, uri.GetRequiredParsedUri().ToString());
            Assert.Equal(filePath, uri.GetRequiredParsedUri().FsPath);
        }
    }

    #region System.Uri Conversion Tests

#pragma warning disable RS0030 // Do not use banned APIs

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData("C:/", "file:///C:/")]
    [InlineData("C:\\", "file:///C:/")]
    [InlineData("C:\\a\\b", "file:///C:/a/b")]
    [InlineData("C:\\a\\\\b", "file:///C:/a//b")]
    [InlineData("C:\\%25\ue25b/a\\b", "file:///C:/%2525%EE%89%9B/a/b")]
    [InlineData("C:\\%25\ue25b/a\\\\b", "file:///C:/%2525%EE%89%9B/a//b")]
    [InlineData("C:\\\u0089\uC7BD", "file:///C:/%C2%89%EC%9E%BD")]
    [InlineData("/\\server\ue25b\\%25\ue25b\\b", "file://server/%2525%EE%89%9B/b")]
    [InlineData("\\\\server\ue25b\\%25\ue25b\\b", "file://server/%2525%EE%89%9B/b")]
    [InlineData("C:\\ !$&'()+,-;=@[]_~#", "file:///C:/%20!$&'()+,-;=@[]_~%23")]
    [InlineData("C:\\ !$&'()+,-;=@[]_~#\ue25b", "file:///C:/%20!$&'()+,-;=@[]_~%23%EE%89%9B")]
    [InlineData("C:\\\u0073\u0323\u0307", "file:///C:/s%CC%A3%CC%87")] // combining marks
    [InlineData("A:/\\\u200e//", "file:///A://%E2%80%8E//")] // cases from https://github.com/dotnet/runtime/issues/1487
    [InlineData("B:\\/\u200e", "file:///B://%E2%80%8E")]
    [InlineData("C:/\\\\-Ā\r", "file:///C:///-%C4%80%0D")]
    [InlineData("D:\\\\\\\\\\\u200e", "file:///D://///%E2%80%8E")]
    public void CreateAbsoluteUri_LocalPaths_Windows(string filePath, string expectedAbsoluteUri)
    {
        var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
        Assert.Equal(expectedAbsoluteUri, uri.AbsoluteUri);
        Assert.Equal(filePath.Replace('/', '\\'), uri.LocalPath);
    }

    [ConditionalTheory(typeof(UnixLikeOnly))]
    [InlineData("/", "file:///")]
    [InlineData("/u", "file:///u")]
    [InlineData("/unix/path", "file:///unix/path")]
    [InlineData("/%25\ue25b/\u0089\uC7BD", "file:///%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    [InlineData("/!$&'()+,-;=@[]_~#", "file:///!$&'()+,-;=@[]_~%23")]
    [InlineData("/!$&'()+,-;=@[]_~#", "file:///!$&'()+,-;=@[]_~%23%EE%89%9B")]
    [InlineData("/\\\u200e//", "file:///%5C%E2%80%8E//")] // cases from https://github.com/dotnet/runtime/issues/1487
    [InlineData("/\\\\-Ā\r", "file:///%5C%5C-%C4%80%0D")]
    public void CreateAbsoluteUri_LocalPaths_Unix(string filePath, string expectedAbsoluteUri)
    {
        var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
        Assert.Equal(expectedAbsoluteUri, uri.AbsoluteUri);
        Assert.Equal(filePath, uri.LocalPath);
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData("C:\\a\\.\\b", "file:///C:/a/b")]
    [InlineData("C:\\a\\..\\b", "file:///C:/b")]
    [InlineData("C:\\\ue25b\\.\\\ue25c", "file:///C:/%EE%89%9B/%EE%89%9C")]
    [InlineData("C:\\\ue25b\\..\\\ue25c", "file:///C:/%EE%89%9C")]
    public void CreateAbsoluteUri_LocalPaths_Normalized_Windows(string filePath, string expectedNormalizedUri)
    {
        var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
        Assert.Equal(expectedNormalizedUri, uri.AbsoluteUri);
        Assert.Equal(Path.GetFullPath(filePath).Replace('/', '\\'), uri.LocalPath);
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(@"\\home$\share\path", @"file://home$/share/path")]
    [InlineData(@"\\home$\share\path\", @"file://home$/share/path")]
    public void CreateRelativePatternBaseUri_UncPathWithDollarSign_Windows(string filePath, string expectedUri)
    {
        // UNC paths with $ in the server name (e.g. admin shares) are valid Windows paths
        // but System.Uri cannot parse them. We should still get a DocumentUri back with the
        // correct URI string, even though System.Uri can't parse it.
        var uri = ProtocolConversions.CreateRelativePatternBaseUri(filePath);
        Assert.Equal(expectedUri, uri.UriString);
        Assert.Null(uri.ParsedUri);
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData(@"\\home$\share\path", @"file://home$/share/path")]
    public void CreateAbsoluteDocumentUri_UncPathWithDollarSign_Windows(string filePath, string expectedUri)
    {
        // UNC paths with $ in the server name (e.g. admin shares) are valid Windows paths
        // but System.Uri cannot parse them. We should still get a DocumentUri back with the
        // correct URI string, even though System.Uri can't parse it.
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(filePath);
        Assert.Equal(expectedUri, uri.UriString);
        Assert.Null(uri.ParsedUri);
    }

    [ConditionalTheory(typeof(UnixLikeOnly))]
    [InlineData("/a/./b", "file:///a/b")]
    [InlineData("/a/../b", "file:///b")]
    [InlineData("/\ue25b/./\ue25c", "file:///%EE%89%9B/%EE%89%9C")]
    [InlineData("/\ue25b/../\ue25c", "file:///%EE%89%9C")]
    public void CreateAbsoluteUri_LocalPaths_Normalized_Unix(string filePath, string expectedNormalizedUri)
    {
        var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
        Assert.Equal(expectedNormalizedUri, uri.AbsoluteUri);
        Assert.Equal(Path.GetFullPath(filePath), uri.LocalPath);
    }

    [Theory]
    [InlineData("git:/x:/%2525%EE%89%9B/%C2%89%EC%9E%BD?abc")]
    [InlineData("git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    [InlineData("xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    public void CreateAbsoluteUri_Urls(string url)
        => Assert.Equal(url, ProtocolConversions.CreateAbsoluteUri(url).AbsoluteUri);

#pragma warning restore RS0030 // Do not use banned APIs

    #endregion

    #region DocumentUri Conversion Tests

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData("C:\\", "file:///c%3A/")]
    [InlineData("C:\\a\\b", "file:///c%3A/a/b")]
    [InlineData("C:\\a\\\\b", "file:///c%3A/a//b")]
    [InlineData("C:\\%25\ue25b/a\\b", "file:///c%3A/%2525%EE%89%9B/a/b")]
    [InlineData("git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD", "git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    [InlineData("xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD", "xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    public void CreateAbsoluteDocumentUri_FilePathsAndUris_Windows(string input, string expectedUri)
    {
        // Covers basic scenarios for CreateAbsoluteDocumentUri.  Full URI parsing / roundtripping is tested in ParsedUriTests
        Assert.Equal(expectedUri, ProtocolConversions.CreateAbsoluteDocumentUri(input).GetRequiredParsedUri().ToString());
    }

    [ConditionalTheory(typeof(UnixLikeOnly))]
    [InlineData("/", "file:///")]
    [InlineData("/unix/path", "file:///unix/path")]
    [InlineData("/%25\ue25b/\u0089\uC7BD", "file:///%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    [InlineData("git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD", "git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    [InlineData("xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD", "xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    public void CreateAbsoluteDocumentUri_FilePathsAndUris_Unix(string input, string expectedUri)
    {
        // Covers basic scenarios for CreateAbsoluteDocumentUri.  Full URI parsing / roundtripping is tested in ParsedUriTests
        Assert.Equal(expectedUri, ProtocolConversions.CreateAbsoluteDocumentUri(input).GetRequiredParsedUri().ToString());
    }

    [ConditionalTheory(typeof(WindowsOnly))]
    [InlineData("C:\\a\\b", "file:///c%3A/a/b")]
    [InlineData("C:\\a\\b\\", "file:///c%3A/a/b")]
    [InlineData("C:\\a\\\\b", "file:///c%3A/a//b")]
    [InlineData("C:\\%25\ue25b/a\\b", "file:///c%3A/%2525%EE%89%9B/a/b")]
    [InlineData("C:\\%25\ue25b/a\\\\b", "file:///c%3A/%2525%EE%89%9B/a//b")]
    [InlineData("C:\\\u0089\uC7BD", "file:///c%3A/%C2%89%EC%9E%BD")]
    [InlineData("/\\server\ue25b\\%25\ue25b\\b", "file://server%EE%89%9B/%2525%EE%89%9B/b")]
    [InlineData("\\\\server\ue25b\\%25\ue25b\\b", "file://server%EE%89%9B/%2525%EE%89%9B/b")]
    [InlineData("\\\\server\ue25b\\%25\ue25b\\b\\", "file://server%EE%89%9B/%2525%EE%89%9B/b")]
    [InlineData("C:\\ !$&'()+,-;=@[]_~#", "file:///c%3A/%20%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23")]
    [InlineData("C:\\ !$&'()+,-;=@[]_~#\ue25b", "file:///c%3A/%20%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23%EE%89%9B")]
    [InlineData("C:\\\u0073\u0323\u0307", "file:///c%3A/s%CC%A3%CC%87")] // combining marks
    [InlineData("A:/\\\u200e//", "file:///a%3A//%E2%80%8E//")] // cases from https://github.com/dotnet/runtime/issues/1487
    [InlineData("B:\\/\u200e", "file:///b%3A//%E2%80%8E")]
    [InlineData("C:/\\\\-Ā\r", "file:///c%3A///-%C4%80%0D")]
    [InlineData("D:\\\\\\\\\u200e", "file:///d%3A////%E2%80%8E")]
    public void CreateRelativePatternBaseUri_LocalPaths_Windows(string filePath, string expectedUri)
    {
        var uri = ProtocolConversions.CreateRelativePatternBaseUri(filePath);
        Assert.Equal(expectedUri, uri.GetRequiredParsedUri().ToString());
    }

    [ConditionalTheory(typeof(UnixLikeOnly))]
    [InlineData("/u", "file:///u")]
    [InlineData("/unix/", "file:///unix")]
    [InlineData("/unix/path", "file:///unix/path")]
    [InlineData("/%25\ue25b/\u0089\uC7BD", "file:///%2525%EE%89%9B/%C2%89%EC%9E%BD")]
    [InlineData("/!$&'()+,-;=@[]_~#", "file:///%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23")]
    [InlineData("/!$&'()+,-;=@[]_~#", "file:///%21%24%26%27%28%29%2B%2C-%3B%3D%40%5B%5D_~%23%EE%89%9B")]
    [InlineData("/\\\u200e//", "file:///%5C%E2%80%8E/")] // cases from https://github.com/dotnet/runtime/issues/1487
    [InlineData("/\\\\-Ā\r", "file:///%5C%5C-%C4%80%0D")]
    public void CreateRelativePatternBaseUri_LocalPaths_Unix(string filePath, string expectedRelativeUri)
    {
        var uri = ProtocolConversions.CreateRelativePatternBaseUri(filePath);
        Assert.Equal(expectedRelativeUri, uri.GetRequiredParsedUri().ToString());
    }

    #endregion

    [Fact]
    public void CompletionItemKind_DoNotUseMethodAndFunction()
    {
        var map = ProtocolConversions.RoslynTagToCompletionItemKinds;
        var containsMethod = map.Values.Any(c => c.Contains(CompletionItemKind.Method));
        var containsFunction = map.Values.Any(c => c.Contains(CompletionItemKind.Function));

        Assert.False(containsFunction && containsMethod, "Don't use Method and Function completion item kinds as it causes user confusion.");
    }

    [Fact]
    public void RangeToTextSpanStartWithNextLine()
    {
        var markup = GetTestMarkup();

        var sourceText = SourceText.From(markup);
        var range = new Range() { Start = new Position(0, 0), End = new Position(1, 0) };
        var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

        // End should be start of the second line
        Assert.Equal(0, textSpan.Start);
        Assert.Equal(10, textSpan.End);
    }

    [Fact]
    public void RangeToTextSpanMidLine()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        // Take just "x = 5"
        var range = new Range() { Start = new Position(2, 8), End = new Position(2, 12) };
        var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

        Assert.Equal(21, textSpan.Start);
        Assert.Equal(25, textSpan.End);
    }

    [Fact]
    public void RangeToTextSpanClampsCharacterPastLineEnd()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        var range = new Range() { Start = new Position(2, 8), End = new Position(2, int.MaxValue) };
        var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

        Assert.Equal(21, textSpan.Start);
        Assert.Equal(27, textSpan.End);
    }

    [Fact]
    public void RangeToTextSpanClampsStartCharacterPastLineEnd()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        var range = new Range() { Start = new Position(2, int.MaxValue), End = new Position(2, int.MaxValue) };
        var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

        Assert.Equal(27, textSpan.Start);
        Assert.Equal(27, textSpan.End);
    }

    [Fact]
    public void RangeToTextSpanLineEndOfDocument()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        var range = new Range() { Start = new Position(0, 0), End = new Position(3, 1) };
        var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

        Assert.Equal(0, textSpan.Start);
        Assert.Equal(30, textSpan.End);
    }

    [Fact]
    public void RangeToTextSpanLineEndOfDocumentWithEndOfLineChars()
    {
        var markup =
            """
            void M()
            {
                var x = 5;
            }

            """.NormalizeLineEndings(); // add additional end line 

        var sourceText = SourceText.From(markup);

        var range = new Range() { Start = new Position(0, 0), End = new Position(4, 0) };
        var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

        // Result now includes end of line characters for line 3
        Assert.Equal(0, textSpan.Start);
        Assert.Equal(32, textSpan.End);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80119")]
    public void RangeToTextSpanDoesNotThrow_WhenReferencingStartOfNextLineAfterLastLine()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        // The spec allows clients to send a range referencing the start of the next line
        // after the last line in the document (and outside the bounds of the document).
        // This should not throw.
        var lastLineIndex = sourceText.Lines.Count - 1;
        var range = new Range()
        {
            Start = new Position(lastLineIndex, 0),
            End = new Position(lastLineIndex + 1, 0)
        };

        var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

        // Should span from the start of the last line to the end of the document
        var lastLine = sourceText.Lines[lastLineIndex];
        Assert.Equal(lastLine.Start, textSpan.Start);
        Assert.Equal(sourceText.Length, textSpan.End);
    }

    [Fact]
    public void RangeToTextSpanThrows_LineOutOfRange()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        // Ranges that are outside the document bounds should throw.
        var range = new Range() { Start = new Position(0, 0), End = new Position(sourceText.Lines.Count + 1, 0) };
        Assert.Throws<ArgumentException>(() => ProtocolConversions.RangeToTextSpan(range, sourceText));
    }

    [Fact]
    public void RangeToTextSpanWThrows_CharacterOutOfRange()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        var range = new Range() { Start = new Position(0, 0), End = new Position(sourceText.Lines.Count, 5) };
        Assert.Throws<ArgumentException>(() => ProtocolConversions.RangeToTextSpan(range, sourceText));
    }

    [Fact]
    public void RangeToTextSpanEndAfterStartError()
    {
        var markup = GetTestMarkup();
        var sourceText = SourceText.From(markup);

        // This start position will still be beyond the end position after clamping.
        var range = new Range() { Start = new Position(2, int.MaxValue), End = new Position(2, 0) };
        Assert.Throws<ArgumentException>(() => ProtocolConversions.RangeToTextSpan(range, sourceText));
    }

    private static string GetTestMarkup()
    {
        // Markup is 31 characters long. Line break (\n) is 2 characters 
        /*
        void M()        [Line = 0; Start = 0; End = 8; End including line break = 10]
        {               [Line = 1; Start = 10; End = 11; End including line break = 13]
            var x = 5;  [Line = 2; Start = 13; End = 27; End including line break = 29]
        }               [Line = 3; Start = 29; End = 30; End including line break = 30]
         */

        var markup =
            """
            void M()
            {
                var x = 5;
            }
            """.NormalizeLineEndings();
        return markup;
    }

    [Theory, CombinatorialData]
    public async Task ProjectToProjectContext_HostWorkspace(bool mutatingLspWorkspace)
    {
        var source = """
            class {|caret:A|}
            {
                void M()
                {
                }
            }
            """;

        // Create a server with an existing file.
        await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        var caret = testLspServer.GetLocations("caret").Single();

        var document = await GetTextDocumentAsync(testLspServer, caret.DocumentUri);
        Assert.NotNull(document);

        var projectContext = ProtocolConversions.ProjectToProjectContext(document.Project);

        Assert.False(projectContext.IsMiscellaneous);
    }

    [Theory, CombinatorialData]
    public async Task ProjectToProjectContext_MiscellaneousFilesWorkspace(bool mutatingLspWorkspace)
    {

        // Create a server that supports LSP misc files.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open an empty loose file.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                }
            }
            """).ConfigureAwait(false);

        var document = await GetTextDocumentAsync(testLspServer, looseFileUri);
        Assert.NotNull(document);

        var projectContext = ProtocolConversions.ProjectToProjectContext(document.Project);

        Assert.True(projectContext.IsMiscellaneous);
    }

    internal static async Task<TextDocument?> GetTextDocumentAsync(TestLspServer testLspServer, DocumentUri uri)
    {
        var (_, _, textDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new TextDocumentIdentifier { DocumentUri = uri }, CancellationToken.None);
        return textDocument;
    }
}
