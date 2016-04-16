// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportOptionProvider, Shared]
    internal class CSharpFormattingOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = new List<IOption>
            {
                CSharpFormattingOptions.SpacingAfterMethodDeclarationName,
                CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis,
                CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses,
                CSharpFormattingOptions.SpaceAfterMethodCallName,
                CSharpFormattingOptions.SpaceWithinMethodCallParentheses,
                CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses,
                CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword,
                CSharpFormattingOptions.SpaceWithinExpressionParentheses,
                CSharpFormattingOptions.SpaceWithinCastParentheses,
                CSharpFormattingOptions.SpaceWithinOtherParentheses,
                CSharpFormattingOptions.SpaceAfterCast,
                CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration,
                CSharpFormattingOptions.SpaceBeforeOpenSquareBracket,
                CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets,
                CSharpFormattingOptions.SpaceWithinSquareBrackets,
                CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration,
                CSharpFormattingOptions.SpaceAfterComma,
                CSharpFormattingOptions.SpaceAfterDot,
                CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement,
                CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration,
                CSharpFormattingOptions.SpaceBeforeComma,
                CSharpFormattingOptions.SpaceBeforeDot,
                CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement,
                CSharpFormattingOptions.SpacingAroundBinaryOperator,
                CSharpFormattingOptions.IndentBraces,
                CSharpFormattingOptions.IndentBlock,
                CSharpFormattingOptions.IndentSwitchSection,
                CSharpFormattingOptions.IndentSwitchCaseSection,
                CSharpFormattingOptions.LabelPositioning,
                CSharpFormattingOptions.WrappingPreserveSingleLine,
                CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine,
                CSharpFormattingOptions.NewLinesForBracesInTypes,
                CSharpFormattingOptions.NewLinesForBracesInMethods,
                CSharpFormattingOptions.NewLinesForBracesInProperties,
                CSharpFormattingOptions.NewLinesForBracesInAccessors,
                CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods,
                CSharpFormattingOptions.NewLinesForBracesInControlBlocks,
                CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes,
                CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers,
                CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody,
                CSharpFormattingOptions.NewLineForElse,
                CSharpFormattingOptions.NewLineForCatch,
                CSharpFormattingOptions.NewLineForFinally,
                CSharpFormattingOptions.NewLineForMembersInObjectInit,
                CSharpFormattingOptions.NewLineForMembersInAnonymousTypes,
                CSharpFormattingOptions.NewLineForClausesInQuery
            }.ToImmutableArray();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
