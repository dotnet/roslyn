// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditorConfig.StorageLocation
{
    public class NamingStylePreferenceEditorConfigStorageLocationTests
    {
        [Fact]
        public static void TestEmptyDictionaryReturnNoNamingStylePreferencesObjectReturnsFalse()
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var result = editorConfigStorageLocation.TryGetOption(new Dictionary<string, string>(), typeof(NamingStylePreferences), out var @object);
            Assert.False(result, "Expected TryParseReadonlyDictionary to return 'false' for empty dictionary");
        }

        [Fact]
        public static void TestEmptyDictionaryDefaultNamingStylePreferencesObjectReturnsFalse()
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var result = editorConfigStorageLocation.TryGetOption(
                new Dictionary<string, string>(),
                typeof(NamingStylePreferences),
                out var @object);

            Assert.False(result, "Expected TryParseReadonlyDictionary to return 'false' for empty dictionary");
        }

        [Fact]
        public static void TestNonEmptyDictionaryReturnsTrue()
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var newDictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.severity"] = "error",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.symbols"] = "method_and_property_symbols",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.style"] = "pascal_case_style",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_accessibilities"] = "*",
                ["dotnet_naming_style.pascal_case_style.capitalization"] = "pascal_case"
            };

            var result = editorConfigStorageLocation.TryGetOption(
                newDictionary,
                typeof(NamingStylePreferences),
                out var combinedNamingStyles);

            Assert.True(result, "Expected non-empty dictionary to return true");
            var isNamingStylePreferencesObject = combinedNamingStyles is NamingStylePreferences;
            Assert.True(isNamingStylePreferencesObject, $"Expected returned object to be of type '{nameof(NamingStylePreferences)}'");
            Assert.Equal(ReportDiagnostic.Error, ((NamingStylePreferences)combinedNamingStyles).Rules.NamingRules[0].EnforcementLevel);
        }

        [Fact]
        public static void TestObjectTypeThrowsInvalidOperationException()
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            Assert.Throws<InvalidOperationException>(() =>
            {
                editorConfigStorageLocation.TryGetOption(new Dictionary<string, string>(), typeof(object), out var @object);
            });
        }
    }
}
