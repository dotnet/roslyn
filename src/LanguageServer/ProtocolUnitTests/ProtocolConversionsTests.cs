// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Range = Roslyn.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    public class ProtocolConversionsTests
    {
        [Fact]
        public void CreateAbsoluteUri_LocalPaths_AllAscii()
        {
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            var unescaped = "!$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~";

            for (var c = '\0'; c < '\u0080'; c++)
            {
                if (invalidFileNameChars.Contains(c))
                {
                    // no need to validate escaping for characters that can't appear in a file/directory name
                    continue;
                }

                var filePath = PathUtilities.IsUnixLikePlatform ? $"/_{c}/" : $"C:\\_{c}\\";
                var uriPrefix = PathUtilities.IsUnixLikePlatform ? "" : "C:/_";

                var expectedAbsoluteUri = "file:///" + uriPrefix + (unescaped.Contains(c) ? c : "%" + ((int)c).ToString("X2")) + "/";

                Assert.Equal(expectedAbsoluteUri, ProtocolConversions.GetAbsoluteUriString(filePath));

                var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
                Assert.Equal(expectedAbsoluteUri, uri.AbsoluteUri);
                Assert.Equal(filePath, uri.LocalPath);
            }
        }

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
            Assert.Equal(expectedAbsoluteUri, ProtocolConversions.GetAbsoluteUriString(filePath));

            var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
            Assert.Equal(expectedAbsoluteUri, uri.AbsoluteUri);
            Assert.Equal(filePath.Replace('/', '\\'), uri.LocalPath);
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData("C:\\a\\.\\b", "file:///C:/a/./b", "file:///C:/a/b")]
        [InlineData("C:\\a\\..\\b", "file:///C:/a/../b", "file:///C:/b")]
        [InlineData("C:\\\ue25b\\.\\\ue25c", "file:///C:/%EE%89%9B/./%EE%89%9C", "file:///C:/%EE%89%9B/%EE%89%9C")]
        [InlineData("C:\\\ue25b\\..\\\ue25c", "file:///C:/%EE%89%9B/../%EE%89%9C", "file:///C:/%EE%89%9C")]
        public void CreateAbsoluteUri_LocalPaths_Normalized_Windows(string filePath, string expectedRawUri, string expectedNormalizedUri)
        {
            Assert.Equal(expectedRawUri, ProtocolConversions.GetAbsoluteUriString(filePath));

            var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
            Assert.Equal(expectedNormalizedUri, uri.AbsoluteUri);
            Assert.Equal(Path.GetFullPath(filePath).Replace('/', '\\'), uri.LocalPath);
        }

        [ConditionalTheory(typeof(UnixLikeOnly))]
        [InlineData("/", "file:///")]
        [InlineData("/u", "file:///u")]
        [InlineData("/unix/path", "file:///unix/path")]
        [InlineData("/%25\ue25b/\u0089\uC7BD", "file:///%2525%EE%89%9B/%C2%89%EC%9E%BD")]
        [InlineData("/!$&'()+,-;=@[]_~#", "file:///!$&'()+,-;=@[]_~%23")]
        [InlineData("/!$&'()+,-;=@[]_~#", "file:///!$&'()+,-;=@[]_~%23%EE%89%9B")]
        [InlineData("/\\\u200e//", "file:////%E2%80%8E//")] // cases from https://github.com/dotnet/runtime/issues/1487
        [InlineData("\\/\u200e", "file:////%E2%80%8E")]
        [InlineData("/\\\\-Ā\r", "file://///-%C4%80%0D")]
        [InlineData("\\\\\\\\\\\u200e", "file:///////%E2%80%8E")]
        public void CreateAbsoluteUri_LocalPaths_Unix(string filePath, string expectedAbsoluteUri)
        {
            Assert.Equal(expectedAbsoluteUri, ProtocolConversions.GetAbsoluteUriString(filePath));

            var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
            Assert.Equal(expectedAbsoluteUri, uri.AbsoluteUri);
            Assert.Equal(filePath, uri.LocalPath);
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData("C:\\a\\b", "file:///C:/a/b")]
        [InlineData("C:\\a\\b\\", "file:///C:/a/b")]
        [InlineData("C:\\a\\\\b", "file:///C:/a//b")]
        [InlineData("C:\\%25\ue25b/a\\b", "file:///C:/%2525%EE%89%9B/a/b")]
        [InlineData("C:\\%25\ue25b/a\\\\b", "file:///C:/%2525%EE%89%9B/a//b")]
        [InlineData("C:\\\u0089\uC7BD", "file:///C:/%C2%89%EC%9E%BD")]
        [InlineData("/\\server\ue25b\\%25\ue25b\\b", "file://server/%2525%EE%89%9B/b")]
        [InlineData("\\\\server\ue25b\\%25\ue25b\\b", "file://server/%2525%EE%89%9B/b")]
        [InlineData("\\\\server\ue25b\\%25\ue25b\\b\\", "file://server/%2525%EE%89%9B/b")]
        [InlineData("C:\\ !$&'()+,-;=@[]_~#", "file:///C:/%20!$&'()+,-;=@[]_~%23")]
        [InlineData("C:\\ !$&'()+,-;=@[]_~#\ue25b", "file:///C:/%20!$&'()+,-;=@[]_~%23%EE%89%9B")]
        [InlineData("C:\\\u0073\u0323\u0307", "file:///C:/s%CC%A3%CC%87")] // combining marks
        [InlineData("A:/\\\u200e//", "file:///A://%E2%80%8E//")] // cases from https://github.com/dotnet/runtime/issues/1487
        [InlineData("B:\\/\u200e", "file:///B://%E2%80%8E")]
        [InlineData("C:/\\\\-Ā\r", "file:///C:///-%C4%80%0D")]
        [InlineData("D:\\\\\\\\\\\u200e", "file:///D://///%E2%80%8E")]
        public void CreateRelativePatternBaseUri_LocalPaths_Windows(string filePath, string expectedUri)
        {
            var uri = ProtocolConversions.CreateRelativePatternBaseUri(filePath);
            Assert.Equal(expectedUri, uri.AbsoluteUri);
        }

        [ConditionalTheory(typeof(UnixLikeOnly))]
        [InlineData("/", "file://")]
        [InlineData("/u", "file:///u")]
        [InlineData("/unix/", "file:///unix")]
        [InlineData("/unix/path", "file:///unix/path")]
        [InlineData("/%25\ue25b/\u0089\uC7BD", "file:///%2525%EE%89%9B/%C2%89%EC%9E%BD")]
        [InlineData("/!$&'()+,-;=@[]_~#", "file:///!$&'()+,-;=@[]_~%23")]
        [InlineData("/!$&'()+,-;=@[]_~#", "file:///!$&'()+,-;=@[]_~%23%EE%89%9B")]
        [InlineData("/\\\u200e//", "file:////%E2%80%8E//")] // cases from https://github.com/dotnet/runtime/issues/1487
        [InlineData("\\/\u200e", "file:////%E2%80%8E")]
        [InlineData("/\\\\-Ā\r", "file://///-%C4%80%0D")]
        [InlineData("\\\\\\\\\\\u200e", "file:///////%E2%80%8E")]
        public void CreateRelativePatternBaseUri_LocalPaths_Unix(string filePath, string expectedRelativeUri)
        {
            var uri = ProtocolConversions.CreateRelativePatternBaseUri(filePath);
            Assert.Equal(expectedRelativeUri, uri.AbsoluteUri);
        }

        [ConditionalTheory(typeof(UnixLikeOnly))]
        [InlineData("/a/./b", "file:///a/./b", "file:///a/b")]
        [InlineData("/a/../b", "file:///a/../b", "file:///b")]
        [InlineData("/\ue25b/./\ue25c", "file:///%EE%89%9B/./%EE%89%9C", "file:///%EE%89%9B/%EE%89%9C")]
        [InlineData("/\ue25b/../\ue25c", "file:///%EE%89%9B/../%EE%89%9C", "file:///%EE%89%9C")]
        public void CreateAbsoluteUri_LocalPaths_Normalized_Unix(string filePath, string expectedRawUri, string expectedNormalizedUri)
        {
            Assert.Equal(expectedRawUri, ProtocolConversions.GetAbsoluteUriString(filePath));

            var uri = ProtocolConversions.CreateAbsoluteUri(filePath);
            Assert.Equal(expectedNormalizedUri, uri.AbsoluteUri);
            Assert.Equal(filePath, uri.LocalPath);
        }

        [Theory]
        [InlineData("git:/x:/%2525%EE%89%9B/%C2%89%EC%9E%BD?abc")]
        [InlineData("git://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
        [InlineData("xy://host/%2525%EE%89%9B/%C2%89%EC%9E%BD")]
        public void CreateAbsoluteUri_Urls(string url)
        {
            Assert.Equal(url, ProtocolConversions.CreateAbsoluteUri(url).AbsoluteUri);
        }

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
@"void M()
{
    var x = 5;
}
"; // add additional end line 

            var sourceText = SourceText.From(markup);

            var range = new Range() { Start = new Position(0, 0), End = new Position(4, 0) };
            var textSpan = ProtocolConversions.RangeToTextSpan(range, sourceText);

            // Result now includes end of line characters for line 3
            Assert.Equal(0, textSpan.Start);
            Assert.Equal(32, textSpan.End);
        }

        [Fact]
        public void RangeToTextSpanLineOutOfRangeError()
        {
            var markup = GetTestMarkup();
            var sourceText = SourceText.From(markup);

            var range = new Range() { Start = new Position(0, 0), End = new Position(sourceText.Lines.Count, 0) };
            Assert.Throws<ArgumentException>(() => ProtocolConversions.RangeToTextSpan(range, sourceText));
        }

        [Fact]
        public void RangeToTextSpanEndAfterStartError()
        {
            var markup = GetTestMarkup();
            var sourceText = SourceText.From(markup);

            // This start position will be beyond the end position
            var range = new Range() { Start = new Position(2, 20), End = new Position(3, 0) };
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
@"void M()
{
    var x = 5;
}";
            return markup;
        }
    }
}
