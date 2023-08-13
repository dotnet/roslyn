// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    using static UpdateObjectCreationHelpers;

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
        protected SemanticModel _semanticModel;
        protected ISyntaxFacts _syntaxFacts;
        protected TObjectCreationExpressionSyntax _objectCreationExpression;
        protected bool _analyzeForCollectionExpression;
        protected CancellationToken _cancellationToken;

        protected TStatementSyntax _containingStatement;
        protected SyntaxNodeOrToken _valuePattern;
        protected ISymbol _initializedSymbol;

        protected AbstractObjectCreationExpressionAnalyzer()
        {
        }

        protected abstract bool ShouldAnalyze();
        protected abstract bool TryAddMatches(ArrayBuilder<TMatch> matches);

        public void Initialize(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TObjectCreationExpressionSyntax objectCreationExpression,
            bool analyzeForCollectionExpression,
            CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _syntaxFacts = syntaxFacts;
            _objectCreationExpression = objectCreationExpression;
            _analyzeForCollectionExpression = analyzeForCollectionExpression;
            _cancellationToken = cancellationToken;
        }

        protected void Clear()
        {
            _semanticModel = null;
            _syntaxFacts = null;
            _objectCreationExpression = null;
            _analyzeForCollectionExpression = false;
            _cancellationToken = default;
            _containingStatement = null;
            _valuePattern = default;
            _initializedSymbol = null;
        }

        protected ImmutableArray<TMatch> AnalyzeWorker()
        {
            if (!ShouldAnalyze())
                return default;

            _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (_containingStatement == null)
                return default;

            var result =
                TryInitializeVariableDeclarationCase(_semanticModel, _syntaxFacts, _objectCreationExpression, _containingStatement, _cancellationToken) ??
                TryInitializeAssignmentCase(_semanticModel, _syntaxFacts, _objectCreationExpression, _containingStatement, _cancellationToken);

            if (result is null)
                return default;

            (_valuePattern, _initializedSymbol) = result.Value;

            using var _ = ArrayBuilder<TMatch>.GetInstance(out var matches);
            if (!TryAddMatches(matches))
                return default;

            return matches.ToImmutable();
        }

        protected bool ExpressionContainsValuePatternOrReferencesInitializedSymbol(SyntaxNode expression)
        {
            foreach (var subExpression in expression.DescendantNodesAndSelf().OfType<TExpressionSyntax>())
            {
                if (!_syntaxFacts.IsNameOfSimpleMemberAccessExpression(subExpression) &&
                    !_syntaxFacts.IsNameOfMemberBindingExpression(subExpression))
                {
                    if (ValuePatternMatches(_syntaxFacts, _valuePattern, subExpression))
                    {
                        return true;
                    }
                }

                if (_initializedSymbol != null &&
                    _initializedSymbol.Equals(
                        _semanticModel.GetSymbolInfo(subExpression, _cancellationToken).GetAnySymbol()))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
