// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

internal abstract class AbstractObjectCreationExpressionAnalyzer<
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TMatch,
    TAnalyzer> : IDisposable
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractObjectCreationExpressionAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TMatch,
        TAnalyzer>, new()
{
    protected UpdateExpressionState<TExpressionSyntax, TStatementSyntax> State;

    protected TObjectCreationExpressionSyntax _objectCreationExpression = null!;
    protected bool _analyzeForCollectionExpression;

    protected ISyntaxFacts SyntaxFacts => this.State.SyntaxFacts;
    protected SemanticModel SemanticModel => this.State.SemanticModel;

    protected abstract bool ShouldAnalyze(CancellationToken cancellationToken);
    protected abstract bool TryAddMatches(ArrayBuilder<TMatch> matches, CancellationToken cancellationToken);
    protected abstract bool IsInitializerOfLocalDeclarationStatement(
        TLocalDeclarationStatementSyntax localDeclarationStatement, TObjectCreationExpressionSyntax rootExpression, [NotNullWhen(true)] out TVariableDeclaratorSyntax? variableDeclarator);

    private static readonly ObjectPool<TAnalyzer> s_pool = SharedPools.Default<TAnalyzer>();

    public static TAnalyzer Allocate()
        => s_pool.Allocate();

    public void Dispose()
    {
        this.Clear();
        s_pool.Free((TAnalyzer)this);
    }

    public void Initialize(
        UpdateExpressionState<TExpressionSyntax, TStatementSyntax> state,
        TObjectCreationExpressionSyntax objectCreationExpression,
        bool analyzeForCollectionExpression)
    {
        State = state;
        _objectCreationExpression = objectCreationExpression;
        _analyzeForCollectionExpression = analyzeForCollectionExpression;
    }

    protected void Clear()
    {
        State = default;
        _objectCreationExpression = null!;
        _analyzeForCollectionExpression = false;
    }

    protected ImmutableArray<TMatch> AnalyzeWorker(CancellationToken cancellationToken)
    {
        if (!ShouldAnalyze(cancellationToken))
            return default;

        using var _ = ArrayBuilder<TMatch>.GetInstance(out var matches);
        if (!TryAddMatches(matches, cancellationToken))
            return default;

        return matches.ToImmutableAndClear();
    }

    protected UpdateExpressionState<TExpressionSyntax, TStatementSyntax>? TryInitializeState(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TObjectCreationExpressionSyntax rootExpression,
        bool analyzeForCollectionExpression,
        CancellationToken cancellationToken)
    {
        var statement = rootExpression.FirstAncestorOrSelf<TStatementSyntax>()!;
        if (statement != null)
        {
            var result =
                TryInitializeVariableDeclarationCase(semanticModel, syntaxFacts, rootExpression, statement, cancellationToken) ??
                TryInitializeAssignmentCase(semanticModel, syntaxFacts, rootExpression, statement, cancellationToken);
            if (result != null)
                return result;
        }

        // Even if the above cases didn't work, we always support converting a `new List<int>()` collection over to
        // a collection expression.  We just won't analyze later statements.
        if (analyzeForCollectionExpression)
        {
            return new UpdateExpressionState<TExpressionSyntax, TStatementSyntax>(
                semanticModel, syntaxFacts, rootExpression, valuePattern: default, initializedSymbol: null);
        }

        return null;
    }

    private UpdateExpressionState<TExpressionSyntax, TStatementSyntax>? TryInitializeVariableDeclarationCase(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TObjectCreationExpressionSyntax rootExpression,
        TStatementSyntax containingStatement,
        CancellationToken cancellationToken)
    {
        if (containingStatement is not TLocalDeclarationStatementSyntax localDeclarationStatement)
            return null;

        if (!this.IsInitializerOfLocalDeclarationStatement(localDeclarationStatement, rootExpression, out var variableDeclarator))
            return null;

        var valuePattern = syntaxFacts.GetIdentifierOfVariableDeclarator(variableDeclarator);
        if (valuePattern == default || valuePattern.IsMissing)
            return null;

        var initializedSymbol = semanticModel.GetDeclaredSymbol(valuePattern.GetRequiredParent(), cancellationToken);
        if (initializedSymbol is not ILocalSymbol local)
            return null;

        // Not supported if we're creating a dynamic local.  The object we're instantiating may not have the members
        // that we're trying to access on the dynamic object.
        if (local.Type is IDynamicTypeSymbol)
            return null;

        return new(semanticModel, syntaxFacts, rootExpression, valuePattern, initializedSymbol);
    }

    private static UpdateExpressionState<TExpressionSyntax, TStatementSyntax>? TryInitializeAssignmentCase(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TObjectCreationExpressionSyntax rootExpression,
        TStatementSyntax containingStatement,
        CancellationToken cancellationToken)
    {
        if (!syntaxFacts.IsSimpleAssignmentStatement(containingStatement))
            return null;

        syntaxFacts.GetPartsOfAssignmentStatement(containingStatement,
            out var left, out var right);
        if (right != rootExpression)
            return null;

        var typeInfo = semanticModel.GetTypeInfo(left, cancellationToken);
        if (typeInfo.Type is IDynamicTypeSymbol || typeInfo.ConvertedType is IDynamicTypeSymbol)
        {
            // Not supported if we're initializing something dynamic.  The object we're instantiating
            // may not have the members that we're trying to access on the dynamic object.
            return null;
        }

        var initializedSymbol = semanticModel.GetSymbolInfo(left, cancellationToken).GetAnySymbol();
        return new(semanticModel, syntaxFacts, rootExpression, left, initializedSymbol);
    }
}
