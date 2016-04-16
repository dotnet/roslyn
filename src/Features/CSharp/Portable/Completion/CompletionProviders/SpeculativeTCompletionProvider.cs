// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class SpeculativeTCompletionProvider : CompletionListProvider
    {
        private TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var showSpeculativeT = await document.IsValidContextForDocumentOrLinkedDocumentsAsync(
                (doc, ct) => ShouldShowSpeculativeTCompletionItemAsync(doc, position, ct),
                cancellationToken).ConfigureAwait(false);

            if (showSpeculativeT)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var filterSpan = this.GetTextChangeSpan(text, position);

                const string T = nameof(T);
                context.AddItem(new CompletionItem(this, T, filterSpan, descriptionFactory: null, glyph: Glyph.TypeParameter));
            }
        }

        private async Task<bool> ShouldShowSpeculativeTCompletionItemAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
                syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
            {
                return false;
            }

            // If we're in a generic type argument context, use the start of the generic type name
            // as the position for the rest of the context checks.
            int testPosition = position;
            var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            var semanticModel = await document.GetSemanticModelForNodeAsync(leftToken.Parent, cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsGenericTypeArgumentContext(position, leftToken, cancellationToken, semanticModel))
            {
                // Walk out until we find the start of the partial written generic
                SyntaxToken nameToken;
                while (syntaxTree.IsInPartiallyWrittenGeneric(testPosition, cancellationToken, out nameToken))
                {
                    testPosition = nameToken.SpanStart;
                }

                // If the user types Foo<T, automatic brace completion will insert the close brace
                // and the generic won't be "partially written".
                if (testPosition == position)
                {
                    var typeArgumentList = leftToken.GetAncestor<TypeArgumentListSyntax>();
                    if (typeArgumentList != null)
                    {
                        if (typeArgumentList.LessThanToken != default(SyntaxToken) && typeArgumentList.GreaterThanToken != default(SyntaxToken))
                        {
                            testPosition = typeArgumentList.LessThanToken.SpanStart;
                        }
                    }
                }
            }

            if ((!leftToken.GetPreviousTokenIfTouchingWord(position).IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) &&
                syntaxTree.IsMemberDeclarationContext(testPosition, contextOpt: null, validModifiers: SyntaxKindSet.AllMemberModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken)) ||
                syntaxTree.IsGlobalMemberDeclarationContext(testPosition, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                syntaxTree.IsGlobalStatementContext(testPosition, cancellationToken) ||
                syntaxTree.IsDelegateReturnTypeContext(testPosition, syntaxTree.FindTokenOnLeftOfPosition(testPosition, cancellationToken), cancellationToken))
            {
                return true;
            }

            return false;
        }
    }
}
