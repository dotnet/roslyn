// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.AnalyzerConfig;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EditorConfigTests
    {
        #region Parsing Tests

        private static AnalyzerConfig ParseConfigFile(string text) => Parse(text, "/.editorconfig");

        [Fact]
        public void SimpleCase()
        {
            var config = Parse(@"
root = true

# Comment1
# Comment2
##################################

my_global_prop = my_global_val

[*.cs]
my_prop = my_val
", "/bogus/.editorconfig");

            Assert.Equal("", config.GlobalSection.Name);
            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_global_prop", "my_global_val"),
                        KeyValuePair.Create("root", "true") },
                properties);

            var namedSections = config.NamedSections;
            Assert.Equal("*.cs", namedSections[0].Name);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val") },
                namedSections[0].Properties);

            Assert.True(config.IsRoot);

            Assert.Equal("/bogus", config.NormalizedDirectory);
        }

        [Fact]
        [WorkItem(52469, "https://github.com/dotnet/roslyn/issues/52469")]
        public void ConfigWithEscapedValues()
        {
            var config = ParseConfigFile(@"is_global = true

[C:/\{f\*i\?le1\}.cs]
build_metadata.Compile.ToRetrieve = abc123

[C:/f\,ile\#2.cs]
build_metadata.Compile.ToRetrieve = def456

[C:/f\;i\!le\[3\].cs]
build_metadata.Compile.ToRetrieve = ghi789
");

            var namedSections = config.NamedSections;
            Assert.Equal("C:/\\{f\\*i\\?le1\\}.cs", namedSections[0].Name);
            AssertEx.Equal(
                new[] { KeyValuePair.Create("build_metadata.compile.toretrieve", "abc123") },
                namedSections[0].Properties
            );

            Assert.Equal("C:/f\\,ile\\#2.cs", namedSections[1].Name);
            AssertEx.Equal(
                new[] { KeyValuePair.Create("build_metadata.compile.toretrieve", "def456") },
                namedSections[1].Properties
            );

            Assert.Equal("C:/f\\;i\\!le\\[3\\].cs", namedSections[2].Name);
            AssertEx.Equal(
                new[] { KeyValuePair.Create("build_metadata.compile.toretrieve", "ghi789") },
                namedSections[2].Properties
            );
        }

        [Fact]
        [WorkItem(52469, "https://github.com/dotnet/roslyn/issues/52469")]
        public void CanGetSectionsWithSpecialCharacters()
        {
            var config = ParseConfigFile(@"is_global = true

[/home/foo/src/\{releaseid\}.cs]
build_metadata.Compile.ToRetrieve = abc123

[/home/foo/src/Pages/\#foo/HomePage.cs]
build_metadata.Compile.ToRetrieve = def456
");

            var set = AnalyzerConfigSet.Create(ImmutableArray.Create(config));

            var sectionOptions = set.GetOptionsForSourcePath("/home/foo/src/{releaseid}.cs");
            Assert.Equal("abc123", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);

            sectionOptions = set.GetOptionsForSourcePath("/home/foo/src/Pages/#foo/HomePage.cs");
            Assert.Equal("def456", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void CanGetSectionsWithDifferentDriveCasing()
        {
            var config = Parse(@"is_global = true
build_metadata.compile.toretrieve = global

[c:/goo/file.cs]
build_metadata.compile.toretrieve = abc123

[C:/goo/other.cs]
build_metadata.compile.toretrieve = def456
", pathToFile: @"C:/.editorconfig");

            var set = AnalyzerConfigSet.Create(ImmutableArray.Create(config));

            var sectionOptions = set.GetOptionsForSourcePath(@"c:\goo\file.cs");
            Assert.Equal("abc123", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);

            sectionOptions = set.GetOptionsForSourcePath(@"C:\goo\file.cs");
            Assert.Equal("abc123", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);

            sectionOptions = set.GetOptionsForSourcePath(@"C:\goo\other.cs");
            Assert.Equal("def456", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);

            sectionOptions = set.GetOptionsForSourcePath(@"c:\goo\other.cs");
            Assert.Equal("def456", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);

            sectionOptions = set.GetOptionsForSourcePath(@"c:\global.cs");
            Assert.Equal("global", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);

            sectionOptions = set.GetOptionsForSourcePath(@"C:\global.cs");
            Assert.Equal("global", sectionOptions.AnalyzerOptions["build_metadata.compile.toretrieve"]);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath_UppercaseDriveLetter_NormalizedDirectory()
        {
            var config = Parse("", @"Z:\\bogus\\.editorconfig");
            Assert.Equal("Z:/bogus", config.NormalizedDirectory);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath_UppercaseDriveLetter_PathToFileIsPreserved()
        {
            const string path = @"Z:\\bogus\\.editorconfig";
            var config = Parse("", path);
            Assert.Equal(path, config.PathToFile);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath_LowercaseDriveLetter_NormalizedDirectory()
        {
            var config = Parse("", @"z:\\bogus\\.editorconfig");
            Assert.Equal("Z:/bogus", config.NormalizedDirectory);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath_LowercaseDriveLetter_PathToFilePreservedOnlyDriveNormalized()
        {
            var config = Parse("", @"z:\\bogus\\.editorconfig");
            Assert.Equal(@"Z:\\bogus\\.editorconfig", config.PathToFile);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath_CaseMismatchDoesNotMatch()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse("""
                [*.cs]
                my_prop = my_val
                """, @"C:\Repo\src\shared\.editorconfig"));

            var configSet = AnalyzerConfigSet.Create(configs, out var diagnostics);
            configs.Free();

            // Path comparison is deliberately case-sensitive: a path can be both
            // case-sensitive and case-insensitive in different segments (e.g. WSL-mounted
            // directories), so the compiler uses ordinal comparison for consistency.
            var options = configSet.GetOptionsForSourcePath(@"C:\Repo\src\Shared\Foo.cs");
            Assert.Empty(options.AnalyzerOptions);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath_ExactCasingMatches()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse("""
                [*.cs]
                my_prop = my_val
                """, @"C:\Repo\src\shared\.editorconfig"));

            var configSet = AnalyzerConfigSet.Create(configs, out var diagnostics);
            configs.Free();

            var options = configSet.GetOptionsForSourcePath(@"C:\Repo\src\shared\Foo.cs");
            Assert.Equal("my_val", options.AnalyzerOptions["my_prop"]);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath_NormalizedDirectoryIsFullyNormalized()
        {
            var config = Parse("", @"C:\Repo\\Src\..\Src/Shared\.editorconfig");

            // Should be collapsed and expanded, but casing is preserved (case-sensitive by design)
            Assert.Equal("C:/Repo/Src/Shared", config.NormalizedDirectory);
        }

        [Fact]
        public void MissingClosingBracket()
        {
            var config = ParseConfigFile(@"
[*.cs
my_prop = my_val");
            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val") },
                properties);

            Assert.Equal(0, config.NamedSections.Length);
        }

        [Fact]
        public void EmptySection()
        {
            var config = ParseConfigFile(@"
[]
my_prop = my_val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(new[] { KeyValuePair.Create("my_prop", "my_val") }, properties);
            Assert.Equal(0, config.NamedSections.Length);
        }

        [Fact]
        public void CaseInsensitivePropKey()
        {
            var config = ParseConfigFile(@"
my_PROP = my_VAL");
            var properties = config.GlobalSection.Properties;

            Assert.True(properties.TryGetValue("my_PrOp", out var val));
            Assert.Equal("my_VAL", val);
            Assert.Equal("my_prop", properties.Keys.Single());
        }

        [Fact]
        public void NonReservedKeyPreservedCaseVal()
        {
            var config = ParseConfigFile(string.Join(Environment.NewLine,
                AnalyzerConfig.ReservedKeys.Select(k => "MY_" + k + " = MY_VAL")));
            AssertEx.SetEqual(
                AnalyzerConfig.ReservedKeys.Select(k => KeyValuePair.Create("my_" + k, "MY_VAL")).ToList(),
                config.GlobalSection.Properties);
        }

        [Fact]
        public void DuplicateKeys()
        {
            var config = ParseConfigFile(@"
my_prop = my_val
my_prop = my_other_val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(new[] { KeyValuePair.Create("my_prop", "my_other_val") }, properties);
        }

        [Fact]
        public void DuplicateKeysCasing()
        {
            var config = ParseConfigFile(@"
my_prop = my_val
my_PROP = my_other_val");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(new[] { KeyValuePair.Create("my_prop", "my_other_val") }, properties);
        }

        [Fact]
        public void MissingKey()
        {
            var config = ParseConfigFile(@"
= my_val1
my_prop = my_val2");

            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val2") },
                properties);
        }

        [Fact]
        public void MissingVal()
        {
            var config = ParseConfigFile(@"
my_prop1 =
my_prop2 = my_val");

            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop1", ""),
                        KeyValuePair.Create("my_prop2", "my_val") },
                properties);
        }

        [Fact]
        public void SpacesInProperties()
        {
            var config = ParseConfigFile(@"
my prop1 = my_val1
my_prop2 = my val2");

            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop2", "my val2") },
                properties);
        }

        [Fact]
        public void EndOfLineComments()
        {
            var config = ParseConfigFile(@"
my_prop2 = my val2 # Comment");

            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop2", "my val2") },
                properties);
        }

        [Fact]
        public void SymbolsStartKeys()
        {
            var config = ParseConfigFile(@"
@!$abc = my_val1
@!$\# = my_val2");

            var properties = config.GlobalSection.Properties;
            Assert.Equal(0, properties.Count);
        }

        [Fact]
        public void EqualsAndColon()
        {
            var config = ParseConfigFile(@"
my:key1 = my_val
my_key2 = my:val");

            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my", "key1 = my_val"),
                        KeyValuePair.Create("my_key2", "my:val")},
                properties);
        }

        [Fact]
        public void SymbolsInProperties()
        {
            var config = ParseConfigFile(@"
my@key1 = my_val
my_key2 = my@val");

            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_key2", "my@val") },
                properties);
        }

        [Fact]
        public void LongLines()
        {
            // This example is described in the Python ConfigParser as allowing
            // line continuation via the RFC 822 specification, section 3.1.1
            // LONG HEADER FIELDS. The VS parser does not accept this as a
            // valid parse for an editorconfig file. We follow similarly.
            var config = ParseConfigFile(@"
long: this value continues
   in the next line");

            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("long", "this value continues") },
                properties);
        }

        [Fact]
        public void CaseInsensitiveRoot()
        {
            var config = ParseConfigFile(@"
RoOt = TruE");
            Assert.True(config.IsRoot);
        }

        [Fact]
        public void ReservedValues()
        {
            int index = 0;
            var config = ParseConfigFile(string.Join(Environment.NewLine,
                AnalyzerConfig.ReservedValues.Select(v => "MY_KEY" + (index++) + " = " + v.ToUpperInvariant())));
            index = 0;
            AssertEx.SetEqual(
                AnalyzerConfig.ReservedValues.Select(v => KeyValuePair.Create("my_key" + (index++), v)).ToList(),
                config.GlobalSection.Properties);
        }

        [Fact]
        public void ReservedKeys()
        {
            var config = ParseConfigFile(string.Join(Environment.NewLine,
                AnalyzerConfig.ReservedKeys.Select(k => k + " = MY_VAL")));
            AssertEx.SetEqual(
                AnalyzerConfig.ReservedKeys.Select(k => KeyValuePair.Create(k, "my_val")).ToList(),
                config.GlobalSection.Properties);
        }

        #endregion

        #region Section Matching Tests

        [Fact]
        public void SimpleNameMatch()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("abc").Value;
            Assert.Equal("^.*/abc$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc"));
            Assert.False(matcher.IsMatch("/aabc"));
            Assert.False(matcher.IsMatch("/ abc"));
            Assert.False(matcher.IsMatch("/cabc"));
        }

        [Fact]
        public void StarOnlyMatch()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("*").Value;
            Assert.Equal("^.*/[^/]*$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc"));
            Assert.True(matcher.IsMatch("/123"));
            Assert.True(matcher.IsMatch("/abc/123"));
        }

        [Fact]
        public void StarNameMatch()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("*.cs").Value;
            Assert.Equal("^.*/[^/]*\\.cs$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc.cs"));
            Assert.True(matcher.IsMatch("/123.cs"));
            Assert.True(matcher.IsMatch("/dir/subpath.cs"));
            // Only '/' is defined as a directory separator, so the caller
            // is responsible for converting any other machine directory
            // separators to '/' before matching
            Assert.True(matcher.IsMatch("/dir\\subpath.cs"));

            Assert.False(matcher.IsMatch("/abc.vb"));
        }

        [Fact]
        public void StarStarNameMatch()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("**.cs").Value;
            Assert.Equal("^.*/.*\\.cs$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc.cs"));
            Assert.True(matcher.IsMatch("/dir/subpath.cs"));
        }

        [Fact]
        public void EscapeDot()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("...").Value;
            Assert.Equal("^.*/\\.\\.\\.$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/..."));
            Assert.True(matcher.IsMatch("/subdir/..."));
            Assert.False(matcher.IsMatch("/aaa"));
            Assert.False(matcher.IsMatch("/???"));
            Assert.False(matcher.IsMatch("/abc"));
        }

        [Fact]
        public void EndBackslashMatch()
        {
            SectionNameMatcher? matcher = TryCreateSectionNameMatcher("abc\\");
            Assert.Null(matcher);
        }

        [Fact]
        public void QuestionMatch()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab?def").Value;
            Assert.Equal("^.*/ab.def$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abcdef"));
            Assert.True(matcher.IsMatch("/ab?def"));
            Assert.True(matcher.IsMatch("/abzdef"));
            Assert.True(matcher.IsMatch("/ab/def"));
            Assert.True(matcher.IsMatch("/ab\\def"));
        }

        [Fact]
        public void LiteralBackslash()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab\\\\c").Value;
            Assert.Equal("^.*/ab\\\\c$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/ab\\c"));
            Assert.False(matcher.IsMatch("/ab/c"));
            Assert.False(matcher.IsMatch("/ab\\\\c"));
        }

        [Fact]
        public void LiteralStars()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("\\***\\*\\**").Value;
            Assert.Equal("^.*/\\*.*\\*\\*[^/]*$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/*ab/cd**efg*"));
            Assert.False(matcher.IsMatch("/ab/cd**efg*"));
            Assert.False(matcher.IsMatch("/*ab/cd*efg*"));
            Assert.False(matcher.IsMatch("/*ab/cd**ef/gh"));
        }

        [Fact]
        public void LiteralQuestions()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("\\??\\?*\\??").Value;
            Assert.Equal("^.*/\\?.\\?[^/]*\\?.$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/?a?cde?f"));
            Assert.True(matcher.IsMatch("/???????f"));
            Assert.False(matcher.IsMatch("/aaaaaaaa"));
            Assert.False(matcher.IsMatch("/aa?cde?f"));
            Assert.False(matcher.IsMatch("/?a?cdexf"));
            Assert.False(matcher.IsMatch("/?axcde?f"));
        }

        [Fact]
        public void LiteralBraces()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("abc\\{\\}def").Value;
            Assert.Equal(@"^.*/abc\{}def$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc{}def"));
            Assert.True(matcher.IsMatch("/subdir/abc{}def"));
            Assert.False(matcher.IsMatch("/abcdef"));
            Assert.False(matcher.IsMatch("/abc}{def"));
        }

        [Fact]
        public void LiteralComma()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("abc\\,def").Value;
            Assert.Equal("^.*/abc,def$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc,def"));
            Assert.True(matcher.IsMatch("/subdir/abc,def"));
            Assert.False(matcher.IsMatch("/abcdef"));
            Assert.False(matcher.IsMatch("/abc\\,def"));
            Assert.False(matcher.IsMatch("/abc`def"));
        }

        [Fact]
        public void SimpleChoice()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("*.{cs,vb,fs}").Value;
            Assert.Equal("^.*/[^/]*\\.(?:cs|vb|fs)$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc.cs"));
            Assert.True(matcher.IsMatch("/abc.vb"));
            Assert.True(matcher.IsMatch("/abc.fs"));
            Assert.True(matcher.IsMatch("/subdir/abc.cs"));
            Assert.True(matcher.IsMatch("/subdir/abc.vb"));
            Assert.True(matcher.IsMatch("/subdir/abc.fs"));

            Assert.False(matcher.IsMatch("/abcxcs"));
            Assert.False(matcher.IsMatch("/abcxvb"));
            Assert.False(matcher.IsMatch("/abcxfs"));
            Assert.False(matcher.IsMatch("/subdir/abcxcs"));
            Assert.False(matcher.IsMatch("/subdir/abcxcb"));
            Assert.False(matcher.IsMatch("/subdir/abcxcs"));
        }

        [Fact]
        public void OneChoiceHasSlashes()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("{*.cs,subdir/test.vb}").Value;
            // This is an interesting case that may be counterintuitive.  A reasonable understanding
            // of the section matching could interpret the choice as generating multiple identical
            // sections, so [{a, b, c}] would be equivalent to [a] ... [b] ... [c] with all of the
            // same properties in each section. This is somewhat true, but the rules of how the matching
            // prefixes are constructed violate this assumption because they are defined as whether or
            // not a section contains a slash, not whether any of the choices contain a slash. So while
            // [*.cs] usually translates into '**/*.cs' because it contains no slashes, the slashes in
            // the second choice make this into '/*.cs', effectively matching only files in the root
            // directory of the match, instead of all subdirectories.
            Assert.Equal("^/(?:[^/]*\\.cs|subdir/test\\.vb)$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/test.cs"));
            Assert.True(matcher.IsMatch("/subdir/test.vb"));

            Assert.False(matcher.IsMatch("/subdir/test.cs"));
            Assert.False(matcher.IsMatch("/subdir/subdir/test.vb"));
            Assert.False(matcher.IsMatch("/test.vb"));
        }

        [Fact]
        public void EmptyChoice()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("{}").Value;
            Assert.Equal("^.*/(?:)$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/"));
            Assert.True(matcher.IsMatch("/subdir/"));
            Assert.False(matcher.IsMatch("/."));
            Assert.False(matcher.IsMatch("/anything"));
        }

        [Fact]
        public void SingleChoice()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("{*.cs}").Value;
            Assert.Equal("^.*/(?:[^/]*\\.cs)$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/test.cs"));
            Assert.True(matcher.IsMatch("/subdir/test.cs"));
            Assert.False(matcher.IsMatch("test.vb"));
            Assert.False(matcher.IsMatch("testxcs"));
        }

        [Fact]
        public void UnmatchedBraces()
        {
            SectionNameMatcher? matcher = TryCreateSectionNameMatcher("{{{{}}");
            Assert.Null(matcher);
        }

        [Fact]
        public void CommaOutsideBraces()
        {
            SectionNameMatcher? matcher = TryCreateSectionNameMatcher("abc,def");
            Assert.Null(matcher);
        }

        [Fact]
        public void RecursiveChoice()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("{test{.cs,.vb},other.{a{bb,cc}}}").Value;
            Assert.Equal("^.*/(?:test(?:\\.cs|\\.vb)|other\\.(?:a(?:bb|cc)))$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/test.cs"));
            Assert.True(matcher.IsMatch("/test.vb"));
            Assert.True(matcher.IsMatch("/subdir/test.cs"));
            Assert.True(matcher.IsMatch("/subdir/test.vb"));
            Assert.True(matcher.IsMatch("/other.abb"));
            Assert.True(matcher.IsMatch("/other.acc"));

            Assert.False(matcher.IsMatch("/test.fs"));
            Assert.False(matcher.IsMatch("/other.bbb"));
            Assert.False(matcher.IsMatch("/other.ccc"));
            Assert.False(matcher.IsMatch("/subdir/other.bbb"));
            Assert.False(matcher.IsMatch("/subdir/other.ccc"));
        }

        [Fact]
        public void DashChoice()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab{-}cd{-,}ef").Value;
            Assert.Equal("^.*/ab(?:-)cd(?:-|)ef$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/ab-cd-ef"));
            Assert.True(matcher.IsMatch("/ab-cdef"));

            Assert.False(matcher.IsMatch("/abcdef"));
            Assert.False(matcher.IsMatch("/ab--cd-ef"));
            Assert.False(matcher.IsMatch("/ab--cd--ef"));
        }

        [Fact]
        public void MiddleMatch()
        {
            SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab{cs,vb,fs}cd").Value;
            Assert.Equal("^.*/ab(?:cs|vb|fs)cd$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abcscd"));
            Assert.True(matcher.IsMatch("/abvbcd"));
            Assert.True(matcher.IsMatch("/abfscd"));

            Assert.False(matcher.IsMatch("/abcs"));
            Assert.False(matcher.IsMatch("/abcd"));
            Assert.False(matcher.IsMatch("/vbcd"));
        }

        private static IEnumerable<(string, string)> RangeAndInverse(string s1, string s2)
        {
            yield return (s1, s2);
            yield return (s2, s1);
        }

        [Fact]
        public void NumberMatch()
        {
            foreach (var (i1, i2) in RangeAndInverse("0", "10"))
            {
                var matcher = TryCreateSectionNameMatcher($"{{{i1}..{i2}}}").Value;

                Assert.True(matcher.IsMatch("/0"));
                Assert.True(matcher.IsMatch("/10"));
                Assert.True(matcher.IsMatch("/5"));
                Assert.True(matcher.IsMatch("/000005"));
                Assert.False(matcher.IsMatch("/-1"));
                Assert.False(matcher.IsMatch("/-00000001"));
                Assert.False(matcher.IsMatch("/11"));
            }
        }

        [Fact]
        public void NumberMatchNegativeRange()
        {
            foreach (var (i1, i2) in RangeAndInverse("-10", "0"))
            {
                var matcher = TryCreateSectionNameMatcher($"{{{i1}..{i2}}}").Value;

                Assert.True(matcher.IsMatch("/0"));
                Assert.True(matcher.IsMatch("/-10"));
                Assert.True(matcher.IsMatch("/-5"));
                Assert.False(matcher.IsMatch("/1"));
                Assert.False(matcher.IsMatch("/-11"));
                Assert.False(matcher.IsMatch("/--0"));
            }
        }

        [Fact]
        public void NumberMatchNegToPos()
        {
            foreach (var (i1, i2) in RangeAndInverse("-10", "10"))
            {
                var matcher = TryCreateSectionNameMatcher($"{{{i1}..{i2}}}").Value;

                Assert.True(matcher.IsMatch("/0"));
                Assert.True(matcher.IsMatch("/-5"));
                Assert.True(matcher.IsMatch("/5"));
                Assert.True(matcher.IsMatch("/-10"));
                Assert.True(matcher.IsMatch("/10"));
                Assert.False(matcher.IsMatch("/-11"));
                Assert.False(matcher.IsMatch("/11"));
                Assert.False(matcher.IsMatch("/--0"));
            }
        }

        [Fact]
        public void MultipleNumberRanges()
        {
            foreach (var matchString in new[] { "a{-10..0}b{0..10}", "a{0..-10}b{10..0}" })
            {
                var matcher = TryCreateSectionNameMatcher(matchString).Value;

                Assert.True(matcher.IsMatch("/a0b0"));
                Assert.True(matcher.IsMatch("/a-5b0"));
                Assert.True(matcher.IsMatch("/a-5b5"));
                Assert.True(matcher.IsMatch("/a-5b10"));
                Assert.True(matcher.IsMatch("/a-10b10"));
                Assert.True(matcher.IsMatch("/a-10b0"));
                Assert.True(matcher.IsMatch("/a-0b0"));
                Assert.True(matcher.IsMatch("/a-0b-0"));

                Assert.False(matcher.IsMatch("/a-11b10"));
                Assert.False(matcher.IsMatch("/a-11b10"));
                Assert.False(matcher.IsMatch("/a-10b11"));
            }
        }

        [Fact]
        public void BadNumberRanges()
        {
            var matcherOpt = TryCreateSectionNameMatcher("{0..");

            Assert.Null(matcherOpt);

            var matcher = TryCreateSectionNameMatcher("{0..}").Value;

            Assert.True(matcher.IsMatch("/0.."));
            Assert.False(matcher.IsMatch("/0"));
            Assert.False(matcher.IsMatch("/0."));
            Assert.False(matcher.IsMatch("/0abc"));

            matcher = TryCreateSectionNameMatcher("{0..A}").Value;
            Assert.True(matcher.IsMatch("/0..A"));
            Assert.False(matcher.IsMatch("/0"));
            Assert.False(matcher.IsMatch("/0abc"));

            // The reference implementation uses atoi here so we can presume
            // numbers out of range of Int32 are not well supported
            matcherOpt = TryCreateSectionNameMatcher($"{{0..{UInt32.MaxValue}}}");

            Assert.Null(matcherOpt);
        }

        [Fact]
        public void CharacterClassSimple()
        {
            var matcher = TryCreateSectionNameMatcher("*.[cf]s").Value;
            Assert.Equal(@"^.*/[^/]*\.[cf]s$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc.cs"));
            Assert.True(matcher.IsMatch("/abc.fs"));
            Assert.False(matcher.IsMatch("/abc.vs"));
        }

        [Fact]
        public void CharacterClassNegative()
        {
            var matcher = TryCreateSectionNameMatcher("*.[!cf]s").Value;
            Assert.Equal(@"^.*/[^/]*\.[^cf]s$", matcher.Regex.ToString());

            Assert.False(matcher.IsMatch("/abc.cs"));
            Assert.False(matcher.IsMatch("/abc.fs"));
            Assert.True(matcher.IsMatch("/abc.vs"));
            Assert.True(matcher.IsMatch("/abc.xs"));
            Assert.False(matcher.IsMatch("/abc.vxs"));
        }

        [Fact]
        public void CharacterClassCaret()
        {
            var matcher = TryCreateSectionNameMatcher("*.[^cf]s").Value;
            Assert.Equal(@"^.*/[^/]*\.[\^cf]s$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/abc.cs"));
            Assert.True(matcher.IsMatch("/abc.fs"));
            Assert.True(matcher.IsMatch("/abc.^s"));
            Assert.False(matcher.IsMatch("/abc.vs"));
            Assert.False(matcher.IsMatch("/abc.xs"));
            Assert.False(matcher.IsMatch("/abc.vxs"));
        }

        [Fact]
        public void CharacterClassRange()
        {
            var matcher = TryCreateSectionNameMatcher("[0-9]x").Value;
            Assert.Equal("^.*/[0-9]x$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/0x"));
            Assert.True(matcher.IsMatch("/1x"));
            Assert.True(matcher.IsMatch("/9x"));
            Assert.False(matcher.IsMatch("/yx"));
            Assert.False(matcher.IsMatch("/00x"));
        }

        [Fact]
        public void CharacterClassNegativeRange()
        {
            var matcher = TryCreateSectionNameMatcher("[!0-9]x").Value;
            Assert.Equal("^.*/[^0-9]x$", matcher.Regex.ToString());

            Assert.False(matcher.IsMatch("/0x"));
            Assert.False(matcher.IsMatch("/1x"));
            Assert.False(matcher.IsMatch("/9x"));
            Assert.True(matcher.IsMatch("/yx"));
            Assert.False(matcher.IsMatch("/00x"));
        }

        [Fact]
        public void CharacterClassRangeAndChoice()
        {
            var matcher = TryCreateSectionNameMatcher("[ab0-9]x").Value;
            Assert.Equal("^.*/[ab0-9]x$", matcher.Regex.ToString());

            Assert.True(matcher.IsMatch("/ax"));
            Assert.True(matcher.IsMatch("/bx"));
            Assert.True(matcher.IsMatch("/0x"));
            Assert.True(matcher.IsMatch("/1x"));
            Assert.True(matcher.IsMatch("/9x"));
            Assert.False(matcher.IsMatch("/yx"));
            Assert.False(matcher.IsMatch("/0ax"));
        }

        [Fact]
        public void CharacterClassOpenEnded()
        {
            var matcher = TryCreateSectionNameMatcher("[");
            Assert.Null(matcher);
        }

        [Fact]
        public void CharacterClassEscapedOpenEnded()
        {
            var matcher = TryCreateSectionNameMatcher(@"[\]");
            Assert.Null(matcher);
        }

        [Fact]
        public void CharacterClassEscapeAtEnd()
        {
            var matcher = TryCreateSectionNameMatcher(@"[\");
            Assert.Null(matcher);
        }

        [Fact]
        public void CharacterClassOpenBracketInside()
        {
            var matcher = TryCreateSectionNameMatcher(@"[[a]bc").Value;

            Assert.True(matcher.IsMatch("/abc"));
            Assert.True(matcher.IsMatch("/[bc"));
            Assert.False(matcher.IsMatch("/ab"));
            Assert.False(matcher.IsMatch("/[b"));
            Assert.False(matcher.IsMatch("/bc"));
            Assert.False(matcher.IsMatch("/ac"));
            Assert.False(matcher.IsMatch("/[c"));

            Assert.Equal(@"^.*/[\[a]bc$", matcher.Regex.ToString());
        }

        [Fact]
        public void CharacterClassStartingDash()
        {
            var matcher = TryCreateSectionNameMatcher(@"[-ac]bd").Value;

            Assert.True(matcher.IsMatch("/abd"));
            Assert.True(matcher.IsMatch("/cbd"));
            Assert.True(matcher.IsMatch("/-bd"));
            Assert.False(matcher.IsMatch("/bbd"));
            Assert.False(matcher.IsMatch("/-cd"));
            Assert.False(matcher.IsMatch("/bcd"));

            Assert.Equal(@"^.*/[-ac]bd$", matcher.Regex.ToString());
        }

        [Fact]
        public void CharacterClassEndingDash()
        {
            var matcher = TryCreateSectionNameMatcher(@"[ac-]bd").Value;

            Assert.True(matcher.IsMatch("/abd"));
            Assert.True(matcher.IsMatch("/cbd"));
            Assert.True(matcher.IsMatch("/-bd"));
            Assert.False(matcher.IsMatch("/bbd"));
            Assert.False(matcher.IsMatch("/-cd"));
            Assert.False(matcher.IsMatch("/bcd"));

            Assert.Equal(@"^.*/[ac-]bd$", matcher.Regex.ToString());
        }

        [Fact]
        public void CharacterClassEndBracketAfter()
        {
            var matcher = TryCreateSectionNameMatcher(@"[ab]]cd").Value;

            Assert.True(matcher.IsMatch("/a]cd"));
            Assert.True(matcher.IsMatch("/b]cd"));
            Assert.False(matcher.IsMatch("/acd"));
            Assert.False(matcher.IsMatch("/bcd"));
            Assert.False(matcher.IsMatch("/acd"));

            Assert.Equal(@"^.*/[ab]]cd$", matcher.Regex.ToString());
        }

        [Fact]
        public void CharacterClassEscapeBackslash()
        {
            var matcher = TryCreateSectionNameMatcher(@"[ab\\]cd").Value;

            Assert.True(matcher.IsMatch("/acd"));
            Assert.True(matcher.IsMatch("/bcd"));
            Assert.True(matcher.IsMatch("/\\cd"));
            Assert.False(matcher.IsMatch("/dcd"));
            Assert.False(matcher.IsMatch("/\\\\cd"));
            Assert.False(matcher.IsMatch("/cd"));

            Assert.Equal(@"^.*/[ab\\]cd$", matcher.Regex.ToString());
        }

        [Fact]
        public void EscapeOpenBracket()
        {
            var matcher = TryCreateSectionNameMatcher(@"ab\[cd").Value;

            Assert.True(matcher.IsMatch("/ab[cd"));
            Assert.False(matcher.IsMatch("/ab[[cd"));
            Assert.False(matcher.IsMatch("/abc"));
            Assert.False(matcher.IsMatch("/abd"));

            Assert.Equal(@"^.*/ab\[cd$", matcher.Regex.ToString());
        }

        #endregion

        #region Processing of dotnet_diagnostic rules

        [Fact]
        public void EditorConfigToDiagnostics()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = none

[*.vb]
dotnet_diagnostic.cs000.severity = error", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                SyntaxTree.EmptyDiagnosticOptions
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/72657")]
        [InlineData("/", "/")]
        [InlineData("/a/b/c/", "/a/b/c/")]
        [InlineData("/a/b//c/", "/a/b/c/")]
        [InlineData("/a/b/c/", "/a/b//c/")]
        [InlineData("/a/b//c/", "/a/b//c/")]
        [InlineData("/a/b/c//", "/a/b/c/")]
        [InlineData("/a/b/c/", "/a/b/c//")]
        [InlineData("/a/b/c//", "/a/b/c//")]
        [InlineData("/a/b//c/", "/a/b///c/")]
        public void EditorConfigToDiagnostics_DoubleSlash(string prefixEditorConfig, string prefixSource)
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse("""
                [*.cs]
                dotnet_diagnostic.cs000.severity = none
                """,
                prefixEditorConfig + ".editorconfig"));

            var options = GetAnalyzerConfigOptions([prefixSource + "test.cs"], configs);
            configs.Free();

            Assert.Equal([
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress))
            ], options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void LaterSectionOverrides()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = none

[test.*]
dotnet_diagnostic.cs000.severity = error", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                SyntaxTree.EmptyDiagnosticOptions
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void BadSectionInConfigIgnored()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = none

[*.vb]
dotnet_diagnostic.cs000.severity = error

[{test.*]
dotnet_diagnostic.cs000.severity = suggestion"
, "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                SyntaxTree.EmptyDiagnosticOptions
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void TwoSettingsSameSection()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = none
dotnet_diagnostic.cs001.severity = suggestion", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress),
                    ("cs001", ReportDiagnostic.Info)),
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void TwoTermsForHidden()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = silent
dotnet_diagnostic.cs001.severity = refactoring", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Hidden),
                    ("cs001", ReportDiagnostic.Hidden)),
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void TwoSettingsDifferentSections()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = none

[test.*]
dotnet_diagnostic.cs001.severity = suggestion", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress),
                    ("cs001", ReportDiagnostic.Info))
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void MultipleEditorConfigs()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[**/*]
dotnet_diagnostic.cs000.severity = none

[**test.*]
dotnet_diagnostic.cs001.severity = suggestion", "/.editorconfig"));
            configs.Add(Parse(@"
[**]
dotnet_diagnostic.cs000.severity = warning

[test.cs]
dotnet_diagnostic.cs001.severity = error", "/subdir/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/subdir/test.cs", "/subdir/test.vb" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Warn),
                    ("cs001", ReportDiagnostic.Error)),
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Warn),
                    ("cs001", ReportDiagnostic.Info))
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void FolderNamePrefixOfFileName()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suggestion", "/root/.editorconfig"));
            configs.Add(Parse(@"
root=true", "/root/test/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/root/testing.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Info)),
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void InheritOuterConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[**/*]
dotnet_diagnostic.cs000.severity = none

[**test.cs]
dotnet_diagnostic.cs001.severity = suggestion", "/.editorconfig"));
            configs.Add(Parse(@"
[test.cs]
dotnet_diagnostic.cs001.severity = error", "/subdir/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/subdir/test.cs", "/subdir/test.vb" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs001", ReportDiagnostic.Info)),
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress),
                    ("cs001", ReportDiagnostic.Error)),
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress))
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsRootConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = none", "Z:\\.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "Z:\\test.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress))
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        #endregion

        #region Processing of Analyzer Options

        private AnalyzerConfigOptionsResult[] GetAnalyzerConfigOptions(string[] filePaths, ArrayBuilder<AnalyzerConfig> configs)
        {
            var set = AnalyzerConfigSet.Create(configs);
            return filePaths.Select(f => set.GetOptionsForSourcePath(f)).ToArray();
        }

        private static void VerifyAnalyzerOptions(
            (string key, string value)[][] expected,
            AnalyzerConfigOptionsResult[] options)
        {
            Assert.Equal(expected.Length, options.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] is null)
                {
                    Assert.NotEqual(default, options[i]);
                }
                else
                {
                    AssertEx.SetEqual(
                        expected[i].Select(t => KeyValuePair.Create(t.key, t.value)),
                        options[i].AnalyzerOptions);
                }
            }
        }

        private static void VerifyTreeOptions(
            (string diagId, ReportDiagnostic severity)[][] expected,
            AnalyzerConfigOptionsResult[] options)
        {
            Assert.Equal(expected.Length, options.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] is null)
                {
                    Assert.NotEqual(default, options[i]);
                }
                else
                {
                    var treeOptions = options[i].TreeOptions;
                    Assert.Equal(expected[i].Length, treeOptions.Count);
                    foreach (var item in expected[i])
                    {
                        Assert.True(treeOptions.TryGetValue(item.diagId, out var severity));
                        Assert.Equal(item.severity, severity);
                    }
                }
            }
        }

        [Fact]
        public void SimpleAnalyzerOptions()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.some_key = some_val", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[] {
                    new[] { ("dotnet_diagnostic.cs000.some_key", "some_val") },
                    new (string, string) [] { }
                },
                options);
        }

        [Fact]
        public void NestedAnalyzerOptionsWithRoot()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.bad_key = bad_val", "/.editorconfig"));
            configs.Add(Parse(@"
root = true

[*.cs]
dotnet_diagnostic.cs000.some_key = some_val", "/src/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/src/test.cs", "/src/test.vb", "/root.cs" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[] {
                    new[] { ("dotnet_diagnostic.cs000.some_key", "some_val") },
                    new (string, string) [] { },
                    new[] { ("dotnet_diagnostic.cs000.bad_key", "bad_val") }
               },
                options);
        }

        [Fact]
        public void NestedAnalyzerOptionsWithOverrides()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.some_key = a_val", "/.editorconfig"));
            configs.Add(Parse(@"
[test.*]
dotnet_diagnostic.cs000.some_key = b_val", "/src/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/src/test.cs", "/src/test.vb", "/root.cs" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[] {
                    new[] { ("dotnet_diagnostic.cs000.some_key", "b_val") },
                    new[] { ("dotnet_diagnostic.cs000.some_key", "b_val") },
                    new[] { ("dotnet_diagnostic.cs000.some_key", "a_val") }
               },
                options);
        }

        [Fact]
        public void NestedAnalyzerOptionsWithSectionOverrides()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.some_key = a_val", "/.editorconfig"));
            configs.Add(Parse(@"
[test.*]
dotnet_diagnostic.cs000.some_key = b_val

[*.cs]
dotnet_diagnostic.cs000.some_key = c_val", "/src/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/src/test.cs", "/src/test.vb", "/root.cs" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[] {
                    new[] { ("dotnet_diagnostic.cs000.some_key", "c_val") },
                    new[] { ("dotnet_diagnostic.cs000.some_key", "b_val") },
                    new[] { ("dotnet_diagnostic.cs000.some_key", "a_val") }
               },
                options);
        }

        [Fact]
        public void NestedBothOptionsWithSectionOverrides()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = warning
somekey = a_val", "/.editorconfig"));
            configs.Add(Parse(@"
[test.*]
dotnet_diagnostic.cs000.severity = error
somekey = b_val

[*.cs]
dotnet_diagnostic.cs000.severity = none
somekey = c_val", "/src/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/src/test.cs", "/src/test.vb", "/root.cs" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[] {
                    new[] { ("somekey", "c_val") },
                    new[] { ("somekey", "b_val") },
                    new[] { ("somekey", "a_val") }
               }, options);

            VerifyTreeOptions(
                new[]
                {
                    new[] { ("cs000", ReportDiagnostic.Suppress) },
                    new[] { ("cs000", ReportDiagnostic.Error) },
                    new[] { ("cs000", ReportDiagnostic.Warn) }
                }, options);
        }

        [Fact]
        public void FromMultipleSectionsAnalyzerOptions()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.some_key = some_val

[test.*]
dotnet_diagnostic.cs001.some_key2 = some_val2
", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[] {
                    new[]
                    {
                        ("dotnet_diagnostic.cs000.some_key", "some_val"),
                        ("dotnet_diagnostic.cs001.some_key2", "some_val2")
                    },
                    new[]
                    {
                        ("dotnet_diagnostic.cs001.some_key2", "some_val2")
                    }
                },
                options);
        }

        [Fact]
        public void AnalyzerOptionsOverride()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[**.cs]
dotnet_diagnostic.cs000.some_key = some_val", "/.editorconfig"));
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.some_key = some_other_val", "/subdir/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/subdir/test.cs" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[]
                {
                    new[]
                    {
                        ("dotnet_diagnostic.cs000.some_key", "some_val")
                    },
                    new[]
                    {
                        ("dotnet_diagnostic.cs000.some_key", "some_other_val")
                    }
                },
                options);
        }

        [Fact]
        public void BadFilePaths()
        {
            Assert.Throws<ArgumentException>(() => Parse("", "relativeDir/file"));
            Assert.Throws<ArgumentException>(() => Parse("", "/"));
            Assert.Throws<ArgumentException>(() => Parse("", "/subdir/"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void BadWindowsFilePaths()
        {
            Assert.Throws<ArgumentException>(() => Parse("", "Z:"));
            Assert.Throws<ArgumentException>(() => Parse("", "Z:\\"));
            Assert.Throws<ArgumentException>(() => Parse("", ":\\.editorconfig"));
        }

        [Fact]
        public void EmptyDiagnosticId()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic..severity = warning
dotnet_diagnostic..some_key = some_val", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("", ReportDiagnostic.Warn)),
            }, options.Select(o => o.TreeOptions).ToArray());

            VerifyAnalyzerOptions(
                new[]
                {
                    new[]
                    {
                        ("dotnet_diagnostic..some_key", "some_val")
                    }
                },
                options);
        }

        [Fact]
        public void NoDiagnosticId()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.severity = warn
dotnet_diagnostic.some_key = some_val", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", },
                configs);
            configs.Free();

            Assert.Equal(new ImmutableDictionary<string, ReportDiagnostic>[]
            {
                SyntaxTree.EmptyDiagnosticOptions
            }, options.Select(o => o.TreeOptions).ToArray());

            VerifyAnalyzerOptions(
                new[]
                {
                    new[]
                    {
                        ("dotnet_diagnostic.severity", "warn"),
                        ("dotnet_diagnostic.some_key", "some_val")
                    }
                },
                options);
        }

        [Fact]
        public void E2ENumberRange()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[a{-10..0}b{0..10}.cs]
dotnet_diagnostic.cs000.severity = warning", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/a0b0.cs", "/test/a-5b5.cs", "/a0b0.vb" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Warn)),
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Warn)),
                SyntaxTree.EmptyDiagnosticOptions
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void DiagnosticIdInstancesAreSharedBetweenMultipleTrees()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = warning", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/a.cs", "/b.cs", "/c.cs" },
                configs);
            configs.Free();

            Assert.Equal("cs000", options[0].TreeOptions.Keys.Single());

            Assert.Same(options[0].TreeOptions.Keys.First(), options[1].TreeOptions.Keys.First());
            Assert.Same(options[1].TreeOptions.Keys.First(), options[2].TreeOptions.Keys.First());
        }

        [Fact]
        public void TreesShareOptionsInstances()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = warning", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/a.cs", "/b.cs", "/c.cs" },
                configs);
            configs.Free();
            Assert.Equal(KeyValuePair.Create("cs000", ReportDiagnostic.Warn), options[0].TreeOptions.Single());

            Assert.Same(options[0].TreeOptions, options[1].TreeOptions);
            Assert.Same(options[0].AnalyzerOptions, options[1].AnalyzerOptions);
            Assert.Same(options[1].TreeOptions, options[2].TreeOptions);
            Assert.Same(options[1].AnalyzerOptions, options[2].AnalyzerOptions);
        }

        #endregion

        #region Processing of Global configs

        [Fact]
        public void IsReportedAsGlobal()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true ", "/.editorconfig"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out _);

            Assert.Empty(configs);
            Assert.NotNull(globalConfig);
            configs.Free();
        }

        [Fact]
        public void IsNotGlobalIfInSection()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
is_global = true ", "/.editorconfig"));
            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out _);

            Assert.Single(configs);
            Assert.NotNull(globalConfig);
            Assert.Empty(globalConfig.GlobalSection.Properties);
            Assert.Empty(globalConfig.NamedSections);
            configs.Free();
        }

        [Fact]
        public void FilterReturnsSingleGlobalConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = value1", "/.globalconfig1"));

            configs.Add(Parse(@"option2 = value2", "/.editorconfig1"));
            configs.Add(Parse(@"option3 = value3", "/.editorconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);

            diagnostics.Verify();
            Assert.Equal(2, configs.Count);
            Assert.NotNull(globalConfig);
            Assert.Equal("value1", globalConfig.GlobalSection.Properties["option1"]);
            configs.Free();
        }

        [Fact]
        public void FilterReturnsSingleCombinedGlobalConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = value1", "/.globalconfig1"));

            configs.Add(Parse(@"is_global = true
option2 = value2", "/.globalconfig2"));

            configs.Add(Parse(@"option3 = value3", "/.editorconfig1"));
            configs.Add(Parse(@"option4 = value4", "/.editorconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);

            diagnostics.Verify();
            Assert.Equal(2, configs.Count);
            Assert.NotNull(globalConfig);
            Assert.Equal("value1", globalConfig.GlobalSection.Properties["option1"]);
            Assert.Equal("value2", globalConfig.GlobalSection.Properties["option2"]);
            configs.Free();
        }

        [Fact]
        public void FilterCombinesSections()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = value1

[/path/to/file1.cs]
option1 = value1

[/path/to/file2.cs]
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse(@"is_global = true
option2 = value2

[/path/to/file1.cs]
option2 = value2

[/path/to/file3.cs]
option1 = value1",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);

            diagnostics.Verify();
            Assert.Empty(configs);
            Assert.NotNull(globalConfig);
            Assert.Equal("value1", globalConfig.GlobalSection.Properties["option1"]);
            Assert.Equal("value2", globalConfig.GlobalSection.Properties["option2"]);

            var file1Section = globalConfig.NamedSections[0];
            var file2Section = globalConfig.NamedSections[1];
            var file3Section = globalConfig.NamedSections[2];

            Assert.Equal(@"/path/to/file1.cs", file1Section.Name);
            Assert.Equal(2, file1Section.Properties.Count);
            Assert.Equal("value1", file1Section.Properties["option1"]);
            Assert.Equal("value2", file1Section.Properties["option2"]);

            Assert.Equal(@"/path/to/file2.cs", file2Section.Name);
            Assert.Equal(1, file2Section.Properties.Count);
            Assert.Equal("value1", file2Section.Properties["option1"]);

            Assert.Equal(@"/path/to/file3.cs", file3Section.Name);
            Assert.Equal(1, file3Section.Properties.Count);
            Assert.Equal("value1", file3Section.Properties["option1"]);
            configs.Free();
        }

        [Fact]
        public void DuplicateOptionsInGlobalConfigsAreUnset()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = value1", "/.globalconfig1"));

            configs.Add(Parse(@"is_global = true
option1 = value2", "/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);

            diagnostics.Verify(
                Diagnostic("MultipleGlobalAnalyzerKeys").WithArguments("option1", "Global Section", "/.globalconfig1, /.globalconfig2").WithLocation(1, 1)
                );
        }

        [Fact]
        public void DuplicateOptionsInGlobalConfigsSectionsAreUnset()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
option1 = value2",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);

            diagnostics.Verify(
                Diagnostic("MultipleGlobalAnalyzerKeys").WithArguments("option1", "/path/to/file1.cs", "/.globalconfig1, /.globalconfig2").WithLocation(1, 1)
                );
        }

        [Fact]
        public void DuplicateGlobalOptionsInNonGlobalConfigsAreKept()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = value1", "/.globalconfig1"));

            configs.Add(Parse(@"
option1 = value2", "/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify();
        }

        [Fact]
        public void DuplicateSectionOptionsInNonGlobalConfigsAreKept()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse(@"
[/path/to/file1.cs]
option1 = value2",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify();
        }

        [Fact]
        public void GlobalConfigsPropertiesAreGlobal()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = value1
", "/.globalconfig1"));

            var options = GetAnalyzerConfigOptions(
                 new[] { "/file1.cs", "/path/to/file1.cs", "/file1.vb" },
                 configs);
            configs.Free();

            VerifyAnalyzerOptions(
              new[]
              {
                    new[] { ("option1", "value1") },
                    new[] { ("option1", "value1") },
                    new[] { ("option1", "value1") }
              },
              options);
        }

        [Fact]
        public void GlobalConfigsSectionsMustBeFullPath()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
option1 = value1

[*.cs]
option2 = value2

[.*/path/*.cs]
option3 = value3

[/.*/*.cs]
option4 = value4
", "/.globalconfig1"));

            var options = GetAnalyzerConfigOptions(
                 new[] { "/file1.cs", "/path/to/file2.cs", "/path/to/file1.cs", "/file1.vb" },
                 configs);
            configs.Free();

            VerifyAnalyzerOptions(
              new[]
              {
                    new (string, string)[] { },
                    new (string, string)[] { },
                    new (string, string)[]
                    {
                        ("option1", "value1")
                    },
                    new (string, string)[] { }
              },
              options);
        }

        [Fact]
        public void GlobalConfigsSectionsAreOverriddenByNonGlobal()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = global

[/path/to/file1.cs]
option2 = global
option3 = global
", "/.globalconfig1"));

            configs.Add(Parse(@"
[*.cs]
option2 = config1
", "/.editorconfig"));

            configs.Add(Parse(@"
[*.cs]
option3 = config2
", "/path/.editorconfig"));

            configs.Add(Parse(@"
[*.cs]
option2 = config3
", "/path/to/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                 new[] { "/path/to/file1.cs", "/path/file1.cs", "/file1.cs" },
                 configs);
            configs.Free();

            VerifyAnalyzerOptions(
              new[]
              {
                    new []
                    {
                        ("option1", "global"),
                        ("option2", "config3"), // overridden by config3
                        ("option3", "config2")  // overridden by config2
                    },
                    new []
                    {
                        ("option1", "global"),
                        ("option2", "config1"),
                        ("option3", "config2")
                    },
                    new []
                    {
                        ("option1", "global"),
                        ("option2", "config1")
                    }
              },
              options);
        }

        [Fact]
        public void GlobalConfigSectionsAreCaseSensitive()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse(@"is_global = true
[/pAth/To/fiLe1.cs]
option1 = value2",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify();

            Assert.Equal(2, globalConfig.NamedSections.Length);
            configs.Free();
        }

        [Fact]
        public void GlobalConfigSectionsPropertiesAreNotCaseSensitive()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
opTioN1 = value2",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify(
                Diagnostic("MultipleGlobalAnalyzerKeys").WithArguments("option1", "/path/to/file1.cs", "/.globalconfig1, /.globalconfig2").WithLocation(1, 1)
                );
            configs.Free();
        }

        [Fact]
        public void GlobalConfigPropertiesAreNotCaseSensitive()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse(@"is_global = true
opTioN1 = value2",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify(
                Diagnostic("MultipleGlobalAnalyzerKeys").WithArguments("option1", "Global Section", "/.globalconfig1, /.globalconfig2").WithLocation(1, 1)
                );
            configs.Free();
        }

        [Fact]
        public void GlobalConfigSectionPathsMustBeNormalized()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
[/path/to/file1.cs]
option1 = value1

[\path\to\file2.cs]
option1 = value1

", "/.globalconfig1"));

            var options = GetAnalyzerConfigOptions(
                 new[] { "/path/to/file1.cs", "/path/to/file2.cs" },
                 configs);
            configs.Free();

            VerifyAnalyzerOptions(
                new[]
                {
                    new []
                    {
                        ("option1", "value1")
                    },
                    new (string, string) [] { }
                },
                options);
        }

        [Fact]
        public void GlobalConfigCanSetSeverity()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true
dotnet_diagnostic.cs000.severity = none
dotnet_diagnostic.cs001.severity = error
", "/.editorconfig"));

            var set = AnalyzerConfigSet.Create(configs);
            configs.Free();

            Assert.Equal(CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress),
                                                   ("cs001", ReportDiagnostic.Error)),
                         set.GlobalConfigOptions.TreeOptions);
        }

        [Fact]
        public void GlobalConfigCanSetSeverityInSection()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true

[/path/to/file.cs]
dotnet_diagnostic.cs000.severity = error
", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/path/to/file.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                SyntaxTree.EmptyDiagnosticOptions,
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error))
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void GlobalConfigInvalidSeverity()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true
dotnet_diagnostic.cs000.severity = foo

[/path/to/file.cs]
dotnet_diagnostic.cs001.severity = bar
", "/.editorconfig"));

            var set = AnalyzerConfigSet.Create(configs);
            var options = new[] { "/test.cs", "/path/to/file.cs" }.Select(f => set.GetOptionsForSourcePath(f)).ToArray();
            configs.Free();

            set.GlobalConfigOptions.Diagnostics.Verify(
                Diagnostic("InvalidSeverityInAnalyzerConfig").WithArguments("cs000", "foo", "<Global Config>").WithLocation(1, 1)
                );

            options[1].Diagnostics.Verify(
                Diagnostic("InvalidSeverityInAnalyzerConfig").WithArguments("cs001", "bar", "<Global Config>").WithLocation(1, 1)
                );
        }

        [Fact]
        public void GlobalConfigSeverityInSectionOverridesGlobal()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true
dotnet_diagnostic.cs000.severity = none

[/path/to/file.cs]
dotnet_diagnostic.cs000.severity = error
", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/path/to/file.cs" },
                configs);
            configs.Free();

            Assert.Equal(
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                options[0].TreeOptions);
        }

        [Fact]
        public void GlobalConfigSeverityIsOverriddenByEditorConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true
dotnet_diagnostic.cs000.severity = error
", "/.globalconfig"));

            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = none
", "/.editorconfig"));

            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = warning
", "/path/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/path/file.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Warn))
            }, options.Select(o => o.TreeOptions).ToArray());
        }

        [Fact]
        public void GlobalKeyIsNotSkippedIfInSection()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true
[/path/to/file.cs]
is_global = true
", "/.globalconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/file.cs", "/path/to/file.cs" },
                configs);
            configs.Free();

            VerifyAnalyzerOptions(
              new[]
              {
                    new (string,string)[] { },
                    new[] { ("is_global", "true") }
              },
              options);
        }

        [Fact]
        public void GlobalConfigIsNotClearedByRootEditorConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"is_global = true
option1 = global

[/path/to/file1.cs]
option2 = global
option3 = global

[/path/file1.cs]
option2 = global
option3 = global

[/file1.cs]
option2 = global
option3 = global

", "/.globalconfig1"));

            configs.Add(Parse(@"
root = true
[*.cs]
option2 = config1
", "/.editorconfig"));

            configs.Add(Parse(@"
[*.cs]
option3 = config2
", "/path/.editorconfig"));

            configs.Add(Parse(@"
root = true
[*.cs]
option2 = config3
", "/path/to/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                 new[] { "/path/to/file1.cs", "/path/file1.cs", "/file1.cs" },
                 configs);
            configs.Free();

            VerifyAnalyzerOptions(
              new[]
              {
                    new []
                    {
                        ("option1", "global"),
                        ("option2", "config3"), // overridden by config3
                        ("option3", "global") // not overridden by config2, because config3 is root
                    },
                    new []
                    {
                        ("option1", "global"),
                        ("option2", "config1"),
                        ("option3", "config2")
                    },
                    new []
                    {
                        ("option1", "global"),
                        ("option2", "config1"),
                        ("option3", "global")
                    }
              },
              options);
        }

        [Fact]
        public void GlobalConfigOptionsAreEmptyWhenNoGlobalConfig()
        {
            var set = AnalyzerConfigSet.Create(ImmutableArray<AnalyzerConfig>.Empty);
            var globalOptions = set.GlobalConfigOptions;

            Assert.NotNull(globalOptions.AnalyzerOptions);
            Assert.NotNull(globalOptions.TreeOptions);

            Assert.Empty(globalOptions.AnalyzerOptions);
            Assert.Empty(globalOptions.Diagnostics);
            Assert.Empty(globalOptions.TreeOptions);
        }

        [Theory]
        [InlineData("/path/to/file.cs", true)]
        [InlineData("file.cs", false)]
        [InlineData("../file.cs", false)]
        [InlineData("**", false)]
        [InlineData("*.cs", false)]
        [InlineData("?abc.cs", false)]
        [InlineData("/path/to/**", false)]
        [InlineData("/path/[a]/to/*.cs", false)]
        [InlineData("/path{", false)]
        [InlineData("/path}", false)]
        [InlineData("/path?", false)]
        [InlineData("/path,", false)]
        [InlineData("/path\"", true)]
        [InlineData(@"/path\", false)] //editorconfig sees a single escape character (special)
        [InlineData(@"/path\\", true)] //editorconfig sees an escaped backslash
        [InlineData("//path", true)]
        [InlineData("//", true)]
        [InlineData(@"\", false)] //invalid: editorconfig sees a single escape character
        [InlineData(@"\\", false)] //invalid: editorconfig sees an escaped, literal backslash
        [InlineData(@"/\{\}\,\[\]\*", true)]
        [InlineData(@"C:\my\file.cs", false)] // invalid: editorconfig sees a single file called 'c:(\m)y(\f)ile.cs' (i.e. \m and \f are escape chars)
        [InlineData(@"\my\file.cs", false)] // invalid: editorconfig sees a single file called '(\m)y(\f)ile.cs' 
        [InlineData(@"\\my\\file.cs", false)] // invalid: editorconfig sees a single file called '\my\file.cs' with literal backslashes
        [InlineData(@"\\\\my\\file.cs", false)] // invalid: editorconfig sees a single file called '\\my\file.cs' not a UNC path
        [InlineData("//server/file.cs", true)]
        [InlineData(@"//server\file.cs", true)]
        [InlineData(@"\/file.cs", true)] // allow escaped chars
        [InlineData("<>a??/b.cs", false)]
        [InlineData(".", false)]
        [InlineData("/", true)]
        [InlineData("", true)] // only true because [] isn't a valid editorconfig section name either and thus never gets parsed
        public void GlobalConfigIssuesWarningWithInvalidSectionNames(string sectionName, bool isValid)
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse($@"
is_global = true
[{sectionName}]
", "/.editorconfig"));

            _ = AnalyzerConfigSet.Create(configs, out var diagnostics);
            configs.Free();

            if (isValid)
            {
                diagnostics.Verify();
            }
            else
            {
                diagnostics.Verify(
                    Diagnostic("InvalidGlobalSectionName", isSuppressed: false).WithArguments(sectionName, "/.editorconfig").WithLocation(1, 1)
                    );
            }
        }

        [Theory]
        [InlineData("C:/myfile.cs", true, false)]
        [InlineData("cd:/myfile.cs", false, false)] // windows only allows a single character as a drive specifier
        [InlineData(@"\c\:\/myfile.cs", true, false)] // allow escaped characters
        [InlineData("/myfile.cs", true, true)] //absolute, with a relative drive root
        [InlineData("c:myfile.cs", false, false)] //relative, wit2h an absolute drive root
        [InlineData(@"C:\myfile.cs", false, false)] //not a valid editorconfig path
        [InlineData("//?/C:/Test/Foo.txt", false, false)] // ? is a special char in editorconfig
        [InlineData(@"//\?/C:/Test/Foo.txt", true, true)]
        [InlineData(@"\\?\C:\Test\Foo.txt", false, false)]
        [InlineData(@"C:", false, false)]
        [InlineData(@"C\", false, false)]
        [InlineData(@"\c\:", false, false)]
        [InlineData("C:/", true, false)]
        [InlineData("C:/*.cs", false, false)]
        public void GlobalConfigIssuesWarningWithInvalidSectionNames_PlatformSpecific(string sectionName, bool isValidWindows, bool isValidOther)
            => GlobalConfigIssuesWarningWithInvalidSectionNames(sectionName, ExecutionConditionUtil.IsWindows ? isValidWindows : isValidOther);

        [Theory]
        [InlineData("/.globalconfig", true)]
        [InlineData("/.GLOBALCONFIG", true)]
        [InlineData("/.glObalConfiG", true)]
        [InlineData("/path/to/.globalconfig", true)]
        [InlineData("/my.globalconfig", false)]
        [InlineData("/globalconfig", false)]
        [InlineData("/path/to/globalconfig", false)]
        [InlineData("/path/to/my.globalconfig", false)]
        [InlineData("/.editorconfig", false)]
        [InlineData("/.globalconfİg", false)]
        public void FileNameCausesConfigToBeReportedAsGlobal(string fileName, bool shouldBeTreatedAsGlobal)
        {
            var config = Parse("", fileName);
            Assert.Equal(shouldBeTreatedAsGlobal, config.IsGlobal);
        }

        [Fact]
        public void GlobalLevelCanBeReadFromAnyConfig()
        {
            var config = Parse("global_level = 5", "/.editorconfig");
            Assert.Equal(5, config.GlobalLevel);
        }

        [Fact]
        public void GlobalLevelDefaultsTo100ForUserGlobalConfigs()
        {
            var config = Parse("", "/" + AnalyzerConfig.UserGlobalConfigName);

            Assert.True(config.IsGlobal);
            Assert.Equal(100, config.GlobalLevel);
        }

        [Fact]
        public void GlobalLevelCanBeOverriddenForUserGlobalConfigs()
        {
            var config = Parse("global_level = 5", "/" + AnalyzerConfig.UserGlobalConfigName);

            Assert.True(config.IsGlobal);
            Assert.Equal(5, config.GlobalLevel);
        }

        [Fact]
        public void GlobalLevelDefaultsToZeroForNonUserGlobalConfigs()
        {
            var config = Parse("is_global = true", "/.nugetconfig");

            Assert.True(config.IsGlobal);
            Assert.Equal(0, config.GlobalLevel);
        }

        [Fact]
        public void GlobalLevelIsNotPresentInConfigSet()
        {
            var config = Parse("global_level = 123", "/.globalconfig");

            var set = AnalyzerConfigSet.Create(ImmutableArray.Create(config));
            var globalOptions = set.GlobalConfigOptions;

            Assert.Empty(globalOptions.AnalyzerOptions);
            Assert.Empty(globalOptions.TreeOptions);
            Assert.Empty(globalOptions.Diagnostics);
        }

        [Fact]
        public void GlobalLevelInSectionIsPresentInConfigSet()
        {
            var config = Parse(@"
[/path]
global_level = 123", "/.globalconfig");

            var set = AnalyzerConfigSet.Create(ImmutableArray.Create(config));
            var globalOptions = set.GlobalConfigOptions;

            Assert.Empty(globalOptions.AnalyzerOptions);
            Assert.Empty(globalOptions.TreeOptions);
            Assert.Empty(globalOptions.Diagnostics);

            var sectionOptions = set.GetOptionsForSourcePath("/path");

            Assert.Single(sectionOptions.AnalyzerOptions);
            Assert.Equal("123", sectionOptions.AnalyzerOptions["global_level"]);
            Assert.Empty(sectionOptions.TreeOptions);
            Assert.Empty(sectionOptions.Diagnostics);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        [InlineData(-2, -1)]
        [InlineData(2, -1)]
        public void GlobalLevelAllowsOverrideOfGlobalKeys(int level1, int level2)
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse($@"
is_global = true
global_level = {level1}
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse($@"
is_global = true
global_level = {level2}
option1 = value2",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify();

            Assert.Single(globalConfig.GlobalSection.Properties.Keys, "option1");

            string expectedValue = level1 > level2 ? "value1" : "value2";
            Assert.Single(globalConfig.GlobalSection.Properties.Values, expectedValue);

            configs.Free();
        }

        [Fact]
        public void GlobalLevelAllowsOverrideOfSectionKeys()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true
global_level = 1

[/path]
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse(@"
is_global = true
global_level = 2

[/path]
option1 = value2",
"/.globalconfig2"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify();

            Assert.Single(globalConfig.NamedSections);
            Assert.Equal("/path", globalConfig.NamedSections[0].Name);
            Assert.Single(globalConfig.NamedSections[0].Properties.Keys, "option1");
            Assert.Single(globalConfig.NamedSections[0].Properties.Values, "value2");

            configs.Free();
        }

        [Theory]
        [InlineData(1, 2, 3, "value3")]
        [InlineData(2, 1, 3, "value3")]
        [InlineData(3, 2, 1, "value1")]
        [InlineData(1, 2, 1, "value2")]
        [InlineData(1, 1, 2, "value3")]
        [InlineData(2, 1, 1, "value1")]
        public void GlobalLevelAllowsOverrideOfDuplicateGlobalKeys(int level1, int level2, int level3, string expectedValue)
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse($@"
is_global = true
global_level = {level1}
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse($@"
is_global = true
global_level = {level2}
option1 = value2",
"/.globalconfig2"));

            configs.Add(Parse($@"
is_global = true
global_level = {level3}
option1 = value3",
"/.globalconfig3"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);
            diagnostics.Verify();

            Assert.Single(globalConfig.GlobalSection.Properties.Keys, "option1");
            Assert.Single(globalConfig.GlobalSection.Properties.Values, expectedValue);

            configs.Free();
        }

        [Fact]
        public void GlobalLevelReportsConflictsOnlyAtTheHighestLevel()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse($@"
is_global = true
global_level = 1
option1 = value1
", "/.globalconfig1"));

            configs.Add(Parse($@"
is_global = true
global_level = 1
option1 = value2",
"/.globalconfig2"));

            configs.Add(Parse($@"
is_global = true
global_level = 3
option1 = value3",
"/.globalconfig3"));

            configs.Add(Parse($@"
is_global = true
global_level = 3
option1 = value4",
"/.globalconfig4"));

            configs.Add(Parse($@"
is_global = true
global_level = 2
option1 = value5",
"/.globalconfig5"));

            configs.Add(Parse($@"
is_global = true
global_level = 2
option1 = value6",
"/.globalconfig6"));

            var globalConfig = AnalyzerConfigSet.MergeGlobalConfigs(configs, out var diagnostics);

            // we don't report config1, 2, 5, or 6, because they didn't conflict: 3 + 4 overrode them, but then themselves were conflicting
            diagnostics.Verify(
                Diagnostic("MultipleGlobalAnalyzerKeys").WithArguments("option1", "Global Section", "/.globalconfig3, /.globalconfig4").WithLocation(1, 1)
                );

            configs.Free();
        }

        [Fact]
        public void InvalidGlobalLevelIsIgnored()
        {
            var userGlobalConfig = Parse($@"
is_global = true
global_level = abc
", "/.globalconfig");

            var nonUserGlobalConfig = Parse($@"
is_global = true
global_level = abc
", "/.editorconfig");

            Assert.Equal(100, userGlobalConfig.GlobalLevel);
            Assert.Equal(0, nonUserGlobalConfig.GlobalLevel);
        }

        [Theory]
        [InlineData("/dir1/dir3/../dir2/file.cs", true)]
        [InlineData("/dir1/./././././dir2/file.cs", true)]
        [InlineData("/dir1/../dir1/../dir1/../dir1/dir2/file.cs", true)]
        [InlineData("/dir1/dir3/dir4/../dir2/file.cs", false)]
        [InlineData("file.cs", false)]
        [InlineData("", false)]
        [InlineData("/../../dir1/dir2/file.cs", true)]
        [InlineData("/./../dir1/dir2/file.cs", true)]
        [InlineData("/dir1/../../dir1/dir2/file.cs", true)]
        [InlineData("/..", false)]
        [InlineData("/../file.cs", false)]
        [InlineData("/dir1/../file.cs", false)]
        [InlineData("./dir1/dir2/file.cs", false)]
        [InlineData("././../.././dir1/dir2/file.cs", false)]
        [InlineData("./dir1/../file.cs", false)]
        [InlineData("../dir1/dir2.cs", false)]
        public void EquivalentSourcePathNames(string sourcePath, bool shouldMatch)
        {
            string sectionName = "/dir1/dir2/file.cs";

            // append the drive root on windows (use something other than C: to ensure its not working by luck)
            if (ExecutionConditionUtil.IsWindows)
            {
                sectionName = sectionName.Insert(0, "X:");
                sourcePath = sourcePath.Insert(0, "X:");
            }

            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse($@"
is_global = true
[{sectionName}]
a = b
", "/.editorconfig"));

            var configSet = AnalyzerConfigSet.Create(configs, out var diagnostics);
            configs.Free();

            var options = configSet.GetOptionsForSourcePath(sourcePath);

            if (shouldMatch)
            {
                Assert.Single(options.AnalyzerOptions);
                Assert.Equal("b", options.AnalyzerOptions["a"]);
            }
            else
            {
                Assert.Empty(options.AnalyzerOptions);
            }
        }

        [Fact]
        public void CorrectlyMergeGlobalConfigWithEscapedPaths()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
is_global = true
[/Test.cs]
a = a
[/\Test.cs]
b = b
", "/.editorconfig"));

            var configSet = AnalyzerConfigSet.Create(configs, out var diagnostics);
            configs.Free();

            var options = configSet.GetOptionsForSourcePath("/Test.cs");

            Assert.Equal(2, options.AnalyzerOptions.Count);
            Assert.Equal("a", options.AnalyzerOptions["a"]);
            Assert.Equal("b", options.AnalyzerOptions["b"]);
        }

        #endregion
    }
}
