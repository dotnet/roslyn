// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractPartialTypeCompletionProvider : CommonCompletionProvider
    {
        protected AbstractPartialTypeCompletionProvider()
        {
        }

        public async sealed override Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            try
            {
                var document = completionContext.Document;
                var position = completionContext.Position;
                var cancellationToken = completionContext.CancellationToken;

                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var node = GetPartialTypeSyntaxNode(tree, position, cancellationToken);

                if (node != null)
                {
                    var semanticModel = await document.GetSemanticModelForNodeAsync(node, cancellationToken).ConfigureAwait(false);
                    var syntaxContext = await CreateSyntaxContextAsync(document, semanticModel, position, cancellationToken).ConfigureAwait(false);

                    if (semanticModel.GetDeclaredSymbol(node, cancellationToken) is INamedTypeSymbol declaredSymbol)
                    {
                        var symbols = LookupCandidateSymbols(syntaxContext, declaredSymbol, cancellationToken);
                        var items = symbols?.Select(s => CreateCompletionItem(s, syntaxContext));

                        if (items != null)
                        {
                            completionContext.AddItems(items);
                        }
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
            }
        }

        private CompletionItem CreateCompletionItem(
            INamedTypeSymbol symbol, SyntaxContext context)
        {
            var (displayText, suffix, insertionText) = GetDisplayAndSuffixAndInsertionText(symbol, context);

            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayText,
                displayTextSuffix: suffix,
                insertionText: insertionText,
                symbols: ImmutableArray.Create(symbol),
                contextPosition: context.Position,
                properties: GetProperties(symbol, context),
                rules: CompletionItemRules.Default);
        }

        protected abstract ImmutableDictionary<string, string> GetProperties(
            INamedTypeSymbol symbol, SyntaxContext context);

        protected abstract Task<SyntaxContext> CreateSyntaxContextAsync(
            Document document,
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken);

        protected abstract SyntaxNode GetPartialTypeSyntaxNode(SyntaxTree tree, int position, CancellationToken cancellationToken);

        protected abstract (string displayText, string suffix, string insertionText) GetDisplayAndSuffixAndInsertionText(INamedTypeSymbol symbol, SyntaxContext context);

        protected virtual IEnumerable<INamedTypeSymbol> LookupCandidateSymbols(SyntaxContext context, INamedTypeSymbol declaredSymbol, CancellationToken cancellationToken)
        {
            if (declaredSymbol == null)
            {
                throw new ArgumentNullException(nameof(declaredSymbol));
            }

            var semanticModel = context.SemanticModel;


            if (!(declaredSymbol.ContainingSymbol is INamespaceOrTypeSymbol containingSymbol))
            {
                return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
            }

            return semanticModel.LookupNamespacesAndTypes(context.Position, containingSymbol)
                                .OfType<INamedTypeSymbol>()
                                .Where(symbol => declaredSymbol.TypeKind == symbol.TypeKind &&
                                                 NotNewDeclaredMember(symbol, context) &&
                                                 InSameProject(symbol, semanticModel.Compilation));
        }

        private static bool InSameProject(INamedTypeSymbol symbol, Compilation compilation)
        {
            return symbol.DeclaringSyntaxReferences.Any(r => compilation.SyntaxTrees.Contains(r.SyntaxTree));
        }

        private static bool NotNewDeclaredMember(INamedTypeSymbol symbol, SyntaxContext context)
        {
            return symbol.DeclaringSyntaxReferences
                         .Select(reference => reference.GetSyntax())
                         .Any(node => !(node.SyntaxTree == context.SyntaxTree && node.Span.IntersectsWith(context.Position)));
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        public override Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            var insertionText = SymbolCompletionItem.GetInsertionText(selectedItem);
            return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, insertionText));
        }
    }
}
