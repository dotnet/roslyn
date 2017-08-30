using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.NamingStyles;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertAutoPropertyToFullPropertyTests
{
    public partial class ConvertAutoPropertyToFullPropertyTests : AbstractCSharpCodeActionTest
    {
        private IDictionary<OptionKey, object> PreferExpressionBodiedAccessorsWhenPossible =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement));

        private IDictionary<OptionKey, object> PreferExpressionBodiedAccessorsWhenOnSingleLine =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenOnSingleLineWithNoneEnforcement));

        private IDictionary<OptionKey, object> DoNotPreferExpressionBodiedAccessors =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> DoNotPreferExpressionBodiedAccessorsAndPropertyOpenBraceOnSameLine =>
             OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                SingleOption(CSharpFormattingOptions.NewLinesForBracesInProperties, false));

        private IDictionary<OptionKey, object> DoNotPreferExpressionBodiedAccessorsAndAccessorOpenBraceOnSameLine =>
             OptionsSet(
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement),
                SingleOption(CSharpFormattingOptions.NewLinesForBracesInAccessors, false));

        private IDictionary<OptionKey, object> UseCustomFieldName =>
            OptionsSet(
                SingleOption(SimplificationOptions.NamingPreferences, CreateCustomFieldNamingStylePreference()),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private IDictionary<OptionKey, object> UseCustomStaticFieldName =>
            OptionsSet(
                SingleOption(SimplificationOptions.NamingPreferences, CreateCustomStaticFieldNamingStylePreference()),
                SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithNoneEnforcement));

        private NamingStylePreferences CreateCustomFieldNamingStylePreference()
        {
            var symbolSpecification = new SymbolSpecification(
                null,
                "Name",
                ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                ImmutableArray<Accessibility>.Empty,
                ImmutableArray<SymbolSpecification.ModifierKind>.Empty);

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
                EnforcementLevel = DiagnosticSeverity.Error
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
                ImmutableArray<Accessibility>.Empty,
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
