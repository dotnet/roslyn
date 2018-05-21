// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles
{
    public sealed class NamingStylesTestOptionSets
    {
        private readonly string languageName;

        public NamingStylesTestOptionSets(string languageName)
        {
            this.languageName = languageName;
        }

        public IDictionary<OptionKey, object> ClassNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), ClassNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> MethodNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), MethodNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> ParameterNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), ParameterNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> LocalNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), LocalNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> PropertyNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), PropertyNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> PrivateFieldNamesAreCamelCaseWithUnderscore =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), PrivateFieldWithUnderscore());

        public IDictionary<OptionKey, object> InterfaceNamesStartWithI =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), InterfacesNamesStartWithIOption());

        public IDictionary<OptionKey, object> ConstantsAreUpperCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), ConstantsAreUpperCaseOption());

        public IDictionary<OptionKey, object> LocalsAreCamelCaseConstantsAreUpperCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), LocalsAreCamelCaseConstantsAreUpperCaseOption());

        internal IDictionary<OptionKey, object> CreateInterfaceNamingRule(
           Accessibility accessibility = Accessibility.NotApplicable,
           Capitalization capitalization = Capitalization.PascalCase,
           string prefix = "I", string suffix = "", string wordSeparator = "") =>
           Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName),
               CreateNamingRule(TypeKind.Interface, capitalization, accessibility, prefix, suffix, wordSeparator));

        internal IDictionary<OptionKey, object> CreateFieldNamingRule(
            Accessibility accessibility = Accessibility.Private,
            Capitalization capitalization = Capitalization.CamelCase,
            string prefix = "", string suffix = "", string wordSeparator = "") =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName),
                CreateNamingRule(SymbolKind.Field, capitalization, accessibility, prefix, suffix, wordSeparator));

        internal IDictionary<OptionKey, object> CreatePropertyNamingRule(
            Accessibility accessibility = Accessibility.Public,
            Capitalization capitalization = Capitalization.PascalCase,
            string prefix = "", string suffix = "", string wordSeparator = "") =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName),
                CreateNamingRule(SymbolKind.Property, capitalization, accessibility, prefix, suffix, wordSeparator));

        private static IDictionary<OptionKey, object> Options(OptionKey option, object value)
        {
            return new Dictionary<OptionKey, object>
            {
                { option, value }
            };
        }

        private static NamingStylePreferences ClassNamesArePascalCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.PascalCase,
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "");
            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };
            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences MethodNamesArePascalCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Method)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.PascalCase,
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };
            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences ParameterNamesAreCamelCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences LocalNamesAreCamelCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences PropertyNamesArePascalCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Property)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.PascalCase,
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences PrivateFieldWithUnderscore()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                ImmutableArray.Create(Accessibility.Private),
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name",
                prefix: "_",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences CreateNamingRule(SymbolKind symbolKind, Capitalization capitalization, Accessibility accessibility,
           string prefix = "", string suffix = "", string wordSeparator = "")
        {
            return CreateNamingRule(
                new SymbolSpecification.SymbolKindOrTypeKind(symbolKind),
                capitalization, accessibility, prefix, suffix, wordSeparator);
        }

        private static NamingStylePreferences CreateNamingRule(TypeKind typeKind, Capitalization capitalization, Accessibility accessibility,
           string prefix = "", string suffix = "", string wordSeparator = "")
        {
            return CreateNamingRule(
                new SymbolSpecification.SymbolKindOrTypeKind(typeKind),
                capitalization, accessibility, prefix, suffix, wordSeparator);
        }


        private static NamingStylePreferences CreateNamingRule(SymbolSpecification.SymbolKindOrTypeKind symbolKindOrTypeKind, 
            Capitalization capitalization, Accessibility accessibility, 
            string prefix = "", string suffix = "", string wordSeparator = "")
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(symbolKindOrTypeKind),
                ImmutableArray.Create(accessibility),
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: capitalization,
                name: "Name",
                prefix: prefix,
                suffix: suffix,
                wordSeparator: wordSeparator);

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences InterfacesNamesStartWithIOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Interface)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.PascalCase,
                name: "Name",
                prefix: "I",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences ConstantsAreUpperCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(
                    new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
                    new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray.Create(new SymbolSpecification.ModifierKind(SymbolSpecification.ModifierKindEnum.IsConst)));

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.AllUpper,
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences LocalsAreCamelCaseConstantsAreUpperCaseOption()
        {
            var localsSymbolSpecification = new SymbolSpecification(
                null,
                "Locals",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

            var constLocalsSymbolSpecification = new SymbolSpecification(
                null,
                "Const Locals",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray.Create(new SymbolSpecification.ModifierKind(SymbolSpecification.ModifierKindEnum.IsConst)));

            var camelCaseNamingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Camel Case",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var allUpperNamingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.AllUpper,
                name: "All Upper",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var localsCamelCaseNamingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = localsSymbolSpecification.ID,
                NamingStyleID = camelCaseNamingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var constLocalsUpperCaseNamingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = constLocalsSymbolSpecification.ID,
                NamingStyleID = allUpperNamingStyle.ID,
                EnforcementLevel = DiagnosticSeverity.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(localsSymbolSpecification, constLocalsSymbolSpecification),
                ImmutableArray.Create(camelCaseNamingStyle, allUpperNamingStyle),
                ImmutableArray.Create(constLocalsUpperCaseNamingRule, localsCamelCaseNamingRule));

            return info;
        }
    }
}
