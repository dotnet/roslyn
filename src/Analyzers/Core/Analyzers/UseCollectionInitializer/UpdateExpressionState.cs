// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

internal readonly struct UpdateExpressionState<
    TExpressionSyntax,
    TStatementSyntax>
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
{
    public readonly SemanticModel SemanticModel;
    public readonly ISyntaxFacts SyntaxFacts;
    public readonly TExpressionSyntax StartExpression;
    public readonly TStatementSyntax ContainingStatement;

    public readonly SyntaxNodeOrToken ValuePattern;
    public readonly ISymbol? InitializedSymbol;

    public UpdateExpressionState(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TExpressionSyntax startExpression,
        SyntaxNodeOrToken valuePattern,
        ISymbol? initializedSymbol)
    {
        SemanticModel = semanticModel;
        SyntaxFacts = syntaxFacts;
        StartExpression = startExpression;
        ContainingStatement = startExpression.FirstAncestorOrSelf<TStatementSyntax>()!;
        ValuePattern = valuePattern;
        InitializedSymbol = initializedSymbol;
    }

    public IEnumerable<TStatementSyntax> GetSubsequentStatements()
        => UseCollectionInitializerHelpers.GetSubsequentStatements(SyntaxFacts, ContainingStatement);

    public bool ValuePatternMatches(TExpressionSyntax expression)
    {
        if (ValuePattern.IsToken)
        {
            return SyntaxFacts.IsIdentifierName(expression) &&
                SyntaxFacts.AreEquivalent(
                    ValuePattern.AsToken(),
                    SyntaxFacts.GetIdentifierOfSimpleName(expression));
        }
        else
        {
            return SyntaxFacts.AreEquivalent(
                ValuePattern.AsNode(), expression);
        }
    }

    public bool NodeContainsValuePatternOrReferencesInitializedSymbol(
        SyntaxNode expression,
        CancellationToken cancellationToken)
    {
        foreach (var subExpression in expression.DescendantNodesAndSelf().OfType<TExpressionSyntax>())
        {
            if (!SyntaxFacts.IsNameOfSimpleMemberAccessExpression(subExpression) &&
                !SyntaxFacts.IsNameOfMemberBindingExpression(subExpression))
            {
                if (ValuePatternMatches(subExpression))
                    return true;
            }

            if (InitializedSymbol != null &&
                InitializedSymbol.Equals(
                    SemanticModel.GetSymbolInfo(subExpression, cancellationToken).GetAnySymbol()))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryAnalyzeInvocation<TExpressionStatementSyntax>(
        TExpressionStatementSyntax statement,
        string addName,
        string? requiredArgumentName,
        bool forCollectionExpression,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out TExpressionSyntax? instance)
        where TExpressionStatementSyntax : TStatementSyntax
    {
        instance = null;

        if (!this.SyntaxFacts.IsExpressionStatement(statement))
            return false;

        var invocationExpression = this.SyntaxFacts.GetExpressionOfExpressionStatement(statement);
        if (!this.SyntaxFacts.IsInvocationExpression(invocationExpression))
            return false;

        var arguments = this.SyntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
        if (arguments.Count < 1)
            return false;

        // Collection expressions can only call the single argument Add/AddRange methods on a type.
        // So if we don't have exactly one argument, fail out.
        if (forCollectionExpression && arguments.Count != 1)
            return false;

        if (requiredArgumentName != null && arguments.Count != 1)
            return false;

        foreach (var argument in arguments)
        {
            if (!this.SyntaxFacts.IsSimpleArgument(argument))
                return false;

            var argumentExpression = this.SyntaxFacts.GetExpressionOfArgument(argument);
            if (NodeContainsValuePatternOrReferencesInitializedSymbol(argumentExpression, cancellationToken))
                return false;

            // VB allows for a collection initializer to be an argument.  i.e. `Goo({a, b, c})`.  This argument
            // cannot be used in an outer collection initializer as it would change meaning.  i.e.:
            //
            //      new List(Of IEnumerable(Of String)) { { a, b, c } }
            //
            // is not legal.  That's because instead of adding `{ a, b, c }` as a single element to the list, VB
            // instead looks for an 3-argument `Add` method to invoke on `List<T>` (which clearly fails).
            if (this.SyntaxFacts.SyntaxKinds.CollectionInitializerExpression == argumentExpression.RawKind)
                return false;

            // If the caller is requiring a particular argument name, then validate that is what this argument
            // is referencing.
            if (requiredArgumentName != null)
            {
                if (!this.SyntaxFacts.IsIdentifierName(argumentExpression))
                    return false;

                this.SyntaxFacts.GetNameAndArityOfSimpleName(argumentExpression, out var suppliedName, out _);
                if (requiredArgumentName != suppliedName)
                    return false;
            }
        }

        var memberAccess = this.SyntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
        if (!this.SyntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
            return false;

        this.SyntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out var localInstance, out var memberName);
        this.SyntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

        if (arity != 0 || !Equals(name, addName))
            return false;

        instance = localInstance as TExpressionSyntax;
        return instance != null;
    }

    public Match<TStatementSyntax>? TryAnalyzeStatementForCollectionExpression(
        IUpdateExpressionSyntaxHelper<TExpressionSyntax, TStatementSyntax> syntaxHelper,
        TStatementSyntax statement,
        CancellationToken cancellationToken)
    {
        var @this = this;

        if (SyntaxFacts.IsExpressionStatement(statement))
            return TryAnalyzeExpressionStatement(statement);

        if (SyntaxFacts.IsForEachStatement(statement))
            return TryAnalyzeForeachStatement(statement);

        if (SyntaxFacts.IsIfStatement(statement))
            return TryAnalyzeIfStatement(statement);

        return null;

        Match<TStatementSyntax>? TryAnalyzeExpressionStatement(TStatementSyntax expressionStatement)
        {
            // Look for a call to Add or AddRange
            if (@this.TryAnalyzeInvocation(
                    expressionStatement,
                    addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                    requiredArgumentName: null,
                    forCollectionExpression: true,
                    cancellationToken,
                    out var instance) &&
                @this.ValuePatternMatches(instance))
            {
                return new Match<TStatementSyntax>(expressionStatement, UseSpread: false);
            }

            if (@this.TryAnalyzeInvocation(
                    expressionStatement,
                    addName: nameof(List<int>.AddRange),
                    requiredArgumentName: null,
                    forCollectionExpression: true,
                    cancellationToken,
                    out instance))
            {
                // AddRange(x) will become `..x` when we make it into a collection expression.
                return new Match<TStatementSyntax>(expressionStatement, UseSpread: true);
            }

            return null;
        }

        Match<TStatementSyntax>? TryAnalyzeForeachStatement(TStatementSyntax foreachStatement)
        {
            syntaxHelper.GetPartsOfForeachStatement(foreachStatement, out var identifier, out _, out var foreachStatements);
            // must be of the form:
            //
            //      foreach (var x in expr)
            //          dest.Add(x)
            //
            // By passing 'x' into TryAnalyzeInvocation below, we ensure that it is an enumerated value from `expr`
            // being added to `dest`.
            if (foreachStatements.ToImmutableArray() is [TStatementSyntax childExpressionStatement] &&
                @this.TryAnalyzeInvocation(
                    childExpressionStatement,
                    addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                    requiredArgumentName: identifier.Text,
                    forCollectionExpression: true,
                    cancellationToken,
                    out var instance) &&
                @this.ValuePatternMatches(instance))
            {
                // `foreach` will become `..expr` when we make it into a collection expression.
                return new Match<TStatementSyntax>(foreachStatement, UseSpread: true);
            }

            return null;
        }

        Match<TStatementSyntax>? TryAnalyzeIfStatement(TStatementSyntax ifStatement)
        {
            // look for the form:
            //
            //  if (x)
            //      expr.Add(y)
            //
            // or
            //
            //  if (x)
            //      expr.Add(y)
            //  else
            //      expr.Add(z)

            syntaxHelper.GetPartsOfIfStatement(ifStatement, out _, out var whenTrue, out var whenFalse);
            var whenTrueStatements = whenTrue.ToImmutableArray();

            if (whenTrueStatements is [TStatementSyntax trueChildStatement] &&
                @this.TryAnalyzeInvocation(
                    trueChildStatement,
                    addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                    requiredArgumentName: null,
                    forCollectionExpression: true,
                    cancellationToken,
                    out var instance) &&
                @this.ValuePatternMatches(instance))
            {
                if (whenFalse is null)
                {
                    // add the form `.. x ? [y] : []` to the result
                    return new Match<TStatementSyntax>(ifStatement, UseSpread: true);
                }

                var whenFalseStatements = whenFalse.ToImmutableArray();
                if (whenFalseStatements is [TStatementSyntax falseChildStatement] &&
                    @this.TryAnalyzeInvocation(
                        falseChildStatement,
                        addName: WellKnownMemberNames.CollectionInitializerAddMethodName,
                        requiredArgumentName: null,
                        forCollectionExpression: true,
                        cancellationToken,
                        out instance) &&
                    @this.ValuePatternMatches(instance))
                {
                    // add the form `x ? y : z` to the result
                    return new Match<TStatementSyntax>(ifStatement, UseSpread: false);
                }
            }

            return null;
        }
    }
}
