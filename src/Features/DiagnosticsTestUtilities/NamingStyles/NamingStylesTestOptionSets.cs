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

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.NamingStyles;

internal sealed class NamingStylesTestOptionSets
{
    private readonly string _languageName;

    public NamingStylesTestOptionSets(string languageName)
    {
        _languageName = languageName;
        OptionKey = new OptionKey2(NamingStyleOptions.NamingPreferences, languageName);
    }

    public OptionKey2 OptionKey { get; }

    internal OptionsCollection MergeStyles(OptionsCollection first, OptionsCollection second)
    {
        var firstPreferences = (NamingStylePreferences)first.First().Value;
        var secondPreferences = (NamingStylePreferences)second.First().Value;

        var mergedPreferences = new NamingStylePreferences(
                firstPreferences.SymbolSpecifications.AddRange(secondPreferences.SymbolSpecifications),
                firstPreferences.NamingStyles.AddRange(secondPreferences.NamingStyles),
                firstPreferences.Rules.NamingRules.AddRange(secondPreferences.Rules.NamingRules));

        return new OptionsCollection(_languageName) { { NamingStyleOptions.NamingPreferences, mergedPreferences } };
    }

    internal OptionsCollection ClassNamesArePascalCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, ClassNamesArePascalCaseOption() } };

    internal OptionsCollection FieldNamesAreCamelCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseOption() } };

    internal OptionsCollection FieldNamesAreCamelCaseWithUnderscorePrefix
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseWithUnderscorePrefixOption() } };

    internal OptionsCollection FieldNamesAreCamelCaseWithFieldUnderscorePrefix
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseWithFieldUnderscorePrefixOption() } };

    internal OptionsCollection FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffix
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffixOption() } };

    internal OptionsCollection MethodNamesArePascalCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, MethodNamesArePascalCaseOption() } };

    internal OptionsCollection MethodNamesAreCamelCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, MethodNamesAreCamelCaseOption() } };

    internal OptionsCollection ParameterNamesAreCamelCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, ParameterNamesAreCamelCaseOption() } };

    internal OptionsCollection ParameterNamesAreCamelCaseWithPUnderscorePrefix
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, ParameterNamesAreCamelCaseWithPUnderscorePrefixOption() } };

    internal OptionsCollection ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffix
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffixOption() } };

    internal OptionsCollection LocalNamesAreCamelCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, LocalNamesAreCamelCaseOption() } };

    internal OptionsCollection LocalFunctionNamesAreCamelCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, LocalFunctionNamesAreCamelCaseOption() } };

    internal OptionsCollection PropertyNamesArePascalCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, PropertyNamesArePascalCaseOption() } };

    internal OptionsCollection InterfaceNamesStartWithI
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, InterfaceNamesStartWithIOption() } };

    internal OptionsCollection TypeParameterNamesStartWithT
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, TypeParameterNamesStartWithTOption() } };

    internal OptionsCollection ConstantsAreUpperCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, ConstantsAreUpperCaseOption() } };

    internal OptionsCollection LocalsAreCamelCaseConstantsAreUpperCase
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, LocalsAreCamelCaseConstantsAreUpperCaseOption() } };

    internal OptionsCollection AsyncFunctionNamesEndWithAsync
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, AsyncFunctionNamesEndWithAsyncOption() } };

    internal OptionsCollection MethodNamesWithAccessibilityArePascalCase(ImmutableArray<Accessibility> accessibilities)
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, MethodNamesArePascalCaseOption(accessibilities) } };

    internal OptionsCollection SymbolKindsArePascalCase(ImmutableArray<SymbolSpecification.SymbolKindOrTypeKind> symbolKinds)
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, SymbolKindsArePascalCaseOption(symbolKinds) } };

    internal OptionsCollection SymbolKindsArePascalCaseEmpty()
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, SymbolKindsArePascalCaseOption([]) } };

    internal OptionsCollection SymbolKindsArePascalCase(object symbolOrTypeKind)
        => SymbolKindsArePascalCase([ToSymbolKindOrTypeKind(symbolOrTypeKind)]);

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
        => new(_languageName) { { NamingStyleOptions.NamingPreferences, AccessibilitiesArePascalCaseOption(accessibilities) } };

    private static NamingStylePreferences ClassNamesArePascalCaseOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Class)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences FieldNamesAreCamelCaseOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences FieldNamesAreCamelCaseWithUnderscorePrefixOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences FieldNamesAreCamelCaseWithFieldUnderscorePrefixOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences FieldNamesAreCamelCaseWithFieldUnderscorePrefixAndUnderscoreEndSuffixOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

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
            [new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences MethodNamesArePascalCaseOption(ImmutableArray<Accessibility> accessibilities)
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences ParameterNamesAreCamelCaseOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences ParameterNamesAreCamelCaseWithPUnderscorePrefixOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name2",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences ParameterNamesAreCamelCaseWithPUnderscorePrefixAndUnderscoreEndSuffixOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name2",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Parameter)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences LocalNamesAreCamelCaseOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences LocalFunctionNamesAreCamelCaseOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences PropertyNamesArePascalCaseOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Property)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences InterfaceNamesStartWithIOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Interface)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences TypeParameterNamesStartWithTOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.TypeParameter)],
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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences ConstantsAreUpperCaseOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [
                new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
                new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local),
            ],
            accessibilityList: default,
            [new SymbolSpecification.ModifierKind(SymbolSpecification.ModifierKindEnum.IsConst)]);

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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }

    private static NamingStylePreferences LocalsAreCamelCaseConstantsAreUpperCaseOption()
    {
        var localsSymbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Locals",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)],
            accessibilityList: default,
            modifiers: default);

        var constLocalsSymbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Const Locals",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Local)],
            accessibilityList: default,
            [new SymbolSpecification.ModifierKind(SymbolSpecification.ModifierKindEnum.IsConst)]);

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
            [localsSymbolSpecification, constLocalsSymbolSpecification],
            [camelCaseNamingStyle, allUpperNamingStyle],
            [constLocalsUpperCaseNamingRule, localsCamelCaseNamingRule]);

        return info;
    }

    private static NamingStylePreferences AsyncFunctionNamesEndWithAsyncOption()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [
                new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.Ordinary),
                new SymbolSpecification.SymbolKindOrTypeKind(MethodKind.LocalFunction),
            ],
            accessibilityList: default,
            [new SymbolSpecification.ModifierKind(SymbolSpecification.ModifierKindEnum.IsAsync)]);

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
            [symbolSpecification],
            [namingStyle],
            [namingRule]);

        return info;
    }
}
