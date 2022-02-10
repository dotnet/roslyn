// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CSharp.Formatting
{
    internal enum OmniSharpLabelPositionOptions
    {
        LeftMost = LabelPositionOptions.LeftMost,
        OneLess = LabelPositionOptions.OneLess,
        NoIndent = LabelPositionOptions.NoIndent
    }

    internal enum OmniSharpBinaryOperatorSpacingOptions
    {
        Single = BinaryOperatorSpacingOptions.Single,
        Ignore = BinaryOperatorSpacingOptions.Ignore,
        Remove = BinaryOperatorSpacingOptions.Remove
    }

    internal sealed record class OmniSharpCSharpSyntaxFormattingOptions(
        bool UseTabs,
        int TabSize,
        int IndentationSize,
        string NewLine,
        bool SeparateImportDirectiveGroups,
        bool SpacingAfterMethodDeclarationName,
        bool SpaceWithinMethodDeclarationParenthesis,
        bool SpaceBetweenEmptyMethodDeclarationParentheses,
        bool SpaceAfterMethodCallName,
        bool SpaceWithinMethodCallParentheses,
        bool SpaceBetweenEmptyMethodCallParentheses,
        bool SpaceAfterControlFlowStatementKeyword,
        bool SpaceWithinExpressionParentheses,
        bool SpaceWithinCastParentheses,
        bool SpaceWithinOtherParentheses,
        bool SpaceAfterCast,
        bool SpacesIgnoreAroundVariableDeclaration,
        bool SpaceBeforeOpenSquareBracket,
        bool SpaceBetweenEmptySquareBrackets,
        bool SpaceWithinSquareBrackets,
        bool SpaceAfterColonInBaseTypeDeclaration,
        bool SpaceAfterComma,
        bool SpaceAfterDot,
        bool SpaceAfterSemicolonsInForStatement,
        bool SpaceBeforeColonInBaseTypeDeclaration,
        bool SpaceBeforeComma,
        bool SpaceBeforeDot,
        bool SpaceBeforeSemicolonsInForStatement,
        OmniSharpBinaryOperatorSpacingOptions SpacingAroundBinaryOperator,
        bool IndentBraces,
        bool IndentBlock,
        bool IndentSwitchSection,
        bool IndentSwitchCaseSection,
        bool IndentSwitchCaseSectionWhenBlock,
        OmniSharpLabelPositionOptions LabelPositioning,
        bool WrappingPreserveSingleLine,
        bool WrappingKeepStatementsOnSingleLine,
        bool NewLinesForBracesInTypes,
        bool NewLinesForBracesInMethods,
        bool NewLinesForBracesInProperties,
        bool NewLinesForBracesInAccessors,
        bool NewLinesForBracesInAnonymousMethods,
        bool NewLinesForBracesInControlBlocks,
        bool NewLinesForBracesInAnonymousTypes,
        bool NewLinesForBracesInObjectCollectionArrayInitializers,
        bool NewLinesForBracesInLambdaExpressionBody,
        bool NewLineForElse,
        bool NewLineForCatch,
        bool NewLineForFinally,
        bool NewLineForMembersInObjectInit,
        bool NewLineForMembersInAnonymousTypes,
        bool NewLineForClausesInQuery) : OmniSharpSyntaxFormattingOptions
    {
        internal override SyntaxFormattingOptions ToSyntaxFormattingOptions()
            => new CSharpSyntaxFormattingOptions(
            useTabs: UseTabs,
            tabSize: TabSize,
            indentationSize: IndentationSize,
            newLine: NewLine,
            separateImportDirectiveGroups: SeparateImportDirectiveGroups,
            spacing:
                (SpacingAfterMethodDeclarationName ? SpacePlacement.AfterMethodDeclarationName : 0) |
                (SpaceBetweenEmptyMethodDeclarationParentheses ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
                (SpaceWithinMethodDeclarationParenthesis ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
                (SpaceAfterMethodCallName ? SpacePlacement.AfterMethodCallName : 0) |
                (SpaceBetweenEmptyMethodCallParentheses ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
                (SpaceWithinMethodCallParentheses ? SpacePlacement.WithinMethodCallParentheses : 0) |
                (SpaceAfterControlFlowStatementKeyword ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
                (SpaceWithinExpressionParentheses ? SpacePlacement.WithinExpressionParentheses : 0) |
                (SpaceWithinCastParentheses ? SpacePlacement.WithinCastParentheses : 0) |
                (SpaceBeforeSemicolonsInForStatement ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
                (SpaceAfterSemicolonsInForStatement ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
                (SpaceWithinOtherParentheses ? SpacePlacement.WithinOtherParentheses : 0) |
                (SpaceAfterCast ? SpacePlacement.AfterCast : 0) |
                (SpaceBeforeOpenSquareBracket ? SpacePlacement.BeforeOpenSquareBracket : 0) |
                (SpaceBetweenEmptySquareBrackets ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
                (SpaceWithinSquareBrackets ? SpacePlacement.WithinSquareBrackets : 0) |
                (SpaceAfterColonInBaseTypeDeclaration ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
                (SpaceBeforeColonInBaseTypeDeclaration ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
                (SpaceAfterComma ? SpacePlacement.AfterComma : 0) |
                (SpaceBeforeComma ? SpacePlacement.BeforeComma : 0) |
                (SpaceAfterDot ? SpacePlacement.AfterDot : 0) |
                (SpaceBeforeDot ? SpacePlacement.BeforeDot : 0),
            spacingAroundBinaryOperator: (BinaryOperatorSpacingOptions)SpacingAroundBinaryOperator,
            newLines:
                (NewLineForMembersInObjectInit ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
                (NewLineForMembersInAnonymousTypes ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
                (NewLineForElse ? NewLinePlacement.BeforeElse : 0) |
                (NewLineForCatch ? NewLinePlacement.BeforeCatch : 0) |
                (NewLineForFinally ? NewLinePlacement.BeforeFinally : 0) |
                (NewLinesForBracesInTypes ? NewLinePlacement.BeforeOpenBraceInTypes : 0) |
                (NewLinesForBracesInAnonymousTypes ? NewLinePlacement.BeforeOpenBraceInAnonymousTypes : 0) |
                (NewLinesForBracesInObjectCollectionArrayInitializers ? NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers : 0) |
                (NewLinesForBracesInProperties ? NewLinePlacement.BeforeOpenBraceInProperties : 0) |
                (NewLinesForBracesInMethods ? NewLinePlacement.BeforeOpenBraceInMethods : 0) |
                (NewLinesForBracesInAccessors ? NewLinePlacement.BeforeOpenBraceInAccessors : 0) |
                (NewLinesForBracesInAnonymousMethods ? NewLinePlacement.BeforeOpenBraceInAnonymousMethods : 0) |
                (NewLinesForBracesInLambdaExpressionBody ? NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody : 0) |
                (NewLinesForBracesInControlBlocks ? NewLinePlacement.BeforeOpenBraceInControlBlocks : 0) |
                (NewLineForClausesInQuery ? NewLinePlacement.BetweenQueryExpressionClauses : 0),
            labelPositioning: (LabelPositionOptions)LabelPositioning,
            indentation:
                (IndentBraces ? IndentationPlacement.Braces : 0) |
                (IndentBlock ? IndentationPlacement.BlockContents : 0) |
                (IndentSwitchCaseSection ? IndentationPlacement.SwitchCaseContents : 0) |
                (IndentSwitchCaseSectionWhenBlock ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
                (IndentSwitchSection ? IndentationPlacement.SwitchSection : 0),
            wrappingKeepStatementsOnSingleLine: WrappingKeepStatementsOnSingleLine,
            wrappingPreserveSingleLine: WrappingPreserveSingleLine);
    }
}
