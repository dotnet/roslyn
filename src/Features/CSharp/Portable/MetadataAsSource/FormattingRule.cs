// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource
{
    internal partial class CSharpMetadataAsSourceService
    {
        private class FormattingRule : AbstractMetadataFormattingRule
        {
            protected override AdjustNewLinesOperation GetAdjustNewLinesOperationBetweenMembersAndUsings(SyntaxToken token1, SyntaxToken token2)
            {
                var previousToken = token1;
                var currentToken = token2;

                // We are not between members or usings if the last token wasn't the end of a statement or if the current token
                // is the end of a scope.
                if ((previousToken.Kind() != SyntaxKind.SemicolonToken && previousToken.Kind() != SyntaxKind.CloseBraceToken) ||
                    currentToken.Kind() == SyntaxKind.CloseBraceToken)
                {
                    return null;
                }

                SyntaxNode previousMember = FormattingRangeHelper.GetEnclosingMember(previousToken);
                SyntaxNode nextMember = FormattingRangeHelper.GetEnclosingMember(currentToken);

                // Is the previous statement an using directive? If so, treat it like a member to add
                // the right number of lines.
                if (previousToken.Kind() == SyntaxKind.SemicolonToken && previousToken.Parent.Kind() == SyntaxKind.UsingDirective)
                {
                    previousMember = previousToken.Parent;
                }

                if (previousMember == null || nextMember == null || previousMember == nextMember)
                {
                    return null;
                }

                // If we have two members of the same kind, we won't insert a blank line 
                if (previousMember.Kind() == nextMember.Kind())
                {
                    return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines);
                }

                // Force a blank line between the two nodes by counting the number of lines of
                // trivia and adding one to it.
                var triviaList = token1.TrailingTrivia.Concat(token2.LeadingTrivia);
                return FormattingOperations.CreateAdjustNewLinesOperation(GetNumberOfLines(triviaList) + 1, AdjustNewLinesOption.ForceLines);
            }

            public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, in NextAnchorIndentationOperationAction nextOperation)
            {
                return;
            }

            protected override bool IsNewLine(char c)
                => SyntaxFacts.IsNewLine(c);
        }
    }
}
