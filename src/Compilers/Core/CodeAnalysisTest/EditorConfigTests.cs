﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EditorConfigTests
    {
        private static EditorConfig ParseConfigFile(string text) => EditorConfig.Parse(text, "");

        [Fact]
        public void SimpleCase()
        {
            var config = EditorConfig.Parse(@"
root = true

# Comment1
# Comment2
##################################

my_global_prop = my_global_val

[*.cs]
my_prop = my_val

", "Z:/bogus");

            Assert.Equal("", config.GlobalSection.Name);
            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_global_prop", "my_global_val"),
                        KeyValuePair.Create("root", "true") },
                properties);

            var namedSections = config.NamedSections;
            Assert.Equal("*.cs", namedSections[0].Name);
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val")},
                namedSections[0].Properties);
            
            Assert.True(config.IsRoot);
            Assert.Equal("Z:/bogus", config.Directory);
        }

        [Fact]
        public void MissingClosingBracket()
        {
            var config = ParseConfigFile(@"
[*.cs
my_prop = my_val");
            var properties = config.GlobalSection.Properties;
            AssertEx.SetEqual(
                new[] { KeyValuePair.Create("my_prop", "my_val")},
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
            Assert.Equal(new[] { KeyValuePair.Create("my_prop", "my_val")}, properties);
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
                EditorConfig.ReservedKeys.Select(k => "MY_" + k + " = MY_VAL")));
            AssertEx.SetEqual(
                EditorConfig.ReservedKeys.Select(k => KeyValuePair.Create("my_" + k, "MY_VAL")).ToList(),
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
                new[] { KeyValuePair.Create("my_key2", "my@val")},
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
                new[] { KeyValuePair.Create("long", "this value continues")},
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
                EditorConfig.ReservedValues.Select(v => "MY_KEY" + (index++) + " = " + v.ToUpperInvariant())));
            index = 0;
            AssertEx.SetEqual(
                EditorConfig.ReservedValues.Select(v => KeyValuePair.Create("my_key" + (index++), v)).ToList(),
                config.GlobalSection.Properties);
        }

        [Fact]
        public void ReservedKeys()
        {
            var config = ParseConfigFile(string.Join(Environment.NewLine,
                EditorConfig.ReservedKeys.Select(k => k + " = MY_VAL")));
            AssertEx.SetEqual(
                EditorConfig.ReservedKeys.Select(k => KeyValuePair.Create(k, "my_val")).ToList(),
                config.GlobalSection.Properties);
        }

        [Fact]
        public void SimpleNameMatch()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("abc");
            Assert.Equal("^abc$", regex);

            Assert.Matches(regex, "abc");
            Assert.DoesNotMatch(regex, "aabc");
            Assert.DoesNotMatch(regex, " abc");
            Assert.DoesNotMatch(regex, "cabc");
        }

        [Fact]
        public void StarOnlyMatch()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("*");
            Assert.Equal("^[^/]*$", regex);

            Assert.Matches(regex, "abc");
            Assert.Matches(regex, "123");
            Assert.DoesNotMatch(regex, "abc/123");
        }

        [Fact]
        public void StarNameMatch()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("*.cs");
            Assert.Equal("^[^/]*.cs$", regex);

            Assert.Matches(regex, "abc.cs");
            Assert.Matches(regex, "123.cs");
            // Only '/' is defined as a directory separator, so the caller
            // is responsible for converting any other machine directory
            // separators to '/' before matching
            Assert.Matches(regex, "dir\\subpath.cs");

            Assert.DoesNotMatch(regex, "abc.vb");
            Assert.DoesNotMatch(regex, "dir/subpath.cs");
        }

        [Fact]
        public void StarStarNameMatch()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("**.cs");
            Assert.Equal("^.*.cs$", regex);

            Assert.Matches(regex, "abc.cs");
            Assert.Matches(regex, "dir/subpath.cs");
        }

        [Fact]
        public void BadEscapeMatch()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("abc\\d.cs");
            Assert.Null(regex);
        }

        [Fact]
        public void EndBackslashMatch()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("abc\\");
            Assert.Null(regex);
        }

        [Fact]
        public void QuestionMatch()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("ab?def");
            Assert.Equal("^ab.def$", regex);

            Assert.Matches(regex, "abcdef");
            Assert.Matches(regex, "ab?def");
            Assert.Matches(regex, "abzdef");
            Assert.Matches(regex, "ab/def");
            Assert.Matches(regex, "ab\\def");
        }

        [Fact]
        public void LiteralBackslash()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("ab\\\\c");
            Assert.Equal("^ab\\\\c$", regex);

            Assert.Matches(regex, "ab\\c");
            Assert.DoesNotMatch(regex, "ab/c");
            Assert.DoesNotMatch(regex, "ab\\\\c");
        }

        [Fact]
        public void LiteralStars()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("\\***\\*\\**");
            Assert.Equal("^\\*.*\\*\\*[^/]*$", regex);

            Assert.Matches(regex, "*ab/cd**efg*");
            Assert.DoesNotMatch(regex, "ab/cd**efg*");
            Assert.DoesNotMatch(regex, "*ab/cd*efg*");
            Assert.DoesNotMatch(regex, "*ab/cd**ef/gh");
        }

        [Fact]
        public void LiteralQuestions()
        {
            string regex = EditorConfig.TryCompileSectionNameToRegEx("\\??\\?*\\??");
            Assert.Equal("^\\?.\\?[^/]*\\?.$", regex);

            Assert.Matches(regex, "?a?cde?f");
            Assert.Matches(regex, "???????f");
            Assert.DoesNotMatch(regex, "aaaaaaaa");
            Assert.DoesNotMatch(regex, "aa?cde?f");
            Assert.DoesNotMatch(regex, "?a?cdexf");
            Assert.DoesNotMatch(regex, "?axcde?f");
        }

        [Fact]
        public void EditorConfigToDiagnostics()
        {
            var configs = ArrayBuilder<EditorConfig>.GetInstance();
            configs.Add(EditorConfig.Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress

[*.vb]
dotnet_diagnostic.cs000.severity = error", "/"));

            var options = CommonCompiler.GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs,
                messageProvider: null,
                diagnostics: null);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Suppress)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                null
            }, options);
        }

        [Fact]
        public void LaterSectionOverrides()
        {
            var configs = ArrayBuilder<EditorConfig>.GetInstance();
            configs.Add(EditorConfig.Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress

[test.*]
dotnet_diagnostic.cs000.severity = error", "/"));

            var options = CommonCompiler.GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/test.vb", "/test" },
                configs,
                messageProvider: null,
                diagnostics: null);
            configs.Free();

            Assert.Equal(new[] {
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                CreateImmutableDictionary(("cs000", ReportDiagnostic.Error)),
                null
            }, options);
        }

        [Fact]
        public void TwoSettingsSameSection()
        {
            var configs = ArrayBuilder<EditorConfig>.GetInstance();
            configs.Add(EditorConfig.Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress
dotnet_diagnostic.cs001.severity = info", "/"));

            var options = CommonCompiler.GetAnalyzerConfigOptions(
                new[] { "/test.cs" },
                configs,
                messageProvider: null,
                diagnostics: null);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress),
                    ("cs001", ReportDiagnostic.Info)),
            }, options);
        }

        [Fact]
        public void TwoSettingsDifferentSections()
        {
            var configs = ArrayBuilder<EditorConfig>.GetInstance();
            configs.Add(EditorConfig.Parse(@"
[*.cs]
dotnet_diagnostic.cs000.severity = suppress

[test.*]
dotnet_diagnostic.cs001.severity = info", "/"));

            var options = CommonCompiler.GetAnalyzerConfigOptions(
                new[] { "/test.cs" },
                configs,
                messageProvider: null,
                diagnostics: null);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Suppress),
                    ("cs001", ReportDiagnostic.Info))
            }, options);
        }

        [Fact]
        public void MultipleEditorConfigs()
        {
            var configs = ArrayBuilder<EditorConfig>.GetInstance();
            configs.Add(EditorConfig.Parse(@"
[**/*]
dotnet_diagnostic.cs000.severity = suppress

[**test.*]
dotnet_diagnostic.cs001.severity = info", "/"));
            configs.Add(EditorConfig.Parse(@"
[**]
dotnet_diagnostic.cs000.severity = warn

[test.cs]
dotnet_diagnostic.cs001.severity = error", "/subdir/"));

            var options = CommonCompiler.GetAnalyzerConfigOptions(
                new[] { "/subdir/test.cs", "/subdir/test.vb" },
                configs,
                messageProvider: null,
                diagnostics: null);
            configs.Free();

            Assert.Equal(new[]
            {
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Warn),
                    ("cs001", ReportDiagnostic.Error)),
                CreateImmutableDictionary(
                    ("cs000", ReportDiagnostic.Warn),
                    ("cs001", ReportDiagnostic.Info))
            }, options);
        }

        [Fact]
        public void InheritOuterConfig()
        {
            var configs = ArrayBuilder<EditorConfig>.GetInstance();
            configs.Add(EditorConfig.Parse(@"
[**/*]
dotnet_diagnostic.cs000.severity = suppress

[**test.cs]
dotnet_diagnostic.cs001.severity = info", "/"));
            configs.Add(EditorConfig.Parse(@"
[test.cs]
dotnet_diagnostic.cs001.severity = error", "/subdir/"));

            var options = CommonCompiler.GetAnalyzerConfigOptions(
                new[] { "/test.cs", "/subdir/test.cs", "/subdir/test.vb" },
                configs,
                messageProvider: null,
                diagnostics: null);
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
            }, options);

        }
    }
}
