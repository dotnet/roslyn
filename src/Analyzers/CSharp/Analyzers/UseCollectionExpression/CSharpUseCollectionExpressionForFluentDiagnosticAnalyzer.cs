// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;
using FluentState = UpdateExpressionState<ExpressionSyntax, StatementSyntax>;

/// <summary>
/// Analyzer/fixer that looks for code of the form <c>X.Empty&lt;T&gt;()</c> or <c>X&lt;T&gt;.Empty</c> and offers to
/// replace with <c>[]</c> if legal to do so.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    private static readonly ImmutableArray<string> s_prefixes = ImmutableArray.Create("To", "As");
    private static readonly ImmutableArray<string> s_suffixes = ImmutableArray.Create(
        nameof(Array),
        nameof(Span<int>),
        nameof(ReadOnlySpan<int>),
        nameof(List<int>),
        nameof(HashSet<int>),
        nameof(LinkedList<int>),
        nameof(Queue<int>),
        nameof(SortedSet<int>),
        nameof(Stack<int>),
        nameof(ICollection<int>),
        nameof(IReadOnlyCollection<int>),
        nameof(IList<int>),
        nameof(IReadOnlyList<int>),
        nameof(ImmutableArray<int>),
        nameof(ImmutableHashSet<int>),
        nameof(ImmutableList<int>),
        nameof(ImmutableQueue<int>),
        nameof(ImmutableSortedSet<int>),
        nameof(ImmutableStack<int>));

    public CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCollectionExpressionForFluentDiagnosticId,
               EnforceOnBuildValues.UseCollectionExpressionForFluent)
    {
    }

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context)
        => context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);

    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        if (memberAccess.Parent is not InvocationExpressionSyntax invocation)
            return;

        var state = new FluentState(
            semanticModel, CSharpSyntaxFacts.Instance, invocation, valuePattern: default, initializedSymbol: null);

        // We want to analyze and report on the highest applicable invocation in an invocation chain.
        // So bail out if our parent is a match.
        if (invocation.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax parentInvocation } parentMemberAccess &&
            IsSyntacticMatch(state, parentMemberAccess, parentInvocation, allowLinq: true, matches: null, cancellationToken))
        {
            return;
        }

        var analysisResult = AnalyzeInvocation(state, invocation, addMatches: true, cancellationToken);
        if (analysisResult is null)
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: ImmutableArray.Create(invocation.GetLocation()),
            properties: null));

        return;
    }

    /// <summary>
    /// Analyzes an expression looking for one of the form <c>CollectionCreation</c>, followed by some number of 
    /// <c>.Add(...)/.AddRange(...)</c> or <c>.ToXXX()</c> calls
    /// </summary>
    public static AnalysisResult? AnalyzeInvocation(
        FluentState state,
        InvocationExpressionSyntax invocation,
        bool addMatches,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>.GetInstance(out var matches);
        if (!AnalyzeInvocation(state, invocation, addMatches ? matches : null, out var existingInitializer, cancellationToken))
            return null;

        if (!CanReplaceWithCollectionExpression(state.SemanticModel, invocation, skipVerificationForReplacedNode: true, cancellationToken))
            return null;

        return new AnalysisResult(existingInitializer, invocation, matches.ToImmutable());
    }

    private static bool AnalyzeInvocation(
        FluentState state,
        InvocationExpressionSyntax invocation,
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>? matches,
        out InitializerExpressionSyntax? existingInitializer,
        CancellationToken cancellationToken)
    {
        existingInitializer = null;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Topmost invocation must be a syntactic match for one of our collection manipulation forms.  At the top level
        // we don't want to end with a linq method as that would be lazy, and a collection expression will eagerly
        // realize the collection.
        if (!IsSyntacticMatch(state, memberAccess, invocation, allowLinq: false, matches, cancellationToken))
            return false;

        var semanticModel = state.SemanticModel;

        using var _1 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var stack);
        stack.Push(memberAccess.Expression);

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = stack.Pop();

            // Methods of the form Add(...)/AddRange(...) or `ToXXX()` count as something to continue recursing down the
            // left hand side of the expression.  In the inner expressions we can have things like `.Concat` calls as
            // the outer expressions will realize the collection.
            if (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax currentMemberAccess } currentInvocation &&
                IsSyntacticMatch(state, currentMemberAccess, currentInvocation, allowLinq: true, matches, cancellationToken))
            {
                stack.Push(currentMemberAccess.Expression);
                continue;
            }

            // `new int[] { ... }` or `new[] { ... }` is a fine base case to make a collection out of.  As arrays are
            // always list-like this is safe to move over.
            if (current is ArrayCreationExpressionSyntax { Initializer: { } initializer })
            {
                existingInitializer = initializer;
                return true;
            }

            if (current is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
            {
                existingInitializer = implicitArrayCreation.Initializer;
                return true;
            }

            // Forms like `Array.Empty<int>()` or `ImmutableArray<int>.Empty` are fine base cases.  Because the
            // collection is empty, we don't have to add any matches.
            if (IsCollectionEmptyAccess(semanticModel, current, cancellationToken))
                return IsListLike(current);

            // `new X()` or `new X { a, b, c}` or `new X() { a, b, c }` are fine base cases.
            if (current is ObjectCreationExpressionSyntax
                {
                    ArgumentList: null or { Arguments.Count: 0 },
                    Initializer: null or { RawKind: (int)SyntaxKind.CollectionInitializerExpression }
                } objectCreation)
            {
                if (!IsListLike(current))
                    return false;

                existingInitializer = objectCreation.Initializer;
                return true;
            }

            // Forms like `ImmutableArray.Create(...)` or `ImmutableArray.CreateRange(...)` are fine base cases.
            if (current is InvocationExpressionSyntax currentInvocationExpression &&
                IsCollectionFactoryCreate(semanticModel, currentInvocationExpression, out var factoryMemberAccess, out var unwrapArgument, cancellationToken))
            {
                if (!IsListLike(current))
                    return false;

                if (matches != null)
                {
                    foreach (var argument in GetArguments(currentInvocationExpression, unwrapArgument))
                        matches.Add(new(argument, UseSpread: false));
                }
            }

            // Something we didn't understand.
            return false;
        }

        return false;

        // We only want to offer this feature when the original collection was list-like (as opposed to being something
        // like a hash-set).  For example: `new List<int> { x, y, z }.ToImmutableArray()` produces different results
        // than `new HashSet<int> { x, y, z }.ToImmutableArray()` in the presence of duplicates.
        bool IsListLike(ExpressionSyntax expression)
        {
            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (type is null or IErrorTypeSymbol)
                return false;

            return
                Implements(type, semanticModel.Compilation.IListOfTType()) ||
                Implements(type, semanticModel.Compilation.IReadOnlyListOfTType());
        }

        static bool Implements(ITypeSymbol type, INamedTypeSymbol? interfaceType)
        {
            if (interfaceType != null)
            {
                foreach (var baseInterface in type.AllInterfaces)
                {
                    if (interfaceType.Equals(baseInterface.OriginalDefinition))
                        return true;
                }
            }

            return false;
        }
    }

    private static bool IsSyntacticMatch(
        FluentState state,
        MemberAccessExpressionSyntax memberAccess,
        InvocationExpressionSyntax invocation,
        bool allowLinq,
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>? matches,
        CancellationToken cancellationToken)
    {
        if (memberAccess.Kind() != SyntaxKind.SimpleMemberAccessExpression)
            return false;

        var name = memberAccess.Name.Identifier.ValueText;

        // Check for Add/AddRange/Concat
        if (state.TryAnalyzeInvocationForCollectionExpression(invocation, allowLinq, cancellationToken, out _, out var useSpread))
        {
            Contract.ThrowIfTrue(useSpread && invocation.ArgumentList.Arguments.Count != 1);
            if (matches != null)
            {
                foreach (var argument in invocation.ArgumentList.Arguments)
                    matches.Add(new(argument, useSpread));
            }

            return true;
        }

        // Now check for ToXXX/AsXXX.  All of these need no args.
        if (invocation.ArgumentList.Arguments.Count > 0)
            return false;

        return IsNameMatch(name);

        static bool IsNameMatch(string name)
        {
            foreach (var prefix in s_prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                    return HasSuffix(name, prefix);
            }

            return false;
        }

        static bool HasSuffix(string name, string prefix)
        {
            foreach (var suffix in s_suffixes)
            {
                if (name.Length == (prefix.Length + suffix.Length) &&
                    name.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Result of analyzing a fluent chain of collection additions (<c>XXX.Create().Add(...).AddRange(...).ToYYY()</c>
    /// expression to see if it can be replaced with a collection expression.
    /// </summary>
    /// <param name="DiagnosticLocation">The location to put the diagnostic to tell they user they can convert this
    /// expression.</param>
    /// <param name="ExistingInitializer">Optional existing initializer (for example: <c>new[] { 1, 2, 3 }</c>). Used to
    /// help determine the best collection expression final syntax.</param>
    /// <param name="CreationExpression">The location of the code like <c>builder.ToImmutable()</c> that will actually be
    /// replaced with the collection expression</param>
    /// <param name="Matches">The arguments being added to the collection that will be converted into elements in the
    /// final collection expression.</param>
    public readonly record struct AnalysisResult(
        // Location DiagnosticLocation,
        InitializerExpressionSyntax? ExistingInitializer,
        InvocationExpressionSyntax CreationExpression,
        ImmutableArray<CollectionExpressionMatch<ArgumentSyntax>> Matches);
}
