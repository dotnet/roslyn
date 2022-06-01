// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(SpeculativeTCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(AwaitCompletionProvider))]
    [Shared]
    internal class SpeculativeTCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SpeculativeTCompletionProvider()
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

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
                    const string T = nameof(T);
                    context.AddItem(CommonCompletionItem.Create(
                        T, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.TypeParameter));
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
            {
                // nop
            }
        }

        private static async Task<bool> ShouldShowSpeculativeTCompletionItemAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
                syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
            {
                return false;
            }

            // We could be in the middle of a ref/generic/tuple type, instead of a simple T case.
            // If we managed to walk out and get a different SpanStart, we treat it as a simple $$T case.

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(token.Parent, cancellationToken).ConfigureAwait(false);

            var context = CSharpSyntaxContext.CreateContext(document, semanticModel, position, cancellationToken);
            if (context.IsInTaskLikeTypeContext)
                return false;

            var spanStart = position;
            while (true)
            {
                var oldSpanStart = spanStart;

                spanStart = WalkOutOfGenericType(syntaxTree, spanStart, semanticModel, cancellationToken);
                spanStart = WalkOutOfTupleType(syntaxTree, spanStart, cancellationToken);
                spanStart = WalkOutOfRefType(syntaxTree, spanStart, cancellationToken);

                if (spanStart == oldSpanStart)
                {
                    break;
                }
            }

            return IsStartOfSpeculativeTContext(syntaxTree, spanStart, cancellationToken);
        }

        private static bool IsStartOfSpeculativeTContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            return syntaxTree.IsMemberDeclarationContext(position, contextOpt: null, SyntaxKindSet.AllMemberModifiers, SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: true, cancellationToken) ||
                   syntaxTree.IsStatementContext(position, token, cancellationToken) ||
                   syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                   syntaxTree.IsGlobalStatementContext(position, cancellationToken) ||
                   syntaxTree.IsDelegateReturnTypeContext(position, token);
        }

        private static int WalkOutOfGenericType(SyntaxTree syntaxTree, int position, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var spanStart = position;
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            if (syntaxTree.IsGenericTypeArgumentContext(position, token, cancellationToken, semanticModel))
            {
                if (syntaxTree.IsInPartiallyWrittenGeneric(spanStart, cancellationToken, out var nameToken))
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
            var prevToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                      .GetPreviousTokenIfTouchingWord(position);

            if (prevToken.IsKind(SyntaxKind.RefKeyword, SyntaxKind.ReadOnlyKeyword) && prevToken.Parent.IsKind(SyntaxKind.RefType))
            {
                return prevToken.SpanStart;
            }

            return position;
        }

        private static int WalkOutOfTupleType(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var prevToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                      .GetPreviousTokenIfTouchingWord(position);

            if (prevToken.IsPossibleTupleOpenParenOrComma())
            {
                return prevToken.Parent!.SpanStart;
            }

            return position;
        }
    }
}
