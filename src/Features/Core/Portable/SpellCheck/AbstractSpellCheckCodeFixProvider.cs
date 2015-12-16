// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SpellCheck
{
    internal abstract class AbstractSpellCheckCodeFixProvider<TSimpleName> : CodeFixProvider
        where TSimpleName : SyntaxNode
    {
        protected abstract bool IsGeneric(TSimpleName nameNode);
        protected abstract bool IsGeneric(CompletionItem completionItem);
        protected abstract SyntaxToken CreateIdentifier(TSimpleName nameNode, string newName);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = syntaxRoot.FindNode(span);
            if (node == null || node.Span != span)
            {
                return;
            }

            SemanticModel semanticModel = null;
            foreach (var name in node.DescendantNodesAndSelf().OfType<TSimpleName>())
            {
                // Only bother with identifiers that are at least 3 characters long.
                // We don't want to be too noisy as you're just starting to type something.
                var nameText = name.GetFirstToken().ValueText;
                if (nameText?.Length >= 3)
                {
                    semanticModel = semanticModel ?? await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var symbolInfo = semanticModel.GetSymbolInfo(name, cancellationToken);
                    if (symbolInfo.Symbol == null)
                    {
                        await CreateSpellCheckCodeIssueAsync(context, name, nameText, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task CreateSpellCheckCodeIssueAsync(CodeFixContext context, TSimpleName nameNode, string nameText, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var completionList = await CompletionService.GetCompletionListAsync(
                document, nameNode.SpanStart, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), cancellationToken: cancellationToken).ConfigureAwait(false);
            if (completionList == null)
            {
                return;
            }

            var completionRules = CompletionService.GetCompletionRules(document);
            var onlyConsiderGenerics = IsGeneric(nameNode);
            var results = new MultiDictionary<double, string>();

            using (var similarityChecker = new WordSimilarityChecker(nameText))
            {
                foreach (var item in completionList.Items)
                {
                    if (onlyConsiderGenerics && !IsGeneric(item))
                    {
                        continue;
                    }

                    var candidateText = item.FilterText;
                    double matchCost;
                    if (!similarityChecker.AreSimilar(candidateText, out matchCost))
                    {
                        continue;
                    }

                    var insertionText = completionRules.GetTextChange(item).NewText;
                    results.Add(matchCost, insertionText);
                }
            }

            var matches = results.OrderBy(kvp => kvp.Key)
                                 .SelectMany(kvp => kvp.Value.Order())
                                 .Where(t => t != nameText)
                                 .Take(3)
                                 .Select(n => CreateCodeAction(nameNode, nameText, n, document));
            context.RegisterFixes(matches, context.Diagnostics);
        }

        private SpellCheckCodeAction CreateCodeAction(TSimpleName nameNode, string oldName, string newName, Document document)
        {
            return new SpellCheckCodeAction(
                string.Format(FeaturesResources.ChangeTo, oldName, newName),
                c => Update(document, nameNode, newName, c),
                equivalenceKey: newName);
        }

        private async Task<Document> Update(Document document, TSimpleName nameNode, string newName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceToken(nameNode.GetFirstToken(), CreateIdentifier(nameNode, newName));

            return document.WithSyntaxRoot(newRoot);
        }

        private class SpellCheckCodeAction : CodeAction.DocumentChangeAction
        {
            public SpellCheckCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
