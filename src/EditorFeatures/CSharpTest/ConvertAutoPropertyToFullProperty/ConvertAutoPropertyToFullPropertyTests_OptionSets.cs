// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullProperty
{
    public partial class ConvertAutoPropertyToFullPropertyTests : AbstractCSharpCodeActionTest
    {
        private OptionsCollection PreferExpressionBodiedAccessorsWhenPossible
            => new OptionsCollection(GetLanguage()) { { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement } };

        private OptionsCollection PreferExpressionBodiedAccessorsWhenOnSingleLine
            => new OptionsCollection(GetLanguage()) { { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement } };

        private OptionsCollection DoNotPreferExpressionBodiedAccessors
            => new OptionsCollection(GetLanguage()) { { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement } };

        private OptionsCollection DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine
            => new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                { CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.All & ~NewLineBeforeOpenBracePlacement.Properties },
            };

        private OptionsCollection DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine
            => new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                { CSharpFormattingOptions2.NewLineBeforeOpenBrace, NewLineBeforeOpenBracePlacement.All & ~NewLineBeforeOpenBracePlacement.Accessors },
            };

        private OptionsCollection PreferExpressionBodiesOnAccessorsAndMethods
            => new OptionsCollection(GetLanguage())
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement },
            };

        private OptionsCollection UseCustomFieldName
            => new OptionsCollection(GetLanguage())
            {
                { NamingStyleOptions.NamingPreferences, CreateCustomFieldNamingStylePreference() },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        private OptionsCollection UseUnderscorePrefixedFieldName
            => new OptionsCollection(GetLanguage())
            {
                { NamingStyleOptions.NamingPreferences, CreateUnderscorePrefixedFieldNamingStylePreference() },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        private OptionsCollection UseCustomStaticFieldName
            => new OptionsCollection(GetLanguage())
            {
                { NamingStyleOptions.NamingPreferences, CreateCustomStaticFieldNamingStylePreference() },
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
            };

        private static NamingStylePreferences CreateCustomFieldNamingStylePreference()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
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
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences CreateUnderscorePrefixedFieldNamingStylePreference()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Field)),
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
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }

        private static NamingStylePreferences CreateCustomStaticFieldNamingStylePreference()
        {
            var symbolSpecification = new SymbolSpecification(
                Guid.NewGuid(),
                "Name",
                ImmutableArray.Create(new SymbolKindOrTypeKind(SymbolKind.Field)),
                accessibilityList: default,
                ImmutableArray.Create(new ModifierKind(DeclarationModifiers.Static)));

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
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));

            return info;
        }
    }
}
