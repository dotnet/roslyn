// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.NamingStyles
{
    public partial class NamingStylesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        private IDictionary<OptionKey, object> ClassNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), ClassNamesArePascalCaseOption());

        private IDictionary<OptionKey, object> MethodNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), MethodNamesArePascalCaseOption());

        private IDictionary<OptionKey, object> ParameterNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), ParameterNamesAreCamelCaseOption());

        private IDictionary<OptionKey, object> LocalNamesAreCamelCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), LocalNamesAreCamelCaseOption());

        private IDictionary<OptionKey, object> PropertyNamesArePascalCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), PropertyNamesArePascalCaseOption());

        private IDictionary<OptionKey, object> InterfaceNamesStartWithI =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), InterfacesNamesStartWithIOption());

        private IDictionary<OptionKey, object> ConstantsAreUpperCase =>
            Options(new OptionKey(SimplificationOptions.NamingPreferences, LanguageNames.CSharp), ConstantsAreUpperCaseOption());

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
    }
}
