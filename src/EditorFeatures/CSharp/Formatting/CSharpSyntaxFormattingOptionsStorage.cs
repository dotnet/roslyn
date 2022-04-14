// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal static class CSharpSyntaxFormattingOptionsStorage
{
    [ExportLanguageService(typeof(ISyntaxFormattingOptionsStorage), LanguageNames.CSharp), Shared]
    private sealed class Service : ISyntaxFormattingOptionsStorage
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Service()
        {
        }

        public SyntaxFormattingOptions GetOptions(IGlobalOptionService globalOptions)
            => GetCSharpSyntaxFormattingOptions(globalOptions);
    }

    public static CSharpSyntaxFormattingOptions GetCSharpSyntaxFormattingOptions(this IGlobalOptionService globalOptions)
        => new(
            lineFormatting: globalOptions.GetLineFormattingOptions(LanguageNames.CSharp),
            separateImportDirectiveGroups: globalOptions.GetOption(GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp),
            spacing:
                (globalOptions.GetOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration) ? SpacePlacement.IgnoreAroundVariableDeclaration : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName) ? SpacePlacement.AfterMethodDeclarationName : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses) ? SpacePlacement.BetweenEmptyMethodDeclarationParentheses : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis) ? SpacePlacement.WithinMethodDeclarationParenthesis : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceAfterMethodCallName) ? SpacePlacement.AfterMethodCallName : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses) ? SpacePlacement.BetweenEmptyMethodCallParentheses : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses) ? SpacePlacement.WithinMethodCallParentheses : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword) ? SpacePlacement.AfterControlFlowStatementKeyword : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceWithinExpressionParentheses) ? SpacePlacement.WithinExpressionParentheses : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceWithinCastParentheses) ? SpacePlacement.WithinCastParentheses : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement) ? SpacePlacement.BeforeSemicolonsInForStatement : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement) ? SpacePlacement.AfterSemicolonsInForStatement : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceWithinOtherParentheses) ? SpacePlacement.WithinOtherParentheses : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceAfterCast) ? SpacePlacement.AfterCast : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket) ? SpacePlacement.BeforeOpenSquareBracket : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets) ? SpacePlacement.BetweenEmptySquareBrackets : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets) ? SpacePlacement.WithinSquareBrackets : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration) ? SpacePlacement.AfterColonInBaseTypeDeclaration : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration) ? SpacePlacement.BeforeColonInBaseTypeDeclaration : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceAfterComma) ? SpacePlacement.AfterComma : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBeforeComma) ? SpacePlacement.BeforeComma : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceAfterDot) ? SpacePlacement.AfterDot : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.SpaceBeforeDot) ? SpacePlacement.BeforeDot : 0),
            spacingAroundBinaryOperator: globalOptions.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator),
            newLines:
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit) ? NewLinePlacement.BeforeMembersInObjectInitializers : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes) ? NewLinePlacement.BeforeMembersInAnonymousTypes : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLineForElse) ? NewLinePlacement.BeforeElse : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLineForCatch) ? NewLinePlacement.BeforeCatch : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLineForFinally) ? NewLinePlacement.BeforeFinally : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInTypes) ? NewLinePlacement.BeforeOpenBraceInTypes : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes) ? NewLinePlacement.BeforeOpenBraceInAnonymousTypes : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers) ? NewLinePlacement.BeforeOpenBraceInObjectCollectionArrayInitializers : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInProperties) ? NewLinePlacement.BeforeOpenBraceInProperties : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInMethods) ? NewLinePlacement.BeforeOpenBraceInMethods : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAccessors) ? NewLinePlacement.BeforeOpenBraceInAccessors : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods) ? NewLinePlacement.BeforeOpenBraceInAnonymousMethods : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody) ? NewLinePlacement.BeforeOpenBraceInLambdaExpressionBody : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks) ? NewLinePlacement.BeforeOpenBraceInControlBlocks : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.NewLineForClausesInQuery) ? NewLinePlacement.BetweenQueryExpressionClauses : 0),
            labelPositioning: globalOptions.GetOption(CSharpFormattingOptions2.LabelPositioning),
            indentation:
                (globalOptions.GetOption(CSharpFormattingOptions2.IndentBraces) ? IndentationPlacement.Braces : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.IndentBlock) ? IndentationPlacement.BlockContents : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSection) ? IndentationPlacement.SwitchCaseContents : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock) ? IndentationPlacement.SwitchCaseContentsWhenBlock : 0) |
                (globalOptions.GetOption(CSharpFormattingOptions2.IndentSwitchSection) ? IndentationPlacement.SwitchSection : 0),
            wrappingKeepStatementsOnSingleLine: globalOptions.GetOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine),
            wrappingPreserveSingleLine: globalOptions.GetOption(CSharpFormattingOptions2.WrappingPreserveSingleLine));
}
