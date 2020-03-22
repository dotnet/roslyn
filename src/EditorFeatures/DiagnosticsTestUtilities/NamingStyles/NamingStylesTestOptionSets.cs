// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Options;
#else
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles
{
    public sealed class NamingStylesTestOptionSets
    {
        private readonly OptionKey _optionKey;

        public NamingStylesTestOptionSets(string languageName)
        {
            _optionKey = NamingStyleOptions.GetNamingPreferencesOptionKey(languageName);
        }

        public OptionKey OptionKey => _optionKey;

        public IDictionary<OptionKey, object> MergeStyles(IDictionary<OptionKey, object> first, IDictionary<OptionKey, object> second, string languageName)
        {
            var firstPreferences = (NamingStylePreferences)first.First().Value;
            var secondPreferences = (NamingStylePreferences)second.First().Value;
            return Options(_optionKey, new NamingStylePreferences(
                firstPreferences.SymbolSpecifications.AddRange(secondPreferences.SymbolSpecifications),
                firstPreferences.NamingStyles.AddRange(secondPreferences.NamingStyles),
                firstPreferences.NamingRules.AddRange(secondPreferences.NamingRules)));
        }

        public IDictionary<OptionKey, object> ClassNamesArePascalCase =>
            Options(_optionKey, ClassNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> FieldNamesAreCamelCase =>
            Options(_optionKey, FieldNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> FieldNamesAreCamelCaseWithUnderscorePrefix =>
            Options(_optionKey, FieldNamesAreCamelCaseWithUnderscorePrefixOption());

        public IDictionary<OptionKey, object> FieldNamesAreCamelCaseWithFieldUnderscorePrefix =>
            Options(_optionKey, FieldNamesAreCamelCaseWithFieldUnderscorePrefixOption());

        public IDictionary<OptionKey, object> FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix =>
            Options(_optionKey, FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffixOption());

        public IDictionary<OptionKey, object> MethodNamesArePascalCase =>
            Options(_optionKey, MethodNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> MethodNamesAreCamelCase =>
            Options(_optionKey, MethodNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> ParameterNamesAreCamelCase =>
            Options(_optionKey, ParameterNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> ParameterNamesAreCamelCaseWithPUnderscorePrefix =>
            Options(_optionKey, ParameterNamesAreCamelCaseWithPUnderscorePrefixOption());

        public IDictionary<OptionKey, object> ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix =>
            Options(_optionKey, ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffixOption());

        public IDictionary<OptionKey, object> LocalNamesAreCamelCase =>
            Options(_optionKey, LocalNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> LocalFunctionNamesAreCamelCase =>
            Options(_optionKey, LocalFunctionNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> PropertyNamesArePascalCase =>
            Options(_optionKey, PropertyNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> InterfaceNamesStartWithI =>
            Options(_optionKey, InterfaceNamesStartWithIOption());

        public IDictionary<OptionKey, object> TypeParameterNamesStartWithT =>
            Options(_optionKey, TypeParameterNamesStartWithTOption());

        public IDictionary<OptionKey, object> ConstantsAreUpperCase =>
            Options(_optionKey, ConstantsAreUpperCaseOption());

        public IDictionary<OptionKey, object> LocalsAreCamelCaseConstantsAreUpperCase =>
            Options(_optionKey, LocalsAreCamelCaseConstantsAreUpperCaseOption());

        public IDictionary<OptionKey, object> AsyncFunctionNamesEndWithAsync =>
            Options(_optionKey, AsyncFunctionNamesEndWithAsyncOption());

        public IDictionary<OptionKey, object> MethodNamesWithAccessibilityArePascalCase(ImmutableArray<Accessibility> accessibilities) =>
            Options(_optionKey, MethodNamesArePascalCaseOption(accessibilities));

        internal IDictionary<OptionKey, object> SymbolKindsArePascalCase(ImmutableArray<SymbolSpecification.SymbolKindOrTypeKind> symbolKinds) =>
            Options(_optionKey, SymbolKindsArePascalCaseOption(symbolKinds));

        internal IDictionary<OptionKey, object> SymbolKindsArePascalCaseEmpty() =>
            Options(_optionKey, SymbolKindsArePascalCaseOption(ImmutableArray<SymbolSpecification.SymbolKindOrTypeKind>.Empty));

        internal IDictionary<OptionKey, object> SymbolKindsArePascalCase(object symbolOrTypeKind) =>
            SymbolKindsArePascalCase(ImmutableArray.Create(ToSymbolKindOrTypeKind(symbolOrTypeKind)));

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

        internal IDictionary<OptionKey, object> AccessibilitiesArePascalCase(ImmutableArray<Accessibility> accessibilities) =>
            Options(_optionKey, AccessibilitiesArePascalCaseOption(accessibilities));

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

        private NamingStylePreferences FieldNamesAreCamelCaseOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
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

        private NamingStylePreferences FieldNamesAreCamelCaseWithUnderscorePrefixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
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

        private NamingStylePreferences FieldNamesAreCamelCaseWithFieldUnderscorePrefixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
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

        private NamingStylePreferences FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffixOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
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
                null,
                "Locals",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)),
                accessibilityList: default,
                modifiers: default);

            var constLocalsSymbolSpecification = new SymbolSpecification(
                null,
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
                null,
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
