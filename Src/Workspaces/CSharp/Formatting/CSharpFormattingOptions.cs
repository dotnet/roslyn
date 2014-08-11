// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    public static class CSharpFormattingOptions
    {
        internal const string SpacingFeatureName = "CSharp/Formatting/Spacing";
        internal const string IndentFeatureName = "CSharp/Indent";
        internal const string WrappingFeatureName = "CSharp/Wrapping";
        internal const string NewLineFormattingFeatureName = "CSharp/New Line";

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpacingAfterMethodDeclarationName = new Option<bool>(SpacingFeatureName, "SpacingAfterMethodDeclarationName", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceWithinMethodDeclarationParenthesis = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodDeclarationParenthesis", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodDeclarationParentheses", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceAfterMethodCallName = new Option<bool>(SpacingFeatureName, "SpaceAfterMethodCallName", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceWithinMethodCallParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodCallParentheses", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBetweenEmptyMethodCallParentheses = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodCallParentheses", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceAfterControlFlowStatementKeyword = new Option<bool>(SpacingFeatureName, "SpaceAfterControlFlowStatementKeyword", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceWithinExpressionParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinExpressionParentheses", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceWithinCastParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinCastParentheses", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceWithinOtherParentheses = new Option<bool>(SpacingFeatureName, "SpaceWithinOtherParentheses", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceAfterCast = new Option<bool>(SpacingFeatureName, "SpaceAfterCast", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> WrappingIgnoreSpacesAroundVariableDeclaration = new Option<bool>(SpacingFeatureName, "WrappingIgnoreSpacesAroundVariableDeclaration", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBeforeOpenSquare = new Option<bool>(SpacingFeatureName, "SpaceBeforeOpenSquare", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBetweenEmptySquares = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptySquares", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceWithinSquares = new Option<bool>(SpacingFeatureName, "SpaceWithinSquares", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceAfterColonInBaseTypeDeclaration = new Option<bool>(SpacingFeatureName, "SpaceAfterColonInBaseTypeDeclaration", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceAfterComma = new Option<bool>(SpacingFeatureName, "SpaceAfterComma", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceAfterDot = new Option<bool>(SpacingFeatureName, "SpaceAfterDot", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceAfterSemicolonsInForStatement = new Option<bool>(SpacingFeatureName, "SpaceAfterSemicolonsInForStatement", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBeforeColonInBaseTypeDeclaration = new Option<bool>(SpacingFeatureName, "SpaceBeforeColonInBaseTypeDeclaration", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBeforeComma = new Option<bool>(SpacingFeatureName, "SpaceBeforeComma", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBeforeDot = new Option<bool>(SpacingFeatureName, "SpaceBeforeDot", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SpaceBeforeSemicolonsInForStatement = new Option<bool>(SpacingFeatureName, "SpaceBeforeSemicolonsInForStatement", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator = new Option<BinaryOperatorSpacingOptions>(SpacingFeatureName, "SpacingAroundBinaryOperator", defaultValue: BinaryOperatorSpacingOptions.Single);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> IndentBraces = new Option<bool>(IndentFeatureName, "OpenCloseBracesIndent", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> IndentBlock = new Option<bool>(IndentFeatureName, "IndentBlock", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> IndentSwitchSection = new Option<bool>(IndentFeatureName, "IndentSwitchSection", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> IndentSwitchCaseSection = new Option<bool>(IndentFeatureName, "IndentSwitchCaseSection", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<LabelPositionOptions> LabelPositioning = new Option<LabelPositionOptions>(IndentFeatureName, "LabelPositioning", defaultValue: LabelPositionOptions.OneLess);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> WrappingPreserveSingleLine = new Option<bool>(WrappingFeatureName, "WrappingPreserveSingleLine", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> WrappingKeepStatementsOnSingleLine = new Option<bool>(WrappingFeatureName, "WrappingKeepStatementsOnSingleLine", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLinesForBracesInTypes = new Option<bool>(NewLineFormattingFeatureName, "NewLinesBracesType", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLinesForBracesInMethods = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInMethods", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLinesForBracesInAnonymousMethods = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousMethods", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLinesForBracesInControlBlocks = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInControlBlocks", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLinesForBracesInAnonymousTypes = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousTypes", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLinesForBracesInObjectInitializers = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInObjectInitializers", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLinesForBracesInLambdaExpressionBody = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInLambdaExpressionBody", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLineForElse = new Option<bool>(NewLineFormattingFeatureName, "NewLineForElse", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLineForCatch = new Option<bool>(NewLineFormattingFeatureName, "NewLineForCatch", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLineForFinally = new Option<bool>(NewLineFormattingFeatureName, "NewLineForFinally", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLineForMembersInObjectInit = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInObjectInit", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLineForMembersInAnonymousTypes = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInAnonymousTypes", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> NewLineForClausesInQuery = new Option<bool>(NewLineFormattingFeatureName, "NewLineForClausesInQuery", defaultValue: true);
    }

    public enum LabelPositionOptions
    {
        // Placed in the Zeroth column of the text editor
        LeftMost,

        // Placed at one less indent to the current context
        OneLess,

        // Placed at the same indent as the current context
        NoIndent
    }

    public enum BinaryOperatorSpacingOptions
    {
        // Single Spacing
        Single,

        // Ignore Formatting
        Ignore,

        // Remove Spacing
        Remove
    }
}