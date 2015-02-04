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

        public static readonly Option<bool> SpacingAfterMethodDeclarationName = new Option<bool>(SpacingFeatureName, "SpacingAfterMethodDeclarationName", defaultValue: false);

        public static readonly Option<bool> SpaceWithinMethodDeclarationParenthesis = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodDeclarationParenthesis", defaultValue: false);

        public static readonly Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodDeclarationParentheses", defaultValue: false);

        public static readonly Option<bool> SpaceAfterMethodCallName = new Option<bool>(SpacingFeatureName, "SpaceAfterMethodCallName", defaultValue: false);

        public static readonly Option<bool> SpaceWithinMethodCallParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodCallParentheses", defaultValue: false);

        public static readonly Option<bool> SpaceBetweenEmptyMethodCallParentheses = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodCallParentheses", defaultValue: false);

        public static readonly Option<bool> SpaceAfterControlFlowStatementKeyword = new Option<bool>(SpacingFeatureName, "SpaceAfterControlFlowStatementKeyword", defaultValue: true);

        public static readonly Option<bool> SpaceWithinExpressionParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinExpressionParentheses", defaultValue: false);

        public static readonly Option<bool> SpaceWithinCastParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinCastParentheses", defaultValue: false);

        public static readonly Option<bool> SpaceWithinOtherParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinOtherParentheses", defaultValue: false);

        public static readonly Option<bool> SpaceAfterCast = new Option<bool>(SpacingFeatureName, "SpaceAfterCast", defaultValue: false);

        public static readonly Option<bool> SpacesIgnoreAroundVariableDeclaration = new Option<bool>(SpacingFeatureName, "SpacesIgnoreAroundVariableDeclaration", defaultValue: false);

        public static readonly Option<bool> SpaceBeforeOpenSquareBracket = new Option<bool>(SpacingFeatureName, "SpaceBeforeOpenSquareBracket", defaultValue: false);

        public static readonly Option<bool> SpaceBetweenEmptySquareBrackets = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptySquareBrackets", defaultValue: false);

        public static readonly Option<bool> SpaceWithinSquareBrackets = new Option<bool>(SpacingFeatureName, "SpaceWithinSquareBrackets", defaultValue: false);

        public static readonly Option<bool> SpaceAfterColonInBaseTypeDeclaration = new Option<bool>(SpacingFeatureName, "SpaceAfterColonInBaseTypeDeclaration", defaultValue: true);

        public static readonly Option<bool> SpaceAfterComma = new Option<bool>(SpacingFeatureName, "SpaceAfterComma", defaultValue: true);

        public static readonly Option<bool> SpaceAfterDot = new Option<bool>(SpacingFeatureName, "SpaceAfterDot", defaultValue: false);

        public static readonly Option<bool> SpaceAfterSemicolonsInForStatement = new Option<bool>(SpacingFeatureName, "SpaceAfterSemicolonsInForStatement", defaultValue: true);

        public static readonly Option<bool> SpaceBeforeColonInBaseTypeDeclaration = new Option<bool>(SpacingFeatureName, "SpaceBeforeColonInBaseTypeDeclaration", defaultValue: true);

        public static readonly Option<bool> SpaceBeforeComma = new Option<bool>(SpacingFeatureName, "SpaceBeforeComma", defaultValue: false);

        public static readonly Option<bool> SpaceBeforeDot = new Option<bool>(SpacingFeatureName, "SpaceBeforeDot", defaultValue: false);

        public static readonly Option<bool> SpaceBeforeSemicolonsInForStatement = new Option<bool>(SpacingFeatureName, "SpaceBeforeSemicolonsInForStatement", defaultValue: false);

        public static readonly Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator = new Option<BinaryOperatorSpacingOptions>(SpacingFeatureName, "SpacingAroundBinaryOperator", defaultValue: BinaryOperatorSpacingOptions.Single);

        public static readonly Option<bool> IndentBraces = new Option<bool>(IndentFeatureName, "OpenCloseBracesIndent", defaultValue: false);

        public static readonly Option<bool> IndentBlock = new Option<bool>(IndentFeatureName, "IndentBlock", defaultValue: true);

        public static readonly Option<bool> IndentSwitchSection = new Option<bool>(IndentFeatureName, "IndentSwitchSection", defaultValue: true);

        public static readonly Option<bool> IndentSwitchCaseSection = new Option<bool>(IndentFeatureName, "IndentSwitchCaseSection", defaultValue: true);

        public static readonly Option<LabelPositionOptions> LabelPositioning = new Option<LabelPositionOptions>(IndentFeatureName, "LabelPositioning", defaultValue: LabelPositionOptions.OneLess);

        public static readonly Option<bool> WrappingPreserveSingleLine = new Option<bool>(WrappingFeatureName, "WrappingPreserveSingleLine", defaultValue: true);

        public static readonly Option<bool> WrappingKeepStatementsOnSingleLine = new Option<bool>(WrappingFeatureName, "WrappingKeepStatementsOnSingleLine", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInTypes = new Option<bool>(NewLineFormattingFeatureName, "NewLinesBracesType", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInMethods = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInMethods", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInProperties = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInProperties", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInAnonymousMethods = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousMethods", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInControlBlocks = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInControlBlocks", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInAnonymousTypes = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousTypes", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInObjectCollectionArrayInitializers", defaultValue: true);

        public static readonly Option<bool> NewLinesForBracesInLambdaExpressionBody = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInLambdaExpressionBody", defaultValue: true);

        public static readonly Option<bool> NewLineForElse = new Option<bool>(NewLineFormattingFeatureName, "NewLineForElse", defaultValue: true);

        public static readonly Option<bool> NewLineForCatch = new Option<bool>(NewLineFormattingFeatureName, "NewLineForCatch", defaultValue: true);

        public static readonly Option<bool> NewLineForFinally = new Option<bool>(NewLineFormattingFeatureName, "NewLineForFinally", defaultValue: true);

        public static readonly Option<bool> NewLineForMembersInObjectInit = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInObjectInit", defaultValue: true);

        public static readonly Option<bool> NewLineForMembersInAnonymousTypes = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInAnonymousTypes", defaultValue: true);

        public static readonly Option<bool> NewLineForClausesInQuery = new Option<bool>(NewLineFormattingFeatureName, "NewLineForClausesInQuery", defaultValue: true);
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
