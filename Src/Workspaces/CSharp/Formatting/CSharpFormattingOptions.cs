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
        public static readonly Option<bool> MethodDeclarationNameParenthesis = new Option<bool>(SpacingFeatureName, "MethodDeclarationNameParenthesis", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> MethodDeclarationParenthesisArgumentList = new Option<bool>(SpacingFeatureName, "MethodDeclarationParenthesisArgumentList", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> MethodDeclarationEmptyArgument = new Option<bool>(SpacingFeatureName, "MethodDeclarationEmptyArgument", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> MethodCallNameParenthesis = new Option<bool>(SpacingFeatureName, "MethodCallNameParenthesis", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> MethodCallArgumentList = new Option<bool>(SpacingFeatureName, "MethodCallArgumentList", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> MethodCallEmptyArgument = new Option<bool>(SpacingFeatureName, "MethodCallEmptyArgument", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OtherAfterControlFlowKeyword = new Option<bool>(SpacingFeatureName, "OtherAfterControlFlowKeyword", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OtherBetweenParenthesisExpression = new Option<bool>(SpacingFeatureName, "OtherBetweenParenthesisExpression", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OtherParenthesisTypeCast = new Option<bool>(SpacingFeatureName, "OtherParenthesisTypeCast", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OtherParenControlFlow = new Option<bool>(SpacingFeatureName, "OtherParenControlFlow", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OtherParenAfterCast = new Option<bool>(SpacingFeatureName, "OtherParenAfterCast", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OtherSpacesDeclarationIgnore = new Option<bool>(SpacingFeatureName, "OtherSpacesDeclarationIgnore", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SquareBracesBefore = new Option<bool>(SpacingFeatureName, "SquareBracesBefore", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SquareBracesEmpty = new Option<bool>(SpacingFeatureName, "SquareBracesEmpty", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> SquareBracesAndValue = new Option<bool>(SpacingFeatureName, "SquareBracesAndValue", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersAfterColonInTypeDeclaration = new Option<bool>(SpacingFeatureName, "DelimitersAfterColonInTypeDeclaration", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersAfterCommaInParameterArgument = new Option<bool>(SpacingFeatureName, "DelimitersAfterCommaInParameterArgument", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersAfterDotMemberAccessQualifiedName = new Option<bool>(SpacingFeatureName, "DelimitersAfterDotMemberAccessQualifiedName", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersAfterSemiColonInForStatement = new Option<bool>(SpacingFeatureName, "DelimitersAfterSemiColonInForStatement", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersBeforeColonInTypeDeclaration = new Option<bool>(SpacingFeatureName, "DelimitersBeforeColonInTypeDeclaration", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersBeforeCommaInParameterArgument = new Option<bool>(SpacingFeatureName, "DelimitersBeforeCommaInParameterArgument", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersBeforeDotMemberAccessQualifiedName = new Option<bool>(SpacingFeatureName, "DelimitersBeforeDotMemberAccessQualifiedName", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> DelimitersBeforeSemiColonInForStatement = new Option<bool>(SpacingFeatureName, "DelimitersBeforeSemiColonInForStatement", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<BinaryOperatorSpacingOptions> SpacingAroundBinaryOperator = new Option<BinaryOperatorSpacingOptions>(SpacingFeatureName, "SpacingAroundBinaryOperator", defaultValue: BinaryOperatorSpacingOptions.Single);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenCloseBracesIndent = new Option<bool>(IndentFeatureName, "OpenCloseBracesIndent", defaultValue: false);

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
        public static readonly Option<bool> LeaveBlockSingleLine = new Option<bool>(WrappingFeatureName, "LeaveBlockSingleLine", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> LeaveStatementMethodDeclarationSameLine = new Option<bool>(WrappingFeatureName, "LeaveStatementMethodDeclarationSameLine", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenBracesInNewLineForTypes = new Option<bool>(NewLineFormattingFeatureName, "OpenBracesInNewLineForTypes", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenBracesInNewLineForMethods = new Option<bool>(NewLineFormattingFeatureName, "OpenBracesInNewLineForMethods", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenBracesInNewLineForAnonymousMethods = new Option<bool>(NewLineFormattingFeatureName, "OpenBracesInNewLineForAnonymousMethods", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenBracesInNewLineForControl = new Option<bool>(NewLineFormattingFeatureName, "OpenBracesInNewLineForControl", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenBracesInNewLineForAnonymousType = new Option<bool>(NewLineFormattingFeatureName, "OpenBracesInNewLineForAnonymousType", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenBracesInNewLineForObjectInitializers = new Option<bool>(NewLineFormattingFeatureName, "OpenBracesInNewLineForObjectInitializers", defaultValue: true);

#if MEF
        [ExportOption]
#endif
        public static readonly Option<bool> OpenBracesInNewLineForLambda = new Option<bool>(NewLineFormattingFeatureName, "OpenBracesInNewLineForLambda", defaultValue: true);

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