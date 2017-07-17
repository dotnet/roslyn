// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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
            foreach (var name in node.DescendantNodesAndSelf(DescendIntoChildren).OfType<TSimpleName>())
            {
                if (!ShouldSpellCheck(name))
                {
                    continue;
                }

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

        protected abstract bool ShouldSpellCheck(TSimpleName name);
        protected abstract bool DescendIntoChildren(SyntaxNode arg);

        private async Task CreateSpellCheckCodeIssueAsync(CodeFixContext context, TSimpleName nameNode, string nameText, CancellationToken cancellationToken)
        {
            var document = context.Document;
            var service = CompletionService.GetService(document);

            // Disable snippets from ever appearing in the completion items. It's
            // very unlikely the user would ever mispell a snippet, then use spell-
            // checking to fix it, then try to invoke the snippet.
            var originalOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var options = originalOptions.WithChangedOption(CompletionOptions.SnippetsBehavior, document.Project.Language, SnippetsRule.NeverInclude);

            var completionList = await service.GetCompletionsAsync(
                document, nameNode.SpanStart, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (completionList == null)
            {
                return;
            }

            var similarityChecker = WordSimilarityChecker.Allocate(nameText, substringsAreSimilar: true);
            try
            {
                await CheckItemsAsync(
                    context, nameNode, nameText,
                    completionList, similarityChecker).ConfigureAwait(false);
            }
            finally
            {
                similarityChecker.Free();
            }
        }

        private async Task CheckItemsAsync(
            CodeFixContext context, TSimpleName nameNode, string nameText, 
            CompletionList completionList, WordSimilarityChecker similarityChecker)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var onlyConsiderGenerics = IsGeneric(nameNode);
            var results = new MultiDictionary<double, string>();

            foreach (var item in completionList.Items)
            {
                if (onlyConsiderGenerics && !IsGeneric(item))
                {
                    continue;
                }

                var candidateText = item.FilterText;
                if (!similarityChecker.AreSimilar(candidateText, out var matchCost))
                {
                    continue;
                }

                var insertionText = await GetInsertionTextAsync(document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
                results.Add(matchCost, insertionText);
            }

            var codeActions = results.OrderBy(kvp => kvp.Key)
                                     .SelectMany(kvp => kvp.Value.Order())
                                     .Where(t => t != nameText)
                                     .Take(3)
                                     .Select(n => CreateCodeAction(nameNode, nameText, n, document))
                                     .ToImmutableArrayOrEmpty<CodeAction>();

            if (codeActions.Length > 1)
            {
                // Wrap the spell checking actions into a single top level suggestion
                // so as to not clutter the list.
                context.RegisterCodeFix(new MyCodeAction(
                    string.Format(FeaturesResources.Spell_check_0, nameText), codeActions), context.Diagnostics);
            }
            else
            {
                context.RegisterFixes(codeActions, context.Diagnostics);
            }
        }

        private async Task<string> GetInsertionTextAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            var service = CompletionService.GetService(document);
            var change = await service.GetChangeAsync(document, item, null, cancellationToken).ConfigureAwait(false);

            return change.TextChange.NewText;
        }

        private SpellCheckCodeAction CreateCodeAction(TSimpleName nameNode, string oldName, string newName, Document document)
        {
            return new SpellCheckCodeAction(
                string.Format(FeaturesResources.Change_0_to_1, oldName, newName),
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

        private class MyCodeAction : CodeAction.CodeActionWithNestedActions
        {
            public MyCodeAction(string title, ImmutableArray<CodeAction> nestedActions)
                : base(title, nestedActions, isInlinable: true)
            {
            }
        }
    }
}
