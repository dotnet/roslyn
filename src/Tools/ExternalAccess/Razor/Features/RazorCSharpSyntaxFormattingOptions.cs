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
        IgnoreAroundVariableDeclaration = 1,
        AfterMethodDeclarationName = 1 << 1,
        BetweenEmptyMethodDeclarationParentheses = 1 << 2,
        WithinMethodDeclarationParenthesis = 1 << 3,
        AfterMethodCallName = 1 << 4,
        BetweenEmptyMethodCallParentheses = 1 << 5,
        WithinMethodCallParentheses = 1 << 6,
        AfterControlFlowStatementKeyword = 1 << 7,
        WithinExpressionParentheses = 1 << 8,
        WithinCastParentheses = 1 << 9,
        BeforeSemicolonsInForStatement = 1 << 10,
        AfterSemicolonsInForStatement = 1 << 11,
        WithinOtherParentheses = 1 << 12,
        AfterCast = 1 << 13,
        BeforeOpenSquareBracket = 1 << 14,
        BetweenEmptySquareBrackets = 1 << 15,
        WithinSquareBrackets = 1 << 16,
        AfterColonInBaseTypeDeclaration = 1 << 17,
        BeforeColonInBaseTypeDeclaration = 1 << 18,
        AfterComma = 1 << 19,
        BeforeComma = 1 << 20,
        AfterDot = 1 << 21,
        BeforeDot = 1 << 22,
        All = (1 << 23) - 1
    }

    [Flags]
    public enum RazorNewLinePlacement
    {
        None = 0,
        BeforeMembersInObjectInitializers = 1,
        BeforeMembersInAnonymousTypes = 1 << 1,
        BeforeElse = 1 << 2,
        BeforeCatch = 1 << 3,
        BeforeFinally = 1 << 4,
        BeforeOpenBraceInTypes = 1 << 5,
        BeforeOpenBraceInAnonymousTypes = 1 << 6,
        BeforeOpenBraceInObjectCollectionArrayInitializers = 1 << 7,
        BeforeOpenBraceInProperties = 1 << 8,
        BeforeOpenBraceInMethods = 1 << 9,
        BeforeOpenBraceInAccessors = 1 << 10,
        BeforeOpenBraceInAnonymousMethods = 1 << 11,
        BeforeOpenBraceInLambdaExpressionBody = 1 << 12,
        BeforeOpenBraceInControlBlocks = 1 << 13,
        BetweenQueryExpressionClauses = 1 << 14,
        All = (1 << 15) - 1
    }

    [Flags]
    public enum RazorIndentationPlacement
    {
        None = 0,
        Braces = 1,
        BlockContents = 1 << 1,
        SwitchCaseContents = 1 << 2,
        SwitchCaseContentsWhenBlock = 1 << 3,
        SwitchSection = 1 << 4,
        All = (1 << 5) - 1
    }

    public enum RazorBinaryOperatorSpacingOptions
    {
        Single = 0,
        Ignore = 1,
        Remove = 2,
    }

    public enum RazorLabelPositionOptions
    {
        LeftMost = 0,
        OneLess = 1,
        NoIndent = 2,
    }

    public enum RazorNamespaceDeclarationPreference
    {
        BlockScoped = 0,
        FileScoped = 1,
    }
}
