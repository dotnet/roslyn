// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.EditorConfig.Parsing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditorConfigParsing
{
    public class SectionParserTests
    {
        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs,vb}")]
        [InlineData(Language.CSharp, "*.cs")]
        [InlineData(Language.VisualBasic, "*.vb")]
        internal void TestSupportsLanguageExactCase(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsLanguage(language));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs,csx,vb,vbx}")]
        [InlineData(Language.CSharp, "*.{cs,csx}")]
        [InlineData(Language.VisualBasic, "*.{vb,vbx}")]
        internal void TestSupportsLanguageExactWithOthersCase(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsLanguage(language, matchKind: SectionMatch.ExactLanguageMatchWithOthers));
        }

        [Theory]
        [InlineData(Language.CSharp, "*.{cs,csx,vb,vbx}")]
        [InlineData(Language.VisualBasic, "*.{cs,csx,vb,vbx}")]
        internal void TestSupportsAnyLanguageCase(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsLanguage(language, matchKind: SectionMatch.AnyLanguageMatch));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*{s,x,b}")]
        [InlineData(Language.CSharp, "*s")]
        [InlineData(Language.VisualBasic, "*b")]
        internal void TestSupportsSupersetFilePatternCase(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsLanguage(language, matchKind: SectionMatch.SupersetFilePatternMatch));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*")]
        [InlineData(Language.CSharp, "*")]
        [InlineData(Language.VisualBasic, "*")]
        internal void TestSupportsLanguageSplat(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsLanguage(language, matchKind: SectionMatch.SplatMatch));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "")]
        [InlineData(Language.CSharp, "")]
        [InlineData(Language.VisualBasic, "")]
        internal void TestSupportsGlobalSectionCase(Language language, string headerText)
        {
            var section = new Section(null, true, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsLanguage(language, matchKind: SectionMatch.GlobalSectionMatch));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic))]
        [InlineData(Language.CSharp)]
        [InlineData(Language.VisualBasic)]
        internal void TestDoesNotSupportsLanguageInIsGlobalCase(Language language)
        {
            var section = new Section(null, true, default(TextSpan), string.Empty, string.Empty);
            Assert.False(section.SupportsLanguage(language));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs,csx}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{vb,vbx}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{vb}")]
        [InlineData(Language.CSharp, "*.vb")]
        [InlineData(Language.VisualBasic, "*.cs")]
        internal void TestDoesNotSupportsLanguageExactCas(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.False(section.SupportsLanguage(language));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs,csx}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{vb,vbx}")]
        [InlineData(Language.CSharp, "*.{cs,vb}")]
        [InlineData(Language.VisualBasic, "*.{cs,vb}")]
        internal void TestDoesNotSupportExactWithOthersCase(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.False(section.SupportsLanguage(language, matchKind: SectionMatch.ExactLanguageMatchWithOthers));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs,csx}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{vb,vbx}")]
        internal void TestDoesNotSupportAnyLanguageMatchCase(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.False(section.SupportsLanguage(language, matchKind: SectionMatch.AnyLanguageMatch));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.x")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{x,y}")]
        internal void TestDoesNotSupportSupersetFilePatternMatchCase(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.False(section.SupportsLanguage(language, matchKind: SectionMatch.SupersetFilePatternMatch));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs,vb}")]
        [InlineData(Language.CSharp, "*.{cs,csx,vb,bx}")]
        [InlineData(Language.CSharp, "*.{cs,b}")]
        [InlineData(Language.CSharp, "*.cs")]
        [InlineData(Language.VisualBasic, "*.{cs,csx,vb,vbx}")]
        [InlineData(Language.VisualBasic, "*.{cs,vb}")]
        [InlineData(Language.VisualBasic, "*.vb")]
        internal void TestSupportsLanguageMatchAny(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsLanguage(language, matchKind: SectionMatch.FilePatternMatch));
        }

        [Theory]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{cs,csx}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{vb}")]
        [InlineData((Language.CSharp | Language.VisualBasic), "*.{vb,vbx}")]
        [InlineData(Language.CSharp, "*.{csx,vb,vbx}")]
        [InlineData(Language.CSharp, "*.{vb}")]
        [InlineData(Language.CSharp, "*.vb")]
        [InlineData(Language.VisualBasic, "*.{cs,csx,vbx}")]
        [InlineData(Language.VisualBasic, "*.{cs}")]
        [InlineData(Language.VisualBasic, "*.cs")]
        internal void TestDoesNotSupportsLanguageMatchAny(Language language, string headerText)
        {
            var section = new Section(null, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.False(section.SupportsLanguage(language, matchKind: SectionMatch.FilePatternMatch));
        }

        [Theory]
        [InlineData("*.{cs,csx,vb,vbx}", @"C:\dev\.editorconfig", @"C:\dev\sources\Program.cs")]
        [InlineData("*.{cs,vb}", @"C:\dev\.editorconfig", @"C:\dev\sources\Program.cs")]
        [InlineData("*.{cs,csx,vb,vbx}", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*.{cs,vb}", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*.cs", @"C:\dev\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("*gram.cs", @"C:\dev\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("*gram.cs", @"C:\dev\sources\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("*gram.cs", @"C:\dev\sources\CSharp\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("Program.cs", @"C:\dev\sources\CSharp\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("sources/**/*.cs", @"C:\dev\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("*.cs", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*gram.cs", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*gram.cs", @"/dev/sources/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*gram.cs", @"/dev/sources/CSharp/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("Program.cs", @"/dev/sources/CSharp/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("sources/**/*.cs", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*.vb", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("*gram.vb", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("*gram.vb", @"C:\dev\sources\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("*gram.vb", @"C:\dev\sources\VisualBasic\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("Program.vb", @"C:\dev\sources\VisualBasic\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("sources/**/*.vb", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("*.vb", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("*gram.vb", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("*gram.vb", @"/dev/sources/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("*gram.vb", @"/dev/sources/VisualBasic/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("Program.vb", @"/dev/sources/VisualBasic/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("sources/**/*.vb", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        internal void TestSupportsFilePathSimpleCase(string headerText, string editorconfigFilePath, string codefilePath)
        {
            var section = new Section(editorconfigFilePath, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsFilePath(codefilePath, matchKind: SectionMatch.FilePatternMatch));
        }

        [Theory]
        [InlineData("*.{cs,csx,vbx}", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("*.cs", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("program.vb", @"C:\dev\sources\VisualBasic\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("program.vb", @"/dev/sources/VisualBasic/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("sources/**/*.cs", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("sources/**/*.cs", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("Sources/**/*.vb", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("Sources/**/*.vb", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("*.{vb,csx,vbx}", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*.vb", @"C:\dev\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("program.cs", @"C:\dev\sources\VisualBasic\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("program.cs", @"/dev/sources/VisualBasic/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("sources/**/*.vb", @"C:\dev\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("sources/**/*.vb", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("Sources/**/*.cs", @"C:\dev\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        [InlineData("Sources/**/*.cs", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        internal void TestDoesNotSupportFilePathSimpleCase(string headerText, string editorconfigFilePath, string codefilePath)
        {
            var section = new Section(editorconfigFilePath, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.False(section.SupportsFilePath(codefilePath, matchKind: SectionMatch.FilePatternMatch));
        }

        [Theory]
        [InlineData("*b", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("*b", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        internal void TestSupportsFilePathMatchAny(string headerText, string editorconfigFilePath, string codefilePath)
        {
            var section = new Section(editorconfigFilePath, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsFilePath(codefilePath, matchKind: SectionMatch.FilePatternMatch));
        }

        [Theory]
        [InlineData("*", @"/dev/.editorconfig", @"/dev/sources/VisualBasic/Program.vb")]
        [InlineData("*", @"/dev/.editorconfig", @"/dev/sources/CSharp/Program.cs")]
        [InlineData("*", @"C:\dev\.editorconfig", @"C:\dev\sources\VisualBasic\Program.vb")]
        [InlineData("*", @"C:\dev\.editorconfig", @"C:\dev\sources\CSharp\Program.cs")]
        internal void TestSupportsSplat(string headerText, string editorconfigFilePath, string codefilePath)
        {
            var section = new Section(editorconfigFilePath, false, default(TextSpan), headerText, $"[{headerText}]");
            Assert.True(section.SupportsFilePath(codefilePath, matchKind: SectionMatch.SplatMatch));
        }
    }
}
