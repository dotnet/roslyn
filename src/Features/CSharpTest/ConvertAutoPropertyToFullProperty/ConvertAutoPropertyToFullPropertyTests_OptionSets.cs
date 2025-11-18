// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NamingStyles;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullProperty;

public partial class ConvertAutoPropertyToFullPropertyTests
{
    private OptionsCollection PreferExpressionBodiedAccessorsWhenPossible
        => new(GetLanguage()) { { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement } };

    private OptionsCollection PreferExpressionBodiedAccessorsWhenOnSingleLine
        => new(GetLanguage()) { { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement } };

    private OptionsCollection DoNotPreferExpressionBodiedAccessors
        => new(GetLanguage()) { { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement } };

    private OptionsCollection DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.All & ~NewLineBeforeOpenBracePlacement.Properties },
        };

    private OptionsCollection DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.All & ~NewLineBeforeOpenBracePlacement.Accessors },
        };

    private OptionsCollection PreferExpressionBodiesOnAccessorsAndMethods
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
        };

    private OptionsCollection UseCustomFieldName
        => new(GetLanguage())
        {
            { NamingStyleOptions.NamingPreferences, CreateCustomFieldNamingStylePreference() },
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection UseUnderscorePrefixedFieldName
        => new(GetLanguage())
        {
            { NamingStyleOptions.NamingPreferences, CreateUnderscorePrefixedFieldNamingStylePreference() },
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private OptionsCollection UseCustomStaticFieldName
        => new(GetLanguage())
        {
            { NamingStyleOptions.NamingPreferences, CreateCustomStaticFieldNamingStylePreference() },
            { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
        };

    private static NamingStylePreferences CreateCustomFieldNamingStylePreference()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)],
            accessibilityList: default,
            modifiers: default);

        var namingStyle = new NamingStyle(
            Guid.NewGuid(),
            capitalizationScheme: Capitalization.PascalCase,
            name: "CustomFieldTest",
            prefix: "testing",
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

    private static NamingStylePreferences CreateUnderscorePrefixedFieldNamingStylePreference()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolKindOrTypeKind(SymbolKind.Field)],
            accessibilityList: default,
            modifiers: default);

        var namingStyle = new NamingStyle(
            Guid.NewGuid(),
            capitalizationScheme: Capitalization.CamelCase,
            name: "CustomFieldTest",
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

    private static NamingStylePreferences CreateCustomStaticFieldNamingStylePreference()
    {
        var symbolSpecification = new SymbolSpecification(
            Guid.NewGuid(),
            "Name",
            [new SymbolKindOrTypeKind(SymbolKind.Field)],
            accessibilityList: default,
            [new ModifierKind(Modifiers.Static)]);

        var namingStyle = new NamingStyle(
            Guid.NewGuid(),
            capitalizationScheme: Capitalization.PascalCase,
            name: "CustomStaticFieldTest",
            prefix: "staticfieldtest",
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
}
