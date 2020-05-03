// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    /// <inheritdoc cref="CSharpFormattingOptions"/>
    public static class CSharpFormattingOptions
    {
        /// <inheritdoc cref="CSharpFormattingOptions2.SpacingAfterMethodDeclarationName"/>
        public static Option<bool> SpacingAfterMethodDeclarationName { get; } = (Option<bool>)CSharpFormattingOptions2.SpacingAfterMethodDeclarationName!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis"/>
        public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses"/>
        public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterMethodCallName"/>
        public static Option<bool> SpaceAfterMethodCallName { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterMethodCallName!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinMethodCallParentheses"/>
        public static Option<bool> SpaceWithinMethodCallParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinMethodCallParentheses!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses"/>
        public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword"/>
        public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinExpressionParentheses"/>
        public static Option<bool> SpaceWithinExpressionParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinExpressionParentheses!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinCastParentheses"/>
        public static Option<bool> SpaceWithinCastParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinCastParentheses!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinOtherParentheses"/>
        public static Option<bool> SpaceWithinOtherParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinOtherParentheses!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterCast"/>
        public static Option<bool> SpaceAfterCast { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterCast!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration"/>
        public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = (Option<bool>)CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket"/>
        public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets"/>
        public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinSquareBrackets"/>
        public static Option<bool> SpaceWithinSquareBrackets { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinSquareBrackets!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration"/>
        public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterComma"/>
        public static Option<bool> SpaceAfterComma { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterComma!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterDot"/>
        public static Option<bool> SpaceAfterDot { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterDot!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement"/>
        public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration"/>
        public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeComma"/>
        public static Option<bool> SpaceBeforeComma { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeComma!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeDot"/>
        public static Option<bool> SpaceBeforeDot { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeDot!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement"/>
        public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement!;

        /// <inheritdoc cref="CSharpFormattingOptions2.SpacingAroundBinaryOperator"/>
        public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = (Option<BinaryOperatorSpacingOptions>)CSharpFormattingOptions2.SpacingAroundBinaryOperator!;

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentBraces"/>
        public static Option<bool> IndentBraces { get; } = (Option<bool>)CSharpFormattingOptions2.IndentBraces!;

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentBlock"/>
        public static Option<bool> IndentBlock { get; } = (Option<bool>)CSharpFormattingOptions2.IndentBlock!;

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchSection"/>
        public static Option<bool> IndentSwitchSection { get; } = (Option<bool>)CSharpFormattingOptions2.IndentSwitchSection!;

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchCaseSection"/>
        public static Option<bool> IndentSwitchCaseSection { get; } = (Option<bool>)CSharpFormattingOptions2.IndentSwitchCaseSection!;

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock"/>
        public static Option<bool> IndentSwitchCaseSectionWhenBlock { get; } = (Option<bool>)CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock!;

        /// <inheritdoc cref="CSharpFormattingOptions2.LabelPositioning"/>
        public static Option<LabelPositionOptions> LabelPositioning { get; } = (Option<LabelPositionOptions>)CSharpFormattingOptions2.LabelPositioning!;

        /// <inheritdoc cref="CSharpFormattingOptions2.WrappingPreserveSingleLine"/>
        public static Option<bool> WrappingPreserveSingleLine { get; } = (Option<bool>)CSharpFormattingOptions2.WrappingPreserveSingleLine!;

        /// <inheritdoc cref="CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine"/>
        public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = (Option<bool>)CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInTypes"/>
        public static Option<bool> NewLinesForBracesInTypes { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInTypes!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInMethods"/>
        public static Option<bool> NewLinesForBracesInMethods { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInMethods!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInProperties"/>
        public static Option<bool> NewLinesForBracesInProperties { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInProperties!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInAccessors"/>
        public static Option<bool> NewLinesForBracesInAccessors { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInAccessors!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods"/>
        public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInControlBlocks"/>
        public static Option<bool> NewLinesForBracesInControlBlocks { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInControlBlocks!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes"/>
        public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers"/>
        public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody"/>
        public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForElse"/>
        public static Option<bool> NewLineForElse { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForElse!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForCatch"/>
        public static Option<bool> NewLineForCatch { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForCatch!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForFinally"/>
        public static Option<bool> NewLineForFinally { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForFinally!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForMembersInObjectInit"/>
        public static Option<bool> NewLineForMembersInObjectInit { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForMembersInObjectInit!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes"/>
        public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes!;

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForClausesInQuery"/>
        public static Option<bool> NewLineForClausesInQuery { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForClausesInQuery!;
    }
}
