// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    public static class CSharpFormattingOptions
    {
        internal const string SpacingFeatureName = "CSharp/Formatting/Spacing";
        internal const string IndentFeatureName = "CSharp/Indent";
        internal const string WrappingFeatureName = "CSharp/Wrapping";
        internal const string NewLineFormattingFeatureName = "CSharp/New Line";

        public static Option<bool> SpacingAfterMethodDeclarationName { get; } = new Option<bool>(SpacingFeatureName, "SpacingAfterMethodDeclarationName", defaultValue: false);

        public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodDeclarationParenthesis", defaultValue: false);

        public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodDeclarationParentheses", defaultValue: false);

        public static Option<bool> SpaceAfterMethodCallName { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterMethodCallName", defaultValue: false);

        public static Option<bool> SpaceWithinMethodCallParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodCallParentheses", defaultValue: false);

        public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodCallParentheses", defaultValue: false);

        public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterControlFlowStatementKeyword", defaultValue: true);

        public static Option<bool> SpaceWithinExpressionParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinExpressionParentheses", defaultValue: false);

        public static Option<bool> SpaceWithinCastParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinCastParentheses", defaultValue: false);

        public static Option<bool> SpaceWithinOtherParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinOtherParentheses", defaultValue: false);

        public static Option<bool> SpaceAfterCast { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterCast", defaultValue: false);

        public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = new Option<bool>(SpacingFeatureName, "SpacesIgnoreAroundVariableDeclaration", defaultValue: false);

        public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeOpenSquareBracket", defaultValue: false);

        public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptySquareBrackets", defaultValue: false);

        public static Option<bool> SpaceWithinSquareBrackets { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinSquareBrackets", defaultValue: false);

        public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterColonInBaseTypeDeclaration", defaultValue: true);

        public static Option<bool> SpaceAfterComma { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterComma", defaultValue: true);

        public static Option<bool> SpaceAfterDot { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterDot", defaultValue: false);

        public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterSemicolonsInForStatement", defaultValue: true);

        public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeColonInBaseTypeDeclaration", defaultValue: true);

        public static Option<bool> SpaceBeforeComma { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeComma", defaultValue: false);

        public static Option<bool> SpaceBeforeDot { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeDot", defaultValue: false);

        public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeSemicolonsInForStatement", defaultValue: false);

        public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = new Option<BinaryOperatorSpacingOptions>(SpacingFeatureName, "SpacingAroundBinaryOperator", defaultValue: BinaryOperatorSpacingOptions.Single);

        public static Option<bool> IndentBraces { get; } = new Option<bool>(IndentFeatureName, "OpenCloseBracesIndent", defaultValue: false);

        public static Option<bool> IndentBlock { get; } = new Option<bool>(IndentFeatureName, "IndentBlock", defaultValue: true);

        public static Option<bool> IndentSwitchSection { get; } = new Option<bool>(IndentFeatureName, "IndentSwitchSection", defaultValue: true);

        public static Option<bool> IndentSwitchCaseSection { get; } = new Option<bool>(IndentFeatureName, "IndentSwitchCaseSection", defaultValue: true);

        public static Option<LabelPositionOptions> LabelPositioning { get; } = new Option<LabelPositionOptions>(IndentFeatureName, "LabelPositioning", defaultValue: LabelPositionOptions.OneLess);

        public static Option<bool> WrappingPreserveSingleLine { get; } = new Option<bool>(WrappingFeatureName, "WrappingPreserveSingleLine", defaultValue: true);

        public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = new Option<bool>(WrappingFeatureName, "WrappingKeepStatementsOnSingleLine", defaultValue: true);

        public static Option<bool> NewLinesForBracesInTypes { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesBracesType", defaultValue: true);

        public static Option<bool> NewLinesForBracesInMethods { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInMethods", defaultValue: true);

        public static Option<bool> NewLinesForBracesInProperties { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInProperties", defaultValue: true);

        public static Option<bool> NewLinesForBracesInAccessors { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAccessors", defaultValue: true);

        public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousMethods", defaultValue: true);

        public static Option<bool> NewLinesForBracesInControlBlocks { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInControlBlocks", defaultValue: true);

        public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousTypes", defaultValue: true);

        public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInObjectCollectionArrayInitializers", defaultValue: true);

        public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInLambdaExpressionBody", defaultValue: true);

        public static Option<bool> NewLineForElse { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForElse", defaultValue: true);

        public static Option<bool> NewLineForCatch { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForCatch", defaultValue: true);

        public static Option<bool> NewLineForFinally { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForFinally", defaultValue: true);

        public static Option<bool> NewLineForMembersInObjectInit { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInObjectInit", defaultValue: true);

        public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInAnonymousTypes", defaultValue: true);

        public static Option<bool> NewLineForClausesInQuery { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForClausesInQuery", defaultValue: true);
    }

    public enum LabelPositionOptions
    {
        /// Placed in the Zeroth column of the text editor
        LeftMost = 0,

        /// Placed at one less indent to the current context
        OneLess = 1,

        /// Placed at the same indent as the current context
        NoIndent = 2
    }

    public enum BinaryOperatorSpacingOptions
    {
        /// Single Spacing
        Single = 0,

        /// Ignore Formatting
        Ignore = 1,

        /// Remove Spacing
        Remove = 2
    }
}
