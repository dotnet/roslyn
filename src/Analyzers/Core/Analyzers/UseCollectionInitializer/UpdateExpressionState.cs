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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

/// <summary>
/// Common immutable state and helpers used by "convert object creation to collection initializer/expression" and "use
/// collection expression for builder pattern".
/// </summary>
internal readonly struct UpdateExpressionState<
    TExpressionSyntax,
    TStatementSyntax>
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
{
    private static readonly ImmutableArray<(string name, bool isLinq)> s_multiAddNames =
    [
        (nameof(List<int>.AddRange), isLinq: false),
        (nameof(Enumerable.Concat), isLinq: true),
        (nameof(Enumerable.Append), isLinq: true),
    ];

    public readonly SemanticModel SemanticModel;
    public readonly ISyntaxFacts SyntaxFacts;

    /// <summary>
    /// The original object-creation or collection-builder-creation expression.
    /// </summary>
    public readonly TExpressionSyntax StartExpression;

    /// <summary>
    /// The statement containing <see cref="StartExpression"/>
    /// </summary>
    public readonly TStatementSyntax? ContainingStatement;

    /// <summary>
    /// The name of the value being mutated.  It is whatever the new object-creation or collection-builder is assigned to.
    /// </summary>
    public readonly SyntaxNodeOrToken ValuePattern;

    /// <summary>
    /// If a different symbol was initialized (for example, a field rather than a local) this will be that symbol.  This
    /// only applies to the object-creation case.
    /// </summary>
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
        => ContainingStatement is null
            ? []
            : UseCollectionInitializerHelpers.GetSubsequentStatements(SyntaxFacts, ContainingStatement);

    /// <summary>
    /// <see langword="true"/> if this <paramref name="expression"/> is a reference to the object-creation value, or the
    /// collection-builder that was created.  For example, when seeing <c>x.Add(y)</c> this can be used to see if
    /// <c>x</c> refers to the value being analyzed, and as such <c>y</c> should be added as an element once this is
    /// converted to a collection-initializer or collection-expression.
    /// </summary>
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

    /// <summary>
    /// <see langword="true"/> if the passed in <paramref name="expression"/> contains some reference to the value being
    /// tracked, or symbol it was assigned to.  This can be used to see if there are other manipulations of that symbol,
    /// preventing the features from offering to convert these more complex scenarios to
    /// collection-initializers/expressions.
    /// </summary>
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

    public bool TryAnalyzeInvocationForCollectionExpression(
        TExpressionSyntax invocationExpression,
        bool allowLinq,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out TExpressionSyntax? instance,
        out bool useSpread)
    {
        // Look for a call to Add taking 1 arg
        if (this.TryAnalyzeAddInvocation(
                invocationExpression,
                requiredArgumentName: null,
                forCollectionExpression: true,
                cancellationToken,
                out instance))
        {
            useSpread = false;
            return true;
        }

        // Then a call to AddRange/Concat/Append, taking 1-n args
        foreach (var (multiAddName, isLinq) in s_multiAddNames)
        {
            if (isLinq && !allowLinq)
                continue;

            if (this.TryAnalyzeMultiAddInvocation(
                    invocationExpression,
                    multiAddName,
                    requiredArgumentName: null,
                    cancellationToken,
                    out instance,
                    out useSpread))
            {
                return true;
            }
        }

        useSpread = false;
        return false;
    }

    /// <summary>
    /// Analyze an expression statement to see if it is a legal call of the form <c>val.Add(...)</c>.
    /// </summary>
    public bool TryAnalyzeAddInvocation(
        TExpressionSyntax invocationExpression,
        string? requiredArgumentName,
        bool forCollectionExpression,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out TExpressionSyntax? instance)
    {
        if (!TryAnalyzeInvocation(
                invocationExpression,
                WellKnownMemberNames.CollectionInitializerAddMethodName,
                requiredArgumentName,
                cancellationToken,
                out instance,
                out var arguments))
        {
            return false;
        }

        // Collection expressions can only call the single argument Add method on a type. So if we don't have exactly
        // one argument, fail out.
        if (forCollectionExpression && arguments.Count != 1)
            return false;

        return true;
    }

    /// <summary>
    /// Analyze an expression statement to see if it is a legal call similar to <c>val.AddRange(...)</c> or
    /// <c>val.Concat(...)</c>.  This method properly handles cases where there are multiple args passed to a <c>params
    /// T[]</c> method, or a single arg which might be passed to the same <c>params</c> method, or which may itself be
    /// an entire collection being added.
    /// </summary>
    private bool TryAnalyzeMultiAddInvocation(
        TExpressionSyntax invocationExpression,
        string methodName,
        string? requiredArgumentName,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out TExpressionSyntax? instance,
        out bool useSpread)
    {
        useSpread = false;
        if (!TryAnalyzeInvocation(
                invocationExpression,
                methodName,
                requiredArgumentName,
                cancellationToken,
                out instance,
                out var arguments))
        {
            return false;
        }

        var memberAccess = instance.GetRequiredParent();

        // TryAnalyzeInvocation ensures these
        Contract.ThrowIfTrue(arguments.Count == 0);
        Contract.ThrowIfFalse(this.SyntaxFacts.IsSimpleMemberAccessExpression(memberAccess));

        // AddRange can be of the form `AddRange(IEnumerable<T> values)` or it could be `AddRange(params T[]
        // values)`  If the former, we only allow a single argument.  If the latter, we can allow multiple
        // expressions.  The former will be converted to a spread element.  The latter will be added
        // individually.
        var method = this.SemanticModel.GetSymbolInfo(memberAccess, cancellationToken).GetAnySymbol() as IMethodSymbol;
        if (method is null)
            return false;

        if (method.Parameters.Length != 1)
            return false;

        var parameter = method.Parameters.Single();
        if (parameter.IsParams)
        {
            // It's a method like `AddRange(T[] values)`.  If we were passed an array to this, we'll use a spread.
            // Otherwise, if we were passed individual elements, we'll add them as is.
            if (arguments.Count > 1)
                return true;

            // For single argument case, have to determine which form we're calling.
            var convertedType = this.SemanticModel.GetTypeInfo(SyntaxFacts.GetExpressionOfArgument(arguments[0]), cancellationToken).ConvertedType;
            useSpread = parameter.Type.Equals(convertedType);
        }
        else
        {
            // It's a method like `AddRange(IEnumerable<T> values)`.  There needs to be a single value passed.  When
            // converted to a collection expression, we'll use a spread expression like `[.. values]`.
            if (arguments.Count != 1)
                return false;

            // Check for things like `Concat<T>(this IEnumerable<T> source, T value)`.  In that case, we wouldn't want to spread.
            useSpread = method.GetOriginalUnreducedDefinition() is not IMethodSymbol { IsExtensionMethod: true, Parameters: [_, { Type: ITypeParameterSymbol }] };
        }

        return true;
    }

    private bool TryAnalyzeInvocation(
        TExpressionSyntax invocationExpression,
        string methodName,
        string? requiredArgumentName,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out TExpressionSyntax? instance,
        out SeparatedSyntaxList<SyntaxNode> arguments)
    {
        arguments = default;
        instance = null;

        if (!this.SyntaxFacts.IsInvocationExpression(invocationExpression))
            return false;

        arguments = this.SyntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
        if (arguments.Count < 1)
            return false;

        if (requiredArgumentName != null && arguments.Count != 1)
            return false;

        var memberAccess = this.SyntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
        if (!this.SyntaxFacts.IsSimpleMemberAccessExpression(memberAccess))
            return false;

        this.SyntaxFacts.GetPartsOfMemberAccessExpression(memberAccess, out var localInstance, out var memberName);
        this.SyntaxFacts.GetNameAndArityOfSimpleName(memberName, out var name, out var arity);

        if (arity != 0 || !Equals(name, methodName))
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

        instance = localInstance as TExpressionSyntax;
        return instance != null;
    }

    /// <summary>
    /// Analyze an statement to see if it it could be converted into elements for a new collection-expression.  This
    /// includes calls to <c>.Add</c> and <c>.AddRange</c>, as well as <c>foreach</c> statements that update the
    /// collection, and <c>if</c> statements that conditionally add items to the collection-expression.
    /// </summary>
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
            var expression = (TExpressionSyntax)@this.SyntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);

            // Look for a call to Add or AddRange
            if (@this.TryAnalyzeInvocationForCollectionExpression(expression, allowLinq: false, cancellationToken, out var instance, out var useSpread) &&
                @this.ValuePatternMatches(instance))
            {
                return new(Statement: expressionStatement, useSpread);
            }

            return null;
        }

        Match<TStatementSyntax>? TryAnalyzeForeachStatement(TStatementSyntax foreachStatement)
        {
            syntaxHelper.GetPartsOfForeachStatement(foreachStatement, out var awaitKeyword, out var identifier, out _, out var foreachStatements);
            if (awaitKeyword != default)
                return null;

            // must be of the form:
            //
            //      foreach (var x in expr)
            //          dest.Add(x)
            //
            // By passing 'x' into TryAnalyzeInvocation below, we ensure that it is an enumerated value from `expr`
            // being added to `dest`.
            if (foreachStatements.ToImmutableArray() is [TStatementSyntax childStatement] &&
                @this.SyntaxFacts.IsExpressionStatement(childStatement) &&
                @this.TryAnalyzeAddInvocation(
                    (TExpressionSyntax)@this.SyntaxFacts.GetExpressionOfExpressionStatement(childStatement),
                    requiredArgumentName: identifier.Text,
                    forCollectionExpression: true,
                    cancellationToken,
                    out var instance) &&
                @this.ValuePatternMatches(instance))
            {
                // `foreach` will become `..expr` when we make it into a collection expression.
                return new(Statement: foreachStatement, UseSpread: true);
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
                @this.SyntaxFacts.IsExpressionStatement(trueChildStatement) &&
                @this.TryAnalyzeAddInvocation(
                    (TExpressionSyntax)@this.SyntaxFacts.GetExpressionOfExpressionStatement(trueChildStatement),
                    requiredArgumentName: null,
                    forCollectionExpression: true,
                    cancellationToken,
                    out var instance) &&
                @this.ValuePatternMatches(instance))
            {
                if (whenFalse is null)
                {
                    // add the form `.. x ? [y] : []` to the result
                    return @this.SyntaxFacts.SupportsCollectionExpressionNaturalType(ifStatement.SyntaxTree.Options)
                        ? new(Statement: ifStatement, UseSpread: true)
                        : null;
                }

                var whenFalseStatements = whenFalse.ToImmutableArray();
                if (whenFalseStatements is [TStatementSyntax falseChildStatement] &&
                    @this.SyntaxFacts.IsExpressionStatement(falseChildStatement) &&
                    @this.TryAnalyzeAddInvocation(
                        (TExpressionSyntax)@this.SyntaxFacts.GetExpressionOfExpressionStatement(falseChildStatement),
                        requiredArgumentName: null,
                        forCollectionExpression: true,
                        cancellationToken,
                        out instance) &&
                    @this.ValuePatternMatches(instance))
                {
                    // add the form `x ? y : z` to the result
                    return new(Statement: ifStatement, UseSpread: false);
                }
            }

            return null;
        }
    }
}
