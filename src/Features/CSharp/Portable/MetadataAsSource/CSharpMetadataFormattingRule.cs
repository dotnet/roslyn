// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource
{
    internal class CSharpMetadataFormattingRule : AbstractMetadataFormattingRule
    {
        public static CSharpMetadataFormattingRule Instance = new CSharpMetadataFormattingRule();

        private CSharpMetadataFormattingRule()
        {
        }

        protected override AdjustNewLinesOperation GetAdjustNewLinesOperationBetweenMembersAndUsings(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            // We are not between members or usings if the last token wasn't the end of a statement or if the current token
            // is the end of a scope.
            if (previousToken.Kind() != SyntaxKind.SemicolonToken && previousToken.Kind() != SyntaxKind.CloseBraceToken)
                return null;

            if (currentToken.Kind() == SyntaxKind.CloseBraceToken)
                return null;

            var previousMember = previousToken.Kind() == SyntaxKind.SemicolonToken && previousToken.Parent.Kind() == SyntaxKind.UsingDirective
                ? previousToken.Parent
                : FormattingRangeHelper.GetEnclosingMember(previousToken);
            var nextMember = FormattingRangeHelper.GetEnclosingMember(currentToken);

            if (previousMember == null || nextMember == null || previousMember == nextMember)
                return null;

            // Ensure that we're only updating the whitespace between members.
            if (previousMember.GetLastToken() != previousToken || nextMember.GetFirstToken() != currentToken)
                return null;

            // If we have two members of the same kind, we won't insert a blank line 
            if (previousMember.Kind() == nextMember.Kind())
                return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);

            // We have different kinds, ensure at least one blank line.

            // See what sort of trivia we have between the items already.  If we have nothing, then just force a single
            // blank line.
            var triviaList = previousToken.TrailingTrivia.Concat(currentToken.LeadingTrivia);
            if (triviaList.All(t => t.IsWhitespaceOrEndOfLine()))
                return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.ForceLines);

            // We have existing non-whitespace trivia. Force a blank line between the two nodes by counting the number
            // of lines of trivia and adding one to it.
            return FormattingOperations.CreateAdjustNewLinesOperation(GetNumberOfLines(triviaList) + 1, AdjustNewLinesOption.ForceLines);
        }

        public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, AnalyzerConfigOptions options, in NextAnchorIndentationOperationAction nextOperation)
        {
        }

        protected override bool IsNewLine(char c)
            => SyntaxFacts.IsNewLine(c);
    }
}
