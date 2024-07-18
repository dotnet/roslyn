// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Naming;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class DocumentExtensions
{
    public static async Task<Document> ReplaceNodeAsync<TNode>(this Document document, TNode oldNode, TNode newNode, CancellationToken cancellationToken)
        where TNode : SyntaxNode
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return document.ReplaceNode(root, oldNode, newNode);
    }

    public static Document ReplaceNodeSynchronously<TNode>(this Document document, TNode oldNode, TNode newNode, CancellationToken cancellationToken)
        where TNode : SyntaxNode
    {
        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        return document.ReplaceNode(root, oldNode, newNode);
    }

    public static Document ReplaceNode<TNode>(this Document document, SyntaxNode root, TNode oldNode, TNode newNode)
        where TNode : SyntaxNode
    {
        Debug.Assert(document.GetRequiredSyntaxRootSynchronously(CancellationToken.None) == root);
        var newRoot = root.ReplaceNode(oldNode, newNode);
        return document.WithSyntaxRoot(newRoot);
    }

    public static async Task<Document> ReplaceNodesAsync(this Document document,
        IEnumerable<SyntaxNode> nodes,
        Func<SyntaxNode, SyntaxNode, SyntaxNode> computeReplacementNode,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNodes(nodes, computeReplacementNode);
        return document.WithSyntaxRoot(newRoot);
    }

    public static async Task<ImmutableArray<T>> GetUnionItemsFromDocumentAndLinkedDocumentsAsync<T>(
        this Document document,
        IEqualityComparer<T> comparer,
        Func<Document, Task<ImmutableArray<T>>> getItemsWorker)
    {
        var totalItems = new HashSet<T>(comparer);

        var values = await getItemsWorker(document).ConfigureAwait(false);
        totalItems.AddRange(values.NullToEmpty());

        foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
        {
            values = await getItemsWorker(document.Project.Solution.GetRequiredDocument(linkedDocumentId)).ConfigureAwait(false);
            totalItems.AddRange(values.NullToEmpty());
        }

        return [.. totalItems];
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
            var linkedDocument = solution.GetRequiredDocument(linkedDocumentId);
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
    public static async Task<ImmutableArray<NamingRule>> GetNamingRulesAsync(
        this Document document, CancellationToken cancellationToken)
    {
        var options = await document.GetNamingStylePreferencesAsync(cancellationToken).ConfigureAwait(false);
        return options.CreateRules().NamingRules.AddRange(FallbackNamingRules.Default);
    }

    public static async Task<NamingRule> GetApplicableNamingRuleAsync(this Document document, ISymbol symbol, CancellationToken cancellationToken)
    {
        var rules = await document.GetNamingRulesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var rule in rules)
        {
            if (rule.SymbolSpecification.AppliesTo(symbol))
                return rule;
        }

        throw ExceptionUtilities.Unreachable();
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

        throw ExceptionUtilities.Unreachable();
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

        throw ExceptionUtilities.Unreachable();
    }
}
