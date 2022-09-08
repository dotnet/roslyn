// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal static class OptionsTestHelpers
    {
        public static readonly Option<bool> CustomPublicOption = new Option<bool>("My Feature", "My Option", defaultValue: true);

        // all public options and their non-default values:
        public static readonly ImmutableArray<(IOption, object)> PublicCustomOptionsWithNonDefaultValues = ImmutableArray.Create<(IOption, object)>(
            (CustomPublicOption, false));

        public static readonly ImmutableArray<(IOption, object)> PublicAutoFormattingOptionsWithNonDefaultValues = ImmutableArray.Create<(IOption, object)>(
            (FormattingOptions.SmartIndent, FormattingOptions2.IndentStyle.Block));

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
            => (option is IPerLanguageValuedOption) ? new[] { LanguageNames.CSharp, LanguageNames.VisualBasic } : new string?[] { null };
    }
}
