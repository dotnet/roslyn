// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [Flags]
    internal enum SpacePlacement
    {
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
    }

    [Flags]
    internal enum NewLinePlacement
    {
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
        BetweenQueryExpressionClauses = 1 << 14
    }

    [Flags]
    internal enum IndentationPlacement
    {
        Braces = 1,
        BlockContents = 1 << 1,
        SwitchCaseContents = 1 << 2,
        SwitchCaseContentsWhenBlock = 1 << 3,
        SwitchSection = 1 << 4
    }

    internal sealed class CSharpSyntaxFormattingOptions : SyntaxFormattingOptions
    {
        public readonly SpacePlacement Spacing;
        public readonly BinaryOperatorSpacingOptions SpacingAroundBinaryOperator;
        public readonly NewLinePlacement NewLines;
        public readonly LabelPositionOptions LabelPositioning;
        public readonly IndentationPlacement Indentation;
        public readonly bool WrappingKeepStatementsOnSingleLine;
        public readonly bool WrappingPreserveSingleLine;

        public CSharpSyntaxFormattingOptions(
            bool useTabs,
            int tabSize,
            int indentationSize,
            string newLine,
            bool separateImportDirectiveGroups,
            SpacePlacement spacing,
            BinaryOperatorSpacingOptions spacingAroundBinaryOperator,
            NewLinePlacement newLines,
            LabelPositionOptions labelPositioning,
            IndentationPlacement indentation,
            bool wrappingKeepStatementsOnSingleLine,
            bool wrappingPreserveSingleLine)
            : base(useTabs,
                  tabSize,
                  indentationSize,
                  newLine,
                  separateImportDirectiveGroups)
        {
            Spacing = spacing;
            SpacingAroundBinaryOperator = spacingAroundBinaryOperator;
            NewLines = newLines;
            LabelPositioning = labelPositioning;
            Indentation = indentation;
            WrappingKeepStatementsOnSingleLine = wrappingKeepStatementsOnSingleLine;
            WrappingPreserveSingleLine = wrappingPreserveSingleLine;
        }

        public static readonly CSharpSyntaxFormattingOptions Default = new(
            useTabs: FormattingOptions2.UseTabs.DefaultValue,
            tabSize: FormattingOptions2.TabSize.DefaultValue,
            indentationSize: FormattingOptions2.IndentationSize.DefaultValue,
            newLine: FormattingOptions2.NewLine.DefaultValue,
            separateImportDirectiveGroups: GenerationOptions.SeparateImportDirectiveGroups.DefaultValue,
            spacing:
                (CSharpFormattingOptions2.SpacingAfterMethodDeclarationName.DefaultValue ? SpacePlacement.AfterMethodDeclarationName : 0) |
                (CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses.DefaultValue ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
                (CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis.DefaultValue ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
                (CSharpFormattingOptions2.SpaceAfterMethodCallName.DefaultValue ? SpacePlacement.AfterMethodCallName : 0) |
                (CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses.DefaultValue ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
                (CSharpFormattingOptions2.SpaceWithinMethodCallParentheses.DefaultValue ? SpacePlacement.WithinMethodCallParentheses : 0) |
                (CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword.DefaultValue ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
                (CSharpFormattingOptions2.SpaceWithinExpressionParentheses.DefaultValue ? SpacePlacement.WithinExpressionParentheses : 0) |
                (CSharpFormattingOptions2.SpaceWithinCastParentheses.DefaultValue ? SpacePlacement.WithinCastParentheses : 0) |
                (CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement.DefaultValue ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
                (CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement.DefaultValue ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
                (CSharpFormattingOptions2.SpaceWithinOtherParentheses.DefaultValue ? SpacePlacement.WithinOtherParentheses : 0) |
                (CSharpFormattingOptions2.SpaceAfterCast.DefaultValue ? SpacePlacement.AfterCast : 0) |
                (CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket.DefaultValue ? SpacePlacement.BeforeOpenSquareBracket : 0) |
                (CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets.DefaultValue ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
                (CSharpFormattingOptions2.SpaceWithinSquareBrackets.DefaultValue ? SpacePlacement.WithinSquareBrackets : 0) |
                (CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration.DefaultValue ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
                (CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration.DefaultValue ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
                (CSharpFormattingOptions2.SpaceAfterComma.DefaultValue ? SpacePlacement.AfterComma : 0) |
                (CSharpFormattingOptions2.SpaceBeforeComma.DefaultValue ? SpacePlacement.BeforeComma : 0) |
                (CSharpFormattingOptions2.SpaceAfterDot.DefaultValue ? SpacePlacement.AfterDot : 0) |
                (CSharpFormattingOptions2.SpaceBeforeDot.DefaultValue ? SpacePlacement.BeforeDot : 0),
            spacingAroundBinaryOperator: CSharpFormattingOptions2.SpacingAroundBinaryOperator.DefaultValue,
            newLines:
                (CSharpFormattingOptions2.NewLineForMembersInObjectInit.DefaultValue ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
                (CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes.DefaultValue ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
                (CSharpFormattingOptions2.NewLineForElse.DefaultValue ? NewLinePlacement.BeforeElse : 0) |
                (CSharpFormattingOptions2.NewLineForCatch.DefaultValue ? NewLinePlacement.BeforeCatch : 0) |
                (CSharpFormattingOptions2.NewLineForFinally.DefaultValue ? NewLinePlacement.BeforeFinally : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInTypes.DefaultValue ? NewLinePlacement.BeforeOpenBraceInTypes : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes.DefaultValue ? NewLinePlacement.BeforeOpenBraceInAnonymousTypes : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers.DefaultValue ? NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInProperties.DefaultValue ? NewLinePlacement.BeforeOpenBraceInProperties : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInMethods.DefaultValue ? NewLinePlacement.BeforeOpenBraceInMethods : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInAccessors.DefaultValue ? NewLinePlacement.BeforeOpenBraceInAccessors : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods.DefaultValue ? NewLinePlacement.BeforeOpenBraceInAnonymousMethods : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody.DefaultValue ? NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody : 0) |
                (CSharpFormattingOptions2.NewLinesForBracesInControlBlocks.DefaultValue ? NewLinePlacement.BeforeOpenBraceInControlBlocks : 0) |
                (CSharpFormattingOptions2.NewLineForClausesInQuery.DefaultValue ? NewLinePlacement.BetweenQueryExpressionClauses : 0),
            labelPositioning: CSharpFormattingOptions2.LabelPositioning.DefaultValue,
            indentation:
                (CSharpFormattingOptions2.IndentBraces.DefaultValue ? IndentationPlacement.Braces : 0) |
                (CSharpFormattingOptions2.IndentBlock.DefaultValue ? IndentationPlacement.BlockContents : 0) |
                (CSharpFormattingOptions2.IndentSwitchCaseSection.DefaultValue ? IndentationPlacement.SwitchCaseContents : 0) |
                (CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock.DefaultValue ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
                (CSharpFormattingOptions2.IndentSwitchSection.DefaultValue ? IndentationPlacement.SwitchSection : 0),
            wrappingKeepStatementsOnSingleLine: CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine.DefaultValue,
            wrappingPreserveSingleLine: CSharpFormattingOptions2.WrappingPreserveSingleLine.DefaultValue);

        public static CSharpSyntaxFormattingOptions Create(AnalyzerConfigOptions options)
            => new(
                useTabs: options.GetOption(FormattingOptions2.UseTabs),
                tabSize: options.GetOption(FormattingOptions2.TabSize),
                indentationSize: options.GetOption(FormattingOptions2.IndentationSize),
                newLine: options.GetOption(FormattingOptions2.NewLine),
                separateImportDirectiveGroups: options.GetOption(GenerationOptions.SeparateImportDirectiveGroups),
                spacing:
                    (options.GetOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration) ? SpacePlacement.IgnoreAroundVariableDeclaration : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName) ? SpacePlacement.AfterMethodDeclarationName : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses) ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis) ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceAfterMethodCallName) ? SpacePlacement.AfterMethodCallName : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses) ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses) ? SpacePlacement.WithinMethodCallParentheses : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword) ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceWithinExpressionParentheses) ? SpacePlacement.WithinExpressionParentheses : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceWithinCastParentheses) ? SpacePlacement.WithinCastParentheses : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement) ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement) ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceWithinOtherParentheses) ? SpacePlacement.WithinOtherParentheses : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceAfterCast) ? SpacePlacement.AfterCast : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket) ? SpacePlacement.BeforeOpenSquareBracket : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets) ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets) ? SpacePlacement.WithinSquareBrackets : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration) ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration) ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceAfterComma) ? SpacePlacement.AfterComma : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBeforeComma) ? SpacePlacement.BeforeComma : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceAfterDot) ? SpacePlacement.AfterDot : 0) |
                    (options.GetOption(CSharpFormattingOptions2.SpaceBeforeDot) ? SpacePlacement.BeforeDot : 0),
                spacingAroundBinaryOperator: options.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator),
                newLines:
                    (options.GetOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit) ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes) ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLineForElse) ? NewLinePlacement.BeforeElse : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLineForCatch) ? NewLinePlacement.BeforeCatch : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLineForFinally) ? NewLinePlacement.BeforeFinally : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInTypes) ? NewLinePlacement.BeforeOpenBraceInTypes : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes) ? NewLinePlacement.BeforeOpenBraceInAnonymousTypes : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers) ? NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInProperties) ? NewLinePlacement.BeforeOpenBraceInProperties : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInMethods) ? NewLinePlacement.BeforeOpenBraceInMethods : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAccessors) ? NewLinePlacement.BeforeOpenBraceInAccessors : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods) ? NewLinePlacement.BeforeOpenBraceInAnonymousMethods : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody) ? NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks) ? NewLinePlacement.BeforeOpenBraceInControlBlocks : 0) |
                    (options.GetOption(CSharpFormattingOptions2.NewLineForClausesInQuery) ? NewLinePlacement.BetweenQueryExpressionClauses : 0),
                labelPositioning: options.GetOption(CSharpFormattingOptions2.LabelPositioning),
                indentation:
                    (options.GetOption(CSharpFormattingOptions2.IndentBraces) ? IndentationPlacement.Braces : 0) |
                    (options.GetOption(CSharpFormattingOptions2.IndentBlock) ? IndentationPlacement.BlockContents : 0) |
                    (options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSection) ? IndentationPlacement.SwitchCaseContents : 0) |
                    (options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock) ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
                    (options.GetOption(CSharpFormattingOptions2.IndentSwitchSection) ? IndentationPlacement.SwitchSection : 0),
                wrappingKeepStatementsOnSingleLine: options.GetOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine),
                wrappingPreserveSingleLine: options.GetOption(CSharpFormattingOptions2.WrappingPreserveSingleLine));
    }
}
