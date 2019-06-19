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
            var namingStylePreferences = Assert.IsAssignableFrom<NamingStylePreferences>(combinedNamingStyles);
            Assert.Equal(ReportDiagnostic.Error, namingStylePreferences.Rules.NamingRules[0].EnforcementLevel);
        }

        [Theory]
        [InlineData("a", "b", "a", "public", "public, private")]
        [InlineData("b", "a", "a", "public, private", "public")]
        [InlineData("b", "a", "b", "public", "public, private")]
        [InlineData("a", "b", "b", "public, private", "public")]
        [InlineData("a", "b", "a", "*", "*")]
        [InlineData("b", "a", "a", "*", "*")]
        [InlineData("A", "b", "A", "*", "*")]
        [InlineData("b", "A", "A", "*", "*")]
        [InlineData("a", "B", "a", "*", "*")]
        [InlineData("B", "a", "a", "*", "*")]
        [InlineData("A", "B", "A", "*", "*")]
        [InlineData("B", "A", "A", "*", "*")]
        [InlineData("a", "A", "A", "*", "*")]
        [InlineData("A", "a", "A", "*", "*")]
        public static void TestOrderedByAccessibilityBeforeName(string firstName, string secondName, string firstNameAfterOrdering, string firstAccessibility, string secondAccessibility)
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var newDictionary = new Dictionary<string, string>()
            {
                [$"dotnet_naming_rule.{firstName}.severity"] = "error",
                [$"dotnet_naming_rule.{firstName}.symbols"] = "first_symbols",
                [$"dotnet_naming_rule.{firstName}.style"] = $"{firstName}_style",
                ["dotnet_naming_symbols.first_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.first_symbols.applicable_accessibilities"] = firstAccessibility,
                [$"dotnet_naming_style.{firstName}_style.capitalization"] = "pascal_case",
                [$"dotnet_naming_style.{secondName}_style.capitalization"] = "camel_case",
                [$"dotnet_naming_rule.{secondName}.severity"] = "error",
                [$"dotnet_naming_rule.{secondName}.symbols"] = "second_symbols",
                [$"dotnet_naming_rule.{secondName}.style"] = $"{secondName}_style",
                ["dotnet_naming_symbols.second_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.second_symbols.applicable_accessibilities"] = secondAccessibility,
            };

            var result = editorConfigStorageLocation.TryGetOption(
                newDictionary,
                typeof(NamingStylePreferences),
                out var combinedNamingStyles);

            var secondNameAfterOrdering = firstNameAfterOrdering == firstName ? secondName : firstName;
            Assert.True(result, "Expected non-empty dictionary to return true");
            var namingStylePreferences = Assert.IsAssignableFrom<NamingStylePreferences>(combinedNamingStyles);
            Assert.Equal($"{firstNameAfterOrdering}_style", namingStylePreferences.Rules.NamingRules[0].NamingStyle.Name);
            Assert.Equal($"{secondNameAfterOrdering}_style", namingStylePreferences.Rules.NamingRules[1].NamingStyle.Name);
        }

        [Theory]
        [InlineData("a", "b", "a", "static, readonly", "static")]
        [InlineData("b", "a", "a", "static", "static, readonly")]
        [InlineData("b", "a", "b", "static, readonly", "static")]
        [InlineData("a", "b", "b", "static", "static, readonly")]
        [InlineData("a", "b", "a", "", "")]
        [InlineData("b", "a", "a", "", "")]
        [InlineData("A", "b", "A", "", "")]
        [InlineData("b", "A", "A", "", "")]
        [InlineData("a", "B", "a", "", "")]
        [InlineData("B", "a", "a", "", "")]
        [InlineData("A", "B", "A", "", "")]
        [InlineData("B", "A", "A", "", "")]
        [InlineData("a", "A", "A", "*", "*")]
        [InlineData("A", "a", "A", "*", "*")]
        public static void TestOrderedByModifiersBeforeName(string firstName, string secondName, string firstNameAfterOrdering, string firstModifiers, string secondModifiers)
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var newDictionary = new Dictionary<string, string>()
            {
                [$"dotnet_naming_rule.{firstName}.severity"] = "error",
                [$"dotnet_naming_rule.{firstName}.symbols"] = "first_symbols",
                [$"dotnet_naming_rule.{firstName}.style"] = $"{firstName}_style",
                ["dotnet_naming_symbols.first_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.first_symbols.required_modifiers"] = firstModifiers,
                [$"dotnet_naming_style.{firstName}_style.capitalization"] = "pascal_case",
                [$"dotnet_naming_style.{secondName}_style.capitalization"] = "camel_case",
                [$"dotnet_naming_rule.{secondName}.severity"] = "error",
                [$"dotnet_naming_rule.{secondName}.symbols"] = "second_symbols",
                [$"dotnet_naming_rule.{secondName}.style"] = $"{secondName}_style",
                ["dotnet_naming_symbols.second_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.second_symbols.required_modifiers"] = secondModifiers,
            };

            var result = editorConfigStorageLocation.TryGetOption(
                newDictionary,
                typeof(NamingStylePreferences),
                out var combinedNamingStyles);

            var secondNameAfterOrdering = firstNameAfterOrdering == firstName ? secondName : firstName;
            Assert.True(result, "Expected non-empty dictionary to return true");
            var namingStylePreferences = Assert.IsAssignableFrom<NamingStylePreferences>(combinedNamingStyles);
            Assert.Equal($"{firstNameAfterOrdering}_style", namingStylePreferences.Rules.NamingRules[0].NamingStyle.Name);
            Assert.Equal($"{secondNameAfterOrdering}_style", namingStylePreferences.Rules.NamingRules[1].NamingStyle.Name);
        }

        [Theory]
        [InlineData("a", "b", "a", "method", "method, property")]
        [InlineData("b", "a", "a", "method, property", "method")]
        [InlineData("b", "a", "b", "method", "method, property")]
        [InlineData("a", "b", "b", "method, property", "method")]
        [InlineData("a", "b", "a", "*", "*")]
        [InlineData("b", "a", "a", "*", "*")]
        [InlineData("A", "b", "A", "*", "*")]
        [InlineData("b", "A", "A", "*", "*")]
        [InlineData("a", "B", "a", "*", "*")]
        [InlineData("B", "a", "a", "*", "*")]
        [InlineData("A", "B", "A", "*", "*")]
        [InlineData("B", "A", "A", "*", "*")]
        [InlineData("a", "A", "A", "*", "*")]
        [InlineData("A", "a", "A", "*", "*")]
        public static void TestOrderedBySymbolsBeforeName(string firstName, string secondName, string firstNameAfterOrdering, string firstSymbols, string secondSymbols)
        {
            var editorConfigStorageLocation = new NamingStylePreferenceEditorConfigStorageLocation();
            var newDictionary = new Dictionary<string, string>()
            {
                [$"dotnet_naming_rule.{firstName}.severity"] = "error",
                [$"dotnet_naming_rule.{firstName}.symbols"] = "first_symbols",
                [$"dotnet_naming_rule.{firstName}.style"] = $"{firstName}_style",
                ["dotnet_naming_symbols.first_symbols.applicable_kinds"] = firstSymbols,
                [$"dotnet_naming_style.{firstName}_style.capitalization"] = "pascal_case",
                [$"dotnet_naming_style.{secondName}_style.capitalization"] = "camel_case",
                [$"dotnet_naming_rule.{secondName}.severity"] = "error",
                [$"dotnet_naming_rule.{secondName}.symbols"] = "second_symbols",
                [$"dotnet_naming_rule.{secondName}.style"] = $"{secondName}_style",
                ["dotnet_naming_symbols.second_symbols.applicable_kinds"] = secondSymbols,
            };

            var result = editorConfigStorageLocation.TryGetOption(
                newDictionary,
                typeof(NamingStylePreferences),
                out var combinedNamingStyles);

            var secondNameAfterOrdering = firstNameAfterOrdering == firstName ? secondName : firstName;
            Assert.True(result, "Expected non-empty dictionary to return true");
            var namingStylePreferences = Assert.IsAssignableFrom<NamingStylePreferences>(combinedNamingStyles);
            Assert.Equal($"{firstNameAfterOrdering}_style", namingStylePreferences.Rules.NamingRules[0].NamingStyle.Name);
            Assert.Equal($"{secondNameAfterOrdering}_style", namingStylePreferences.Rules.NamingRules[1].NamingStyle.Name);
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
