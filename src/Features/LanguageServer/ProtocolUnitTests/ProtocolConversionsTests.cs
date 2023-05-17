// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    public class ProtocolConversionsTests
    {
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
