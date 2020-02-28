﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    // the default provider that will be called by the engine at the end of provider's chain.
    // there is no way for a user to be remove this provider.
    //
    // to reduce number of unnecessary heap allocations, most of them just return null.
    internal sealed class DefaultOperationProvider : AbstractFormattingRule
    {
        public static readonly DefaultOperationProvider Instance = new DefaultOperationProvider();

        private DefaultOperationProvider()
        {
        }

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextSuppressOperationAction nextOperation)
        {
        }

        public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, OptionSet optionSet, in NextAnchorIndentationOperationAction nextOperation)
        {
        }

        public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
        {
        }

        public override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, OptionSet optionSet, in NextAlignTokensOperationAction nextOperation)
        {
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
        {
            return null;
        }

        // return 1 space for every token pairs as a default operation
        public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
        {
            var space = currentToken.Kind() == SyntaxKind.EndOfFileToken ? 0 : 1;
            return FormattingOperations.CreateAdjustSpacesOperation(space, AdjustSpacesOption.DefaultSpacesIfOnSingleLine);
        }
    }
}
