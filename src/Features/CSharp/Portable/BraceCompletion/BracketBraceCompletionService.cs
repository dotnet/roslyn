// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.BraceCompletion
{
    [Export(LanguageNames.CSharp, typeof(IBraceCompletionService)), Shared]
    internal class BracketBraceCompletionService : AbstractCurlyBraceOrBracketCompletionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BracketBraceCompletionService()
        {
        }

        protected override char OpeningBrace => Bracket.OpenCharacter;

        protected override char ClosingBrace => Bracket.CloseCharacter;

        public override Task<bool> AllowOverTypeAsync(BraceCompletionContext context, CancellationToken cancellationToken)
            => AllowOverTypeInUserCodeWithValidClosingTokenAsync(context, cancellationToken);

        protected override bool IsValidOpeningBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.OpenBracketToken);

        protected override bool IsValidClosingBraceToken(SyntaxToken token) => token.IsKind(SyntaxKind.CloseBracketToken);

        protected override int AdjustFormattingEndPoint(SourceText text, SyntaxNode root, int startPoint, int endPoint)
            => endPoint;

        protected override ImmutableArray<AbstractFormattingRule> GetBraceFormattingIndentationRulesAfterReturn(IndentationOptions options)
        {
            return ImmutableArray.Create(BracketCompletionFormattingRule.Instance);
        }

        private sealed class BracketCompletionFormattingRule : BaseFormattingRule
        {
            public static readonly AbstractFormattingRule Instance = new BracketCompletionFormattingRule();

            public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
            {
                if (currentToken.IsKind(SyntaxKind.OpenBracketToken) && currentToken.Parent.IsKind(SyntaxKind.ListPattern))
                {
                    // For list patterns we format brackets as though they are a block, so when formatting after Return
                    // we add a newline
                    return CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines);
                }

                return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
            }

            public override void AddAlignTokensOperations(List<AlignTokensOperation> list, SyntaxNode node, in NextAlignTokensOperationAction nextOperation)
            {
                base.AddAlignTokensOperations(list, node, in nextOperation);

                var bracketPair = node.GetBracketPair();
                if (bracketPair.IsValidBracketOrBracePair() && node is ListPatternSyntax)
                {
                    // For list patterns we format brackets as though they are a block, so ensure the close bracket
                    // is aligned with the open bracket
                    AddAlignIndentationOfTokensToBaseTokenOperation(list, node, bracketPair.openBracket,
                        SpecializedCollections.SingletonEnumerable(bracketPair.closeBracket), AlignTokensOption.AlignIndentationOfTokensToFirstTokenOfBaseTokenLine);
                }
            }
        }
    }
}
