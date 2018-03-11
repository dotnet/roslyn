// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractPartialTypeCompletionProvider : CommonCompletionProvider
    {
        private readonly AbstractSymbolCompletionFormat _format;

        protected AbstractPartialTypeCompletionProvider(AbstractSymbolCompletionFormat format)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            _format = format;
        }

        public async sealed override Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            try
            {
                var document = completionContext.Document;
                var position = completionContext.Position;
                var options = completionContext.Options;
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
                        var items = symbols?.Select(s => CreateCompletionItem(s, syntaxContext, options));

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
            INamedTypeSymbol symbol, SyntaxContext context, OptionSet options)
        {
            var displayAndInsertionText = _format.GetMinimalDisplayAndInsertionText(symbol, context, options);

            return SymbolCompletionItem.CreateWithSymbolId(
                displayText: displayAndInsertionText.displayText,
                insertionText: displayAndInsertionText.insertionText,
                symbols: ImmutableArray.Create(symbol),
                contextPosition: context.Position,
                rules: CompletionItemRules.Default);
        }

        protected abstract Task<SyntaxContext> CreateSyntaxContextAsync(
            Document document,
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken);

        protected abstract SyntaxNode GetPartialTypeSyntaxNode(SyntaxTree tree, int position, CancellationToken cancellationToken);

        protected virtual IEnumerable<INamedTypeSymbol> LookupCandidateSymbols(SyntaxContext context, INamedTypeSymbol declaredSymbol, CancellationToken cancellationToken)
        {
            if (declaredSymbol == null)
            {
                throw new ArgumentNullException(nameof(declaredSymbol));
            }

            SemanticModel semanticModel = context.SemanticModel;

            INamespaceOrTypeSymbol containingSymbol = declaredSymbol.ContainingSymbol as INamespaceOrTypeSymbol;

            if (containingSymbol == null)
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
            var insertionText = _format.GetInsertionTextAtInsertionTime(selectedItem, ch);
            return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, insertionText));
        }
    }
}
