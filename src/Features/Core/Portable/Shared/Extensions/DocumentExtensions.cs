// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class DocumentExtensions
    {
        public static bool ShouldHideAdvancedMembers(this Document document)
        {
            // Since we don't actually have a way to configure this per-document, we can fetch from the core workspace
            return document.Project.Solution.Workspace.Options.GetOption(CompletionOptions.HideAdvancedMembers, document.Project.Language);
        }

        public static async Task<Document> ReplaceNodeAsync<TNode>(this Document document, TNode oldNode, TNode newNode, CancellationToken cancellationToken) where TNode : SyntaxNode
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(oldNode, newNode);
            return document.WithSyntaxRoot(newRoot);
        }

        public static async Task<Document> ReplaceNodesAsync(this Document document,
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNodes(nodes, computeReplacementNode);
            return document.WithSyntaxRoot(newRoot);
        }

        public static async Task<IEnumerable<T>> GetUnionItemsFromDocumentAndLinkedDocumentsAsync<T>(
            this Document document,
            IEqualityComparer<T> comparer,
            Func<Document, CancellationToken, Task<IEnumerable<T>>> getItemsWorker,
            CancellationToken cancellationToken)
        {
            var linkedDocumentIds = document.GetLinkedDocumentIds();
            var itemsForCurrentContext = await getItemsWorker(document, cancellationToken).ConfigureAwait(false) ?? SpecializedCollections.EmptyEnumerable<T>();
            if (!linkedDocumentIds.Any())
            {
                return itemsForCurrentContext;
            }

            var totalItems = itemsForCurrentContext.ToSet(comparer);
            foreach (var linkedDocumentId in linkedDocumentIds)
            {
                var linkedDocument = document.Project.Solution.GetDocument(linkedDocumentId);
                var items = await getItemsWorker(linkedDocument, cancellationToken).ConfigureAwait(false);
                if (items != null)
                {
                    totalItems.AddRange(items);
                }
            }

            return totalItems;
        }

        public static async Task<bool> IsValidContextForDocumentOrLinkedDocumentsAsync(
            this Document document,
            Func<Document, CancellationToken, Task<bool>> contextChecker,
            CancellationToken cancellationToken)
        {
            if (await contextChecker(document, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            var solution = document.Project.Solution;
            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                var linkedDocument = solution.GetDocument(linkedDocumentId);
                if (await contextChecker(linkedDocument, cancellationToken).ConfigureAwait(false))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the set of naming rules the user has set for this document.  Will include a set of default naming rules
        /// that match if the user hasn't specified any for a particular symbol type.  The are added at the end so they
        /// will only be used if the user hasn't specified a preference.
        /// </summary>
        public static Task<ImmutableArray<NamingRule>> GetNamingRulesAsync(
            this Document document, CancellationToken cancellationToken)
            => document.GetNamingRulesAsync(FallbackNamingRules.Default, cancellationToken);

        /// <summary>
        /// Get the user-specified naming rules, with the added <paramref name="defaultRules"/>.
        /// </summary>
        public static async Task<ImmutableArray<NamingRule>> GetNamingRulesAsync(this Document document,
            ImmutableArray<NamingRule> defaultRules, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var namingStyleOptions = options.GetOption(NamingStyleOptions.NamingPreferences);
            var rules = namingStyleOptions.CreateRules().NamingRules;

            return defaultRules.IsDefaultOrEmpty ? rules : rules.AddRange(defaultRules);
        }

        public static async Task<NamingRule> GetApplicableNamingRuleAsync(this Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(symbol))
                    return rule;
            }

            throw ExceptionUtilities.Unreachable;
        }

        public static async Task<NamingRule> GetApplicableNamingRuleAsync(
            this Document document, SymbolKind symbolKind, Accessibility accessibility, CancellationToken cancellationToken)
        {
            var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(symbolKind, accessibility))
                    return rule;
            }

            throw ExceptionUtilities.Unreachable;
        }

        public static async Task<NamingRule> GetApplicableNamingRuleAsync(
            this Document document, SymbolKindOrTypeKind kind, DeclarationModifiers modifiers, Accessibility? accessibility, CancellationToken cancellationToken)
        {
            var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(kind, modifiers, accessibility))
                    return rule;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
