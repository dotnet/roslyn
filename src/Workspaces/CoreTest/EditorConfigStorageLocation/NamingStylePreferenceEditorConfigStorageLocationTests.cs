// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.EditorConfig.StorageLocation
{
    public class NamingStylePreferenceEditorConfigStorageLocationTests
    {
        [Fact]
        public static void TestEmptyDictionaryReturnNoNamingStylePreferencesObjectReturnsFalse()
        {
            var options = StructuredAnalyzerConfigOptions.Create(DictionaryAnalyzerConfigOptions.EmptyDictionary);
            var value = options.GetNamingStylePreferences();
            Assert.True(value.IsEmpty);
        }

        [Fact]
        public static void TestNonEmptyDictionaryReturnsTrue()
        {
            var options = StructuredAnalyzerConfigOptions.Create(new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.severity"] = "error",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.symbols"] = "method_and_property_symbols",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.style"] = "pascal_case_style",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_accessibilities"] = "*",
                ["dotnet_naming_style.pascal_case_style.capitalization"] = "pascal_case"
            }.ToImmutableDictionary(AnalyzerConfigOptions.KeyComparer));

            var value = options.GetNamingStylePreferences();
            Assert.Equal(ReportDiagnostic.Error, value.Rules.NamingRules[0].EnforcementLevel);
        }
    }
}
