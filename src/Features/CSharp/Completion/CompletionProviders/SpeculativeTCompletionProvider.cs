// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class SpeculativeTCompletionProvider : AbstractCompletionProvider
    {
        private TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CompletionUtilities.GetTextChangeSpan(text, position);
        }

        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return CompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            return await document.GetUnionResultsFromDocumentAndLinks(
               UnionCompletionItemComparer.Instance,
               async (doc, ct) => await GetSpeculativeTCompletions(doc, position, ct).ConfigureAwait(false),
               cancellationToken).ConfigureAwait(false);
        }

        private async Task<IEnumerable<CompletionItem>> GetSpeculativeTCompletions(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
                syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
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
                var text = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var textChangeSpan = this.GetTextChangeSpan(text, position);

                const string T = "T";
                return SpecializedCollections.SingletonEnumerable(
                    new CSharpCompletionItem(document.Project.Solution.Workspace, this, T, textChangeSpan, descriptionFactory: null, glyph: Glyph.TypeParameter));
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }
    }
}
