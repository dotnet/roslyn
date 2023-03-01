// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class OptionsTestHelpers
    {
        public static readonly Option<bool> CustomPublicOption = new Option<bool>("My Feature", "My Option", defaultValue: true);

        // all public options and their non-default values:
        public static readonly ImmutableArray<(IOption, object)> PublicCustomOptionsWithNonDefaultValues = ImmutableArray.Create<(IOption, object)>(
            (CustomPublicOption, false));

        public static readonly ImmutableArray<(IOption, object)> PublicAutoFormattingOptionsWithNonDefaultValues = ImmutableArray.Create<(IOption, object)>(
            (FormattingOptions.SmartIndent, FormattingOptions.IndentStyle.Block));

        public static readonly ImmutableArray<(IOption, object)> PublicFormattingOptionsWithNonDefaultValues = ImmutableArray.Create<(IOption, object)>(
            (FormattingOptions.UseTabs, true),
            (FormattingOptions.TabSize, 5),
            (FormattingOptions.IndentationSize, 7),
            (FormattingOptions.NewLine, "\r"),
            (CSharpFormattingOptions.IndentBlock, false),
            (CSharpFormattingOptions.IndentBraces, true),
            (CSharpFormattingOptions.IndentSwitchCaseSection, false),
            (CSharpFormattingOptions.IndentSwitchCaseSectionWhenBlock, false),
            (CSharpFormattingOptions.IndentSwitchSection, false),
            (CSharpFormattingOptions.LabelPositioning, LabelPositionOptions.LeftMost),
            (CSharpFormattingOptions.NewLineForCatch, false),
            (CSharpFormattingOptions.NewLineForClausesInQuery, false),
            (CSharpFormattingOptions.NewLineForElse, false),
            (CSharpFormattingOptions.NewLineForFinally, false),
            (CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, false),
            (CSharpFormattingOptions.NewLineForMembersInObjectInit, false),
            (CSharpFormattingOptions.NewLinesForBracesInAccessors, false),
            (CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, false),
            (CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, false),
            (CSharpFormattingOptions.NewLinesForBracesInControlBlocks, false),
            (CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, false),
            (CSharpFormattingOptions.NewLinesForBracesInMethods, false),
            (CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false),
            (CSharpFormattingOptions.NewLinesForBracesInProperties, false),
            (CSharpFormattingOptions.NewLinesForBracesInTypes, false),
            (CSharpFormattingOptions.SpaceAfterCast, true),
            (CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, false),
            (CSharpFormattingOptions.SpaceAfterComma, false),
            (CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, false),
            (CSharpFormattingOptions.SpaceAfterDot, true),
            (CSharpFormattingOptions.SpaceAfterMethodCallName, true),
            (CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, false),
            (CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, false),
            (CSharpFormattingOptions.SpaceBeforeComma, true),
            (CSharpFormattingOptions.SpaceBeforeDot, true),
            (CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, true),
            (CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, true),
            (CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, true),
            (CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, true),
            (CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, true),
            (CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, true),
            (CSharpFormattingOptions.SpaceWithinCastParentheses, true),
            (CSharpFormattingOptions.SpaceWithinExpressionParentheses, true),
            (CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true),
            (CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, true),
            (CSharpFormattingOptions.SpaceWithinOtherParentheses, true),
            (CSharpFormattingOptions.SpaceWithinSquareBrackets, true),
            (CSharpFormattingOptions.SpacingAfterMethodDeclarationName, true),
            (CSharpFormattingOptions.SpacingAroundBinaryOperator, BinaryOperatorSpacingOptions.Remove),
            (CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, false),
            (CSharpFormattingOptions.WrappingPreserveSingleLine, false));

        public static readonly ImmutableArray<(IOption, object)> PublicCodeStyleOptionsWithNonDefaultValues = ImmutableArray.Create<(IOption, object)>(
            (CodeStyleOptions.QualifyFieldAccess, new CodeStyleOption<bool>(true, NotificationOption.Suggestion)),
            (CodeStyleOptions.QualifyPropertyAccess, new CodeStyleOption<bool>(true, NotificationOption.Suggestion)),
            (CodeStyleOptions.QualifyMethodAccess, new CodeStyleOption<bool>(true, NotificationOption.Suggestion)),
            (CodeStyleOptions.QualifyEventAccess, new CodeStyleOption<bool>(true, NotificationOption.Suggestion)),
            (CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, new CodeStyleOption<bool>(false, NotificationOption.Suggestion)),
            (CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, new CodeStyleOption<bool>(false, NotificationOption.Suggestion)));

        public static readonly IEnumerable<(IOption, object)> AllPublicOptionsWithNonDefaultValues =
            PublicCustomOptionsWithNonDefaultValues
            .Concat(PublicAutoFormattingOptionsWithNonDefaultValues)
            .Concat(PublicFormattingOptionsWithNonDefaultValues)
            .Concat(PublicCodeStyleOptionsWithNonDefaultValues);

        public static OptionSet GetOptionSetWithChangedOptions(OptionSet options, IEnumerable<(IOption, object)> optionsWithNonDefaultValues)
        {
            var updatedOptions = options;
            foreach (var (option, newValue) in optionsWithNonDefaultValues)
            {
                foreach (var language in GetApplicableLanguages(option))
                {
                    updatedOptions = updatedOptions.WithChangedOption(new OptionKey(option, language), newValue);
                }
            }

            return updatedOptions;
        }

        public static IEnumerable<string?> GetApplicableLanguages(IOption option)
            => option.IsPerLanguage ? new[] { LanguageNames.CSharp, LanguageNames.VisualBasic } : new string?[] { null };

        public static object? GetDifferentValue(Type type, object? value)
            => value switch
            {
                _ when type == typeof(bool) => !(bool)value!,
                _ when type == typeof(string) => (string?)value == "X" ? "Y" : "X",
                _ when type == typeof(int) => (int)value! == 0 ? 1 : 0,
                _ when type == typeof(long) => (long)value! == 0 ? 1L : 0L,
                _ when type.IsEnum => GetDifferentEnumValue(type, value!),
                _ when Nullable.GetUnderlyingType(type) is { IsEnum: true } underlying => value is null ? Enum.ToObject(underlying, 1) : null,
                ICodeStyleOption codeStyle => codeStyle
                    .WithValue(GetDifferentValue(codeStyle.GetType().GetGenericArguments()[0], codeStyle.Value!)!)
                    .WithNotification((codeStyle.Notification == NotificationOption2.Error) ? NotificationOption2.Warning : NotificationOption2.Error),
                NamingStylePreferences namingPreference => namingPreference.IsEmpty ? NamingStylePreferences.Default : NamingStylePreferences.Empty,
                _ when type == typeof(bool?) => value is null ? true : null,
                _ when type == typeof(int?) => value is null ? 1 : null,
                _ when type == typeof(long?) => value is null ? 1L : null,
                ImmutableArray<bool> array => array.IsEmpty ? ImmutableArray.Create(true) : ImmutableArray<bool>.Empty,
                ImmutableArray<string> array => array is ["X"] ? ImmutableArray.Create("X", "Y") : ImmutableArray.Create("X"),
                ImmutableArray<int> array => array.IsEmpty ? ImmutableArray.Create(1) : ImmutableArray<int>.Empty,
                ImmutableArray<long> array => array.IsEmpty ? ImmutableArray.Create(1L) : ImmutableArray<long>.Empty,

                // Hit when a new option is introduced that uses type not handled above:
                _ => throw ExceptionUtilities.UnexpectedValue(type)
            };

        private static object GetDifferentEnumValue(Type type, object defaultValue)
        {
            var zero = Enum.ToObject(type, 0);
            return defaultValue.Equals(zero) ? Enum.ToObject(type, 1) : zero;
        }

        public static NamingStylePreferences GetNonDefaultNamingStylePreference()
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

            return new NamingStylePreferences(
                ImmutableArray.Create(symbolSpecification),
                ImmutableArray.Create(namingStyle),
                ImmutableArray.Create(namingRule));
        }
    }
}
