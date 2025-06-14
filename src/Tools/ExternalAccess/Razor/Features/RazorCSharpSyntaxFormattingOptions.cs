// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features
{
    /// <summary>
    /// Wrapper for CSharpSyntaxFormattingOptions for Razor external access.
    /// </summary>
    internal sealed record class RazorCSharpSyntaxFormattingOptions
    {
        public RazorSpacePlacement Spacing { get; init; }
        public RazorBinaryOperatorSpacingOptions SpacingAroundBinaryOperator { get; init; }
        public RazorNewLinePlacement NewLines { get; init; }
        public RazorLabelPositionOptions LabelPositioning { get; init; }
        public RazorIndentationPlacement Indentation { get; init; }
        public bool WrappingKeepStatementsOnSingleLine { get; init; }
        public bool WrappingPreserveSingleLine { get; init; }
        public RazorNamespaceDeclarationPreference NamespaceDeclarations { get; init; }
        public bool PreferTopLevelStatements { get; init; }
        public int CollectionExpressionWrappingLength { get; init; }

        public RazorCSharpSyntaxFormattingOptions(
            RazorSpacePlacement spacing,
            RazorBinaryOperatorSpacingOptions spacingAroundBinaryOperator,
            RazorNewLinePlacement newLines,
            RazorLabelPositionOptions labelPositioning,
            RazorIndentationPlacement indentation,
            bool wrappingKeepStatementsOnSingleLine,
            bool wrappingPreserveSingleLine,
            RazorNamespaceDeclarationPreference namespaceDeclarations,
            bool preferTopLevelStatements,
            int collectionExpressionWrappingLength)
        {
            Spacing = spacing;
            SpacingAroundBinaryOperator = spacingAroundBinaryOperator;
            NewLines = newLines;
            LabelPositioning = labelPositioning;
            Indentation = indentation;
            WrappingKeepStatementsOnSingleLine = wrappingKeepStatementsOnSingleLine;
            WrappingPreserveSingleLine = wrappingPreserveSingleLine;
            NamespaceDeclarations = namespaceDeclarations;
            PreferTopLevelStatements = preferTopLevelStatements;
            CollectionExpressionWrappingLength = collectionExpressionWrappingLength;
        }

        public RazorCSharpSyntaxFormattingOptions()
        {
            var options = CSharpSyntaxFormattingOptions.Default;
            Spacing = (RazorSpacePlacement)options.Spacing;
            SpacingAroundBinaryOperator = (RazorBinaryOperatorSpacingOptions)options.SpacingAroundBinaryOperator;
            NewLines = (RazorNewLinePlacement)options.NewLines;
            LabelPositioning = (RazorLabelPositionOptions)options.LabelPositioning;
            Indentation = (RazorIndentationPlacement)options.Indentation;
            WrappingKeepStatementsOnSingleLine = options.WrappingKeepStatementsOnSingleLine;
            WrappingPreserveSingleLine = options.WrappingPreserveSingleLine;
            NamespaceDeclarations = (RazorNamespaceDeclarationPreference)options.NamespaceDeclarations.Value;
            PreferTopLevelStatements = options.PreferTopLevelStatements.Value;
            CollectionExpressionWrappingLength = options.CollectionExpressionWrappingLength;
        }

        public CSharpSyntaxFormattingOptions ToCSharpSyntaxFormattingOptions()
        {
            return new CSharpSyntaxFormattingOptions
            {
                Spacing = (SpacePlacement)Spacing,
                SpacingAroundBinaryOperator = (BinaryOperatorSpacingOptions)SpacingAroundBinaryOperator,
                NewLines = (NewLinePlacement)NewLines,
                LabelPositioning = (LabelPositionOptions)LabelPositioning,
                Indentation = (IndentationPlacement)Indentation,
                WrappingKeepStatementsOnSingleLine = WrappingKeepStatementsOnSingleLine,
                WrappingPreserveSingleLine = WrappingPreserveSingleLine,
                NamespaceDeclarations = new CodeStyleOption2<NamespaceDeclarationPreference>(
                    (NamespaceDeclarationPreference)NamespaceDeclarations,
                    CSharpSyntaxFormattingOptions.Default.NamespaceDeclarations.Notification),
                PreferTopLevelStatements = new CodeStyleOption2<bool>(
                    PreferTopLevelStatements,
                    CSharpSyntaxFormattingOptions.Default.PreferTopLevelStatements.Notification),
                CollectionExpressionWrappingLength = CollectionExpressionWrappingLength
            };
        }
    }

    [Flags]
    public enum RazorSpacePlacement
    {
        None = 0,
        IgnoreAroundVariableDeclaration = SpacePlacement.IgnoreAroundVariableDeclaration,
        AfterMethodDeclarationName = SpacePlacement.AfterMethodDeclarationName,
        BetweenEmptyMethodDeclarationParentheses = SpacePlacement.BetweenEmptyMethodDeclarationParentheses,
        WithinMethodDeclarationParenthesis = SpacePlacement.WithinMethodDeclarationParenthesis,
        AfterMethodCallName = SpacePlacement.AfterMethodCallName,
        BetweenEmptyMethodCallParentheses = SpacePlacement.BetweenEmptyMethodCallParentheses,
        WithinMethodCallParentheses = SpacePlacement.WithinMethodCallParentheses,
        AfterControlFlowStatementKeyword = SpacePlacement.AfterControlFlowStatementKeyword,
        WithinExpressionParentheses = SpacePlacement.WithinExpressionParentheses,
        WithinCastParentheses = SpacePlacement.WithinCastParentheses,
        BeforeSemicolonsInForStatement = SpacePlacement.BeforeSemicolonsInForStatement,
        AfterSemicolonsInForStatement = SpacePlacement.AfterSemicolonsInForStatement,
        WithinOtherParentheses = SpacePlacement.WithinOtherParentheses,
        AfterCast = SpacePlacement.AfterCast,
        BeforeOpenSquareBracket = SpacePlacement.BeforeOpenSquareBracket,
        BetweenEmptySquareBrackets = SpacePlacement.BetweenEmptySquareBrackets,
        WithinSquareBrackets = SpacePlacement.WithinSquareBrackets,
        AfterColonInBaseTypeDeclaration = SpacePlacement.AfterColonInBaseTypeDeclaration,
        BeforeColonInBaseTypeDeclaration = SpacePlacement.BeforeColonInBaseTypeDeclaration,
        AfterComma = SpacePlacement.AfterComma,
        BeforeComma = SpacePlacement.BeforeComma,
        AfterDot = SpacePlacement.AfterDot,
        BeforeDot = SpacePlacement.BeforeDot,
    }

    [Flags]
    public enum RazorNewLinePlacement
    {
        None = 0,
        BeforeMembersInObjectInitializers = NewLinePlacement.BeforeMembersInObjectInitializers,
        BeforeMembersInAnonymousTypes = NewLinePlacement.BeforeMembersInAnonymousTypes,
        BeforeElse = NewLinePlacement.BeforeElse,
        BeforeCatch = NewLinePlacement.BeforeCatch,
        BeforeFinally = NewLinePlacement.BeforeFinally,
        BeforeOpenBraceInTypes = NewLinePlacement.BeforeOpenBraceInTypes,
        BeforeOpenBraceInAnonymousTypes = NewLinePlacement.BeforeOpenBraceInAnonymousTypes,
        BeforeOpenBraceInObjectCollectionArrayInitializers = NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers,
        BeforeOpenBraceInProperties = NewLinePlacement.BeforeOpenBraceInProperties,
        BeforeOpenBraceInMethods = NewLinePlacement.BeforeOpenBraceInMethods,
        BeforeOpenBraceInAccessors = NewLinePlacement.BeforeOpenBraceInAccessors,
        BeforeOpenBraceInAnonymousMethods = NewLinePlacement.BeforeOpenBraceInAnonymousMethods,
        BeforeOpenBraceInLambdaExpressionBody = NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody,
        BeforeOpenBraceInControlBlocks = NewLinePlacement.BeforeOpenBraceInControlBlocks,
        BetweenQueryExpressionClauses = NewLinePlacement.BetweenQueryExpressionClauses,
    }

    [Flags]
    public enum RazorIndentationPlacement
    {
        None = 0,
        Braces = IndentationPlacement.Braces,
        BlockContents = IndentationPlacement.BlockContents,
        SwitchCaseContents = IndentationPlacement.SwitchCaseContents,
        SwitchCaseContentsWhenBlock = IndentationPlacement.SwitchCaseContentsWhenBlock,
        SwitchSection = IndentationPlacement.SwitchSection,
    }

    public enum RazorBinaryOperatorSpacingOptions
    {
        Single = BinaryOperatorSpacingOptions.Single,
        Ignore = BinaryOperatorSpacingOptions.Ignore,
        Remove = BinaryOperatorSpacingOptions.Remove,
    }

    public enum RazorLabelPositionOptions
    {
        LeftMost = LabelPositionOptions.LeftMost,
        OneLess = LabelPositionOptions.OneLess,
        NoIndent = LabelPositionOptions.NoIndent,
    }

    public enum RazorNamespaceDeclarationPreference
    {
        BlockScoped = NamespaceDeclarationPreference.BlockScoped,
        FileScoped = NamespaceDeclarationPreference.FileScoped,
    }
}
