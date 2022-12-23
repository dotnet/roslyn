// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    /// <inheritdoc cref="CSharpFormattingOptions2"/>
    public static class CSharpFormattingOptions
    {
        private const string FeatureName = "CSharpFormattingOptions";

        /// <inheritdoc cref="CSharpFormattingOptions2.SpacingAfterMethodDeclarationName"/>
        public static Option<bool> SpacingAfterMethodDeclarationName { get; } = CSharpFormattingOptions2.SpacingAfterMethodDeclarationName.ToPublicOption(FeatureName, "SpacingAfterMethodDeclarationName");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis"/>
        public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis.ToPublicOption(FeatureName, "SpaceWithinMethodDeclarationParenthesis");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses"/>
        public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses.ToPublicOption(FeatureName, "SpaceBetweenEmptyMethodDeclarationParentheses");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterMethodCallName"/>
        public static Option<bool> SpaceAfterMethodCallName { get; } = CSharpFormattingOptions2.SpaceAfterMethodCallName.ToPublicOption(FeatureName, "SpaceAfterMethodCallName");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinMethodCallParentheses"/>
        public static Option<bool> SpaceWithinMethodCallParentheses { get; } = CSharpFormattingOptions2.SpaceWithinMethodCallParentheses.ToPublicOption(FeatureName, "SpaceWithinMethodCallParentheses");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses"/>
        public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses.ToPublicOption(FeatureName, "SpaceBetweenEmptyMethodCallParentheses");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword"/>
        public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword.ToPublicOption(FeatureName, "SpaceAfterControlFlowStatementKeyword");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinExpressionParentheses"/>
        public static Option<bool> SpaceWithinExpressionParentheses { get; } = CSharpFormattingOptions2.SpaceWithinExpressionParentheses.ToPublicOption(FeatureName, "SpaceWithinExpressionParentheses");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinCastParentheses"/>
        public static Option<bool> SpaceWithinCastParentheses { get; } = CSharpFormattingOptions2.SpaceWithinCastParentheses.ToPublicOption(FeatureName, "SpaceWithinCastParentheses");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinOtherParentheses"/>
        public static Option<bool> SpaceWithinOtherParentheses { get; } = CSharpFormattingOptions2.SpaceWithinOtherParentheses.ToPublicOption(FeatureName, "SpaceWithinOtherParentheses");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterCast"/>
        public static Option<bool> SpaceAfterCast { get; } = CSharpFormattingOptions2.SpaceAfterCast.ToPublicOption(FeatureName, "SpaceAfterCast");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration"/>
        public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration.ToPublicOption(FeatureName, "SpacesIgnoreAroundVariableDeclaration");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket"/>
        public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket.ToPublicOption(FeatureName, "SpaceBeforeOpenSquareBracket");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets"/>
        public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets.ToPublicOption(FeatureName, "SpaceBetweenEmptySquareBrackets");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceWithinSquareBrackets"/>
        public static Option<bool> SpaceWithinSquareBrackets { get; } = CSharpFormattingOptions2.SpaceWithinSquareBrackets.ToPublicOption(FeatureName, "SpaceWithinSquareBrackets");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration"/>
        public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration.ToPublicOption(FeatureName, "SpaceAfterColonInBaseTypeDeclaration");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterComma"/>
        public static Option<bool> SpaceAfterComma { get; } = CSharpFormattingOptions2.SpaceAfterComma.ToPublicOption(FeatureName, "SpaceAfterComma");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterDot"/>
        public static Option<bool> SpaceAfterDot { get; } = CSharpFormattingOptions2.SpaceAfterDot.ToPublicOption(FeatureName, "SpaceAfterDot");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement"/>
        public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement.ToPublicOption(FeatureName, "SpaceAfterSemicolonsInForStatement");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration"/>
        public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration.ToPublicOption(FeatureName, "SpaceBeforeColonInBaseTypeDeclaration");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeComma"/>
        public static Option<bool> SpaceBeforeComma { get; } = CSharpFormattingOptions2.SpaceBeforeComma.ToPublicOption(FeatureName, "SpaceBeforeComma");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeDot"/>
        public static Option<bool> SpaceBeforeDot { get; } = CSharpFormattingOptions2.SpaceBeforeDot.ToPublicOption(FeatureName, "SpaceBeforeDot");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement"/>
        public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement.ToPublicOption(FeatureName, "SpaceBeforeSemicolonsInForStatement");

        /// <inheritdoc cref="CSharpFormattingOptions2.SpacingAroundBinaryOperator"/>
        public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = CSharpFormattingOptions2.SpacingAroundBinaryOperator.ToPublicOption(FeatureName, "SpacingAroundBinaryOperator");

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentBraces"/>
        public static Option<bool> IndentBraces { get; } = CSharpFormattingOptions2.IndentBraces.ToPublicOption(FeatureName, "IndentBraces");

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentBlock"/>
        public static Option<bool> IndentBlock { get; } = CSharpFormattingOptions2.IndentBlock.ToPublicOption(FeatureName, "IndentBlock");

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchSection"/>
        public static Option<bool> IndentSwitchSection { get; } = CSharpFormattingOptions2.IndentSwitchSection.ToPublicOption(FeatureName, "IndentSwitchSection");

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchCaseSection"/>
        public static Option<bool> IndentSwitchCaseSection { get; } = CSharpFormattingOptions2.IndentSwitchCaseSection.ToPublicOption(FeatureName, "IndentSwitchCaseSection");

        /// <inheritdoc cref="CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock"/>
        public static Option<bool> IndentSwitchCaseSectionWhenBlock { get; } = CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock.ToPublicOption(FeatureName, "IndentSwitchCaseSectionWhenBlock");

        /// <inheritdoc cref="CSharpFormattingOptions2.LabelPositioning"/>
        public static Option<LabelPositionOptions> LabelPositioning { get; } = CSharpFormattingOptions2.LabelPositioning.ToPublicOption(FeatureName, "LabelPositioning");

        /// <inheritdoc cref="CSharpFormattingOptions2.WrappingPreserveSingleLine"/>
        public static Option<bool> WrappingPreserveSingleLine { get; } = CSharpFormattingOptions2.WrappingPreserveSingleLine.ToPublicOption(FeatureName, "WrappingPreserveSingleLine");

        /// <inheritdoc cref="CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine"/>
        public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine.ToPublicOption(FeatureName, "WrappingKeepStatementsOnSingleLine");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInTypes"/>
        public static Option<bool> NewLinesForBracesInTypes { get; } = CSharpFormattingOptions2.NewLinesForBracesInTypes.ToPublicOption(FeatureName, "NewLinesForBracesInTypes");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInMethods"/>
        public static Option<bool> NewLinesForBracesInMethods { get; } = CSharpFormattingOptions2.NewLinesForBracesInMethods.ToPublicOption(FeatureName, "NewLinesForBracesInMethods");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInProperties"/>
        public static Option<bool> NewLinesForBracesInProperties { get; } = CSharpFormattingOptions2.NewLinesForBracesInProperties.ToPublicOption(FeatureName, "NewLinesForBracesInProperties");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInAccessors"/>
        public static Option<bool> NewLinesForBracesInAccessors { get; } = CSharpFormattingOptions2.NewLinesForBracesInAccessors.ToPublicOption(FeatureName, "NewLinesForBracesInAccessors");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods"/>
        public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods.ToPublicOption(FeatureName, "NewLinesForBracesInAnonymousMethods");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInControlBlocks"/>
        public static Option<bool> NewLinesForBracesInControlBlocks { get; } = CSharpFormattingOptions2.NewLinesForBracesInControlBlocks.ToPublicOption(FeatureName, "NewLinesForBracesInControlBlocks");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes"/>
        public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes.ToPublicOption(FeatureName, "NewLinesForBracesInAnonymousTypes");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers"/>
        public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers.ToPublicOption(FeatureName, "NewLinesForBracesInObjectCollectionArrayInitializers");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody"/>
        public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody.ToPublicOption(FeatureName, "NewLinesForBracesInLambdaExpressionBody");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForElse"/>
        public static Option<bool> NewLineForElse { get; } = CSharpFormattingOptions2.NewLineForElse.ToPublicOption(FeatureName, "NewLineForElse");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForCatch"/>
        public static Option<bool> NewLineForCatch { get; } = CSharpFormattingOptions2.NewLineForCatch.ToPublicOption(FeatureName, "NewLineForCatch");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForFinally"/>
        public static Option<bool> NewLineForFinally { get; } = CSharpFormattingOptions2.NewLineForFinally.ToPublicOption(FeatureName, "NewLineForFinally");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForMembersInObjectInit"/>
        public static Option<bool> NewLineForMembersInObjectInit { get; } = CSharpFormattingOptions2.NewLineForMembersInObjectInit.ToPublicOption(FeatureName, "NewLineForMembersInObjectInit");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes"/>
        public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes.ToPublicOption(FeatureName, "NewLineForMembersInAnonymousTypes");

        /// <inheritdoc cref="CSharpFormattingOptions2.NewLineForClausesInQuery"/>
        public static Option<bool> NewLineForClausesInQuery { get; } = CSharpFormattingOptions2.NewLineForClausesInQuery.ToPublicOption(FeatureName, "NewLineForClausesInQuery");
    }
}
