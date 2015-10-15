// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorAdapter
{
    public class TextSnapshotImplementationTest
    {
        private Tuple<ITextSnapshot, SourceText> Create(params string[] lines)
        {
            var buffer = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, lines);
            var text = buffer.CurrentSnapshot.AsText();
            return Tuple.Create(buffer.CurrentSnapshot, text);
        }

        [WpfFact]
        public void Basic1()
        {
            var tuple = Create("foo", "bar");
            var text = tuple.Item2;
            Assert.Equal(tuple.Item1.LineCount, text.Lines.Count);
            Assert.Equal(tuple.Item1.Length, text.Length);
            Assert.Equal(tuple.Item1.GetText(), text.ToString());
        }

        [WpfFact]
        public void GetLineFromLineNumber1()
        {
            var tuple = Create("foo", "bar");
            var text = tuple.Item2;
            var line1 = text.Lines[0];
            Assert.Equal(new TextSpan(0, 3), line1.Span);
            Assert.Equal(new TextSpan(0, 5), line1.SpanIncludingLineBreak);
            Assert.Equal("foo", line1.ToString());
        }

        [WpfFact]
        public void GetLineFromLineNumber2()
        {
            var tuple = Create("foo", "bar");
            var text = tuple.Item2;
            var line1 = text.Lines[1];
            Assert.Equal(new TextSpan(5, 3), line1.Span);
            Assert.Equal(new TextSpan(5, 3), line1.SpanIncludingLineBreak);
            Assert.Equal("bar", line1.ToString());
        }

        [WpfFact]
        public void Lines1()
        {
            var tuple = Create("foo", "bar");
            var lines = tuple.Item2.Lines;
            Assert.Equal(2, lines.Count);
        }
    }
}
