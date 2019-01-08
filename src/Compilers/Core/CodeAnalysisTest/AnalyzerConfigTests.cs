// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.AnalyzerConfig;
using static Microsoft.CodeAnalysis.CommonCompiler;
using static Roslyn.Test.Utilities.TestHelpers;
using KeyValuePair = Roslyn.Utilities.KeyValuePairUtil;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EditorConfigTests
    {
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

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsPath()
        {
            const string path = "Z:\\bogus\\.editorconfig";
            var config = Parse("", path);
            
            Assert.Equal("Z:/bogus", config.NormalizedDirectory);
            Assert.Equal(path, config.PathToFile);
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
            Assert.Equal(val, "my_VAL");
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
            // PROTOTYPE(editorconfig): This example is described in the Python ConfigParser as
            // allowing line continuation via the RFC 822 specification, section 3.1.1 LONG
            // HEADER FIELDS. It's unclear whether it's intended for editorconfig to permit
            // this interpretation. The VS parser does not. We should probably check other
            // parsers for completeness.
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
        public void BadEscapeMatch()
        {
            SectionNameMatcher? matcher = TryCreateSectionNameMatcher("abc\\d.cs");
            Assert.Null(matcher);
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
            Assert.Equal("^.*/abc\\{\\}def$", matcher.Regex.ToString());

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
            foreach (var matchString in new[] { "a{-10..0}b{0..10}", "a{0..-10}b{10..0}"})
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
        public void EditorConfigToDiagnostics()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress

[*.vb]
dotnet_diagnostic.cs000.severity = error", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                null
            }, options.TreeOptions);
        }

        [Fact]
        public void LaterSectionOverrides()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress

[test.*]
dotnet_diagnostic.cs000.severity = error", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                null
            }, options.TreeOptions);
        }

        [Fact]
        public void BadSectionInConfigIgnored()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress

[*.vb]
dotnet_diagnostic.cs000.severity = error

[{test.*]
dotnet_diagnostic.cs000.severity = info"
, "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                null
            }, options.TreeOptions);
        }

        [Fact]
        public void TwoSettingsSameSection()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress
dotnet_diagnostic.cs001.severity = info", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress),
                    ("cs001", ReportDiagnostic.Info)),
            }, options.TreeOptions);
        }

        [Fact]
        public void TwoSettingsDifferentSections()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress

[test.*]
dotnet_diagnostic.cs001.severity = info", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress),
                    ("cs001", ReportDiagnostic.Info))
            }, options.TreeOptions);
        }

        [Fact]
        public void MultipleEditorConfigs()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[**/*]
dotnet_diagnostic.cs000.severity = suppress

[**test.*]
dotnet_diagnostic.cs001.severity = info", "/.editorconfig"));
            configs.Add(Parse(@"
[**]
dotnet_diagnostic.cs000.severity = warn

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
            }, options.TreeOptions);
        }

        [Fact]
        public void InheritOuterConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[**/*]
dotnet_diagnostic.cs000.severity = suppress

[**test.cs]
dotnet_diagnostic.cs001.severity = info", "/.editorconfig"));
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
            }, options.TreeOptions);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void WindowsRootConfig()
        {
            var configs = ArrayBuilder<AnalyzerConfig>.GetInstance();
            configs.Add(Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress", "Z:\\.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "Z:\\test.cs" },
                configs);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress))
            }, options.TreeOptions);
        }

        private static void VerifyAnalyzerOptions(
            (string key, string val)[][] expected,
            AnalyzerConfigOptionsResult options)
        {
            var analyzerOptions = options.AnalyzerOptions;
            Assert.Equal(expected.Length, analyzerOptions.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                if (expected[i] is null)
                {
                    Assert.Null(analyzerOptions[i]);
                }
                else
                {
                    AssertEx.SetEqual(
                        expected[i].Select(KeyValuePair.ToKeyValuePair),
                        analyzerOptions[i]);
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
                    null
                },
                options);
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
dotnet_diagnostic..severity = warn
dotnet_diagnostic..some_key = some_val", "/.editorconfig"));

            var options = GetAnalyzerConfigOptions(
                new[] { "/test.cs", },
                configs);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("", ReportDiagnostic.Warn)),
            }, options.TreeOptions);

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
                null
            }, options.TreeOptions);

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
dotnet_diagnostic.cs000.severity = warn", "/.editorconfig"));

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
                null
            }, options.TreeOptions);
        }
    }
}
