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

        public static Option<bool> SpacingAfterMethodDeclarationName { get; } = new Option<bool>(SpacingFeatureName, "SpacingAfterMethodDeclarationName", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpacingAfterMethodDeclarationName"));

        public static Option<bool> SpaceWithinMethodDeclarationParenthesis { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodDeclarationParenthesis", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceWithinMethodDeclarationParenthesis"));

        public static Option<bool> SpaceBetweenEmptyMethodDeclarationParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodDeclarationParentheses", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodDeclarationParentheses"));

        public static Option<bool> SpaceAfterMethodCallName { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterMethodCallName", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceAfterMethodCallName"));

        public static Option<bool> SpaceWithinMethodCallParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinMethodCallParentheses", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceWithinMethodCallParentheses"));

        public static Option<bool> SpaceBetweenEmptyMethodCallParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptyMethodCallParentheses", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBetweenEmptyMethodCallParentheses"));

        public static Option<bool> SpaceAfterControlFlowStatementKeyword { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterControlFlowStatementKeyword", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceAfterControlFlowStatementKeyword"));

        public static Option<bool> SpaceWithinExpressionParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinExpressionParentheses", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceWithinExpressionParentheses"));

        public static Option<bool> SpaceWithinCastParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinCastParentheses", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceWithinCastParentheses"));

        public static Option<bool> SpaceWithinOtherParentheses { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinOtherParentheses", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceWithinOtherParentheses"));

        public static Option<bool> SpaceAfterCast { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterCast", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceAfterCast"));

        public static Option<bool> SpacesIgnoreAroundVariableDeclaration { get; } = new Option<bool>(SpacingFeatureName, "SpacesIgnoreAroundVariableDeclaration", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpacesIgnoreAroundVariableDeclaration"));

        public static Option<bool> SpaceBeforeOpenSquareBracket { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeOpenSquareBracket", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBeforeOpenSquareBracket"));

        public static Option<bool> SpaceBetweenEmptySquareBrackets { get; } = new Option<bool>(SpacingFeatureName, "SpaceBetweenEmptySquareBrackets", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBetweenEmptySquareBrackets"));

        public static Option<bool> SpaceWithinSquareBrackets { get; } = new Option<bool>(SpacingFeatureName, "SpaceWithinSquareBrackets", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceWithinSquareBrackets"));

        public static Option<bool> SpaceAfterColonInBaseTypeDeclaration { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterColonInBaseTypeDeclaration", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceAfterColonInBaseTypeDeclaration"));

        public static Option<bool> SpaceAfterComma { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterComma", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceAfterComma"));

        public static Option<bool> SpaceAfterDot { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterDot", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceAfterDot"));

        public static Option<bool> SpaceAfterSemicolonsInForStatement { get; } = new Option<bool>(SpacingFeatureName, "SpaceAfterSemicolonsInForStatement", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceAfterSemicolonsInForStatement"));

        public static Option<bool> SpaceBeforeColonInBaseTypeDeclaration { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeColonInBaseTypeDeclaration", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBeforeColonInBaseTypeDeclaration"));

        public static Option<bool> SpaceBeforeComma { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeComma", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBeforeComma"));

        public static Option<bool> SpaceBeforeDot { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeDot", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBeforeDot"));

        public static Option<bool> SpaceBeforeSemicolonsInForStatement { get; } = new Option<bool>(SpacingFeatureName, "SpaceBeforeSemicolonsInForStatement", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.SpaceBeforeSemicolonsInForStatement"));

        // This property is currently serialized into multiple properties
        public static Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator { get; } = new Option<BinaryOperatorSpacingOptions>(SpacingFeatureName, "SpacingAroundBinaryOperator", defaultValue: BinaryOperatorSpacingOptions.Single);

        public static Option<bool> IndentBraces { get; } = new Option<bool>(IndentFeatureName, "OpenCloseBracesIndent", defaultValue: false,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.OpenCloseBracesIndent"));

        public static Option<bool> IndentBlock { get; } = new Option<bool>(IndentFeatureName, "IndentBlock", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.IndentBlock"));

        public static Option<bool> IndentSwitchSection { get; } = new Option<bool>(IndentFeatureName, "IndentSwitchSection", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.IndentSwitchSection"));

        public static Option<bool> IndentSwitchCaseSection { get; } = new Option<bool>(IndentFeatureName, "IndentSwitchCaseSection", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.IndentSwitchCaseSection"));

        // This property is currently serialized into multiple properties
        public static Option<LabelPositionOptions> LabelPositioning { get; } = new Option<LabelPositionOptions>(IndentFeatureName, "LabelPositioning", defaultValue: LabelPositionOptions.OneLess);

        public static Option<bool> WrappingPreserveSingleLine { get; } = new Option<bool>(WrappingFeatureName, "WrappingPreserveSingleLine", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.WrappingPreserveSingleLine"));

        public static Option<bool> WrappingKeepStatementsOnSingleLine { get; } = new Option<bool>(WrappingFeatureName, "WrappingKeepStatementsOnSingleLine", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.WrappingKeepStatementsOnSingleLine"));

        public static Option<bool> NewLinesForBracesInTypes { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesBracesType", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesBracesType"));

        public static Option<bool> NewLinesForBracesInMethods { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInMethods", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInMethods"));

        public static Option<bool> NewLinesForBracesInProperties { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInProperties", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInProperties"));

        public static Option<bool> NewLinesForBracesInAccessors { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAccessors", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInAccessors"));

        public static Option<bool> NewLinesForBracesInAnonymousMethods { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousMethods", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousMethods"));

        public static Option<bool> NewLinesForBracesInControlBlocks { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInControlBlocks", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInControlBlocks"));

        public static Option<bool> NewLinesForBracesInAnonymousTypes { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInAnonymousTypes", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInAnonymousTypes"));

        public static Option<bool> NewLinesForBracesInObjectCollectionArrayInitializers { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInObjectCollectionArrayInitializers", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInObjectCollectionArrayInitializers"));

        public static Option<bool> NewLinesForBracesInLambdaExpressionBody { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLinesForBracesInLambdaExpressionBody", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLinesForBracesInLambdaExpressionBody"));

        public static Option<bool> NewLineForElse { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForElse", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLineForElse"));

        public static Option<bool> NewLineForCatch { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForCatch", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLineForCatch"));

        public static Option<bool> NewLineForFinally { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForFinally", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLineForFinally"));

        public static Option<bool> NewLineForMembersInObjectInit { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInObjectInit", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLineForMembersInObjectInit"));

        public static Option<bool> NewLineForMembersInAnonymousTypes { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForMembersInAnonymousTypes", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLineForMembersInAnonymousTypes"));

        public static Option<bool> NewLineForClausesInQuery { get; } = new Option<bool>(NewLineFormattingFeatureName, "NewLineForClausesInQuery", defaultValue: true,
            persistences: new RoamingProfilePersistence("TextEditor.CSharp.Specific.NewLineForClausesInQuery"));
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
