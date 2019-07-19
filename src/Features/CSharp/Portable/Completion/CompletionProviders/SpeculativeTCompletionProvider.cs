// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class SpeculativeTCompletionProvider : CommonCompletionProvider
    {
        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
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

                    const string T = nameof(T);
                    context.AddItem(CommonCompletionItem.Create(
                        T, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.TypeParameter));
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
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

            var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            var semanticModel = await document.GetSemanticModelForNodeAsync(leftToken.Parent, cancellationToken).ConfigureAwait(false);

            // If we're in a ref type or a generic type argument context, use the start of the ref/generic type
            // as the position for the rest of the context checks.
            var testPosition = position;
            var prevToken = leftToken.GetPreviousTokenIfTouchingWord(position);

            if (syntaxTree.IsGenericTypeArgumentContext(position, leftToken, cancellationToken, semanticModel))
            {
                // Walk out until we find the start of the partial written generic
                while (syntaxTree.IsInPartiallyWrittenGeneric(testPosition, cancellationToken, out var nameToken))
                {
                    testPosition = nameToken.SpanStart;
                }

                // If the user types Goo<T, automatic brace completion will insert the close brace
                // and the generic won't be "partially written".
                if (testPosition == position)
                {
                    testPosition = leftToken.GetAncestor<GenericNameSyntax>()?.SpanStart ?? testPosition;
                }

                var tokenLeftOfType = syntaxTree.FindTokenOnLeftOfPosition(testPosition, cancellationToken);
                if (tokenLeftOfType.IsKind(SyntaxKind.DotToken) && tokenLeftOfType.Parent.IsKind(SyntaxKind.QualifiedName))
                {
                    testPosition = tokenLeftOfType.Parent.SpanStart;
                    tokenLeftOfType = syntaxTree.FindTokenOnLeftOfPosition(testPosition, cancellationToken);
                }
                MoveTestPositionIfRefType(tokenLeftOfType, ref testPosition);
            }
            else
            {
                MoveTestPositionIfRefType(prevToken, ref testPosition);
            }

            if ((!prevToken.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) &&
                syntaxTree.IsMemberDeclarationContext(testPosition, contextOpt: null, validModifiers: SyntaxKindSet.AllMemberModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken)) ||
                syntaxTree.IsStatementContext(testPosition, leftToken, cancellationToken) ||
                syntaxTree.IsGlobalMemberDeclarationContext(testPosition, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                syntaxTree.IsGlobalStatementContext(testPosition, cancellationToken) ||
                syntaxTree.IsDelegateReturnTypeContext(testPosition, syntaxTree.FindTokenOnLeftOfPosition(testPosition, cancellationToken), cancellationToken))
            {
                return true;
            }

            return false;

            static void MoveTestPositionIfRefType(SyntaxToken tokenLeftOfType, ref int testPosition)
            {
                if (tokenLeftOfType.IsKind(SyntaxKind.RefKeyword, SyntaxKind.ReadOnlyKeyword) && tokenLeftOfType.Parent.IsKind(SyntaxKind.RefType))
                {
                    testPosition = tokenLeftOfType.Parent.SpanStart;
                }
            }
        }
    }
}
