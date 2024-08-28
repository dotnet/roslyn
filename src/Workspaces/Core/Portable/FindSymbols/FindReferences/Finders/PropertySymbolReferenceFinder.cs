// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders;

internal sealed class PropertySymbolReferenceFinder : AbstractMethodOrPropertyOrEventSymbolReferenceFinder<IPropertySymbol>
{
    public static readonly PropertySymbolReferenceFinder Instance = new();

    private PropertySymbolReferenceFinder()
    {
    }

    protected override bool CanFind(IPropertySymbol symbol)
        => true;

    protected override async ValueTask<ImmutableArray<ISymbol>> DetermineCascadedSymbolsAsync(
        IPropertySymbol symbol,
        Solution solution,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);

        await DiscoverImpliedSymbolsAsync(symbol, solution, result, cancellationToken).ConfigureAwait(false);
        CascadeToOtherPartOfPartial(symbol, result);
        CascadeToBackingFields(symbol, result);
        CascadeToAccessors(symbol, result);
        CascadeToPrimaryConstructorParameters(symbol, result, cancellationToken);

        return result.ToImmutable();
    }

    private static void CascadeToOtherPartOfPartial(IPropertySymbol symbol, ArrayBuilder<ISymbol> result)
    {
        result.AddIfNotNull(symbol.PartialDefinitionPart);
        result.AddIfNotNull(symbol.PartialImplementationPart);
    }

    private static void CascadeToBackingFields(IPropertySymbol symbol, ArrayBuilder<ISymbol> result)
    {
        foreach (var member in symbol.ContainingType.GetMembers())
        {
            if (member is IFieldSymbol field &&
                symbol.Equals(field.AssociatedSymbol))
            {
                result.Add(field);
            }
        }
    }

    private static void CascadeToAccessors(IPropertySymbol symbol, ArrayBuilder<ISymbol> result)
    {
        result.AddIfNotNull(symbol.GetMethod);
        result.AddIfNotNull(symbol.SetMethod);
    }

    private static void CascadeToPrimaryConstructorParameters(IPropertySymbol property, ArrayBuilder<ISymbol> result, CancellationToken cancellationToken)
    {
        if (property is
            {
                IsStatic: false,
                DeclaringSyntaxReferences.Length: > 0,
                ContainingType:
                {
                    IsRecord: true,
                    DeclaringSyntaxReferences.Length: > 0,
                } containingType,
            })
        {
            // OK, we have a property in a record.  See if we can find a primary constructor that has a parameter that synthesized this
            var containingTypeSyntaxes = containingType.DeclaringSyntaxReferences.SelectAsArray(r => r.GetSyntax(cancellationToken));
            foreach (var constructor in containingType.Constructors)
            {
                if (constructor.DeclaringSyntaxReferences.Length > 0)
                {
                    var constructorSyntax = constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                    if (containingTypeSyntaxes.Contains(constructorSyntax))
                    {
                        // OK found the primary construct.  Try to find a parameter that corresponds to this property.
                        foreach (var parameter in constructor.Parameters)
                        {
                            if (property.Name.Equals(parameter.Name) &&
                                property.Equals(parameter.GetAssociatedSynthesizedRecordProperty(cancellationToken)))
                            {
                                result.Add(parameter);
                            }
                        }
                    }
                }
            }
        }
    }

    protected sealed override async Task DetermineDocumentsToSearchAsync<TData>(
        IPropertySymbol symbol,
        HashSet<string>? globalAliases,
        Project project,
        IImmutableSet<Document>? documents,
        Action<Document, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        await FindDocumentsAsync(project, documents, processResult, processResultData, cancellationToken, symbol.Name).ConfigureAwait(false);

        if (IsForEachProperty(symbol))
            await FindDocumentsWithForEachStatementsAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (symbol.IsIndexer)
            await FindDocumentWithExplicitOrImplicitElementAccessExpressionsAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        if (symbol.IsIndexer)
            await FindDocumentWithIndexerMemberCrefAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);

        await FindDocumentsWithGlobalSuppressMessageAttributeAsync(project, documents, processResult, processResultData, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsForEachProperty(IPropertySymbol symbol)
        => symbol.Name == WellKnownMemberNames.CurrentPropertyName;

    protected sealed override void FindReferencesInDocument<TData>(
        IPropertySymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        FindReferencesInDocumentUsingSymbolName(
            symbol,
            state,
            static (loc, data) =>
            {
                var useResult = true;
                if (data.options.AssociatePropertyReferencesWithSpecificAccessor)
                {
                    // We want to associate property references to a specific accessor (if an accessor
                    // is being referenced).  Check if this reference would match an accessor. If so, do
                    // not add it.  It will be added by PropertyAccessorSymbolReferenceFinder.
                    var accessors = GetReferencedAccessorSymbols(
                        data.state, data.symbol, loc.Node, data.cancellationToken);
                    useResult = accessors.IsEmpty;
                }

                if (useResult)
                    data.processResult(loc, data.processResultData);
            },
            processResultData: (self: this, symbol, state, processResult, processResultData, options, cancellationToken),
            cancellationToken);

        if (IsForEachProperty(symbol))
            FindReferencesInForEachStatements(symbol, state, processResult, processResultData, cancellationToken);

        if (symbol.IsIndexer)
            FindIndexerReferences(symbol, state, processResult, processResultData, options, cancellationToken);

        FindReferencesInDocumentInsideGlobalSuppressions(
            symbol, state, processResult, processResultData, cancellationToken);
    }

    private static Task FindDocumentWithExplicitOrImplicitElementAccessExpressionsAsync<TData>(
        Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
    {
        return FindDocumentsWithPredicateAsync(
            project, documents, static index => index.ContainsExplicitOrImplicitElementAccessExpression, processResult, processResultData, cancellationToken);
    }

    private static Task FindDocumentWithIndexerMemberCrefAsync<TData>(
        Project project, IImmutableSet<Document>? documents, Action<Document, TData> processResult, TData processResultData, CancellationToken cancellationToken)
    {
        return FindDocumentsWithPredicateAsync(
            project, documents, static index => index.ContainsIndexerMemberCref, processResult, processResultData, cancellationToken);
    }

    private static void FindIndexerReferences<TData>(
        IPropertySymbol symbol,
        FindReferencesDocumentState state,
        Action<FinderLocation, TData> processResult,
        TData processResultData,
        FindReferencesSearchOptions options,
        CancellationToken cancellationToken)
    {
        if (options.AssociatePropertyReferencesWithSpecificAccessor)
        {
            // Looking for individual get/set references.  Don't find anything here. 
            // these results will be provided by the PropertyAccessorSymbolReferenceFinder
            return;
        }

        var syntaxFacts = state.SyntaxFacts;

        var indexerReferenceExpressions = state.Root.DescendantNodes(descendIntoTrivia: true)
            .Where(node =>
                syntaxFacts.IsElementAccessExpression(node) ||
                syntaxFacts.IsImplicitElementAccess(node) ||
                syntaxFacts.IsConditionalAccessExpression(node) ||
                syntaxFacts.IsIndexerMemberCref(node));

        foreach (var node in indexerReferenceExpressions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (matched, candidateReason, indexerReference) = ComputeIndexerInformation(symbol, state, node, cancellationToken);
            if (!matched)
                continue;

            var location = state.SyntaxTree.GetLocation(new TextSpan(indexerReference.SpanStart, 0));
            var symbolUsageInfo = GetSymbolUsageInfo(node, state, cancellationToken);

            var result = new FinderLocation(node,
                new ReferenceLocation(
                    state.Document, alias: null, location, isImplicit: false, symbolUsageInfo,
                    GetAdditionalFindUsagesProperties(node, state),
                    candidateReason));
            processResult(result, processResultData);
        }
    }

    private static (bool matched, CandidateReason reason, SyntaxNode indexerReference) ComputeIndexerInformation(
        IPropertySymbol symbol,
        FindReferencesDocumentState state,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = state.SyntaxFacts;

        if (syntaxFacts.IsElementAccessExpression(node))
        {
            // The indexerReference for an element access expression will not be null
            return ComputeElementAccessInformation(symbol, node, state, cancellationToken)!;
        }
        else if (syntaxFacts.IsImplicitElementAccess(node))
        {
            return ComputeImplicitElementAccessInformation(symbol, node, state, cancellationToken)!;
        }
        else if (syntaxFacts.IsConditionalAccessExpression(node))
        {
            return ComputeConditionalAccessInformation(symbol, node, state, cancellationToken);
        }
        else
        {
            Debug.Assert(syntaxFacts.IsIndexerMemberCref(node));
            return ComputeIndexerMemberCRefInformation(symbol, state, node, cancellationToken);
        }
    }

    private static (bool matched, CandidateReason reason, SyntaxNode indexerReference) ComputeIndexerMemberCRefInformation(
        IPropertySymbol symbol, FindReferencesDocumentState state, SyntaxNode node, CancellationToken cancellationToken)
    {
        var (matched, reason) = SymbolsMatch(symbol, state, node, cancellationToken);

        // For an IndexerMemberCRef the node itself is the indexer we are looking for.
        return (matched, reason, node);
    }

    private static (bool matched, CandidateReason reason, SyntaxNode indexerReference) ComputeConditionalAccessInformation(
        IPropertySymbol symbol, SyntaxNode node, FindReferencesDocumentState state, CancellationToken cancellationToken)
    {
        // For a ConditionalAccessExpression the whenNotNull component is the indexer reference we are looking for
        var syntaxFacts = state.SyntaxFacts;
        syntaxFacts.GetPartsOfConditionalAccessExpression(node, out _, out var indexerReference);

        if (syntaxFacts.IsInvocationExpression(indexerReference))
        {
            // call to something like: goo?.bar(1)
            //
            // this will already be handled by the existing method ref finder.
            return default;
        }

        var (matched, reason) = SymbolsMatch(symbol, state, indexerReference, cancellationToken);
        return (matched, reason, indexerReference);
    }

    private static (bool matched, CandidateReason reason, SyntaxNode? indexerReference) ComputeElementAccessInformation(
        IPropertySymbol symbol, SyntaxNode node, FindReferencesDocumentState state, CancellationToken cancellationToken)
    {
        // For an ElementAccessExpression the indexer we are looking for is the argumentList component.
        state.SyntaxFacts.GetPartsOfElementAccessExpression(node, out var expression, out var indexerReference);
        if (expression != null && SymbolsMatch(symbol, state, expression, cancellationToken).matched)
        {
            // Element access with explicit member name (allowed in VB). We will have
            // already added a reference location for the member name identifier, so skip
            // this one.
            return default;
        }

        var (matched, reason) = SymbolsMatch(symbol, state, node, cancellationToken);
        return (matched, reason, indexerReference);
    }

    private static (bool matched, CandidateReason reason, SyntaxNode indexerReference) ComputeImplicitElementAccessInformation(
        IPropertySymbol symbol, SyntaxNode node, FindReferencesDocumentState state, CancellationToken cancellationToken)
    {
        var argumentList = state.SyntaxFacts.GetArgumentListOfImplicitElementAccess(node);
        var (matched, reason) = SymbolsMatch(symbol, state, node, cancellationToken);
        return (matched, reason, argumentList);
    }
}
