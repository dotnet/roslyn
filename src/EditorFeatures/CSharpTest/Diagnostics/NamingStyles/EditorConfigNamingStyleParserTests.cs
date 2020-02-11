﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.EditorConfigNamingStyleParser;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public class EditorConfigNamingStyleParserTests
    {
        internal static SymbolKindOrTypeKind ToSymbolKindOrTypeKind(object symbolOrTypeKind)
        {
            switch (symbolOrTypeKind)
            {
                case TypeKind typeKind:
                    return new SymbolKindOrTypeKind(typeKind);

                case SymbolKind symbolKind:
                    return new SymbolKindOrTypeKind(symbolKind);

                case MethodKind methodKind:
                    return new SymbolKindOrTypeKind(methodKind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbolOrTypeKind);
            }
        }

        [Fact]
        public static void TestPascalCaseRule()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.severity"] = "warning",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.symbols"] = "method_and_property_symbols",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.style"] = "pascal_case_style",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_accessibilities"] = "*",
                ["dotnet_naming_style.pascal_case_style.capitalization"] = "pascal_case"
            };
            var result = ParseDictionary(dictionary);
            Assert.Single(result.NamingRules);
            var namingRule = result.NamingRules.Single();
            Assert.Single(result.NamingStyles);
            var namingStyle = result.NamingStyles.Single();
            Assert.Single(result.SymbolSpecifications);
            var symbolSpec = result.SymbolSpecifications.Single();
            Assert.Equal(namingStyle.ID, namingRule.NamingStyleID);
            Assert.Equal(symbolSpec.ID, namingRule.SymbolSpecificationID);
            Assert.Equal(ReportDiagnostic.Warn, namingRule.EnforcementLevel);
            Assert.Equal("method_and_property_symbols", symbolSpec.Name);
            var expectedApplicableSymbolKindList = new[]
            {
                new SymbolKindOrTypeKind(MethodKind.Ordinary),
                new SymbolKindOrTypeKind(SymbolKind.Property)
            };
            AssertEx.SetEqual(expectedApplicableSymbolKindList, symbolSpec.ApplicableSymbolKindList);
            Assert.Empty(symbolSpec.RequiredModifierList);
            var expectedApplicableAccessibilityList = new[]
            {
                Accessibility.NotApplicable,
                Accessibility.Public,
                Accessibility.Internal,
                Accessibility.Private,
                Accessibility.Protected,
                Accessibility.ProtectedAndInternal,
                Accessibility.ProtectedOrInternal
            };
            AssertEx.SetEqual(expectedApplicableAccessibilityList, symbolSpec.ApplicableAccessibilityList);
            Assert.Equal("pascal_case_style", namingStyle.Name);
            Assert.Equal("", namingStyle.Prefix);
            Assert.Equal("", namingStyle.Suffix);
            Assert.Equal("", namingStyle.WordSeparator);
            Assert.Equal(Capitalization.PascalCase, namingStyle.CapitalizationScheme);
        }

        [Fact]
        [WorkItem(40705, "https://github.com/dotnet/roslyn/issues/40705")]
        public static void TestPascalCaseRuleWithKeyCapitalization()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.severity"] = "warning",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.symbols"] = "Method_and_Property_symbols",
                ["dotnet_naming_rule.methods_and_properties_must_be_pascal_case.style"] = "Pascal_Case_style",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_kinds"] = "method,property",
                ["dotnet_naming_symbols.method_and_property_symbols.applicable_accessibilities"] = "*",
                ["dotnet_naming_style.pascal_case_style.capitalization"] = "pascal_case"
            };
            var result = ParseDictionary(dictionary);
            var namingRule = Assert.Single(result.NamingRules);
            var namingStyle = Assert.Single(result.NamingStyles);
            var symbolSpec = Assert.Single(result.SymbolSpecifications);
            Assert.Equal(namingStyle.ID, namingRule.NamingStyleID);
            Assert.Equal(symbolSpec.ID, namingRule.SymbolSpecificationID);
            Assert.Equal(ReportDiagnostic.Warn, namingRule.EnforcementLevel);
        }

        [Fact]
        public static void TestAsyncMethodsAndLocalFunctionsRule()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.async_methods_must_end_with_async.severity"] = "error",
                ["dotnet_naming_rule.async_methods_must_end_with_async.symbols"] = "method_symbols",
                ["dotnet_naming_rule.async_methods_must_end_with_async.style"] = "end_in_async_style",
                ["dotnet_naming_symbols.method_symbols.applicable_kinds"] = "method,local_function",
                ["dotnet_naming_symbols.method_symbols.required_modifiers"] = "async",
                ["dotnet_naming_style.end_in_async_style.capitalization "] = "pascal_case",
                ["dotnet_naming_style.end_in_async_style.required_suffix"] = "Async",
            };
            var result = ParseDictionary(dictionary);
            Assert.Single(result.NamingRules);
            var namingRule = result.NamingRules.Single();
            Assert.Single(result.NamingStyles);
            var namingStyle = result.NamingStyles.Single();
            Assert.Single(result.SymbolSpecifications);
            var symbolSpec = result.SymbolSpecifications.Single();
            Assert.Equal(namingStyle.ID, namingRule.NamingStyleID);
            Assert.Equal(symbolSpec.ID, namingRule.SymbolSpecificationID);
            Assert.Equal(ReportDiagnostic.Error, namingRule.EnforcementLevel);
            Assert.Equal("method_symbols", symbolSpec.Name);
            var expectedApplicableSymbolKindList = new[]
            {
                new SymbolKindOrTypeKind(MethodKind.Ordinary),
                new SymbolKindOrTypeKind(MethodKind.LocalFunction)
            };
            AssertEx.SetEqual(expectedApplicableSymbolKindList, symbolSpec.ApplicableSymbolKindList);
            Assert.Single(symbolSpec.RequiredModifierList);
            Assert.Contains(new ModifierKind(ModifierKindEnum.IsAsync), symbolSpec.RequiredModifierList);
            Assert.Equal(
                new[] { Accessibility.NotApplicable, Accessibility.Public, Accessibility.Internal, Accessibility.Private, Accessibility.Protected, Accessibility.ProtectedAndInternal, Accessibility.ProtectedOrInternal },
                symbolSpec.ApplicableAccessibilityList);
            Assert.Equal("end_in_async_style", namingStyle.Name);
            Assert.Equal("", namingStyle.Prefix);
            Assert.Equal("Async", namingStyle.Suffix);
            Assert.Equal("", namingStyle.WordSeparator);
            Assert.Equal(Capitalization.PascalCase, namingStyle.CapitalizationScheme);
        }

        [Fact]
        public static void TestRuleWithoutCapitalization()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.async_methods_must_end_with_async.symbols"] = "any_async_methods",
                ["dotnet_naming_rule.async_methods_must_end_with_async.style"] = "end_in_async",
                ["dotnet_naming_rule.async_methods_must_end_with_async.severity"] = "suggestion",
                ["dotnet_naming_symbols.any_async_methods.applicable_kinds"] = "method",
                ["dotnet_naming_symbols.any_async_methods.applicable_accessibilities"] = "*",
                ["dotnet_naming_symbols.any_async_methods.required_modifiers"] = "async",
                ["dotnet_naming_style.end_in_async.required_suffix"] = "Async",
            };
            var result = ParseDictionary(dictionary);
            Assert.Empty(result.NamingStyles);
        }

        [Fact]
        public static void TestPublicMembersCapitalizedRule()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.public_members_must_be_capitalized.severity"] = "suggestion",
                ["dotnet_naming_rule.public_members_must_be_capitalized.symbols"] = "public_symbols",
                ["dotnet_naming_rule.public_members_must_be_capitalized.style"] = "first_word_upper_case_style",
                ["dotnet_naming_symbols.public_symbols.applicable_kinds"] = "property,method,field,event,delegate",
                ["dotnet_naming_symbols.public_symbols.applicable_accessibilities"] = "public,internal,protected,protected_internal",
                ["dotnet_naming_style.first_word_upper_case_style.capitalization"] = "first_word_upper",
            };
            var result = ParseDictionary(dictionary);
            Assert.Single(result.NamingRules);
            var namingRule = result.NamingRules.Single();
            Assert.Single(result.NamingStyles);
            var namingStyle = result.NamingStyles.Single();
            Assert.Single(result.SymbolSpecifications);
            var symbolSpec = result.SymbolSpecifications.Single();
            Assert.Equal(namingStyle.ID, namingRule.NamingStyleID);
            Assert.Equal(symbolSpec.ID, namingRule.SymbolSpecificationID);
            Assert.Equal(ReportDiagnostic.Info, namingRule.EnforcementLevel);
            Assert.Equal("public_symbols", symbolSpec.Name);
            var expectedApplicableSymbolKindList = new[]
            {
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary),
                new SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolKindOrTypeKind(SymbolKind.Event),
                new SymbolKindOrTypeKind(TypeKind.Delegate)
            };
            AssertEx.SetEqual(expectedApplicableSymbolKindList, symbolSpec.ApplicableSymbolKindList);
            var expectedApplicableAccessibilityList = new[]
            {
                Accessibility.Public,
                Accessibility.Internal,
                Accessibility.Protected,
                Accessibility.ProtectedOrInternal
            };
            AssertEx.SetEqual(expectedApplicableAccessibilityList, symbolSpec.ApplicableAccessibilityList);
            Assert.Empty(symbolSpec.RequiredModifierList);
            Assert.Equal("first_word_upper_case_style", namingStyle.Name);
            Assert.Equal("", namingStyle.Prefix);
            Assert.Equal("", namingStyle.Suffix);
            Assert.Equal("", namingStyle.WordSeparator);
            Assert.Equal(Capitalization.FirstUpper, namingStyle.CapitalizationScheme);
        }

        [Fact]
        public static void TestNonPublicMembersLowerCaseRule()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.non_public_members_must_be_lower_case.severity"] = "incorrect",
                ["dotnet_naming_rule.non_public_members_must_be_lower_case.symbols "] = "non_public_symbols",
                ["dotnet_naming_rule.non_public_members_must_be_lower_case.style   "] = "all_lower_case_style",
                ["dotnet_naming_symbols.non_public_symbols.applicable_kinds  "] = "property,method,field,event,delegate",
                ["dotnet_naming_symbols.non_public_symbols.applicable_accessibilities"] = "private",
                ["dotnet_naming_style.all_lower_case_style.capitalization"] = "all_lower",
            };
            var result = ParseDictionary(dictionary);
            Assert.Single(result.NamingRules);
            var namingRule = result.NamingRules.Single();
            Assert.Single(result.NamingStyles);
            var namingStyle = result.NamingStyles.Single();
            Assert.Single(result.SymbolSpecifications);
            var symbolSpec = result.SymbolSpecifications.Single();
            Assert.Equal(namingStyle.ID, namingRule.NamingStyleID);
            Assert.Equal(symbolSpec.ID, namingRule.SymbolSpecificationID);
            Assert.Equal(ReportDiagnostic.Hidden, namingRule.EnforcementLevel);
            Assert.Equal("non_public_symbols", symbolSpec.Name);
            var expectedApplicableSymbolKindList = new[]
            {
                new SymbolKindOrTypeKind(SymbolKind.Property),
                new SymbolKindOrTypeKind(MethodKind.Ordinary),
                new SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolKindOrTypeKind(SymbolKind.Event),
                new SymbolKindOrTypeKind(TypeKind.Delegate)
            };
            AssertEx.SetEqual(expectedApplicableSymbolKindList, symbolSpec.ApplicableSymbolKindList);
            Assert.Single(symbolSpec.ApplicableAccessibilityList);
            Assert.Contains(Accessibility.Private, symbolSpec.ApplicableAccessibilityList);
            Assert.Empty(symbolSpec.RequiredModifierList);
            Assert.Equal("all_lower_case_style", namingStyle.Name);
            Assert.Equal("", namingStyle.Prefix);
            Assert.Equal("", namingStyle.Suffix);
            Assert.Equal("", namingStyle.WordSeparator);
            Assert.Equal(Capitalization.AllLower, namingStyle.CapitalizationScheme);
        }

        [Fact]
        public static void TestParametersAndLocalsAreCamelCaseRule()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.parameters_and_locals_are_camel_case.severity"] = "suggestion",
                ["dotnet_naming_rule.parameters_and_locals_are_camel_case.symbols"] = "parameters_and_locals",
                ["dotnet_naming_rule.parameters_and_locals_are_camel_case.style"] = "camel_case_style",
                ["dotnet_naming_symbols.parameters_and_locals.applicable_kinds"] = "parameter,local",
                ["dotnet_naming_style.camel_case_style.capitalization"] = "camel_case",
            };

            var result = ParseDictionary(dictionary);
            Assert.Single(result.NamingRules);
            var namingRule = result.NamingRules.Single();
            Assert.Single(result.NamingStyles);
            var namingStyle = result.NamingStyles.Single();
            Assert.Single(result.SymbolSpecifications);

            var symbolSpec = result.SymbolSpecifications.Single();
            Assert.Equal(namingStyle.ID, namingRule.NamingStyleID);
            Assert.Equal(symbolSpec.ID, namingRule.SymbolSpecificationID);
            Assert.Equal(ReportDiagnostic.Info, namingRule.EnforcementLevel);

            Assert.Equal("parameters_and_locals", symbolSpec.Name);
            var expectedApplicableSymbolKindList = new[]
            {
                new SymbolKindOrTypeKind(SymbolKind.Parameter),
                new SymbolKindOrTypeKind(SymbolKind.Local),
            };
            AssertEx.SetEqual(expectedApplicableSymbolKindList, symbolSpec.ApplicableSymbolKindList);
            Assert.Equal(
                new[] { Accessibility.NotApplicable, Accessibility.Public, Accessibility.Internal, Accessibility.Private, Accessibility.Protected, Accessibility.ProtectedAndInternal, Accessibility.ProtectedOrInternal },
                symbolSpec.ApplicableAccessibilityList);
            Assert.Empty(symbolSpec.RequiredModifierList);

            Assert.Equal("camel_case_style", namingStyle.Name);
            Assert.Equal("", namingStyle.Prefix);
            Assert.Equal("", namingStyle.Suffix);
            Assert.Equal("", namingStyle.WordSeparator);
            Assert.Equal(Capitalization.CamelCase, namingStyle.CapitalizationScheme);
        }

        [Fact]
        public static void TestLocalFunctionsAreCamelCaseRule()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.local_functions_are_camel_case.severity"] = "suggestion",
                ["dotnet_naming_rule.local_functions_are_camel_case.symbols"] = "local_functions",
                ["dotnet_naming_rule.local_functions_are_camel_case.style"] = "camel_case_style",
                ["dotnet_naming_symbols.local_functions.applicable_kinds"] = "local_function",
                ["dotnet_naming_style.camel_case_style.capitalization"] = "camel_case",
            };

            var result = ParseDictionary(dictionary);
            Assert.Single(result.NamingRules);
            var namingRule = result.NamingRules.Single();
            Assert.Single(result.NamingStyles);
            var namingStyle = result.NamingStyles.Single();
            Assert.Single(result.SymbolSpecifications);

            var symbolSpec = result.SymbolSpecifications.Single();
            Assert.Equal(namingStyle.ID, namingRule.NamingStyleID);
            Assert.Equal(symbolSpec.ID, namingRule.SymbolSpecificationID);
            Assert.Equal(ReportDiagnostic.Info, namingRule.EnforcementLevel);

            Assert.Equal("local_functions", symbolSpec.Name);
            var expectedApplicableSymbolKindList = new[] { new SymbolKindOrTypeKind(MethodKind.LocalFunction) };
            AssertEx.SetEqual(expectedApplicableSymbolKindList, symbolSpec.ApplicableSymbolKindList);
            Assert.Equal(
                new[] { Accessibility.NotApplicable, Accessibility.Public, Accessibility.Internal, Accessibility.Private, Accessibility.Protected, Accessibility.ProtectedAndInternal, Accessibility.ProtectedOrInternal },
                symbolSpec.ApplicableAccessibilityList);
            Assert.Empty(symbolSpec.RequiredModifierList);

            Assert.Equal("camel_case_style", namingStyle.Name);
            Assert.Equal("", namingStyle.Prefix);
            Assert.Equal("", namingStyle.Suffix);
            Assert.Equal("", namingStyle.WordSeparator);
            Assert.Equal(Capitalization.CamelCase, namingStyle.CapitalizationScheme);
        }

        [Fact]
        public static void TestNoRulesAreReturned()
        {
            var dictionary = new Dictionary<string, string>()
            {
                ["dotnet_naming_symbols.non_public_symbols.applicable_kinds  "] = "property,method,field,event,delegate",
                ["dotnet_naming_symbols.non_public_symbols.applicable_accessibilities"] = "private",
                ["dotnet_naming_style.all_lower_case_style.capitalization"] = "all_lower",
            };
            var result = ParseDictionary(dictionary);
            Assert.Empty(result.NamingRules);
            Assert.Empty(result.NamingStyles);
            Assert.Empty(result.SymbolSpecifications);
        }

        [Theory]
        [InlineData("property,method", new object[] { SymbolKind.Property, MethodKind.Ordinary })]
        [InlineData("namespace", new object[] { SymbolKind.Namespace })]
        [InlineData("type_parameter", new object[] { SymbolKind.TypeParameter })]
        [InlineData("*", new object[] { SymbolKind.Namespace, TypeKind.Class, TypeKind.Struct, TypeKind.Interface, TypeKind.Enum, SymbolKind.Property, MethodKind.Ordinary, MethodKind.LocalFunction, SymbolKind.Field, SymbolKind.Event, TypeKind.Delegate, SymbolKind.Parameter, SymbolKind.TypeParameter, SymbolKind.Local })]
        [InlineData(null, new object[] { SymbolKind.Namespace, TypeKind.Class, TypeKind.Struct, TypeKind.Interface, TypeKind.Enum, SymbolKind.Property, MethodKind.Ordinary, MethodKind.LocalFunction, SymbolKind.Field, SymbolKind.Event, TypeKind.Delegate, SymbolKind.Parameter, SymbolKind.TypeParameter, SymbolKind.Local })]
        [InlineData("property,method,invalid", new object[] { SymbolKind.Property, MethodKind.Ordinary })]
        [InlineData("invalid", new object[] { })]
        [InlineData("", new object[] { })]
        [WorkItem(20907, "https://github.com/dotnet/roslyn/issues/20907")]
        public static void TestApplicableKindsParse(string specification, object[] typeOrSymbolKinds)
        {
            var rule = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.kinds_parse.severity"] = "error",
                ["dotnet_naming_rule.kinds_parse.symbols"] = "kinds",
                ["dotnet_naming_rule.kinds_parse.style"] = "pascal_case",
                ["dotnet_naming_style.pascal_case.capitalization "] = "pascal_case",
            };

            if (specification != null)
            {
                rule["dotnet_naming_symbols.kinds.applicable_kinds"] = specification;
            }

            var kinds = typeOrSymbolKinds.Select(ToSymbolKindOrTypeKind).ToArray();
            var result = ParseDictionary(rule);
            Assert.Equal(kinds, result.SymbolSpecifications.SelectMany(x => x.ApplicableSymbolKindList));
        }

        [Theory]
        [InlineData("internal,protected_internal", new[] { Accessibility.Internal, Accessibility.ProtectedOrInternal })]
        [InlineData("friend,protected_friend", new[] { Accessibility.Friend, Accessibility.ProtectedOrFriend })]
        [InlineData("private_protected", new[] { Accessibility.ProtectedAndInternal })]
        [InlineData("local", new[] { Accessibility.NotApplicable })]
        [InlineData("*", new[] { Accessibility.NotApplicable, Accessibility.Public, Accessibility.Internal, Accessibility.Private, Accessibility.Protected, Accessibility.ProtectedAndInternal, Accessibility.ProtectedOrInternal })]
        [InlineData(null, new[] { Accessibility.NotApplicable, Accessibility.Public, Accessibility.Internal, Accessibility.Private, Accessibility.Protected, Accessibility.ProtectedAndInternal, Accessibility.ProtectedOrInternal })]
        [InlineData("internal,protected,invalid", new[] { Accessibility.Internal, Accessibility.Protected })]
        [InlineData("invalid", new Accessibility[] { })]
        [InlineData("", new Accessibility[] { })]
        [WorkItem(20907, "https://github.com/dotnet/roslyn/issues/20907")]
        public static void TestApplicableAccessibilitiesParse(string specification, Accessibility[] accessibilities)
        {
            var rule = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.accessibilities_parse.severity"] = "error",
                ["dotnet_naming_rule.accessibilities_parse.symbols"] = "accessibilities",
                ["dotnet_naming_rule.accessibilities_parse.style"] = "pascal_case",
                ["dotnet_naming_style.pascal_case.capitalization "] = "pascal_case",
            };

            if (specification != null)
            {
                rule["dotnet_naming_symbols.accessibilities.applicable_accessibilities"] = specification;
            }

            var result = ParseDictionary(rule);
            Assert.Equal(accessibilities, result.SymbolSpecifications.SelectMany(x => x.ApplicableAccessibilityList));
        }

        [Fact]
        public static void TestRequiredModifiersParse()
        {
            var charpRule = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.modifiers_parse.severity"] = "error",
                ["dotnet_naming_rule.modifiers_parse.symbols"] = "modifiers",
                ["dotnet_naming_rule.modifiers_parse.style"] = "pascal_case",
                ["dotnet_naming_symbols.modifiers.required_modifiers"] = "abstract,static",
                ["dotnet_naming_style.pascal_case.capitalization "] = "pascal_case",
            };
            var vbRule = new Dictionary<string, string>()
            {
                ["dotnet_naming_rule.modifiers_parse.severity"] = "error",
                ["dotnet_naming_rule.modifiers_parse.symbols"] = "modifiers",
                ["dotnet_naming_rule.modifiers_parse.style"] = "pascal_case",
                ["dotnet_naming_symbols.modifiers.required_modifiers"] = "must_inherit,shared",
                ["dotnet_naming_style.pascal_case.capitalization "] = "pascal_case",
            };

            var csharpResult = ParseDictionary(charpRule);
            var vbResult = ParseDictionary(vbRule);

            Assert.Equal(csharpResult.SymbolSpecifications.SelectMany(x => x.RequiredModifierList.Select(y => y.Modifier)),
                         vbResult.SymbolSpecifications.SelectMany(x => x.RequiredModifierList.Select(y => y.Modifier)));
            Assert.Equal(csharpResult.SymbolSpecifications.SelectMany(x => x.RequiredModifierList.Select(y => y.ModifierKindWrapper)),
                         vbResult.SymbolSpecifications.SelectMany(x => x.RequiredModifierList.Select(y => y.ModifierKindWrapper)));
        }
    }
}
