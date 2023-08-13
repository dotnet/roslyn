// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal abstract class AbstractObjectCreationExpressionAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TVariableDeclaratorSyntax,
        TMatch>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        protected UpdateObjectCreationState<TExpressionSyntax, TStatementSyntax> State;

        protected TObjectCreationExpressionSyntax _objectCreationExpression;
        protected bool _analyzeForCollectionExpression;

        protected ISyntaxFacts SyntaxFacts => this.State.SyntaxFacts;
        protected SemanticModel SemanticModel => this.State.SemanticModel;

        protected abstract bool ShouldAnalyze();
        protected abstract bool TryAddMatches(ArrayBuilder<TMatch> matches, CancellationToken cancellationToken);

        public void Initialize(
            UpdateObjectCreationState<TExpressionSyntax, TStatementSyntax> state,
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
            _objectCreationExpression = null;
            _analyzeForCollectionExpression = false;
        }

        protected ImmutableArray<TMatch> AnalyzeWorker(CancellationToken cancellationToken)
        {
            if (!ShouldAnalyze())
                return default;

            using var _ = ArrayBuilder<TMatch>.GetInstance(out var matches);
            if (!TryAddMatches(matches, cancellationToken))
                return default;

            return matches.ToImmutable();
        }
    }
}
