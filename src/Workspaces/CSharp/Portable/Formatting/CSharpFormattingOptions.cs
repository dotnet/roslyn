// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    public static partial class CSharpFormattingOptions
    {
        public static Option<bool> SpacingAfterMethodDeclarationName { get; } = (Option<bool>)CSharpFormattingOptions2.SpacingAfterMethodDeclarationName;

        public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis;

        public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses;

        public static Option<bool> SpaceAfterMethodCallName { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterMethodCallName;

        public static Option<bool> SpaceWithinMethodCallParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinMethodCallParentheses;

        public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses;

        public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword;

        public static Option<bool> SpaceWithinExpressionParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinExpressionParentheses;

        public static Option<bool> SpaceWithinCastParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinCastParentheses;

        public static Option<bool> SpaceWithinOtherParentheses { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinOtherParentheses;

        public static Option<bool> SpaceAfterCast { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterCast;

        public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = (Option<bool>)CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration;

        public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket;

        public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets;

        public static Option<bool> SpaceWithinSquareBrackets { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceWithinSquareBrackets;

        public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration;

        public static Option<bool> SpaceAfterComma { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterComma;

        public static Option<bool> SpaceAfterDot { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterDot;

        public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement;

        public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration;

        public static Option<bool> SpaceBeforeComma { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeComma;

        public static Option<bool> SpaceBeforeDot { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeDot;

        public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = (Option<bool>)CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement;

        public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = (Option<BinaryOperatorSpacingOptions>)CSharpFormattingOptions2.SpacingAroundBinaryOperator;

        public static Option<bool> IndentBraces { get; } = (Option<bool>)CSharpFormattingOptions2.IndentBraces;

        public static Option<bool> IndentBlock { get; } = (Option<bool>)CSharpFormattingOptions2.IndentBlock;

        public static Option<bool> IndentSwitchSection { get; } = (Option<bool>)CSharpFormattingOptions2.IndentSwitchSection;

        public static Option<bool> IndentSwitchCaseSection { get; } = (Option<bool>)CSharpFormattingOptions2.IndentSwitchCaseSection;

        public static Option<bool> IndentSwitchCaseSectionWhenBlock { get; } = (Option<bool>)CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock;

        public static Option<LabelPositionOptions> LabelPositioning { get; } = (Option<LabelPositionOptions>)CSharpFormattingOptions2.LabelPositioning;

        public static Option<bool> WrappingPreserveSingleLine { get; } = (Option<bool>)CSharpFormattingOptions2.WrappingPreserveSingleLine;

        public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = (Option<bool>)CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine;

        public static Option<bool> NewLinesForBracesInTypes { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInTypes;

        public static Option<bool> NewLinesForBracesInMethods { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInMethods;

        public static Option<bool> NewLinesForBracesInProperties { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInProperties;

        public static Option<bool> NewLinesForBracesInAccessors { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInAccessors;

        public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods;

        public static Option<bool> NewLinesForBracesInControlBlocks { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInControlBlocks;

        public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes;

        public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers;

        public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = (Option<bool>)CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody;

        public static Option<bool> NewLineForElse { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForElse;

        public static Option<bool> NewLineForCatch { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForCatch;

        public static Option<bool> NewLineForFinally { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForFinally;

        public static Option<bool> NewLineForMembersInObjectInit { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForMembersInObjectInit;

        public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes;

        public static Option<bool> NewLineForClausesInQuery { get; } = (Option<bool>)CSharpFormattingOptions2.NewLineForClausesInQuery;
    }
}
