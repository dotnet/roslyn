// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditorConfig.StorageLocation
{
    public class NamingStylePreferenceEditorConfigStorageLocationTests
    {
        [Fact]
        public static void TestEmptyDictionaryReturnNoNamingStylePreferencesObjectReturnsFalse()
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var result = editorConfigStorageLocation.TryGetOption(StructuredAnalyzerConfigOptions.Create(DictionaryAnalyzerConfigOptions.EmptyDictionary), typeof(NamingStylePreferences), out _);
            Assert.False(result, "Expected TryParseReadonlyDictionary to return 'false' for empty dictionary");
        }

        [Fact]
        public static void TestNonEmptyDictionaryReturnsTrue()
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var options = StructuredAnalyzerConfigOptions.Create(new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.severity"] = "error",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.symbols"] = "method_and_property_symbols",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.style"] = "pascal_case_style",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_accessibilities"] = "*",
                ["dotnet_naming_style.pascal_case_style.capitalization"] = "pascal_case"
            }.ToImmutableDictionary(AnalyzerConfigOptions.KeyComparer));

            var result = editorConfigStorageLocation.TryGetOption(options, typeof(NamingStylePreferences), out var value);

            Assert.True(result, "Expected non-empty dictionary to return true");
            var namingStylePreferences = Assert.IsAssignableFrom<NamingStylePreferences>(value);
            Assert.Equal(ReportDiagnostic.Error, namingStylePreferences.Rules.NamingRules[0].EnforcementLevel);
        }

        [Fact]
        public static void TestObjectTypeThrowsInvalidOperationException()
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            Assert.Throws<InvalidOperationException>(() =>
            {
                editorConfigStorageLocation.TryGetOption(StructuredAnalyzerConfigOptions.Create(DictionaryAnalyzerConfigOptions.EmptyDictionary), typeof(object), out var @object);
            });
        }
    }
}
