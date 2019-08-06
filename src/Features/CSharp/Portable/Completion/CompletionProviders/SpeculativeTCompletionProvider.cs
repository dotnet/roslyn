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

            // We could be in the middle of a ref/generic/tuple type, instead of a simple T case.
            // If we managed to walk out and get a different SpanStart, we treat it as a slightly different $$T case, otherwise it's a simple T case.

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var semanticModel = await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(false);

            var spanStart = position;
            while (true)
            {
                var orgSpanStart = spanStart;

                spanStart = WalkOutOfGenericType(syntaxTree, spanStart, semanticModel, cancellationToken);
                spanStart = WalkOutOfTupleType(syntaxTree, spanStart, cancellationToken);
                spanStart = WalkOutOfRefType(syntaxTree, spanStart, cancellationToken);

                if (spanStart == orgSpanStart)
                {
                    break;
                }
            }

            if (IsStartOfSpeculativeTContext(syntaxTree, spanStart, allowAsyncKeyword: spanStart != position, cancellationToken))
            {
                return true;
            }

            return false;
        }

        private static bool IsStartOfSpeculativeTContext(SyntaxTree syntaxTree, int position, bool allowAsyncKeyword, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var prevToken = token.GetPreviousTokenIfTouchingWord(position);

            if (((allowAsyncKeyword || !prevToken.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword)) &&
                syntaxTree.IsMemberDeclarationContext(position, contextOpt: null, validModifiers: SyntaxKindSet.AllMemberModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations, canBePartial: true, cancellationToken: cancellationToken)) ||
                syntaxTree.IsStatementContext(position, token, cancellationToken) ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                syntaxTree.IsGlobalStatementContext(position, cancellationToken) ||
                syntaxTree.IsDelegateReturnTypeContext(position, token, cancellationToken))
            {
                return true;
            }

            return false;
        }

        private static int WalkOutOfGenericType(SyntaxTree syntaxTree, int position, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var spanStart = position;
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            if (syntaxTree.IsGenericTypeArgumentContext(position, token, cancellationToken, semanticModel))
            {
                // Walk out until we find the start of the partial written generic
                while (syntaxTree.IsInPartiallyWrittenGeneric(spanStart, cancellationToken, out var nameToken))
                {
                    spanStart = nameToken.SpanStart;
                }

                // If the user types Goo<T, automatic brace completion will insert the close brace
                // and the generic won't be "partially written".
                if (spanStart == position)
                {
                    spanStart = token.GetAncestor<GenericNameSyntax>()?.SpanStart ?? spanStart;
                }

                var tokenLeftOfGenericName = syntaxTree.FindTokenOnLeftOfPosition(spanStart, cancellationToken);
                if (tokenLeftOfGenericName.IsKind(SyntaxKind.DotToken) && tokenLeftOfGenericName.Parent.IsKind(SyntaxKind.QualifiedName))
                {
                    spanStart = tokenLeftOfGenericName.Parent.SpanStart;
                }
            }

            return spanStart;
        }

        private static int WalkOutOfRefType(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var prevToken = token.GetPreviousTokenIfTouchingWord(position);

            if (prevToken.IsKind(SyntaxKind.RefKeyword, SyntaxKind.ReadOnlyKeyword) && prevToken.Parent.IsKind(SyntaxKind.RefType))
            {
                return prevToken.SpanStart;
            }

            return position;
        }

        private static int WalkOutOfTupleType(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var prevToken = token.GetPreviousTokenIfTouchingWord(position);

            if (prevToken.IsPossibleTupleOpenParenOrComma())
            {
                return prevToken.Parent.SpanStart;
            }

            return position;
        }
    }
}
