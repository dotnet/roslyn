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

        public IDictionary<OptionKey, object> FieldNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), FieldNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> FieldNamesAreCamelCaseWithUnderscore =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), FieldNamesAreCamelCaseWithUnderscoreOption());

        public IDictionary<OptionKey, object> MethodNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), MethodNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> ParameterNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), ParameterNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> LocalNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), LocalNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> LocalFunctionNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), LocalFunctionNamesAreCamelCaseOption());

        public IDictionary<OptionKey, object> PropertyNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), PropertyNamesArePascalCaseOption());

        public IDictionary<OptionKey, object> InterfaceNamesStartWithI =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), InterfacesNamesStartWithIOption());

        public IDictionary<OptionKey, object> ConstantsAreUpperCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), ConstantsAreUpperCaseOption());

        public IDictionary<OptionKey, object> LocalsAreCamelCaseConstantsAreUpperCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), LocalsAreCamelCaseConstantsAreUpperCaseOption());

        public IDictionary<OptionKey, object> AsyncFunctionNamesEndWithAsync =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, languageName), AsyncFunctionNamesEndWithAsyncOption());

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
                EnforcementLevel = ReportDiagnostic.Error
            };

            var info = new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private NamingStylePreferences FieldNamesAreCamelCaseWithUnderscoreOption()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                ImmutableArray<Accessibility>.Empty,
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
                EnforcementLevel = ReportDiagnostic.Error
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
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary)),
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
                EnforcementLevel = ReportDiagnostic.Error
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
                ImmutableArray<Accessibility>.Empty,
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
