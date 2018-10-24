// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            string regex = TryCompileSectionNameToRegEx("abc");
            Assert.Equal("^.*/abc$", regex);

            Assert.Matches(regex, "/abc");
            Assert.DoesNotMatch(regex, "/aabc");
            Assert.DoesNotMatch(regex, "/ abc");
            Assert.DoesNotMatch(regex, "/cabc");
        }

        [Fact]
        public void StarOnlyMatch()
        {
            string regex = TryCompileSectionNameToRegEx("*");
            Assert.Equal("^.*/[^/]*$", regex);

            Assert.Matches(regex, "/abc");
            Assert.Matches(regex, "/123");
            Assert.Matches(regex, "/abc/123");
        }

        [Fact]
        public void StarNameMatch()
        {
            string regex = TryCompileSectionNameToRegEx("*.cs");
            Assert.Equal("^.*/[^/]*\\.cs$", regex);

            Assert.Matches(regex, "/abc.cs");
            Assert.Matches(regex, "/123.cs");
            Assert.Matches(regex, "/dir/subpath.cs");
            // Only '/' is defined as a directory separator, so the caller
            // is responsible for converting any other machine directory
            // separators to '/' before matching
            Assert.Matches(regex, "/dir\\subpath.cs");

            Assert.DoesNotMatch(regex, "/abc.vb");
        }

        [Fact]
        public void StarStarNameMatch()
        {
            string regex = TryCompileSectionNameToRegEx("**.cs");
            Assert.Equal("^.*/.*\\.cs$", regex);

            Assert.Matches(regex, "/abc.cs");
            Assert.Matches(regex, "/dir/subpath.cs");
        }

        [Fact]
        public void EscapeDot()
        {
            string regex = TryCompileSectionNameToRegEx("...");
            Assert.Equal("^.*/\\.\\.\\.$", regex);

            Assert.Matches(regex, "/...");
            Assert.Matches(regex, "/subdir/...");
            Assert.DoesNotMatch(regex, "/aaa");
            Assert.DoesNotMatch(regex, "/???");
            Assert.DoesNotMatch(regex, "/abc");
        }

        [Fact]
        public void BadEscapeMatch()
        {
            string regex = TryCompileSectionNameToRegEx("abc\\d.cs");
            Assert.Null(regex);
        }

        [Fact]
        public void EndBackslashMatch()
        {
            string regex = TryCompileSectionNameToRegEx("abc\\");
            Assert.Null(regex);
        }

        [Fact]
        public void QuestionMatch()
        {
            string regex = TryCompileSectionNameToRegEx("ab?def");
            Assert.Equal("^.*/ab.def$", regex);

            Assert.Matches(regex, "/abcdef");
            Assert.Matches(regex, "/ab?def");
            Assert.Matches(regex, "/abzdef");
            Assert.Matches(regex, "/ab/def");
            Assert.Matches(regex, "/ab\\def");
        }

        [Fact]
        public void LiteralBackslash()
        {
            string regex = TryCompileSectionNameToRegEx("ab\\\\c");
            Assert.Equal("^.*/ab\\\\c$", regex);

            Assert.Matches(regex, "/ab\\c");
            Assert.DoesNotMatch(regex, "/ab/c");
            Assert.DoesNotMatch(regex, "/ab\\\\c");
        }

        [Fact]
        public void LiteralStars()
        {
            string regex = TryCompileSectionNameToRegEx("\\***\\*\\**");
            Assert.Equal("^.*/\\*.*\\*\\*[^/]*$", regex);

            Assert.Matches(regex, "/*ab/cd**efg*");
            Assert.DoesNotMatch(regex, "/ab/cd**efg*");
            Assert.DoesNotMatch(regex, "/*ab/cd*efg*");
            Assert.DoesNotMatch(regex, "/*ab/cd**ef/gh");
        }

        [Fact]
        public void LiteralQuestions()
        {
            string regex = TryCompileSectionNameToRegEx("\\??\\?*\\??");
            Assert.Equal("^.*/\\?.\\?[^/]*\\?.$", regex);

            Assert.Matches(regex, "/?a?cde?f");
            Assert.Matches(regex, "/???????f");
            Assert.DoesNotMatch(regex, "/aaaaaaaa");
            Assert.DoesNotMatch(regex, "/aa?cde?f");
            Assert.DoesNotMatch(regex, "/?a?cdexf");
            Assert.DoesNotMatch(regex, "/?axcde?f");
        }

        [Fact]
        public void LiteralBraces()
        {
            string regex = TryCompileSectionNameToRegEx("abc\\{\\}def");
            Assert.Equal("^.*/abc\\{\\}def$", regex);

            Assert.Matches(regex, "/abc{}def");
            Assert.Matches(regex, "/subdir/abc{}def");
            Assert.DoesNotMatch(regex, "/abcdef");
            Assert.DoesNotMatch(regex, "/abc}{def");
        }

        [Fact]
        public void LiteralComma()
        {
            string regex = TryCompileSectionNameToRegEx("abc\\,def");
            Assert.Equal("^.*/abc,def$", regex);

            Assert.Matches(regex, "/abc,def");
            Assert.Matches(regex, "/subdir/abc,def");
            Assert.DoesNotMatch(regex, "/abcdef");
            Assert.DoesNotMatch(regex, "/abc\\,def");
            Assert.DoesNotMatch(regex, "/abc`def");
        }

        [Fact]
        public void SimpleChoice()
        {
            string regex = TryCompileSectionNameToRegEx("*.{cs,vb,fs}");
            Assert.Equal("^.*/[^/]*\\.(?:cs|vb|fs)$", regex);

            Assert.Matches(regex, "/abc.cs");
            Assert.Matches(regex, "/abc.vb");
            Assert.Matches(regex, "/abc.fs");
            Assert.Matches(regex, "/subdir/abc.cs");
            Assert.Matches(regex, "/subdir/abc.vb");
            Assert.Matches(regex, "/subdir/abc.fs");

            Assert.DoesNotMatch(regex, "/abcxcs");
            Assert.DoesNotMatch(regex, "/abcxvb");
            Assert.DoesNotMatch(regex, "/abcxfs");
            Assert.DoesNotMatch(regex, "/subdir/abcxcs");
            Assert.DoesNotMatch(regex, "/subdir/abcxcb");
            Assert.DoesNotMatch(regex, "/subdir/abcxcs");
        }

        [Fact]
        public void OneChoiceHasSlashes()
        {
            string regex = TryCompileSectionNameToRegEx("{*.cs,subdir/test.vb}");
            // This is an interesting case that may be counterintuitive.  A reasonable understanding
            // of the section matching could interpret the choice as generating multiple identical
            // sections, so [{a, b, c}] would be equivalent to [a] ... [b] ... [c] with all of the
            // same properties in each section. This is somewhat true, but the rules of how the matching
            // prefixes are constructed violate this assumption because they are defined as whether or
            // not a section contains a slash, not whether any of the choices contain a slash. So while
            // [*.cs] usually translates into '**/*.cs' because it contains no slashes, the slashes in
            // the second choice make this into '/*.cs', effectively matching only files in the root
            // directory of the match, instead of all subdirectories.
            Assert.Equal("^/(?:[^/]*\\.cs|subdir/test\\.vb)$", regex);

            Assert.Matches(regex, "/test.cs");
            Assert.Matches(regex, "/subdir/test.vb");

            Assert.DoesNotMatch(regex, "/subdir/test.cs");
            Assert.DoesNotMatch(regex, "/subdir/subdir/test.vb");
            Assert.DoesNotMatch(regex, "/test.vb");
        }

        [Fact]
        public void EmptyChoice()
        {
            string regex = TryCompileSectionNameToRegEx("{}");
            Assert.Equal("^.*/(?:)$", regex);

            Assert.Matches(regex, "/");
            Assert.Matches(regex, "/subdir/");
            Assert.DoesNotMatch(regex, "/.");
            Assert.DoesNotMatch(regex, "/anything");
        }

        [Fact]
        public void SingleChoice()
        {
            string regex = TryCompileSectionNameToRegEx("{*.cs}");
            Assert.Equal("^.*/(?:[^/]*\\.cs)$", regex);

            Assert.Matches(regex, "/test.cs");
            Assert.Matches(regex, "/subdir/test.cs");
            Assert.DoesNotMatch(regex, "test.vb");
            Assert.DoesNotMatch(regex, "testxcs");
        }

        [Fact]
        public void UnmatchedBraces()
        {
            string regex = TryCompileSectionNameToRegEx("{{{{}}");
            Assert.Null(regex);
        }

        [Fact]
        public void CommaOutsideBraces()
        {
            string regex = TryCompileSectionNameToRegEx("abc,def");
            Assert.Null(regex);
        }

        [Fact]
        public void RecursiveChoice()
        {
            string regex = TryCompileSectionNameToRegEx("{test{.cs,.vb},other.{a{bb,cc}}}");
            Assert.Equal("^.*/(?:test(?:\\.cs|\\.vb)|other\\.(?:a(?:bb|cc)))$", regex);

            Assert.Matches(regex, "/test.cs");
            Assert.Matches(regex, "/test.vb");
            Assert.Matches(regex, "/subdir/test.cs");
            Assert.Matches(regex, "/subdir/test.vb");
            Assert.Matches(regex, "/other.abb");
            Assert.Matches(regex, "/other.acc");

            Assert.DoesNotMatch(regex, "/test.fs");
            Assert.DoesNotMatch(regex, "/other.bbb");
            Assert.DoesNotMatch(regex, "/other.ccc");
            Assert.DoesNotMatch(regex, "/subdir/other.bbb");
            Assert.DoesNotMatch(regex, "/subdir/other.ccc");
        }

        [Fact]
        public void DashChoice()
        {
            string regex = TryCompileSectionNameToRegEx("ab{-}cd{-,}ef");
            Assert.Equal("^.*/ab(?:-)cd(?:-|)ef$", regex);

            Assert.Matches(regex, "/ab-cd-ef");
            Assert.Matches(regex, "/ab-cdef");

            Assert.DoesNotMatch(regex, "/abcdef");
            Assert.DoesNotMatch(regex, "/ab--cd-ef");
            Assert.DoesNotMatch(regex, "/ab--cd--ef");
        }

        [Fact]
        public void MiddleMatch()
        {
            string regex = TryCompileSectionNameToRegEx("ab{cs,vb,fs}cd");
            Assert.Equal("^.*/ab(?:cs|vb|fs)cd$", regex);

            Assert.Matches(regex, "/abcscd");
            Assert.Matches(regex, "/abvbcd");
            Assert.Matches(regex, "/abfscd");

            Assert.DoesNotMatch(regex, "/abcs");
            Assert.DoesNotMatch(regex, "/abcd");
            Assert.DoesNotMatch(regex, "/vbcd");
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
    }
}
