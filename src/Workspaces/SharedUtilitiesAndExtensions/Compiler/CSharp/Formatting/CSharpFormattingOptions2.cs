// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CSharp.EditorConfigSettings;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using static Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions2;

#if CODE_STYLE
using CSharpWorkspaceResources = Microsoft.CodeAnalysis.CSharp.CSharpCodeStyleResources;
using WorkspacesResources = Microsoft.CodeAnalysis.CodeStyleResources;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal static partial class CSharpFormattingOptions2
    {
        private static readonly ImmutableArray<IOption2>.Builder s_allOptionsBuilder = ImmutableArray.CreateBuilder<IOption2>();

        // Maps to store mapping between special option kinds and the corresponding options.
        private static readonly ImmutableDictionary<Option2<bool>, SpacingWithinParenthesesOption>.Builder s_spacingWithinParenthesisOptionsMapBuilder
            = ImmutableDictionary.CreateBuilder<Option2<bool>, SpacingWithinParenthesesOption>();
        private static readonly ImmutableDictionary<Option2<bool>, NewLineOption>.Builder s_newLineOptionsMapBuilder
            = ImmutableDictionary.CreateBuilder<Option2<bool>, NewLineOption>();

        // Maps to store mapping between special option kinds and the corresponding editor config string representations.
        #region Editor Config maps
        private static readonly BidirectionalMap<string, NewLineOption> s_legacyNewLineOptionsEditorConfigMap =
            new(new[]
            {
                KeyValuePairUtil.Create("object_collection_array_initalizers", NewLineOption.ObjectCollectionsArrayInitializers),
            });
        #endregion

        internal static ImmutableArray<IOption2> AllOptions { get; }
        private static ImmutableDictionary<Option2<bool>, SpacingWithinParenthesesOption> SpacingWithinParenthesisOptionsMap { get; }
        private static ImmutableDictionary<Option2<bool>, NewLineOption> NewLineOptionsMap { get; }

        private static Option2<T> CreateOption<T>(
            OptionGroup group, string name, T defaultValue,
            OptionStorageLocation2 storageLocation1,
            OptionStorageLocation2 storageLocation2)
        {
            var option = new Option2<T>(
                "CSharpFormattingOptions",
                group, name, defaultValue,
                ImmutableArray.Create(storageLocation1, storageLocation2), LanguageNames.CSharp);

            s_allOptionsBuilder.Add(option);
            return option;
        }

        private static Option2<bool> CreateSpaceWithinParenthesesOption(SpacingWithinParenthesesOption parenthesesOption, string name, bool defaultValue)
        {
            var option = CreateOption(
                CSharpFormattingOptionGroups.Spacing, name,
                defaultValue,
                new EditorConfigStorageLocation<bool>(
                    CSharpEditorConfigSettingsData.SpaceBetweenParentheses.GetSettingName(),
                    s => DetermineIfSpaceOptionIsSet(s, parenthesesOption, CSharpEditorConfigSettingsData.SpaceBetweenParentheses),
                    optionSet => GetSpacingWithParenthesesEditorConfigString(optionSet, CSharpEditorConfigSettingsData.SpaceBetweenParentheses)),
            new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{name}"));

            Debug.Assert(CSharpEditorConfigSettingsData.SpaceBetweenParentheses.GetEditorConfigStringFromValue(parenthesesOption) != "");
            s_spacingWithinParenthesisOptionsMapBuilder.Add(option, parenthesesOption);

            return option;
        }

        private static Option2<bool> CreateNewLineForBracesOption(NewLineOption newLineOption, string name, bool defaultValue)
        {
            var option = CreateOption(
                CSharpFormattingOptionGroups.NewLine, name,
                defaultValue,
                new EditorConfigStorageLocation<bool>(
                    CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace.GetSettingName(),
                    value => DetermineIfNewLineOptionIsSet(value, newLineOption, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace),
                    optionSet => GetNewLineOptionEditorConfigString(optionSet, CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace)),
                new RoamingProfileStorageLocation($"TextEditor.CSharp.Specific.{name}"));

            Debug.Assert(CSharpEditorConfigSettingsData.NewLineBeforeOpenBrace.GetEditorConfigStringFromValue(newLineOption) != "");
            s_newLineOptionsMapBuilder.Add(option, newLineOption);

            return option;
        }

        public static Option2<bool> SpacingAfterMethodDeclarationName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacingAfterMethodDeclarationName),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodDeclarationName),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpacingAfterMethodDeclarationName),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName"));

        public static Option2<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinMethodDeclarationParenthesis),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodDeclarationParenthesis),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceWithinMethodDeclarationParenthesis),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis"));

        public static Option2<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptyMethodDeclarationParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodDeclarationParentheses),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBetweenEmptyMethodDeclarationParentheses),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses"));

        public static Option2<bool> SpaceAfterMethodCallName { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterMethodCallName),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterMethodCallName),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceAfterMethodCallName),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterMethodCallName"));

        public static Option2<bool> SpaceWithinMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinMethodCallParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinMethodCallParentheses),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceWithinMethodCallParentheses),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses"));

        public static Option2<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptyMethodCallParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptyMethodCallParentheses),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBetweenEmptyMethodCallParentheses),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses"));

        public static Option2<bool> SpaceAfterControlFlowStatementKeyword { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterControlFlowStatementKeyword),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterControlFlowStatementKeyword),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceAfterControlFlowStatementKeyword),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword"));

        public static Option2<bool> SpaceWithinExpressionParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.Expressions, nameof(SpaceWithinExpressionParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinExpressionParentheses));

        public static Option2<bool> SpaceWithinCastParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.TypeCasts, nameof(SpaceWithinCastParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinCastParentheses));

        public static Option2<bool> SpaceWithinOtherParentheses { get; } = CreateSpaceWithinParenthesesOption(
            SpacingWithinParenthesesOption.ControlFlowStatements, nameof(SpaceWithinOtherParentheses),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinOtherParentheses));

        public static Option2<bool> SpaceAfterCast { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterCast),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterCast),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceAfterCast),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterCast"));

        public static Option2<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacesIgnoreAroundVariableDeclaration),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.IgnoreAroundVariableDeclaration),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpacesIgnoreAroundVariableDeclaration),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration"));

        public static Option2<bool> SpaceBeforeOpenSquareBracket { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeOpenSquareBracket),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeOpenSquareBracket),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBeforeOpenSquareBracket),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket"));

        public static Option2<bool> SpaceBetweenEmptySquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBetweenEmptySquareBrackets),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BetweenEmptySquareBrackets),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBetweenEmptySquareBrackets),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets"));

        public static Option2<bool> SpaceWithinSquareBrackets { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceWithinSquareBrackets),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.WithinSquareBrackets),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceWithinSquareBrackets),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceWithinSquareBrackets"));

        public static Option2<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterColonInBaseTypeDeclaration),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterColonInBaseTypeDeclaration),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceAfterColonInBaseTypeDeclaration),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration"));

        public static Option2<bool> SpaceAfterComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterComma),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterComma),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceAfterComma),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterComma"));

        public static Option2<bool> SpaceAfterDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterDot),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterDot),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceAfterDot),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterDot"));

        public static Option2<bool> SpaceAfterSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceAfterSemicolonsInForStatement),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.AfterSemicolonsInForStatement),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceAfterSemicolonsInForStatement),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement"));

        public static Option2<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeColonInBaseTypeDeclaration),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeColonInBaseTypeDeclaration),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBeforeColonInBaseTypeDeclaration),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration"));

        public static Option2<bool> SpaceBeforeComma { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeComma),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeComma),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBeforeComma),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeComma"));

        public static Option2<bool> SpaceBeforeDot { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeDot),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeDot),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBeforeDot),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeDot"));

        public static Option2<bool> SpaceBeforeSemicolonsInForStatement { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpaceBeforeSemicolonsInForStatement),
            CSharpSyntaxFormattingOptions.SpacingDefault.HasFlag(SpacePlacement.BeforeSemicolonsInForStatement),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.SpaceBeforeSemicolonsInForStatement),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement"));

        public static Option2<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CreateOption(
            CSharpFormattingOptionGroups.Spacing, nameof(SpacingAroundBinaryOperator),
            CSharpSyntaxFormattingOptions.Default.SpacingAroundBinaryOperator,
            new EditorConfigStorageLocation<BinaryOperatorSpacingOptions>(CSharpEditorConfigSettingsData.SpacingAroundBinaryOperator),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.SpacingAroundBinaryOperator"));

        public static Option2<bool> IndentBraces { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentBraces),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.Braces),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.IndentBraces),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.OpenCloseBracesIndent"));

        public static Option2<bool> IndentBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentBlock),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.BlockContents),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.IndentBlock),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentBlock"));

        public static Option2<bool> IndentSwitchSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchSection),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.IndentSwitchSection),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchSection"));

        public static Option2<bool> IndentSwitchCaseSection { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchCaseSection),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchSection),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.IndentSwitchCaseSection),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSection"));

        public static Option2<bool> IndentSwitchCaseSectionWhenBlock { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(IndentSwitchCaseSectionWhenBlock),
            CSharpSyntaxFormattingOptions.IndentationDefault.HasFlag(IndentationPlacement.SwitchCaseContentsWhenBlock),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.IndentSwitchCaseSectionWhenBlock),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.IndentSwitchCaseSectionWhenBlock"));

        public static Option2<LabelPositionOptions> LabelPositioning { get; } = CreateOption(
            CSharpFormattingOptionGroups.Indentation, nameof(LabelPositioning),
            CSharpSyntaxFormattingOptions.Default.LabelPositioning,
            new EditorConfigStorageLocation<LabelPositionOptions>(CSharpEditorConfigSettingsData.LabelPositioning),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.LabelPositioning"));

        public static Option2<bool> WrappingPreserveSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, nameof(WrappingPreserveSingleLine),
            CSharpSyntaxFormattingOptions.Default.WrappingPreserveSingleLine,
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.WrappingPreserveSingleLine),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingPreserveSingleLine"));

        public static Option2<bool> WrappingKeepStatementsOnSingleLine { get; } = CreateOption(
            CSharpFormattingOptionGroups.Wrapping, nameof(WrappingKeepStatementsOnSingleLine),
            CSharpSyntaxFormattingOptions.Default.WrappingKeepStatementsOnSingleLine,
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.WrappingKeepStatementsOnSingleLine),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine"));

        public static Option2<bool> NewLinesForBracesInTypes { get; } = CreateNewLineForBracesOption(
            NewLineOption.Types, nameof(NewLinesForBracesInTypes),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes));

        public static Option2<bool> NewLinesForBracesInMethods { get; } = CreateNewLineForBracesOption(
            NewLineOption.Methods, nameof(NewLinesForBracesInMethods),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInMethods));

        public static Option2<bool> NewLinesForBracesInProperties { get; } = CreateNewLineForBracesOption(
            NewLineOption.Properties, nameof(NewLinesForBracesInProperties),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInProperties));

        public static Option2<bool> NewLinesForBracesInAccessors { get; } = CreateNewLineForBracesOption(
            NewLineOption.Accessors, nameof(NewLinesForBracesInAccessors),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAccessors));

        public static Option2<bool> NewLinesForBracesInAnonymousMethods { get; } = CreateNewLineForBracesOption(
            NewLineOption.AnonymousMethods, nameof(NewLinesForBracesInAnonymousMethods),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousMethods));

        public static Option2<bool> NewLinesForBracesInControlBlocks { get; } = CreateNewLineForBracesOption(
            NewLineOption.ControlBlocks, nameof(NewLinesForBracesInControlBlocks),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInControlBlocks));

        public static Option2<bool> NewLinesForBracesInAnonymousTypes { get; } = CreateNewLineForBracesOption(
            NewLineOption.AnonymousTypes, nameof(NewLinesForBracesInAnonymousTypes),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInAnonymousTypes));

        public static Option2<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = CreateNewLineForBracesOption(
            NewLineOption.ObjectCollectionsArrayInitializers, nameof(NewLinesForBracesInObjectCollectionArrayInitializers),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers));

        public static Option2<bool> NewLinesForBracesInLambdaExpressionBody { get; } = CreateNewLineForBracesOption(
            NewLineOption.Lambdas, nameof(NewLinesForBracesInLambdaExpressionBody),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody));

        public static Option2<bool> NewLineForElse { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForElse),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeElse),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.NewLineForElse),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForElse"));

        public static Option2<bool> NewLineForCatch { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForCatch),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeCatch),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.NewLineForCatch),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForCatch"));

        public static Option2<bool> NewLineForFinally { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForFinally),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeFinally),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.NewLineForFinally),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForFinally"));

        public static Option2<bool> NewLineForMembersInObjectInit { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForMembersInObjectInit),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInObjectInitializers),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.NewLineForMembersInObjectInit),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInObjectInit"));

        public static Option2<bool> NewLineForMembersInAnonymousTypes { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForMembersInAnonymousTypes),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BeforeMembersInAnonymousTypes),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.NewLineForMembersInAnonymousTypes),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes"));

        public static Option2<bool> NewLineForClausesInQuery { get; } = CreateOption(
            CSharpFormattingOptionGroups.NewLine, nameof(NewLineForClausesInQuery),
            CSharpSyntaxFormattingOptions.NewLinesDefault.HasFlag(NewLinePlacement.BetweenQueryExpressionClauses),
            new EditorConfigStorageLocation<bool>(CSharpEditorConfigSettingsData.NewLineForClausesInQuery),
            new RoamingProfileStorageLocation("TextEditor.CSharp.Specific.NewLineForClausesInQuery"));

        static CSharpFormattingOptions2()
        {
            // Note that the static constructor executes after all the static field initializers for the options have executed,
            // and each field initializer adds the created option to the following builders.

            AllOptions = s_allOptionsBuilder.ToImmutable();
            SpacingWithinParenthesisOptionsMap = s_spacingWithinParenthesisOptionsMapBuilder.ToImmutable();
            NewLineOptionsMap = s_newLineOptionsMapBuilder.ToImmutable();
        }
    }

#if CODE_STYLE
    internal enum LabelPositionOptions
#else
    public enum LabelPositionOptions
#endif
    {
        /// Placed in the Zeroth column of the text editor
        LeftMost = 0,

        /// Placed at one less indent to the current context
        OneLess = 1,

        /// Placed at the same indent as the current context
        NoIndent = 2
    }

#if CODE_STYLE
    internal enum BinaryOperatorSpacingOptions
#else
    public enum BinaryOperatorSpacingOptions
#endif
    {
        /// Single Spacing
        Single = 0,

        /// Ignore Formatting
        Ignore = 1,

        /// Remove Spacing
        Remove = 2
    }

    internal static class CSharpFormattingOptionGroups
    {
        public static readonly OptionGroup NewLine = new(WorkspacesResources.New_line_preferences, priority: 1);
        public static readonly OptionGroup Indentation = new(CSharpWorkspaceResources.Indentation_preferences, priority: 2);
        public static readonly OptionGroup Spacing = new(CSharpWorkspaceResources.Space_preferences, priority: 3);
        public static readonly OptionGroup Wrapping = new(CSharpWorkspaceResources.Wrapping_preferences, priority: 4);
    }
}
