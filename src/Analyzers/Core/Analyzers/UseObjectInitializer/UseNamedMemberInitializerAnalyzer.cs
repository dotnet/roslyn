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
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.UseObjectInitializer;

internal abstract class AbstractUseNamedMemberInitializerAnalyzer<
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TAssignmentStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer> : AbstractObjectCreationExpressionAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>,
        TAnalyzer>, IDisposable
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TAssignmentStatementSyntax : TStatementSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractUseNamedMemberInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TAssignmentStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer>, new()
{
    public ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> Analyze(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TObjectCreationExpressionSyntax objectCreationExpression,
        CancellationToken cancellationToken)
    {
        var state = TryInitializeState(semanticModel, syntaxFacts, objectCreationExpression, analyzeForCollectionExpression: false, cancellationToken);
        if (state is null)
            return default;

        this.Initialize(state.Value, objectCreationExpression, analyzeForCollectionExpression: false);
        return this.AnalyzeWorker(cancellationToken);
    }

    protected sealed override bool ShouldAnalyze(CancellationToken cancellationToken)
    {
        // Can't add member initializers if the object already has a collection initializer attached to it.
        return !this.SyntaxFacts.IsObjectCollectionInitializer(this.SyntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression));
    }

    protected sealed override bool TryAddMatches(
        ArrayBuilder<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches,
        CancellationToken cancellationToken)
    {
        using var _1 = PooledHashSet<string>.GetInstance(out var seenNames);

        var initializer = this.SyntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
        if (initializer != null)
        {
            foreach (var init in this.SyntaxFacts.GetInitializersOfObjectMemberInitializer(initializer))
            {
                if (this.SyntaxFacts.IsNamedMemberInitializer(init))
                {
                    this.SyntaxFacts.GetPartsOfNamedMemberInitializer(init, out var name, out _);
                    seenNames.Add(this.SyntaxFacts.GetIdentifierOfIdentifierName(name).ValueText);
                }
            }
        }

        foreach (var subsequentStatement in this.State.GetSubsequentStatements())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (subsequentStatement is not TAssignmentStatementSyntax statement)
                break;

            if (!this.SyntaxFacts.IsSimpleAssignmentStatement(statement))
                break;

            this.SyntaxFacts.GetPartsOfAssignmentStatement(
                statement, out var left, out var right);

            var rightExpression = (TExpressionSyntax)right;
            var leftMemberAccess = left as TMemberAccessExpressionSyntax;

            if (!this.SyntaxFacts.IsSimpleMemberAccessExpression(leftMemberAccess))
                break;

            var expression = (TExpressionSyntax?)this.SyntaxFacts.GetExpressionOfMemberAccessExpression(leftMemberAccess);
            if (expression is null)
                break;

            if (!this.State.ValuePatternMatches(expression))
                break;

            var leftSymbol = this.SemanticModel.GetSymbolInfo(leftMemberAccess, cancellationToken).GetAnySymbol();
            if (leftSymbol?.IsStatic is true)
            {
                // Static members cannot be initialized through an object initializer.
                break;
            }

            var type = this.SemanticModel.GetTypeInfo(_objectCreationExpression, cancellationToken).Type;
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
            if (this.State.NodeContainsValuePatternOrReferencesInitializedSymbol(rightExpression, cancellationToken))
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
            var name = this.SyntaxFacts.GetNameOfMemberAccessExpression(leftMemberAccess);
            var identifier = this.SyntaxFacts.GetIdentifierOfSimpleName(name);
            if (!seenNames.Add(identifier.ValueText))
                break;

            matches.Add(new Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>(
                statement, leftMemberAccess, rightExpression, typeMember?.Name ?? identifier.ValueText));
        }

        return true;
    }

    private static bool IsExplicitlyImplemented(
        ITypeSymbol classOrStructType,
        ISymbol? member,
        [NotNullWhen(true)] out ISymbol? typeMember)
    {
        if (member != null && member.ContainingType.IsInterfaceType())
        {
            typeMember = classOrStructType?.FindImplementationForInterfaceMember(member);
            return typeMember is IPropertySymbol
            {
                DeclaredAccessibility: Accessibility.Private,
                ExplicitInterfaceImplementations.Length: > 0,
            };
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
                if (child.IsNode &&
                    ImplicitMemberAccessWouldBeAffected(child.AsNode()!))
                {
                    return true;
                }
            }

            if (this.SyntaxFacts.IsSimpleMemberAccessExpression(node))
            {
                var expression = this.SyntaxFacts.GetExpressionOfMemberAccessExpression(
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
