// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.UseCollectionExpression;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

internal abstract class AbstractUseCollectionInitializerAnalyzer<
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TInvocationExpressionSyntax,
    TExpressionStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer> : AbstractObjectCreationExpressionAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        CollectionMatch<SyntaxNode>, TAnalyzer>
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TExpressionStatementSyntax : TStatementSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractUseCollectionInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer>, new()
{
    protected bool _analyzeForCollectionExpression;

    protected abstract bool IsComplexElementInitializer(SyntaxNode expression, out int initializerElementCount);

    protected abstract bool HasExistingInvalidInitializerForCollection();
    protected abstract bool AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
        ArrayBuilder<CollectionMatch<SyntaxNode>> preMatches,
        ArrayBuilder<CollectionMatch<SyntaxNode>> postMatches,
        out bool mayChangeSemantics,
        CancellationToken cancellationToken);

    protected abstract IUpdateExpressionSyntaxHelper<TExpressionSyntax, TStatementSyntax> SyntaxHelper { get; }

    protected override void Clear()
    {
        base.Clear();
        _analyzeForCollectionExpression = false;
    }

    public AnalysisResult Analyze(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TObjectCreationExpressionSyntax objectCreationExpression,
        bool analyzeForCollectionExpression,
        CancellationToken cancellationToken)
    {
        _analyzeForCollectionExpression = analyzeForCollectionExpression;
        var state = TryInitializeState(semanticModel, syntaxFacts, objectCreationExpression, cancellationToken);

        // If we didn't find something we're assigned to, then we normally can't continue.  However, we always support
        // converting a `new List<int>()` collection over to a collection expression.  We just won't analyze later
        // statements.  
        if (state.ValuePattern == default && !analyzeForCollectionExpression)
            return default;

        this.Initialize(state, objectCreationExpression);
        var (preMatches, postMatches, mayChangeSemantics) = this.AnalyzeWorker(cancellationToken);

        // If analysis failed entirely, immediately bail out.
        if (preMatches.IsDefault || postMatches.IsDefault)
            return default;

        // Analysis succeeded, but the result may be empty or non empty.
        //
        // For collection expressions, it's fine for this result to be empty.  In other words, it's ok to offer
        // changing `new List<int>() { 1 }` (on its own) to `[1]`.
        //
        // However, for collection initializers we always want at least one element to add to the initializer.  In
        // other words, we don't want to suggest changing `new List<int>()` to `new List<int>() { }` as that's just
        // noise.  So convert empty results to an invalid result here.
        if (analyzeForCollectionExpression)
            return new(preMatches, postMatches, mayChangeSemantics);

        // Downgrade an empty result to a failure for the normal collection-initializer case.
        return postMatches.IsEmpty ? default : new(preMatches, postMatches, mayChangeSemantics);
    }

    protected sealed override bool TryAddMatches(
        ArrayBuilder<CollectionMatch<SyntaxNode>> preMatches,
        ArrayBuilder<CollectionMatch<SyntaxNode>> postMatches,
        out bool mayChangeSemantics,
        CancellationToken cancellationToken)
    {
        mayChangeSemantics = false;
        var seenInvocation = false;
        var seenIndexAssignment = false;

        var initializer = this.SyntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
        if (initializer != null)
        {
            var initializerExpressions = this.SyntaxFacts.GetExpressionsOfObjectCollectionInitializer(initializer);
            if (initializerExpressions is [var firstInit, ..])
            {
                // if we have an object creation, and it *already* has an initializer in it (like `new T { { x, y } }`)
                // this can't legally become a collection expression.  Unless there are exactly two elements in the
                // initializer, and we support k:v elements.
                if (_analyzeForCollectionExpression && this.IsComplexElementInitializer(firstInit, out var initializerElementCount))
                {
                    if (initializerElementCount != 2 || !this.SyntaxFacts.SupportsKeyValuePairElement(_objectCreationExpression.SyntaxTree.Options))
                        return false;
                }

                seenIndexAssignment = this.SyntaxFacts.IsElementAccessInitializer(firstInit);
                seenInvocation = !seenIndexAssignment;

                // An indexer can't be used with a collection expression (except for dictionary expressions).  So fail
                // out immediately if we see that.
                if (_analyzeForCollectionExpression && seenIndexAssignment && !this.SyntaxFacts.SupportsKeyValuePairElement(_objectCreationExpression.SyntaxTree.Options))
                    return false;
            }
        }

        if (State.ValuePattern != default)
        {
            foreach (var statement in this.State.GetSubsequentStatements())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var match = TryAnalyzeStatement(statement, ref seenInvocation, ref seenIndexAssignment, cancellationToken);
                if (match is null)
                    break;

                postMatches.Add(match.Value);
            }
        }

        if (_analyzeForCollectionExpression)
        {
            return AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
                preMatches, postMatches, out mayChangeSemantics, cancellationToken);
        }

        return true;
    }

    private CollectionMatch<SyntaxNode>? TryAnalyzeStatement(
        TStatementSyntax statement, ref bool seenInvocation, ref bool seenIndexAssignment, CancellationToken cancellationToken)
    {
        return _analyzeForCollectionExpression
            ? State.TryAnalyzeStatementForCollectionExpression(this.SyntaxHelper, statement, cancellationToken)
            : TryAnalyzeStatementForCollectionInitializer(statement, ref seenInvocation, ref seenIndexAssignment, cancellationToken);
    }

    private CollectionMatch<SyntaxNode>? TryAnalyzeStatementForCollectionInitializer(
        TStatementSyntax statement, ref bool seenInvocation, ref bool seenIndexAssignment, CancellationToken cancellationToken)
    {
        // At least one of these has to be false.
        Contract.ThrowIfTrue(seenInvocation && seenIndexAssignment);

        if (statement is not TExpressionStatementSyntax expressionStatement)
            return null;

        // Can't mix Adds and indexing.
        if (!seenIndexAssignment)
        {
            // Look for a call to Add or AddRange
            if (this.State.TryAnalyzeAddInvocation(
                    (TExpressionSyntax)this.SyntaxFacts.GetExpressionOfExpressionStatement(expressionStatement),
                    requiredArgumentName: null,
                    forCollectionExpression: false,
                    cancellationToken,
                    out var instance,
                    out var useKeyValue) &&
                this.State.ValuePatternMatches(instance))
            {
                seenInvocation = true;
                return new(expressionStatement, UseSpread: false, useKeyValue);
            }
        }

        if (!seenInvocation)
        {
            if (this.State.TryAnalyzeIndexAssignment(expressionStatement, cancellationToken, out var instance) &&
                this.State.ValuePatternMatches(instance))
            {
                seenIndexAssignment = true;
                return new(expressionStatement, UseSpread: false, UseKeyValue: this.State.SyntaxFacts.SupportsKeyValuePairElement(statement.SyntaxTree.Options));
            }
        }

        return null;
    }

    protected sealed override bool ShouldAnalyze(CancellationToken cancellationToken)
    {
        if (this.HasExistingInvalidInitializerForCollection())
            return false;

        return GetAddMethods(cancellationToken).Any();
    }

    protected ImmutableArray<IMethodSymbol> GetAddMethods(CancellationToken cancellationToken)
    {
        var type = this.SemanticModel.GetTypeInfo(_objectCreationExpression, cancellationToken).Type;
        if (type == null)
            return [];

        var addMethods = this.SemanticModel.LookupSymbols(
            _objectCreationExpression.SpanStart,
            container: type,
            name: WellKnownMemberNames.CollectionInitializerAddMethodName,
            includeReducedExtensionMethods: true);
        return addMethods.SelectAsArray(s => s is IMethodSymbol { Parameters: [_, ..] }, s => (IMethodSymbol)s);
    }
}
