// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Roslyn.Utilities;

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
    protected abstract bool IsComplexElementInitializer(SyntaxNode expression);
    protected abstract bool HasExistingInvalidInitializerForCollection();
    protected abstract bool AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
        ArrayBuilder<CollectionMatch<SyntaxNode>> preMatches, ArrayBuilder<CollectionMatch<SyntaxNode>> postMatches, CancellationToken cancellationToken);

    protected abstract IUpdateExpressionSyntaxHelper<TExpressionSyntax, TStatementSyntax> SyntaxHelper { get; }

    public AnalysisResult Analyze(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TObjectCreationExpressionSyntax objectCreationExpression,
        bool analyzeForCollectionExpression,
        CancellationToken cancellationToken)
    {
        var state = TryInitializeState(semanticModel, syntaxFacts, objectCreationExpression, analyzeForCollectionExpression, cancellationToken);
        if (state is null)
            return default;

        this.Initialize(state.Value, objectCreationExpression, analyzeForCollectionExpression);
        var (preMatches, postMatches) = this.AnalyzeWorker(cancellationToken);

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
            return new(preMatches, postMatches);

        // Downgrade an empty result to a failure for the normal collection-initializer case.
        return postMatches.IsEmpty ? default : new(preMatches, postMatches);
    }

    protected sealed override bool TryAddMatches(
        ArrayBuilder<CollectionMatch<SyntaxNode>> preMatches, ArrayBuilder<CollectionMatch<SyntaxNode>> postMatches, CancellationToken cancellationToken)
    {
        var seenInvocation = false;
        var seenIndexAssignment = false;

        var initializer = this.SyntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
        if (initializer != null)
        {
            var initializerExpressions = this.SyntaxFacts.GetExpressionsOfObjectCollectionInitializer(initializer);
            if (initializerExpressions is [var firstInit, ..])
            {
                // if we have an object creation, and it *already* has an initializer in it (like `new T { { x, y } }`)
                // this can't legally become a collection expression.
                if (_analyzeForCollectionExpression && this.IsComplexElementInitializer(firstInit))
                    return false;

                seenIndexAssignment = this.SyntaxFacts.IsElementAccessInitializer(firstInit);
                seenInvocation = !seenIndexAssignment;

                // An indexer can't be used with a collection expression.  So fail out immediately if we see that.
                if (seenIndexAssignment && _analyzeForCollectionExpression)
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
            return AnalyzeMatchesAndCollectionConstructorForCollectionExpression(preMatches, postMatches, cancellationToken);

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
                    out var instance) &&
                this.State.ValuePatternMatches(instance))
            {
                seenInvocation = true;
                return new(expressionStatement, UseSpread: false);
            }
        }

        if (!seenInvocation)
        {
            if (TryAnalyzeIndexAssignment(expressionStatement, cancellationToken, out var instance) &&
                this.State.ValuePatternMatches(instance))
            {
                seenIndexAssignment = true;
                return new(expressionStatement, UseSpread: false);
            }
        }

        return null;
    }

    protected sealed override bool ShouldAnalyze(CancellationToken cancellationToken)
    {
        if (this.HasExistingInvalidInitializerForCollection())
            return false;

        var type = this.SemanticModel.GetTypeInfo(_objectCreationExpression, cancellationToken).Type;
        if (type == null)
            return false;

        var addMethods = this.SemanticModel.LookupSymbols(
            _objectCreationExpression.SpanStart,
            container: type,
            name: WellKnownMemberNames.CollectionInitializerAddMethodName,
            includeReducedExtensionMethods: true);

        return addMethods.Any(static m => m is IMethodSymbol methodSymbol && methodSymbol.Parameters.Any());
    }

    private bool TryAnalyzeIndexAssignment(
        TExpressionStatementSyntax statement,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out TExpressionSyntax? instance)
    {
        instance = null;
        if (!this.SyntaxFacts.SupportsIndexingInitializer(statement.SyntaxTree.Options))
            return false;

        if (!this.SyntaxFacts.IsSimpleAssignmentStatement(statement))
            return false;

        this.SyntaxFacts.GetPartsOfAssignmentStatement(statement, out var left, out var right);

        if (!this.SyntaxFacts.IsElementAccessExpression(left))
            return false;

        // If we're initializing a variable, then we can't reference that variable on the right 
        // side of the initialization.  Rewriting this into a collection initializer would lead
        // to a definite-assignment error.
        if (this.State.NodeContainsValuePatternOrReferencesInitializedSymbol(right, cancellationToken))
            return false;

        // Can't reference the variable being initialized in the arguments of the indexing expression.
        this.SyntaxFacts.GetPartsOfElementAccessExpression(left, out var elementInstance, out var argumentList);
        var elementAccessArguments = this.SyntaxFacts.GetArgumentsOfArgumentList(argumentList);
        foreach (var argument in elementAccessArguments)
        {
            if (this.State.NodeContainsValuePatternOrReferencesInitializedSymbol(argument, cancellationToken))
                return false;

            // An index/range expression implicitly references the value being initialized.  So it cannot be used in the
            // indexing expression.
            var argExpression = this.SyntaxFacts.GetExpressionOfArgument(argument);
            argExpression = this.SyntaxFacts.WalkDownParentheses(argExpression);

            if (this.SyntaxFacts.IsIndexExpression(argExpression) || this.SyntaxFacts.IsRangeExpression(argExpression))
                return false;
        }

        instance = elementInstance as TExpressionSyntax;
        return instance != null;
    }
}
