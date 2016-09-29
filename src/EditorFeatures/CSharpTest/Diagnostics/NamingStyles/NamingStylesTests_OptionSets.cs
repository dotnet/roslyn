// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public partial class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private IDictionary<OptionKey, object> ClassNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), ClassNamesArePascalCaseOptionString());

        private IDictionary<OptionKey, object> MethodNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), MethodNamesArePascalCaseOptionString());

        private IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            var options = new Dictionary<OptionKey, object>();
            options.Add(option, value);
            return options;
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        private string ClassNamesArePascalCaseOptionString()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(), 
                "Name", 
                SpecializedCollections.SingletonEnumerable(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class)).ToList(),
                SpecializedCollections.EmptyList<SymbolSpecification.AccessibilityKind>(),
                SpecializedCollections.EmptyList<SymbolSpecification.ModifierKind>(),
                SpecializedCollections.EmptyList<string>());

            var namingStyle = new NamingStyle();
            namingStyle.CapitalizationScheme = Capitalization.PascalCase;
            namingStyle.Name = "Name";
            namingStyle.Prefix = "";
            namingStyle.Suffix = "";
            namingStyle.WordSeparator = "";


            var namingRule = new SerializableNamingRule();
            namingRule.SymbolSpecificationID = symbolSpecification.ID;
            namingRule.NamingStyleID = namingStyle.ID;
            namingRule.EnforcementLevel = DiagnosticSeverity.Error;
            namingRule.Title = "Title";

            var info = new SerializableNamingStylePreferencesInfo();
            info.SymbolSpecifications.Add(symbolSpecification);
            info.NamingStyles.Add(namingStyle);
            info.NamingRules.Add(namingRule);

            return info.CreateXElement().ToString();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.NamingStyle)]
        private string MethodNamesArePascalCaseOptionString()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                SpecializedCollections.SingletonEnumerable(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Method)).ToList(),
                SpecializedCollections.EmptyList<SymbolSpecification.AccessibilityKind>(),
                SpecializedCollections.EmptyList<SymbolSpecification.ModifierKind>(),
                SpecializedCollections.EmptyList<string>());

            var namingStyle = new NamingStyle();
            namingStyle.CapitalizationScheme = Capitalization.PascalCase;
            namingStyle.Name = "Name";
            namingStyle.Prefix = "";
            namingStyle.Suffix = "";
            namingStyle.WordSeparator = "";


            var namingRule = new SerializableNamingRule();
            namingRule.SymbolSpecificationID = symbolSpecification.ID;
            namingRule.NamingStyleID = namingStyle.ID;
            namingRule.EnforcementLevel = DiagnosticSeverity.Error;
            namingRule.Title = "Title";

            var info = new SerializableNamingStylePreferencesInfo();
            info.SymbolSpecifications.Add(symbolSpecification);
            info.NamingStyles.Add(namingStyle);
            info.NamingRules.Add(namingRule);

            return info.CreateXElement().ToString();
        }
    }
}