// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles
{
    internal sealed class NamingStylesTestOptionSets
    {
        private readonly string _languageName;
        private readonly OptionKey2 _optionKey;

        public NamingStylesTestOptionSets(string languageName)
        {
            _languageName = languageName;
            _optionKey = new OptionKey2(NamingStyleOptions.NamingPreferences, languageName);
        }

        public OptionKey2 OptionKey => _optionKey;

        internal OptionsCollection MergeStyles(OptionsCollection first, OptionsCollection second)
        {
            var firstPreferences = (NamingStylePreferences)first.First().Value;
            var secondPreferences = (NamingStylePreferences)second.First().Value;
            return new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, new NamingStylePreferences(
                firstPreferences.SymbolSpecifications.AddRange(secondPreferences.SymbolSpecifications),
                firstPreferences.NamingStyles.AddRange(secondPreferences.NamingStyles),
                firstPreferences.NamingRules.AddRange(secondPreferences.NamingRules)) } };
        }

        internal OptionsCollection ClassNamesArePascalCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, ClassNamesArePascalCaseOption() } };

        internal OptionsCollection FieldNamesAreCamelCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseOption() } };

        internal OptionsCollection FieldNamesAreCamelCaseWithUnderscorePrefix
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseWithUnderscorePrefixOption() } };

        internal OptionsCollection FieldNamesAreCamelCaseWithFieldUnderscorePrefix
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseWithFieldUnderscorePrefixOption() } };

        internal OptionsCollection FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffixOption() } };

        internal OptionsCollection MethodNamesArePascalCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, MethodNamesArePascalCaseOption() } };

        internal OptionsCollection MethodNamesAreCamelCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, MethodNamesAreCamelCaseOption() } };

        internal OptionsCollection ParameterNamesAreCamelCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, ParameterNamesAreCamelCaseOption() } };

        internal OptionsCollection ParameterNamesAreCamelCaseWithPUnderscorePrefix
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, ParameterNamesAreCamelCaseWithPUnderscorePrefixOption() } };

        internal OptionsCollection ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffixOption() } };

        internal OptionsCollection LocalNamesAreCamelCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, LocalNamesAreCamelCaseOption() } };

        internal OptionsCollection LocalFunctionNamesAreCamelCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, LocalFunctionNamesAreCamelCaseOption() } };

        internal OptionsCollection PropertyNamesArePascalCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, PropertyNamesArePascalCaseOption() } };

        internal OptionsCollection InterfaceNamesStartWithI
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, InterfaceNamesStartWithIOption() } };

        internal OptionsCollection TypeParameterNamesStartWithT
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, TypeParameterNamesStartWithTOption() } };

        internal OptionsCollection ConstantsAreUpperCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, ConstantsAreUpperCaseOption() } };

        internal OptionsCollection LocalsAreCamelCaseConstantsAreUpperCase
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, LocalsAreCamelCaseConstantsAreUpperCaseOption() } };

        internal OptionsCollection AsyncFunctionNamesEndWithAsync
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, AsyncFunctionNamesEndWithAsyncOption() } };

        internal OptionsCollection MethodNamesWithAccessibilityArePascalCase(ImmutableArray<Accessibility> accessibilities)
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, MethodNamesArePascalCaseOption(accessibilities) } };

        internal OptionsCollection SymbolKindsArePascalCase(ImmutableArray<SymbolSpecification.SymbolKindOrTypeKind> symbolKinds)
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, SymbolKindsArePascalCaseOption(symbolKinds) } };

        internal OptionsCollection SymbolKindsArePascalCaseEmpty()
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, SymbolKindsArePascalCaseOption(ImmutableArray<SymbolSpecification.SymbolKindOrTypeKind>.Empty) } };

        internal OptionsCollection SymbolKindsArePascalCase(object symbolOrTypeKind)
            => SymbolKindsArePascalCase(ImmutableArray.Create(ToSymbolKindOrTypeKind(symbolOrTypeKind)));

        internal static SymbolSpecification.SymbolKindOrTypeKind ToSymbolKindOrTypeKind(object symbolOrTypeKind)
        {
            switch (symbolOrTypeKind)
            {
                case TypeKind typeKind:
                    return new SymbolSpecification.SymbolKindOrTypeKind(typeKind);

                case SymbolKind symbolKind:
                    return new SymbolSpecification.SymbolKindOrTypeKind(symbolKind);

                case MethodKind methodKind:
                    return new SymbolSpecification.SymbolKindOrTypeKind(methodKind);

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbolOrTypeKind);
            }
        }

        internal OptionsCollection AccessibilitiesArePascalCase(ImmutableArray<Accessibility> accessibilities)
            => new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, AccessibilitiesArePascalCaseOption(accessibilities) } };

        private static NamingStylePreferences ClassNamesArePascalCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };
            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences FieldNamesAreCamelCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences FieldNamesAreCamelCaseWithUnderscorePrefixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences FieldNamesAreCamelCaseWithFieldUnderscorePrefixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name",
                prefix: "field_",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name",
                prefix: "field_",
                suffix: "_End",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences MethodNamesArePascalCaseOption()
            => MethodNamesAreCasedOption(Capitalization.PascalCase);

        internal static NamingStylePreferences MethodNamesAreCamelCaseOption()
            => MethodNamesAreCasedOption(Capitalization.CamelCase);

        private static NamingStylePreferences MethodNamesAreCasedOption(Capitalization capitalization)
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: capitalization,
                name: "Name",
                prefix: "",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };
            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences MethodNamesArePascalCaseOption(ImmutableArray<Accessibility> accessibilities)
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary)),
                accessibilities,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };
            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences SymbolKindsArePascalCaseOption(ImmutableArray<SymbolSpecification.SymbolKindOrTypeKind> symbolKinds)
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                symbolKinds,
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };
            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences AccessibilitiesArePascalCaseOption(ImmutableArray<Accessibility> accessibilities)
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                symbolKindList: default,
                accessibilityList: accessibilities,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
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
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences ParameterNamesAreCamelCaseWithPUnderscorePrefixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name2",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name2",
                prefix: "p_",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name2",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.CamelCase,
                name: "Name2",
                prefix: "p_",
                suffix: "_End",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
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
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences LocalFunctionNamesAreCamelCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
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
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Property)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences InterfaceNamesStartWithIOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Interface)),
                accessibilityList: default,
                modifiers: default);

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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences TypeParameterNamesStartWithTOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.TypeParameter)),
                accessibilityList: default,
                modifiers: default);

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.PascalCase,
                name: "Name",
                prefix: "T",
                suffix: "",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
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
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(
                    new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
                    new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                accessibilityList: default,
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
                EnforcementLevel = ReportDiagnostic.Error
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
                Guid.NewGuid(),
                "Locals",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                accessibilityList: default,
                modifiers: default);

            var constLocalsSymbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Const Locals",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                accessibilityList: default,
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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var constLocalsUpperCaseNamingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = constLocalsSymbolSpecification.ID,
                NamingStyleID = allUpperNamingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(localsSymbolSpecification, constLocalsSymbolSpecification),
                ImmutableArray.Create(camelCaseNamingStyle, allUpperNamingStyle),
                ImmutableArray.Create(constLocalsUpperCaseNamingRule, localsCamelCaseNamingRule));

            return info;
        }

        private static NamingStylePreferences AsyncFunctionNamesEndWithAsyncOption()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(
                    new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary),
                    new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction)),
                accessibilityList: default,
                ImmutableArray.Create(new SymbolSpecification.ModifierKind(SymbolSpecification.ModifierKindEnum.IsAsync)));

            var namingStyle = new NamingStyle(
                Guid.NewGuid(),
                capitalizationScheme: Capitalization.PascalCase,
                name: "Name",
                prefix: "",
                suffix: "Async",
                wordSeparator: "");

            var namingRule = new SerializableNamingRule()
            {
                SymbolSpecificationID = symbolSpecification.ID,
                NamingStyleID = namingStyle.ID,
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }
    }
}
