// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;
using FluentState = UpdateExpressionState<ExpressionSyntax, StatementSyntax>;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    private const string ToPrefix = "To";
    private const string AsSpanName = "AsSpan";

    /// <summary>
    /// Standard names to look at for the final <c>ToXXX</c> method.  For example "ToList", "ToArray", "ToImmutable",
    /// etc.  Note: this will just be done for a syntactic check of the method being called.  Additional checks will
    /// ensure that we are preserving semantics.
    /// </summary>
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
        nameof(ImmutableStack<int>),
        nameof(System.Collections.Immutable));

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
            IsSyntacticMatch(state, parentMemberAccess, parentInvocation, allowLinq: true, matchesInReverse: null, out _, cancellationToken))
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
        // Because we're recursing from top to bottom in the expression tree, we build up the matches in reverse.  Right
        // before returning them, we'll reverse them again to get the proper order.
        using var _ = ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>.GetInstance(out var matchesInReverse);
        if (!AnalyzeInvocation(state, invocation, addMatches ? matchesInReverse : null, out var existingInitializer, cancellationToken))
            return null;

        if (!CanReplaceWithCollectionExpression(state.SemanticModel, invocation, skipVerificationForReplacedNode: true, cancellationToken))
            return null;

        matchesInReverse.ReverseContents();
        return new AnalysisResult(existingInitializer, invocation, matchesInReverse.ToImmutable());
    }

    private static bool AnalyzeInvocation(
        FluentState state,
        InvocationExpressionSyntax invocation,
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>? matchesInReverse,
        out InitializerExpressionSyntax? existingInitializer,
        CancellationToken cancellationToken)
    {
        existingInitializer = null;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Topmost invocation must be a syntactic match for one of our collection manipulation forms.  At the top level
        // we don't want to end with a linq method as that would be lazy, and a collection expression will eagerly
        // realize the collection.
        if (!IsSyntacticMatch(state, memberAccess, invocation, allowLinq: false, matchesInReverse, out var isAdditionMatch, cancellationToken))
            return false;

        // We don't want to offer this feature on top of some builder-type.  They will commonly end with something like
        // `builder.ToImmutable()`.  We want that case to be handled by the 'ForBuilder' analyzer instead.
        var expressionType = state.SemanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        if (expressionType is null || expressionType.Name.EndsWith("Builder", StringComparison.Ordinal))
            return false;

        var semanticModel = state.SemanticModel;
        var compilation = semanticModel.Compilation;

        using var _1 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var stack);
        stack.Push(memberAccess.Expression);

        var copiedData = false;

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = stack.Pop();

            // Methods of the form Add(...)/AddRange(...) or `ToXXX()` count as something to continue recursing down the
            // left hand side of the expression.  In the inner expressions we can have things like `.Concat/.Append`
            // calls as the outer expressions will realize the collection.
            if (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax currentMemberAccess } currentInvocation &&
                IsSyntacticMatch(state, currentMemberAccess, currentInvocation, allowLinq: true, matchesInReverse, out _, cancellationToken))
            {
                copiedData = true;
                stack.Push(currentMemberAccess.Expression);
                continue;
            }

            // `new int[] { ... }` or `new[] { ... }` is a fine base case to make a collection out of.  As arrays are
            // always list-like this is safe to move over.
            if (current is ArrayCreationExpressionSyntax { Initializer: var initializer } arrayCreation)
            {
                if (initializer is null || !IsLegalInitializer(initializer))
                    return false;

                existingInitializer = initializer;
                return true;
            }

            if (current is ImplicitArrayCreationExpressionSyntax implicitArrayCreation)
            {
                if (!IsLegalInitializer(implicitArrayCreation.Initializer))
                    return false;

                existingInitializer = implicitArrayCreation.Initializer;
                return true;
            }

            // Forms like `Array.Empty<int>()` or `ImmutableArray<int>.Empty` are fine base cases.  Because the
            // collection is empty, we don't have to add any matches.
            if (IsCollectionEmptyAccess(semanticModel, current, cancellationToken))
                return IsListLike(current);

            // `new X()` or `new X { a, b, c}` or `new X() { a, b, c }` are fine base cases.
            if (current is ObjectCreationExpressionSyntax objectCreation)
            {
                if (objectCreation is not
                    {
                        ArgumentList: null or { Arguments.Count: 0 },
                        Initializer: null or { RawKind: (int)SyntaxKind.CollectionInitializerExpression }
                    })
                {
                    return false;
                }

                if (!IsLegalInitializer(objectCreation.Initializer))
                    return false;

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

                if (matchesInReverse != null)
                {
                    AddArgumentsInReverse(matchesInReverse, GetArguments(currentInvocationExpression, unwrapArgument), useSpread: false);
                }

                return true;
            }

            // If we're bottomed out at some different type of expression, and we started with an AsSpan, and we did not
            // perform a copy of the data, then do not convert this.  The above cases produce a fresh-collection (an
            // rvalue), which is fine to get a span out of.  However, this may be wrapping a *non-fresh* (an lvalue)
            // collection.  That means the user could mutate the underlying data the span wraps.  Since we are
            // converting to a form that will create a fresh collection, that could be noticeable.
            if (memberAccess.Name.Identifier.ValueText == AsSpanName && !copiedData)
                return false;

            // Down to some final collection.  Like `x` in `x.Concat(y).ToArray()`.  If `x` is itself is something that
            // can be iterated, we can convert this to `[.. x, .. y]`.  Note: we only want to do this if ending with one
            // of the ToXXX Methods.  If we just have `x.AddRange(y)` it's preference to keep that, versus `[.. x, ..y]`
            if (!isAdditionMatch && IsIterable(current))
            {
                matchesInReverse?.Add(new CollectionExpressionMatch<ArgumentSyntax>(SyntaxFactory.Argument(current), UseSpread: true));
                return true;
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
                Implements(type, compilation.IListOfTType()) ||
                Implements(type, compilation.IReadOnlyListOfTType());
        }

        bool IsIterable(ExpressionSyntax expression)
        {
            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (type is null or IErrorTypeSymbol)
                return false;

            return Implements(type, compilation.IEnumerableOfTType()) ||
                type.Equals(compilation.SpanOfTType()) ||
                type.Equals(compilation.ReadOnlySpanOfTType());
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

        static bool IsLegalInitializer(InitializerExpressionSyntax? initializer)
        {
            // We can't convert any initializer that contains an initializer in it.  For example `new SomeType() { { 1,
            // 2, 3 } }`.  These become `.Add(1, 2, 3)` calls that collection expressions do not support.
            if (initializer != null)
            {
                foreach (var expression in initializer.Expressions)
                {
                    if (expression is InitializerExpressionSyntax)
                        return false;
                }
            }
            return true;
        }
    }

    private static void AddArgumentsInReverse(
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>> matchesInReverse,
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        bool useSpread)
    {
        Contract.ThrowIfTrue(useSpread && arguments.Count != 1);

        for (var i = arguments.Count - 1; i >= 0; i--)
            matchesInReverse.Add(new(arguments[i], useSpread));
    }

    /// <summary>
    /// Tests if this single `expr.SomeInvocation(...)` syntactically matches one of the allowed forms
    /// (ToList/AsSpan/etc.).  That includes that the arguments present to the invocation are acceptable for that
    /// particular method call.  If <paramref name="matchesInReverse"/> is provided, the arguments to the method will be
    /// appropriately extracted so that they can be placed in the final collection expression.
    /// </summary>
    private static bool IsSyntacticMatch(
        FluentState state,
        MemberAccessExpressionSyntax memberAccess,
        InvocationExpressionSyntax invocation,
        bool allowLinq,
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>? matchesInReverse,
        out bool isAdditionMatch,
        CancellationToken cancellationToken)
    {
        isAdditionMatch = false;
        if (memberAccess.Kind() != SyntaxKind.SimpleMemberAccessExpression)
            return false;

        var name = memberAccess.Name.Identifier.ValueText;

        // Check for Add/AddRange/Concat
        if (state.TryAnalyzeInvocationForCollectionExpression(invocation, allowLinq, cancellationToken, out _, out var useSpread))
        {
            if (matchesInReverse != null)
            {
                AddArgumentsInReverse(matchesInReverse, invocation.ArgumentList.Arguments, useSpread);
            }

            isAdditionMatch = true;
            return true;
        }

        // Now check for ToXXX/AsXXX.  All of these need no args.
        if (invocation.ArgumentList.Arguments.Count > 0)
            return false;

        return IsAnyNameMatch(name);

        static bool IsAnyNameMatch(string name)
        {
            if (name == AsSpanName)
                return true;

            if (!name.StartsWith(ToPrefix, StringComparison.Ordinal))
                return false;

            return HasAnySuffix(name);
        }

        static bool HasAnySuffix(string name)
        {
            foreach (var suffix in s_suffixes)
            {
                if (name.Length == (ToPrefix.Length + suffix.Length) &&
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
