// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
        protected SemanticModel _semanticModel;
        protected ISyntaxFacts _syntaxFacts;
        protected TObjectCreationExpressionSyntax _objectCreationExpression;
        protected CancellationToken _cancellationToken;

        protected TStatementSyntax _containingStatement;
        private SyntaxNodeOrToken _valuePattern;
        private ISymbol _initializedSymbol;

        protected AbstractObjectCreationExpressionAnalyzer()
        {
        }

        public void Initialize(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TObjectCreationExpressionSyntax objectCreationExpression,
            CancellationToken cancellationToken)
        {
            _semanticModel = semanticModel;
            _syntaxFacts = syntaxFacts;
            _objectCreationExpression = objectCreationExpression;
            _cancellationToken = cancellationToken;
        }

        protected void Clear()
        {
            _semanticModel = null;
            _syntaxFacts = null;
            _objectCreationExpression = null;
            _cancellationToken = default;
            _containingStatement = null;
            _valuePattern = default;
            _initializedSymbol = null;
        }

        protected abstract bool ShouldAnalyze();
        protected abstract void AddMatches(ArrayBuilder<TMatch> matches);

        protected ImmutableArray<TMatch>? AnalyzeWorker()
        {
            if (!ShouldAnalyze())
                return null;

            _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (_containingStatement == null)
                return null;

            if (!TryInitializeVariableDeclarationCase() &&
                !TryInitializeAssignmentCase())
            {
                return null;
            }

            using var _ = ArrayBuilder<TMatch>.GetInstance(out var matches);
            AddMatches(matches);
            return matches.ToImmutable();
        }

        private bool TryInitializeVariableDeclarationCase()
        {
            if (!_syntaxFacts.IsLocalDeclarationStatement(_containingStatement))
            {
                return false;
            }

            if (_objectCreationExpression.Parent.Parent is not TVariableDeclaratorSyntax containingDeclarator)
            {
                return false;
            }

            _initializedSymbol = _semanticModel.GetDeclaredSymbol(containingDeclarator, _cancellationToken);
            if (_initializedSymbol is ILocalSymbol local &&
                local.Type is IDynamicTypeSymbol)
            {
                // Not supported if we're creating a dynamic local.  The object we're instantiating
                // may not have the members that we're trying to access on the dynamic object.
                return false;
            }

            if (!_syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(containingDeclarator, _containingStatement))
            {
                return false;
            }

            _valuePattern = _syntaxFacts.GetIdentifierOfVariableDeclarator(containingDeclarator);
            return true;
        }

        private bool TryInitializeAssignmentCase()
        {
            if (!_syntaxFacts.IsSimpleAssignmentStatement(_containingStatement))
            {
                return false;
            }

            _syntaxFacts.GetPartsOfAssignmentStatement(_containingStatement,
                out var left, out var right);
            if (right != _objectCreationExpression)
            {
                return false;
            }

            var typeInfo = _semanticModel.GetTypeInfo(left, _cancellationToken);
            if (typeInfo.Type is IDynamicTypeSymbol || typeInfo.ConvertedType is IDynamicTypeSymbol)
            {
                // Not supported if we're initializing something dynamic.  The object we're instantiating
                // may not have the members that we're trying to access on the dynamic object.
                return false;
            }

            _valuePattern = left;
            _initializedSymbol = _semanticModel.GetSymbolInfo(left, _cancellationToken).GetAnySymbol();
            return true;
        }

        protected bool ValuePatternMatches(TExpressionSyntax expression)
        {
            if (_valuePattern.IsToken)
            {
                return _syntaxFacts.IsIdentifierName(expression) &&
                    _syntaxFacts.AreEquivalent(
                        _valuePattern.AsToken(),
                        _syntaxFacts.GetIdentifierOfSimpleName(expression));
            }
            else
            {
                return _syntaxFacts.AreEquivalent(
                    _valuePattern.AsNode(), expression);
            }
        }

        protected bool ExpressionContainsValuePatternOrReferencesInitializedSymbol(SyntaxNode expression)
        {
            foreach (var subExpression in expression.DescendantNodesAndSelf().OfType<TExpressionSyntax>())
            {
                if (!_syntaxFacts.IsNameOfSimpleMemberAccessExpression(subExpression) &&
                    !_syntaxFacts.IsNameOfMemberBindingExpression(subExpression))
                {
                    if (ValuePatternMatches(subExpression))
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
