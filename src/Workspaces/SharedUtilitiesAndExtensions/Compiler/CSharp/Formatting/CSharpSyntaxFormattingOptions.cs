// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal sealed class CSharpSyntaxFormattingOptions : SyntaxFormattingOptions
    {
        public readonly bool IndentBraces;

        public readonly bool SpacesIgnoreAroundVariableDeclaration;
        public readonly bool SpacingAfterMethodDeclarationName;
        public readonly bool SpaceBetweenEmptyMethodDeclarationParentheses;
        public readonly bool SpaceWithinMethodDeclarationParenthesis;
        public readonly bool SpaceAfterMethodCallName;
        public readonly bool SpaceBetweenEmptyMethodCallParentheses;
        public readonly bool SpaceWithinMethodCallParentheses;
        public readonly bool SpaceAfterControlFlowStatementKeyword;
        public readonly bool SpaceWithinExpressionParentheses;
        public readonly bool SpaceWithinCastParentheses;
        public readonly bool SpaceBeforeSemicolonsInForStatement;
        public readonly bool SpaceAfterSemicolonsInForStatement;
        public readonly bool SpaceWithinOtherParentheses;
        public readonly bool SpaceAfterCast;
        public readonly bool SpaceBeforeOpenSquareBracket;
        public readonly bool SpaceBetweenEmptySquareBrackets;
        public readonly bool SpaceWithinSquareBrackets;
        public readonly bool SpaceAfterColonInBaseTypeDeclaration;
        public readonly bool SpaceBeforeColonInBaseTypeDeclaration;
        public readonly bool SpaceAfterComma;
        public readonly bool SpaceBeforeComma;
        public readonly bool SpaceAfterDot;
        public readonly bool SpaceBeforeDot;
        public readonly BinaryOperatorSpacingOptions SpacingAroundBinaryOperator;

        public readonly bool NewLineForMembersInObjectInit;
        public readonly bool NewLineForMembersInAnonymousTypes;
        public readonly bool NewLineForElse;
        public readonly bool NewLineForCatch;
        public readonly bool NewLineForFinally;
        public readonly bool NewLinesForBracesInTypes;
        public readonly bool NewLinesForBracesInAnonymousTypes;
        public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers;
        public readonly bool NewLinesForBracesInProperties;
        public readonly bool NewLinesForBracesInMethods;
        public readonly bool NewLinesForBracesInAccessors;
        public readonly bool NewLinesForBracesInAnonymousMethods;
        public readonly bool NewLinesForBracesInLambdaExpressionBody;
        public readonly bool NewLinesForBracesInControlBlocks;
        public readonly bool WrappingKeepStatementsOnSingleLine;
        public readonly LabelPositionOptions LabelPositioning;
        public readonly bool IndentBlock;
        public readonly bool IndentSwitchCaseSection;
        public readonly bool IndentSwitchCaseSectionWhenBlock;
        public readonly bool IndentSwitchSection;
        public readonly bool NewLineForClausesInQuery;
        public readonly bool WrappingPreserveSingleLine;

        public CSharpSyntaxFormattingOptions(
            bool useTabs,
            int tabSize,
            int indentationSize,
            string newLine,
            bool separateImportDirectiveGroups,
            bool indentBraces,
            bool spacesIgnoreAroundVariableDeclaration,
            bool spacingAfterMethodDeclarationName,
            bool spaceBetweenEmptyMethodDeclarationParentheses,
            bool spaceWithinMethodDeclarationParenthesis,
            bool spaceAfterMethodCallName,
            bool spaceBetweenEmptyMethodCallParentheses,
            bool spaceWithinMethodCallParentheses,
            bool spaceAfterControlFlowStatementKeyword,
            bool spaceWithinExpressionParentheses,
            bool spaceWithinCastParentheses,
            bool spaceBeforeSemicolonsInForStatement,
            bool spaceAfterSemicolonsInForStatement,
            bool spaceWithinOtherParentheses,
            bool spaceAfterCast,
            bool spaceBeforeOpenSquareBracket,
            bool spaceBetweenEmptySquareBrackets,
            bool spaceWithinSquareBrackets,
            bool spaceAfterColonInBaseTypeDeclaration,
            bool spaceBeforeColonInBaseTypeDeclaration,
            bool spaceAfterComma,
            bool spaceBeforeComma,
            bool spaceAfterDot,
            bool spaceBeforeDot,
            BinaryOperatorSpacingOptions spacingAroundBinaryOperator,
            bool newLineForMembersInObjectInit,
            bool newLineForMembersInAnonymousTypes,
            bool newLineForElse,
            bool newLineForCatch,
            bool newLineForFinally,
            bool newLinesForBracesInTypes,
            bool newLinesForBracesInAnonymousTypes,
            bool newLinesForBracesInObjectCollectionArrayInitializers,
            bool newLinesForBracesInProperties,
            bool newLinesForBracesInMethods,
            bool newLinesForBracesInAccessors,
            bool newLinesForBracesInAnonymousMethods,
            bool newLinesForBracesInLambdaExpressionBody,
            bool newLinesForBracesInControlBlocks,
            bool wrappingKeepStatementsOnSingleLine,
            LabelPositionOptions labelPositioning,
            bool indentBlock,
            bool indentSwitchCaseSection,
            bool indentSwitchCaseSectionWhenBlock,
            bool indentSwitchSection,
            bool newLineForClausesInQuery,
            bool wrappingPreserveSingleLine)
            : base(useTabs,
                  tabSize,
                  indentationSize,
                  newLine,
                  separateImportDirectiveGroups)
        {
            IndentBraces = indentBraces;
            SpacesIgnoreAroundVariableDeclaration = spacesIgnoreAroundVariableDeclaration;
            SpacingAfterMethodDeclarationName = spacingAfterMethodDeclarationName;
            SpaceBetweenEmptyMethodDeclarationParentheses = spaceBetweenEmptyMethodDeclarationParentheses;
            SpaceWithinMethodDeclarationParenthesis = spaceWithinMethodDeclarationParenthesis;
            SpaceAfterMethodCallName = spaceAfterMethodCallName;
            SpaceBetweenEmptyMethodCallParentheses = spaceBetweenEmptyMethodCallParentheses;
            SpaceWithinMethodCallParentheses = spaceWithinMethodCallParentheses;
            SpaceAfterControlFlowStatementKeyword = spaceAfterControlFlowStatementKeyword;
            SpaceWithinExpressionParentheses = spaceWithinExpressionParentheses;
            SpaceWithinCastParentheses = spaceWithinCastParentheses;
            SpaceBeforeSemicolonsInForStatement = spaceBeforeSemicolonsInForStatement;
            SpaceAfterSemicolonsInForStatement = spaceAfterSemicolonsInForStatement;
            SpaceWithinOtherParentheses = spaceWithinOtherParentheses;
            SpaceAfterCast = spaceAfterCast;
            SpaceBeforeOpenSquareBracket = spaceBeforeOpenSquareBracket;
            SpaceBetweenEmptySquareBrackets = spaceBetweenEmptySquareBrackets;
            SpaceWithinSquareBrackets = spaceWithinSquareBrackets;
            SpaceAfterColonInBaseTypeDeclaration = spaceAfterColonInBaseTypeDeclaration;
            SpaceBeforeColonInBaseTypeDeclaration = spaceBeforeColonInBaseTypeDeclaration;
            SpaceAfterComma = spaceAfterComma;
            SpaceBeforeComma = spaceBeforeComma;
            SpaceAfterDot = spaceAfterDot;
            SpaceBeforeDot = spaceBeforeDot;
            SpacingAroundBinaryOperator = spacingAroundBinaryOperator;
            NewLineForMembersInObjectInit = newLineForMembersInObjectInit;
            NewLineForMembersInAnonymousTypes = newLineForMembersInAnonymousTypes;
            NewLineForElse = newLineForElse;
            NewLineForCatch = newLineForCatch;
            NewLineForFinally = newLineForFinally;
            NewLinesForBracesInTypes = newLinesForBracesInTypes;
            NewLinesForBracesInAnonymousTypes = newLinesForBracesInAnonymousTypes;
            NewLinesForBracesInObjectCollectionArrayInitializers = newLinesForBracesInObjectCollectionArrayInitializers;
            NewLinesForBracesInProperties = newLinesForBracesInProperties;
            NewLinesForBracesInMethods = newLinesForBracesInMethods;
            NewLinesForBracesInAccessors = newLinesForBracesInAccessors;
            NewLinesForBracesInAnonymousMethods = newLinesForBracesInAnonymousMethods;
            NewLinesForBracesInLambdaExpressionBody = newLinesForBracesInLambdaExpressionBody;
            NewLinesForBracesInControlBlocks = newLinesForBracesInControlBlocks;
            WrappingKeepStatementsOnSingleLine = wrappingKeepStatementsOnSingleLine;
            LabelPositioning = labelPositioning;
            IndentBlock = indentBlock;
            IndentSwitchCaseSection = indentSwitchCaseSection;
            IndentSwitchCaseSectionWhenBlock = indentSwitchCaseSectionWhenBlock;
            IndentSwitchSection = indentSwitchSection;
            NewLineForClausesInQuery = newLineForClausesInQuery;
            WrappingPreserveSingleLine = wrappingPreserveSingleLine;
        }

        public static readonly CSharpSyntaxFormattingOptions Default = new(
            useTabs: FormattingOptions2.UseTabs.DefaultValue,
            tabSize: FormattingOptions2.TabSize.DefaultValue,
            indentationSize: FormattingOptions2.IndentationSize.DefaultValue,
            newLine: FormattingOptions2.NewLine.DefaultValue,
            separateImportDirectiveGroups: GenerationOptions.SeparateImportDirectiveGroups.DefaultValue,
            indentBraces: CSharpFormattingOptions2.IndentBraces.DefaultValue,
            spacesIgnoreAroundVariableDeclaration: CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration.DefaultValue,
            spacingAfterMethodDeclarationName: CSharpFormattingOptions2.SpacingAfterMethodDeclarationName.DefaultValue,
            spaceBetweenEmptyMethodDeclarationParentheses: CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses.DefaultValue,
            spaceWithinMethodDeclarationParenthesis: CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis.DefaultValue,
            spaceAfterMethodCallName: CSharpFormattingOptions2.SpaceAfterMethodCallName.DefaultValue,
            spaceBetweenEmptyMethodCallParentheses: CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses.DefaultValue,
            spaceWithinMethodCallParentheses: CSharpFormattingOptions2.SpaceWithinMethodCallParentheses.DefaultValue,
            spaceAfterControlFlowStatementKeyword: CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword.DefaultValue,
            spaceWithinExpressionParentheses: CSharpFormattingOptions2.SpaceWithinExpressionParentheses.DefaultValue,
            spaceWithinCastParentheses: CSharpFormattingOptions2.SpaceWithinCastParentheses.DefaultValue,
            spaceBeforeSemicolonsInForStatement: CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement.DefaultValue,
            spaceAfterSemicolonsInForStatement: CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement.DefaultValue,
            spaceWithinOtherParentheses: CSharpFormattingOptions2.SpaceWithinOtherParentheses.DefaultValue,
            spaceAfterCast: CSharpFormattingOptions2.SpaceAfterCast.DefaultValue,
            spaceBeforeOpenSquareBracket: CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket.DefaultValue,
            spaceBetweenEmptySquareBrackets: CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets.DefaultValue,
            spaceWithinSquareBrackets: CSharpFormattingOptions2.SpaceWithinSquareBrackets.DefaultValue,
            spaceAfterColonInBaseTypeDeclaration: CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration.DefaultValue,
            spaceBeforeColonInBaseTypeDeclaration: CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration.DefaultValue,
            spaceAfterComma: CSharpFormattingOptions2.SpaceAfterComma.DefaultValue,
            spaceBeforeComma: CSharpFormattingOptions2.SpaceBeforeComma.DefaultValue,
            spaceAfterDot: CSharpFormattingOptions2.SpaceAfterDot.DefaultValue,
            spaceBeforeDot: CSharpFormattingOptions2.SpaceBeforeDot.DefaultValue,
            spacingAroundBinaryOperator: CSharpFormattingOptions2.SpacingAroundBinaryOperator.DefaultValue,
            newLineForMembersInObjectInit: CSharpFormattingOptions2.NewLineForMembersInObjectInit.DefaultValue,
            newLineForMembersInAnonymousTypes: CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes.DefaultValue,
            newLineForElse: CSharpFormattingOptions2.NewLineForElse.DefaultValue,
            newLineForCatch: CSharpFormattingOptions2.NewLineForCatch.DefaultValue,
            newLineForFinally: CSharpFormattingOptions2.NewLineForFinally.DefaultValue,
            newLinesForBracesInTypes: CSharpFormattingOptions2.NewLinesForBracesInTypes.DefaultValue,
            newLinesForBracesInAnonymousTypes: CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes.DefaultValue,
            newLinesForBracesInObjectCollectionArrayInitializers: CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers.DefaultValue,
            newLinesForBracesInProperties: CSharpFormattingOptions2.NewLinesForBracesInProperties.DefaultValue,
            newLinesForBracesInMethods: CSharpFormattingOptions2.NewLinesForBracesInMethods.DefaultValue,
            newLinesForBracesInAccessors: CSharpFormattingOptions2.NewLinesForBracesInAccessors.DefaultValue,
            newLinesForBracesInAnonymousMethods: CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods.DefaultValue,
            newLinesForBracesInLambdaExpressionBody: CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody.DefaultValue,
            newLinesForBracesInControlBlocks: CSharpFormattingOptions2.NewLinesForBracesInControlBlocks.DefaultValue,
            wrappingKeepStatementsOnSingleLine: CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine.DefaultValue,
            labelPositioning: CSharpFormattingOptions2.LabelPositioning.DefaultValue,
            indentBlock: CSharpFormattingOptions2.IndentBlock.DefaultValue,
            indentSwitchCaseSection: CSharpFormattingOptions2.IndentSwitchCaseSection.DefaultValue,
            indentSwitchCaseSectionWhenBlock: CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock.DefaultValue,
            indentSwitchSection: CSharpFormattingOptions2.IndentSwitchSection.DefaultValue,
            newLineForClausesInQuery: CSharpFormattingOptions2.NewLineForClausesInQuery.DefaultValue,
            wrappingPreserveSingleLine: CSharpFormattingOptions2.WrappingPreserveSingleLine.DefaultValue);

        public static CSharpSyntaxFormattingOptions Create(AnalyzerConfigOptions options)
            => new(
                useTabs: options.GetOption(FormattingOptions2.UseTabs),
                tabSize: options.GetOption(FormattingOptions2.TabSize),
                indentationSize: options.GetOption(FormattingOptions2.IndentationSize),
                newLine: options.GetOption(FormattingOptions2.NewLine),
                separateImportDirectiveGroups: options.GetOption(GenerationOptions.SeparateImportDirectiveGroups),
                indentBraces: options.GetOption(CSharpFormattingOptions2.IndentBraces),
                spacesIgnoreAroundVariableDeclaration: options.GetOption(CSharpFormattingOptions2.SpacesIgnoreAroundVariableDeclaration),
                spacingAfterMethodDeclarationName: options.GetOption(CSharpFormattingOptions2.SpacingAfterMethodDeclarationName),
                spaceBetweenEmptyMethodDeclarationParentheses: options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodDeclarationParentheses),
                spaceWithinMethodDeclarationParenthesis: options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodDeclarationParenthesis),
                spaceAfterMethodCallName: options.GetOption(CSharpFormattingOptions2.SpaceAfterMethodCallName),
                spaceBetweenEmptyMethodCallParentheses: options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptyMethodCallParentheses),
                spaceWithinMethodCallParentheses: options.GetOption(CSharpFormattingOptions2.SpaceWithinMethodCallParentheses),
                spaceAfterControlFlowStatementKeyword: options.GetOption(CSharpFormattingOptions2.SpaceAfterControlFlowStatementKeyword),
                spaceWithinExpressionParentheses: options.GetOption(CSharpFormattingOptions2.SpaceWithinExpressionParentheses),
                spaceWithinCastParentheses: options.GetOption(CSharpFormattingOptions2.SpaceWithinCastParentheses),
                spaceBeforeSemicolonsInForStatement: options.GetOption(CSharpFormattingOptions2.SpaceBeforeSemicolonsInForStatement),
                spaceAfterSemicolonsInForStatement: options.GetOption(CSharpFormattingOptions2.SpaceAfterSemicolonsInForStatement),
                spaceWithinOtherParentheses: options.GetOption(CSharpFormattingOptions2.SpaceWithinOtherParentheses),
                spaceAfterCast: options.GetOption(CSharpFormattingOptions2.SpaceAfterCast),
                spaceBeforeOpenSquareBracket: options.GetOption(CSharpFormattingOptions2.SpaceBeforeOpenSquareBracket),
                spaceBetweenEmptySquareBrackets: options.GetOption(CSharpFormattingOptions2.SpaceBetweenEmptySquareBrackets),
                spaceWithinSquareBrackets: options.GetOption(CSharpFormattingOptions2.SpaceWithinSquareBrackets),
                spaceAfterColonInBaseTypeDeclaration: options.GetOption(CSharpFormattingOptions2.SpaceAfterColonInBaseTypeDeclaration),
                spaceBeforeColonInBaseTypeDeclaration: options.GetOption(CSharpFormattingOptions2.SpaceBeforeColonInBaseTypeDeclaration),
                spaceAfterComma: options.GetOption(CSharpFormattingOptions2.SpaceAfterComma),
                spaceBeforeComma: options.GetOption(CSharpFormattingOptions2.SpaceBeforeComma),
                spaceAfterDot: options.GetOption(CSharpFormattingOptions2.SpaceAfterDot),
                spaceBeforeDot: options.GetOption(CSharpFormattingOptions2.SpaceBeforeDot),
                spacingAroundBinaryOperator: options.GetOption(CSharpFormattingOptions2.SpacingAroundBinaryOperator),
                newLineForMembersInObjectInit: options.GetOption(CSharpFormattingOptions2.NewLineForMembersInObjectInit),
                newLineForMembersInAnonymousTypes: options.GetOption(CSharpFormattingOptions2.NewLineForMembersInAnonymousTypes),
                newLineForElse: options.GetOption(CSharpFormattingOptions2.NewLineForElse),
                newLineForCatch: options.GetOption(CSharpFormattingOptions2.NewLineForCatch),
                newLineForFinally: options.GetOption(CSharpFormattingOptions2.NewLineForFinally),
                newLinesForBracesInTypes: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInTypes),
                newLinesForBracesInAnonymousTypes: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousTypes),
                newLinesForBracesInObjectCollectionArrayInitializers: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers),
                newLinesForBracesInProperties: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInProperties),
                newLinesForBracesInMethods: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInMethods),
                newLinesForBracesInAccessors: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAccessors),
                newLinesForBracesInAnonymousMethods: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInAnonymousMethods),
                newLinesForBracesInLambdaExpressionBody: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInLambdaExpressionBody),
                newLinesForBracesInControlBlocks: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInControlBlocks),
                wrappingKeepStatementsOnSingleLine: options.GetOption(CSharpFormattingOptions2.WrappingKeepStatementsOnSingleLine),
                labelPositioning: options.GetOption(CSharpFormattingOptions2.LabelPositioning),
                indentBlock: options.GetOption(CSharpFormattingOptions2.IndentBlock),
                indentSwitchCaseSection: options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSection),
                indentSwitchCaseSectionWhenBlock: options.GetOption(CSharpFormattingOptions2.IndentSwitchCaseSectionWhenBlock),
                indentSwitchSection: options.GetOption(CSharpFormattingOptions2.IndentSwitchSection),
                newLineForClausesInQuery: options.GetOption(CSharpFormattingOptions2.NewLineForClausesInQuery),
                wrappingPreserveSingleLine: options.GetOption(CSharpFormattingOptions2.WrappingPreserveSingleLine));
    }
}
