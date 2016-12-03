// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer
{
    internal struct ObjectCreationExpressionAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TVariableDeclaratorSyntax>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TExpressionStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly TObjectCreationExpressionSyntax _objectCreationExpression;

        private TStatementSyntax _containingStatement;
        private SyntaxNodeOrToken _valuePattern;

        public ObjectCreationExpressionAnalyzer(
            ISyntaxFactsService syntaxFacts,
            TObjectCreationExpressionSyntax objectCreationExpression) : this()
        {
            _syntaxFacts = syntaxFacts;
            _objectCreationExpression = objectCreationExpression;
        }

        internal ImmutableArray<TExpressionStatementSyntax>? Analyze()
        {
            if (_syntaxFacts.GetObjectCreationInitializer(_objectCreationExpression) != null)
            {
                // Don't bother if this already has an initializer.
                return null;
            }

            _containingStatement = _objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
            if (_containingStatement == null)
            {
                return null;
            }

            if (!TryInitializeVariableDeclarationCase() &&
                !TryInitializeAssignmentCase())
            {
                return null;
            }

            var matches = ArrayBuilder<TExpressionStatementSyntax>.GetInstance();
            AddMatches(matches);
            return matches.ToImmutableAndFree();
        }

        private void AddMatches(ArrayBuilder<TExpressionStatementSyntax> matches)
        {
            var containingBlock = _containingStatement.Parent;
            var foundStatement = false;

            var seenInvocation = false;
            var seenIndexAssignment = false;

            foreach (var child in containingBlock.ChildNodesAndTokens())
            {
                if (!foundStatement)
                {
                    if (child == _containingStatement)
                    {
                        foundStatement = true;
                    }

                    continue;
                }

                if (child.IsToken)
                {
                    return;
                }

                var statement = child.AsNode() as TExpressionStatementSyntax;
                if (statement == null)
                {
                    return;
                }

                SyntaxNode instance = null;
                if (!seenIndexAssignment)
                {
                    if (TryAnalyzeAddInvocation(statement, out instance))
                    {
                        seenInvocation = true;
                    }
                }

                if (!seenInvocation)
                {
                    if (TryAnalyzeIndexAssignment(statement, out instance))
                    {
                        seenIndexAssignment = true;
                    }
                }

                if (instance == null)
                {
                    return;
                }

                if (!ValuePatternMatches((TExpressionSyntax)instance))
                {
                    return;
                }

                matches.Add(statement);
            }
        }

        private bool TryAnalyzeIndexAssignment(
            TExpressionStatementSyntax statement,
            out SyntaxNode instance)
        {
            instance = null;
            if (!_syntaxFacts.SupportsIndexingInitializer(statement.SyntaxTree.Options))
            {
                return false;
            }

            if (!_syntaxFacts.IsSimpleAssignmentStatement(statement))
            {
                return false;
            }

            _syntaxFacts.GetPartsOfAssignmentStatement(statement,
                out var left, out var right);

            if (!_syntaxFacts.IsElementAccessExpression(left))
            {
                return false;
            }

            instance = _syntaxFacts.GetExpressionOfElementAccessExpression(left);
            return true;
        }

        private bool TryAnalyzeAddInvocation(
            TExpressionStatementSyntax statement,
            out SyntaxNode instance)
        {
            instance = null;
            var invocationExpression = _syntaxFacts.GetExpressionOfExpressionStatement(statement) as TInvocationExpressionSyntax;
            if (invocationExpression == null)
            {
                return false;
            }

            var arguments = _syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
            if (arguments.Count < 1)
            {
                return false;
            }

            foreach (var argument in arguments)
            {
                if (!_syntaxFacts.IsSimpleArgument(argument))
                {
                    return false;
                }
            }

            var memberAccess = _syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression) as TMemberAccessExpressionSyntax;
            if (memberAccess == null)
            {
                return false;
            }

            if (!_syntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
            {
                return false;
            }

            _syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out instance, out var memberName);
            _syntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

            if (arity != 0 || !name.Equals(nameof(IList.Add)))
            {
                return false;
            }

            return true;
        }

        private bool ValuePatternMatches(TExpressionSyntax expression)
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

            _valuePattern = left;
            return true;
        }

        private bool TryInitializeVariableDeclarationCase()
        {
            if (!_syntaxFacts.IsLocalDeclarationStatement(_containingStatement))
            {
                return false;
            }

            var containingDeclarator = _objectCreationExpression.Parent.Parent as TVariableDeclaratorSyntax;
            if (containingDeclarator == null)
            {
                return false;
            }

            if (!_syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(containingDeclarator, _containingStatement))
            {
                return false;
            }

            _valuePattern = _syntaxFacts.GetIdentifierOfVariableDeclarator(containingDeclarator);
            return true;
        }
    }
}