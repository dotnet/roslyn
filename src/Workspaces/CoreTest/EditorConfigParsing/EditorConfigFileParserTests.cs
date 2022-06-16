// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.EditorConfig.Parsing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditorConfigParsing
{
    public class EditorConfigFileParserTests
    {
        internal static EditorConfigFile<EditorConfigOption> CreateParseResults(string editorconfigFilePath, params (string headerText, TextSpan span, bool isGlobal)[] sections)
        {
            var list = new List<EditorConfigOption>();
            foreach (var (headerText, span, isGlobal) in sections)
            {
                var section = new Section(editorconfigFilePath, isGlobal, span, headerText, $"[{headerText}]");
                var parseResult = new EditorConfigOption(section, null);
                list.Add(parseResult);
            }

            return new EditorConfigFile<EditorConfigOption>(editorconfigFilePath, list.ToImmutableArray());
        }

        [Fact]
        internal void TestGetBestSection()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*.cs", TextSpan.FromBounds(10, 19), false),
                ("*.vb", TextSpan.FromBounds(20, 29), false),
                ("*.{cs,vb}", TextSpan.FromBounds(30, 39), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false));

            Assert.True(parseResults.TryGetSectionForLanguage(Language.CSharp, out var section));
            Assert.Equal(10, section?.Span.Start);
            Assert.Equal("*.cs", section?.Text);

            Assert.True(parseResults.TryGetSectionForLanguage(Language.VisualBasic, out section));
            Assert.Equal(20, section?.Span.Start);
            Assert.Equal("*.vb", section?.Text);

            Assert.True(parseResults.TryGetSectionForLanguage((Language.CSharp | Language.VisualBasic), out section));
            Assert.Equal(30, section?.Span.Start);
            Assert.Equal("*.{cs,vb}", section?.Text);
        }

        [Fact]
        internal void TestNoBestSection()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*.vb", TextSpan.FromBounds(20, 29), false),
                ("*.{cs,vb}", TextSpan.FromBounds(30, 39), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*", TextSpan.FromBounds(60, 69), false),
                ("*.{cs,csx}", TextSpan.FromBounds(70, 79), false));

            Assert.False(parseResults.TryGetSectionForLanguage(Language.CSharp, out _));
        }

        [Fact]
        internal void TestGetBestSectionWithDifferMatchCriteria()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*.vb", TextSpan.FromBounds(20, 29), false),
                ("*.{cs,vb}", TextSpan.FromBounds(30, 39), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*", TextSpan.FromBounds(60, 69), false),
                ("*.{cs,csx}", TextSpan.FromBounds(70, 79), false));

            Assert.True(parseResults.TryGetSectionForLanguage(Language.CSharp, SectionMatch.ExactLanguageMatchWithOthers, out var section));
            Assert.Equal(70, section?.Span.Start);
            Assert.Equal("*.{cs,csx}", section?.Text);
        }

        [Fact]
        internal void TestGetBestSectionWithAnyPattern()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*.vb", TextSpan.FromBounds(20, 29), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*", TextSpan.FromBounds(60, 69), false),
                ("*.{cs,csx}", TextSpan.FromBounds(70, 79), false));

            Assert.True(parseResults.TryGetSectionForLanguage(Language.CSharp, SectionMatch.Any, out var section));
            Assert.Equal(70, section?.Span.Start);
            Assert.Equal("*.{cs,csx}", section?.Text);

            Assert.True(parseResults.TryGetSectionForLanguage(Language.VisualBasic, SectionMatch.Any, out section));
            Assert.Equal(20, section?.Span.Start);
            Assert.Equal("*.vb", section?.Text);

            Assert.True(parseResults.TryGetSectionForLanguage((Language.CSharp | Language.VisualBasic), SectionMatch.Any, out section));
            Assert.Equal(40, section?.Span.Start);
            Assert.Equal("*.{cs,csx,vb,vbx}", section?.Text);
        }

        [Fact]
        internal void TestGetBestSectionForFile()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*.cs", TextSpan.FromBounds(10, 19), false),
                ("*.vb", TextSpan.FromBounds(20, 29), false),
                ("*.{cs,vb}", TextSpan.FromBounds(30, 39), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*", TextSpan.FromBounds(60, 69), false),
                ("*.{cs,csx}", TextSpan.FromBounds(70, 79), false));

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\CSharp\Program.cs", out var section));
            Assert.Equal(10, section?.Span.Start);
            Assert.Equal("*.cs", section?.Text);

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\VisualBasic\Program.vb", out section));
            Assert.Equal(20, section?.Span.Start);
            Assert.Equal("*.vb", section?.Text);
        }

        [Fact]
        internal void TestGetBestSectionForFilePatternWithDefaults()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("sources/**/*.cs", TextSpan.FromBounds(10, 19), false),
                ("sources/**/*.vb", TextSpan.FromBounds(20, 29), false),
                ("*.{cs,vb}", TextSpan.FromBounds(30, 39), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*", TextSpan.FromBounds(60, 69), false),
                ("*.{cs,csx}", TextSpan.FromBounds(70, 79), false));

            Assert.False(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\CSharp\Program.cs", out _));
            Assert.False(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\VisualBasic\Program.vb", out _));
        }

        [Fact]
        internal void TestGetBestSectionForFilePattern()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("sources/**/*.cs", TextSpan.FromBounds(10, 19), false),
                ("sources/**/*.vb", TextSpan.FromBounds(20, 29), false),
                ("*.{cs,vb}", TextSpan.FromBounds(30, 39), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*", TextSpan.FromBounds(60, 69), false),
                ("*.{cs,csx}", TextSpan.FromBounds(70, 79), false));

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\CSharp\Program.cs", SectionMatch.Any, out var section));
            Assert.Equal(70, section?.Span.Start);
            Assert.Equal("*.{cs,csx}", section?.Text);

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\VisualBasic\Program.vb", SectionMatch.Any, out section));
            Assert.Equal(40, section?.Span.Start);
            Assert.Equal("*.{cs,csx,vb,vbx}", section?.Text);
        }

        [Fact]
        internal void TestGetBestSectionForFilePatternTie()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*.{cs,csx,vbx}", TextSpan.FromBounds(30, 39), false),
                ("*.{cs,csx,vb,vbx}", TextSpan.FromBounds(40, 49), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*", TextSpan.FromBounds(60, 69), false));

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\CSharp\Program.cs", SectionMatch.Any, out var section));
            Assert.Equal(30, section?.Span.Start);
            Assert.Equal("*.{cs,csx,vbx}", section?.Text);

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\VisualBasic\Program.vb", SectionMatch.Any, out section));
            Assert.Equal(40, section?.Span.Start);
            Assert.Equal("*.{cs,csx,vb,vbx}", section?.Text);
        }

        [Fact]
        internal void TestGetBestSectionForSuperetFilePatternTie()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*.*b", TextSpan.FromBounds(30, 39), false),
                ("*.*b", TextSpan.FromBounds(80, 89), false),
                ("*.*b", TextSpan.FromBounds(130, 139), false),
                ("*.*s", TextSpan.FromBounds(40, 49), false),
                ("*.*s", TextSpan.FromBounds(90, 99), false),
                ("*.*s", TextSpan.FromBounds(120, 129), false),
                ("*s", TextSpan.FromBounds(50, 59), false),
                ("*s", TextSpan.FromBounds(100, 109), false),
                ("*", TextSpan.FromBounds(60, 69), false),
                ("*b", TextSpan.FromBounds(70, 79), false),
                ("*b", TextSpan.FromBounds(110, 119), false));

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\CSharp\Program.cs", SectionMatch.Any, out var section));
            Assert.Equal(120, section?.Span.Start);
            Assert.Equal("*.*s", section?.Text);

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\VisualBasic\Program.vb", SectionMatch.Any, out section));
            Assert.Equal(130, section?.Span.Start);
            Assert.Equal("*.*b", section?.Text);
        }

        [Fact]
        internal void TestGetBestSectionWithSplat()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*s", TextSpan.FromBounds(100, 109), false),
                ("*", TextSpan.FromBounds(60, 69), false));

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\CSharp\Program.cs", SectionMatch.Any, out var section));
            Assert.Equal(100, section?.Span.Start);
            Assert.Equal("*s", section?.Text);

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\VisualBasic\Program.vb", SectionMatch.Any, out section));
            Assert.Equal(60, section?.Span.Start);
            Assert.Equal("*", section?.Text);
        }

        [Fact]
        internal void TestGetBestSectionWithGlobalSection()
        {
            var parseResults = CreateParseResults(@"C:\dev\.editorconfig",
                (string.Empty, TextSpan.FromBounds(0, 9), true),
                ("*s", TextSpan.FromBounds(100, 109), false));

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\CSharp\Program.cs", SectionMatch.Any, out var section));
            Assert.Equal(100, section?.Span.Start);
            Assert.Equal("*s", section?.Text);

            Assert.True(parseResults.TryGetSectionForFilePath(@"C:\dev\sources\VisualBasic\Program.vb", SectionMatch.Any, out section));
            Assert.Equal(0, section?.Span.Start);
            Assert.Equal(string.Empty, section?.Text);
        }
    }
}
