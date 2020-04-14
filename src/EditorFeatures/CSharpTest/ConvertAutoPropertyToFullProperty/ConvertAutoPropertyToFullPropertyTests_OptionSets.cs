// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullProperty
{
    public partial class ConvertAutoPropertyToFullPropertyTests : AbstractCSharpCodeActionTest
    {
        private IDictionary<OptionKey2, object> PreferExpressionBodiedAccessorsWhenPossible
            => OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));

        private IDictionary<OptionKey2, object> PreferExpressionBodiedAccessorsWhenOnSingleLine
            => OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenOnSingleLineWithSilentEnforcement));

        private IDictionary<OptionKey2, object> DoNotPreferExpressionBodiedAccessors
            => OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement));

        private IDictionary<OptionKey2, object> DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine
            => OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                SingleOption(CSharpFormattingOptions2.NewLinesForBracesInProperties, false));

        private IDictionary<OptionKey2, object> DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine
            => OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement),
                SingleOption(CSharpFormattingOptions2.NewLinesForBracesInAccessors, false));

        private IDictionary<OptionKey2, object> PreferExpressionBodiesOnAccessorsAndMethods
            => OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement));

        private IDictionary<OptionKey2, object> UseCustomFieldName
            => OptionsSet(
                SingleOption(NamingStyleOptions.NamingPreferences, CreateCustomFieldNamingStylePreference()),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement));

        private IDictionary<OptionKey2, object> UseUnderscorePrefixedFieldName
            => OptionsSet(
                SingleOption(NamingStyleOptions.NamingPreferences, CreateUnderscorePrefixedFieldNamingStylePreference()),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement));

        private IDictionary<OptionKey2, object> UseCustomStaticFieldName
            => OptionsSet(
                SingleOption(NamingStyleOptions.NamingPreferences, CreateCustomStaticFieldNamingStylePreference()),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement));

        private NamingStylePreferences CreateCustomFieldNamingStylePreference()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
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

        private NamingStylePreferences CreateUnderscorePrefixedFieldNamingStylePreference()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
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

        private NamingStylePreferences CreateCustomStaticFieldNamingStylePreference()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
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
