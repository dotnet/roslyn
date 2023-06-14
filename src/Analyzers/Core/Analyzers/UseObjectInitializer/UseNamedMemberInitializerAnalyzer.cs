// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.UseObjectInitializer
{
    internal class UseNamedMemberInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TVariableDeclaratorSyntax> : AbstractObjectCreationExpressionAnalyzer<
            TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TVariableDeclaratorSyntax,
            Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>>
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        private static readonly ObjectPool<UseNamedMemberInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax, TVariableDeclaratorSyntax>> s_pool
            = SharedPools.Default<UseNamedMemberInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax, TVariableDeclaratorSyntax>>();

        public static ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>>? Analyze(
            SemanticModel semanticModel,
            ISyntaxFacts syntaxFacts,
            TObjectCreationExpressionSyntax objectCreationExpression,
            CancellationToken cancellationToken)
        {
            var analyzer = s_pool.Allocate();
            analyzer.Initialize(semanticModel, syntaxFacts, objectCreationExpression, cancellationToken);
            try
            {
                return analyzer.AnalyzeWorker();
            }
            finally
            {
                analyzer.Clear();
                s_pool.Free(analyzer);
            }
        }

        protected override bool ShouldAnalyze()
        {
            // Can't add member initializers if the object already has a collection initializer attached to it.
            return !_syntaxFacts.IsObjectCollectionInitializer(_syntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression));
        }

        protected override void AddMatches(ArrayBuilder<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches)
        {
            // If containing statement is inside a block (e.g. method), than we need to iterate through its child statements.
            // If containing statement is in top-level code, than we need to iterate through child statements of containing compilation unit.
            var containingBlockOrCompilationUnit = _containingStatement.Parent;

            // In case of top-level code parent of the statement will be GlobalStatementSyntax,
            // so we need to get its parent in order to get CompilationUnitSyntax
            if (_syntaxFacts.IsGlobalStatement(containingBlockOrCompilationUnit))
            {
                containingBlockOrCompilationUnit = containingBlockOrCompilationUnit.Parent;
            }

            var foundStatement = false;

            using var _1 = PooledHashSet<string>.GetInstance(out var seenNames);

            var initializer = _syntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
            if (initializer != null)
            {
                foreach (var init in _syntaxFacts.GetInitializersOfObjectMemberInitializer(initializer))
                {
                    if (_syntaxFacts.IsNamedMemberInitializer(init))
                    {
                        _syntaxFacts.GetPartsOfNamedMemberInitializer(init, out var name, out _);
                        seenNames.Add(_syntaxFacts.GetIdentifierOfIdentifierName(name).ValueText);
                    }
                }
            }

            foreach (var child in containingBlockOrCompilationUnit.ChildNodesAndTokens())
            {
                if (child.IsToken)
                    continue;

                var childNode = child.AsNode();
                var extractedChild = _syntaxFacts.IsGlobalStatement(childNode) ? _syntaxFacts.GetStatementOfGlobalStatement(childNode) : childNode;

                if (!foundStatement)
                {
                    if (extractedChild == _containingStatement)
                    {
                        foundStatement = true;
                        continue;
                    }

                    continue;
                }

                if (extractedChild is not TAssignmentStatementSyntax statement)
                    break;

                if (!_syntaxFacts.IsSimpleAssignmentStatement(statement))
                    break;

                _syntaxFacts.GetPartsOfAssignmentStatement(
                    statement, out var left, out var right);

                var rightExpression = right as TExpressionSyntax;
                var leftMemberAccess = left as TMemberAccessExpressionSyntax;

                if (!_syntaxFacts.IsSimpleMemberAccessExpression(leftMemberAccess))
                    break;

                var expression = (TExpressionSyntax)_syntaxFacts.GetExpressionOfMemberAccessExpression(leftMemberAccess);
                if (!ValuePatternMatches(expression))
                    break;

                var leftSymbol = _semanticModel.GetSymbolInfo(leftMemberAccess, _cancellationToken).GetAnySymbol();
                if (leftSymbol?.IsStatic == true)
                {
                    // Static members cannot be initialized through an object initializer.
                    break;
                }

                var type = _semanticModel.GetTypeInfo(_objectCreationExpression, _cancellationToken).Type;
                if (type == null)
                    break;

                if (IsExplicitlyImplemented(type, leftSymbol, out var typeMember))
                    break;

                // Don't offer this fix if the value we're initializing is itself referenced
                // on the RHS of the assignment.  For example:
                //
                //      var v = new X();
                //      v.Prop = v.Prop.WithSomething();
                //
                // Or with
                //
                //      v = new X();
                //      v.Prop = v.Prop.WithSomething();
                //
                // In the first case, 'v' is being initialized, and so will not be available 
                // in the object initializer we create.
                // 
                // In the second case we'd change semantics because we'd access the old value 
                // before the new value got written.
                if (ExpressionContainsValuePatternOrReferencesInitializedSymbol(rightExpression))
                    break;

                // If we have code like "x.v = .Length.ToString()"
                // then we don't want to change this into:
                //
                //      var x = new Whatever() With { .v = .Length.ToString() }
                //
                // The problem here is that .Length will change it's meaning to now refer to the 
                // object that we're creating in our object-creation expression.
                if (ImplicitMemberAccessWouldBeAffected(rightExpression))
                    break;

                // found a match!
                //
                // If we see an assignment to the same property/field, we can't convert it
                // to an initializer.
                var name = _syntaxFacts.GetNameOfMemberAccessExpression(leftMemberAccess);
                var identifier = _syntaxFacts.GetIdentifierOfSimpleName(name);
                if (!seenNames.Add(identifier.ValueText))
                    break;

                matches.Add(new Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>(
                    statement, leftMemberAccess, rightExpression, typeMember?.Name ?? identifier.ValueText));
            }
        }

        private static bool IsExplicitlyImplemented(
            ITypeSymbol classOrStructType,
            ISymbol member,
            out ISymbol typeMember)
        {
            if (member != null && member.ContainingType.IsInterfaceType())
            {
                typeMember = classOrStructType?.FindImplementationForInterfaceMember(member);
                return typeMember is IPropertySymbol property &&
                    property.ExplicitInterfaceImplementations.Length > 0 &&
                    property.DeclaredAccessibility == Accessibility.Private;
            }

            typeMember = member;
            return false;
        }

        private bool ImplicitMemberAccessWouldBeAffected(SyntaxNode node)
        {
            if (node != null)
            {
                foreach (var child in node.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                    {
                        if (ImplicitMemberAccessWouldBeAffected(child.AsNode()))
                        {
                            return true;
                        }
                    }
                }

                if (_syntaxFacts.IsSimpleMemberAccessExpression(node))
                {
                    var expression = _syntaxFacts.GetExpressionOfMemberAccessExpression(
                        node, allowImplicitTarget: true);

                    // If we're implicitly referencing some target that is before the 
                    // object creation expression, then our semantics will change.
                    if (expression != null && expression.SpanStart < _objectCreationExpression.SpanStart)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    internal readonly struct Match<
        TExpressionSyntax,
        TStatementSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax>(
        TAssignmentStatementSyntax statement,
        TMemberAccessExpressionSyntax memberAccessExpression,
        TExpressionSyntax initializer,
        string memberName)
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
        where TMemberAccessExpressionSyntax : TExpressionSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
    {
        public readonly TAssignmentStatementSyntax Statement = statement;
        public readonly TMemberAccessExpressionSyntax MemberAccessExpression = memberAccessExpression;
        public readonly TExpressionSyntax Initializer = initializer;
        public readonly string MemberName = memberName;
    }
}
