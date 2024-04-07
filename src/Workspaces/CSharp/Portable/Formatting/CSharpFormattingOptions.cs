// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

/// <inheritdoc cref="CSharpFormattingOptions2"/>
public static class CSharpFormattingOptions
{
    private const string PublicFeatureName = "CSharpFormattingOptions";

    private static Option<bool> CreateNewLineForBracesOption(string publicName, NewLineBeforeOpenBracePlacement flag)
        => new(
            feature: PublicFeatureName,
            name: publicName,
            group: FormattingOptionGroups.NewLine,
            defaultValue: CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.HasFlag(flag),
            storageLocations: [],
            storageMapping: new NewLineForBracesInternalStorageMapping(CSharpFormattingOptions2.NewLineBeforeOpenBrace, flag),
            isEditorConfigOption: true);

    private static Option<bool> CreateSpaceWithinOption(string publicName, SpacePlacementWithinParentheses flag)
        => new(
            feature: PublicFeatureName,
            name: publicName,
            group: CSharpFormattingOptionGroups.Spacing,
            defaultValue: CSharpFormattingOptions2.SpaceBetweenParentheses.DefaultValue.HasFlag(flag),
            storageLocations: [],
            storageMapping: new SpacePlacementInternalStorageMapping(CSharpFormattingOptions2.SpaceBetweenParentheses, flag),
            isEditorConfigOption: true);

    private sealed class NewLineForBracesInternalStorageMapping : OptionStorageMapping
    {
        private readonly NewLineBeforeOpenBracePlacement _flag;

        public NewLineForBracesInternalStorageMapping(IOption2 internalOption, NewLineBeforeOpenBracePlacement flag)
            : base(internalOption)
        {
            _flag = flag;
        }

        public override object? ToPublicOptionValue(object? internalValue)
            => ((NewLineBeforeOpenBracePlacement)internalValue!).HasFlag(_flag);

        public override object? UpdateInternalOptionValue(object? currentInternalValue, object? newPublicValue)
            => ((NewLineBeforeOpenBracePlacement)currentInternalValue!).WithFlagValue(_flag, (bool)newPublicValue!);
    }

    private sealed class SpacePlacementInternalStorageMapping : OptionStorageMapping
    {
        private readonly SpacePlacementWithinParentheses _flag;

        public SpacePlacementInternalStorageMapping(IOption2 internalOption, SpacePlacementWithinParentheses flag)
            : base(internalOption)
        {
            _flag = flag;
        }

        public override object? ToPublicOptionValue(object? internalValue)
            => ((SpacePlacementWithinParentheses)internalValue!).HasFlag(_flag);

        public override object? UpdateInternalOptionValue(object? currentInternalValue, object? newPublicValue)
            => ((SpacePlacementWithinParentheses)currentInternalValue!).WithFlagValue(_flag, (bool)newPublicValue!);
    }

    /// <inheritdoc cref="CSharpFormattingOptions2.SpacingAfterMethodDeclarationName"/>
    public static Option<bool> SpacingAfterMethodDeclarationName { get; } = CSharpFormattingOptions2.SpacingAfterMethodDeclarationName.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis"/>
    public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses"/>
    public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterMethodCallName"/>
    public static Option<bool> SpaceAfterMethodCallName { get; } = CSharpFormattingOptions2.SpaceAfterMethodCallName.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinMethodCallParentheses"/>
    public static Option<bool> SpaceWithinMethodCallParentheses { get; } = CSharpFormattingOptions2.SpaceWithinMethodCallParentheses.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses"/>
    public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword"/>
    public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword.ToPublicOption();

    public static Option<bool> SpaceWithinExpressionParentheses { get; } = CreateSpaceWithinOption("SpaceWithinExpressionParentheses", SpacePlacementWithinParentheses.Expressions);
    public static Option<bool> SpaceWithinCastParentheses { get; } = CreateSpaceWithinOption("SpaceWithinCastParentheses", SpacePlacementWithinParentheses.TypeCasts);
    public static Option<bool> SpaceWithinOtherParentheses { get; } = CreateSpaceWithinOption("SpaceWithinOtherParentheses", SpacePlacementWithinParentheses.ControlFlowStatements);

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterCast"/>
    public static Option<bool> SpaceAfterCast { get; } = CSharpFormattingOptions2.SpaceAfterCast.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration"/>
    public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket"/>
    public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets"/>
    public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinSquareBrackets"/>
    public static Option<bool> SpaceWithinSquareBrackets { get; } = CSharpFormattingOptions2.SpaceWithinSquareBrackets.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration"/>
    public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterComma"/>
    public static Option<bool> SpaceAfterComma { get; } = CSharpFormattingOptions2.SpaceAfterComma.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterDot"/>
    public static Option<bool> SpaceAfterDot { get; } = CSharpFormattingOptions2.SpaceAfterDot.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement"/>
    public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration"/>
    public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeComma"/>
    public static Option<bool> SpaceBeforeComma { get; } = CSharpFormattingOptions2.SpaceBeforeComma.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeDot"/>
    public static Option<bool> SpaceBeforeDot { get; } = CSharpFormattingOptions2.SpaceBeforeDot.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement"/>
    public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.SpacingAroundBinaryOperator"/>
    public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CSharpFormattingOptions2.SpacingAroundBinaryOperator.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.IndentBraces"/>
    public static Option<bool> IndentBraces { get; } = CSharpFormattingOptions2.IndentBraces.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.IndentBlock"/>
    public static Option<bool> IndentBlock { get; } = CSharpFormattingOptions2.IndentBlock.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchSection"/>
    public static Option<bool> IndentSwitchSection { get; } = CSharpFormattingOptions2.IndentSwitchSection.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchCaseSection"/>
    public static Option<bool> IndentSwitchCaseSection { get; } = CSharpFormattingOptions2.IndentSwitchCaseSection.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock"/>
    public static Option<bool> IndentSwitchCaseSectionWhenBlock { get; } = CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.LabelPositioning"/>
    public static Option<LabelPositionOptions> LabelPositioning { get; } = CSharpFormattingOptions2.LabelPositioning.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.WrappingPreserveSingleLine"/>
    public static Option<bool> WrappingPreserveSingleLine { get; } = CSharpFormattingOptions2.WrappingPreserveSingleLine.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine"/>
    public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine.ToPublicOption();

    public static Option<bool> NewLinesForBracesInTypes { get; } = CreateNewLineForBracesOption("NewLinesForBracesInTypes", NewLineBeforeOpenBracePlacement.Types);
    public static Option<bool> NewLinesForBracesInMethods { get; } = CreateNewLineForBracesOption("NewLinesForBracesInMethods", NewLineBeforeOpenBracePlacement.Methods);
    public static Option<bool> NewLinesForBracesInProperties { get; } = CreateNewLineForBracesOption("NewLinesForBracesInProperties", NewLineBeforeOpenBracePlacement.Properties);
    public static Option<bool> NewLinesForBracesInAccessors { get; } = CreateNewLineForBracesOption("NewLinesForBracesInAccessors", NewLineBeforeOpenBracePlacement.Accessors);
    public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = CreateNewLineForBracesOption("NewLinesForBracesInAnonymousMethods", NewLineBeforeOpenBracePlacement.AnonymousMethods);
    public static Option<bool> NewLinesForBracesInControlBlocks { get; } = CreateNewLineForBracesOption("NewLinesForBracesInControlBlocks", NewLineBeforeOpenBracePlacement.ControlBlocks);
    public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = CreateNewLineForBracesOption("NewLinesForBracesInAnonymousTypes", NewLineBeforeOpenBracePlacement.AnonymousTypes);
    public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = CreateNewLineForBracesOption("NewLinesForBracesInObjectCollectionArrayInitializers", NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers);
    public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = CreateNewLineForBracesOption("NewLinesForBracesInLambdaExpressionBody", NewLineBeforeOpenBracePlacement.LambdaExpressionBody);

    /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForElse"/>
    public static Option<bool> NewLineForElse { get; } = CSharpFormattingOptions2.NewLineForElse.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForCatch"/>
    public static Option<bool> NewLineForCatch { get; } = CSharpFormattingOptions2.NewLineForCatch.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForFinally"/>
    public static Option<bool> NewLineForFinally { get; } = CSharpFormattingOptions2.NewLineForFinally.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForMembersInObjectInit"/>
    public static Option<bool> NewLineForMembersInObjectInit { get; } = CSharpFormattingOptions2.NewLineForMembersInObjectInit.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes"/>
    public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes.ToPublicOption();

    /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForClausesInQuery"/>
    public static Option<bool> NewLineForClausesInQuery { get; } = CSharpFormattingOptions2.NewLineForClausesInQuery.ToPublicOption();
}
