// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
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

    internal static class OmniSharpSyntaxFormattingOptionsFactory
    {
        public static OmniSharpSyntaxFormattingOptionsWrapper Create(
            bool useTabs,
            int tabSize,
            int indentationSize,
            string newLine,
            bool separateImportDirectiveGroups,
            bool spacingAfterMethodDeclarationName,
            bool spaceWithinMethodDeclarationParenthesis,
            bool spaceBetweenEmptyMethodDeclarationParentheses,
            bool spaceAfterMethodCallName,
            bool spaceWithinMethodCallParentheses,
            bool spaceBetweenEmptyMethodCallParentheses,
            bool spaceAfterControlFlowStatementKeyword,
            bool spaceWithinExpressionParentheses,
            bool spaceWithinCastParentheses,
            bool spaceWithinOtherParentheses,
            bool spaceAfterCast,
            bool spaceBeforeOpenSquareBracket,
            bool spaceBetweenEmptySquareBrackets,
            bool spaceWithinSquareBrackets,
            bool spaceAfterColonInBaseTypeDeclaration,
            bool spaceAfterComma,
            bool spaceAfterDot,
            bool spaceAfterSemicolonsInForStatement,
            bool spaceBeforeColonInBaseTypeDeclaration,
            bool spaceBeforeComma,
            bool spaceBeforeDot,
            bool spaceBeforeSemicolonsInForStatement,
            OmniSharpBinaryOperatorSpacingOptions spacingAroundBinaryOperator,
            bool indentBraces,
            bool indentBlock,
            bool indentSwitchSection,
            bool indentSwitchCaseSection,
            bool indentSwitchCaseSectionWhenBlock,
            OmniSharpLabelPositionOptions labelPositioning,
            bool wrappingPreserveSingleLine,
            bool wrappingKeepStatementsOnSingleLine,
            bool newLinesForBracesInTypes,
            bool newLinesForBracesInMethods,
            bool newLinesForBracesInProperties,
            bool newLinesForBracesInAccessors,
            bool newLinesForBracesInAnonymousMethods,
            bool newLinesForBracesInControlBlocks,
            bool newLinesForBracesInAnonymousTypes,
            bool newLinesForBracesInObjectCollectionArrayInitializers,
            bool newLinesForBracesInLambdaExpressionBody,
            bool newLineForElse,
            bool newLineForCatch,
            bool newLineForFinally,
            bool newLineForMembersInObjectInit,
            bool newLineForMembersInAnonymousTypes,
            bool newLineForClausesInQuery)
            => new(new(
                FormattingOptions: new CSharpSyntaxFormattingOptions(
                    new LineFormattingOptions(
                        UseTabs: useTabs,
                        TabSize: tabSize,
                        IndentationSize: indentationSize,
                        NewLine: newLine),
                    separateImportDirectiveGroups: separateImportDirectiveGroups,
                    spacing:
                        (spacingAfterMethodDeclarationName ? SpacePlacement.AfterMethodDeclarationName : 0) |
                        (spaceBetweenEmptyMethodDeclarationParentheses ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
                        (spaceWithinMethodDeclarationParenthesis ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
                        (spaceAfterMethodCallName ? SpacePlacement.AfterMethodCallName : 0) |
                        (spaceBetweenEmptyMethodCallParentheses ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
                        (spaceWithinMethodCallParentheses ? SpacePlacement.WithinMethodCallParentheses : 0) |
                        (spaceAfterControlFlowStatementKeyword ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
                        (spaceWithinExpressionParentheses ? SpacePlacement.WithinExpressionParentheses : 0) |
                        (spaceWithinCastParentheses ? SpacePlacement.WithinCastParentheses : 0) |
                        (spaceBeforeSemicolonsInForStatement ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
                        (spaceAfterSemicolonsInForStatement ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
                        (spaceWithinOtherParentheses ? SpacePlacement.WithinOtherParentheses : 0) |
                        (spaceAfterCast ? SpacePlacement.AfterCast : 0) |
                        (spaceBeforeOpenSquareBracket ? SpacePlacement.BeforeOpenSquareBracket : 0) |
                        (spaceBetweenEmptySquareBrackets ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
                        (spaceWithinSquareBrackets ? SpacePlacement.WithinSquareBrackets : 0) |
                        (spaceAfterColonInBaseTypeDeclaration ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
                        (spaceBeforeColonInBaseTypeDeclaration ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
                        (spaceAfterComma ? SpacePlacement.AfterComma : 0) |
                        (spaceBeforeComma ? SpacePlacement.BeforeComma : 0) |
                        (spaceAfterDot ? SpacePlacement.AfterDot : 0) |
                        (spaceBeforeDot ? SpacePlacement.BeforeDot : 0),
                    spacingAroundBinaryOperator: (BinaryOperatorSpacingOptions)spacingAroundBinaryOperator,
                    newLines:
                        (newLineForMembersInObjectInit ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
                        (newLineForMembersInAnonymousTypes ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
                        (newLineForElse ? NewLinePlacement.BeforeElse : 0) |
                        (newLineForCatch ? NewLinePlacement.BeforeCatch : 0) |
                        (newLineForFinally ? NewLinePlacement.BeforeFinally : 0) |
                        (newLinesForBracesInTypes ? NewLinePlacement.BeforeOpenBraceInTypes : 0) |
                        (newLinesForBracesInAnonymousTypes ? NewLinePlacement.BeforeOpenBraceInAnonymousTypes : 0) |
                        (newLinesForBracesInObjectCollectionArrayInitializers ? NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers : 0) |
                        (newLinesForBracesInProperties ? NewLinePlacement.BeforeOpenBraceInProperties : 0) |
                        (newLinesForBracesInMethods ? NewLinePlacement.BeforeOpenBraceInMethods : 0) |
                        (newLinesForBracesInAccessors ? NewLinePlacement.BeforeOpenBraceInAccessors : 0) |
                        (newLinesForBracesInAnonymousMethods ? NewLinePlacement.BeforeOpenBraceInAnonymousMethods : 0) |
                        (newLinesForBracesInLambdaExpressionBody ? NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody : 0) |
                        (newLinesForBracesInControlBlocks ? NewLinePlacement.BeforeOpenBraceInControlBlocks : 0) |
                        (newLineForClausesInQuery ? NewLinePlacement.BetweenQueryExpressionClauses : 0),
                    labelPositioning: (LabelPositionOptions)labelPositioning,
                    indentation:
                        (indentBraces ? IndentationPlacement.Braces : 0) |
                        (indentBlock ? IndentationPlacement.BlockContents : 0) |
                        (indentSwitchCaseSection ? IndentationPlacement.SwitchCaseContents : 0) |
                        (indentSwitchCaseSectionWhenBlock ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
                        (indentSwitchSection ? IndentationPlacement.SwitchSection : 0),
                    wrappingKeepStatementsOnSingleLine: wrappingKeepStatementsOnSingleLine,
                    wrappingPreserveSingleLine: wrappingPreserveSingleLine),
                SimplifierOptions: CSharpSimplifierOptions.Default,
                AddImportOptions: AddImportPlacementOptions.Default));
    }
}
