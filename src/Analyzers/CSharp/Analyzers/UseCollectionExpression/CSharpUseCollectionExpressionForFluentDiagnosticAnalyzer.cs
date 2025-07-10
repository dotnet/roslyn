// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;
using static UseCollectionExpressionHelpers;
using FluentState = UpdateExpressionState<ExpressionSyntax, StatementSyntax>;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForFluentDiagnosticAnalyzer()
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer(
        IDEDiagnosticIds.UseCollectionExpressionForFluentDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForFluent)
{
    private const string ToPrefix = "To";
    private const string AsSpanName = "AsSpan";

    /// <summary>
    /// Standard names to look at for the final <c>ToXXX</c> method.  For example "ToList", "ToArray", "ToImmutable",
    /// etc.  Note: this will just be done for a syntactic check of the method being called.  Additional checks will
    /// ensure that we are preserving semantics.
    /// </summary>
    private static readonly ImmutableArray<string> s_suffixes =
    [
        nameof(Array),
        nameof(Span<>),
        nameof(ReadOnlySpan<>),
        nameof(System.Collections.Generic.List<>),
        nameof(HashSet<>),
        nameof(LinkedList<>),
        nameof(Queue<>),
        nameof(SortedSet<>),
        nameof(Stack<>),
        nameof(ICollection<>),
        nameof(IReadOnlyCollection<>),
        nameof(IList<>),
        nameof(IReadOnlyList<>),
        nameof(ImmutableArray<>),
        nameof(ImmutableHashSet<>),
        nameof(ImmutableList<>),
        nameof(ImmutableQueue<>),
        nameof(ImmutableSortedSet<>),
        nameof(ImmutableStack<>),
        nameof(System.Collections.Immutable),
    ];

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol? expressionType)
        => context.RegisterSyntaxNodeAction(context => AnalyzeMemberAccess(context, expressionType), SyntaxKind.SimpleMemberAccessExpression);

    private void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        var memberAccess = (MemberAccessExpressionSyntax)context.Node;
        if (memberAccess.Parent is not InvocationExpressionSyntax invocation)
            return;

        var state = new FluentState(
            semanticModel, CSharpSyntaxFacts.Instance, invocation, valuePattern: default, initializedSymbol: null);

        // We want to analyze and report on the highest applicable invocation in an invocation chain.
        // So bail out if our parent is a match.
        if (invocation.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax parentInvocation } parentMemberAccess &&
            IsMatch(state, parentMemberAccess, parentInvocation, allowLinq: true, matchesInReverse: null, out _, cancellationToken))
        {
            return;
        }

        var sourceText = semanticModel.SyntaxTree.GetText(cancellationToken);
        var allowSemanticsChange = option.Value is CollectionExpressionPreference.WhenTypesLooselyMatch;
        var analysisResult = AnalyzeInvocation(sourceText, state, invocation, expressionType, allowSemanticsChange, addMatches: true, cancellationToken);
        if (analysisResult is null)
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: [invocation.GetLocation()],
            properties: analysisResult.Value.ChangesSemantics ? ChangesSemantics : null));

        return;
    }

    /// <summary>
    /// Analyzes an expression looking for one of the form <c>CollectionCreation</c>, followed by some number of 
    /// <c>.Add(...)/.AddRange(...)</c> or <c>.ToXXX()</c> calls
    /// </summary>
    public static AnalysisResult? AnalyzeInvocation(
        SourceText text,
        FluentState state,
        InvocationExpressionSyntax invocation,
        INamedTypeSymbol? expressionType,
        bool allowSemanticsChange,
        bool addMatches,
        CancellationToken cancellationToken)
    {
        // Because we're recursing from top to bottom in the expression tree, we build up the matches in reverse.  Right
        // before returning them, we'll reverse them again to get the proper order.
        using var _1 = ArrayBuilder<CollectionMatch<ArgumentSyntax>>.GetInstance(out var preMatchesInReverse);
        using var _2 = ArrayBuilder<CollectionMatch<ArgumentSyntax>>.GetInstance(out var postMatchesInReverse);
        if (!AnalyzeInvocation(
                text, state, invocation,
                addMatches ? preMatchesInReverse : null,
                addMatches ? postMatchesInReverse : null,
                out var existingInitializer, cancellationToken))
        {
            return null;
        }

        if (!CanReplaceWithCollectionExpression(
                state.SemanticModel, invocation, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out var changesSemantics))
        {
            return null;
        }

        preMatchesInReverse.ReverseContents();
        postMatchesInReverse.ReverseContents();
        return new AnalysisResult(existingInitializer, invocation, preMatchesInReverse.ToImmutable(), postMatchesInReverse.ToImmutable(), changesSemantics);
    }

    private static bool AnalyzeInvocation(
        SourceText text,
        FluentState state,
        InvocationExpressionSyntax invocation,
        ArrayBuilder<CollectionMatch<ArgumentSyntax>>? preMatchesInReverse,
        ArrayBuilder<CollectionMatch<ArgumentSyntax>>? postMatchesInReverse,
        out InitializerExpressionSyntax? existingInitializer,
        CancellationToken cancellationToken)
    {
        var semanticModel = state.SemanticModel;
        var compilation = semanticModel.Compilation;

        existingInitializer = null;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        // Topmost invocation must be a syntactic match for one of our collection manipulation forms.  At the top level
        // we don't want to end with a linq method as that would be lazy, and a collection expression will eagerly
        // realize the collection.
        if (!IsMatch(state, memberAccess, invocation, allowLinq: false, postMatchesInReverse, out var isAdditionMatch, cancellationToken))
            return false;

        // We don't want to offer this feature on top of some builder-type.  They will commonly end with something like
        // `builder.ToImmutable()`.  We want that case to be handled by the 'ForBuilder' analyzer instead.
        var expressionType = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        if (expressionType is null || expressionType.Name.EndsWith("Builder", StringComparison.Ordinal))
            return false;

        var ienumerableOfTType = compilation.IEnumerableOfTType();

        var current = memberAccess.Expression;
        var copiedData = false;

        // Methods of the form Add(...)/AddRange(...) or `ToXXX()` count as something to continue recursing down the
        // left hand side of the expression.  In the inner expressions we can have things like `.Concat/.Append`
        // calls as the outer expressions will realize the collection.
        while (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax currentMemberAccess } currentInvocation &&
            IsMatch(state, currentMemberAccess, currentInvocation, allowLinq: true, postMatchesInReverse, out _, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            copiedData = true;
            current = currentMemberAccess.Expression;
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
                    Initializer: null or { RawKind: (int)SyntaxKind.CollectionInitializerExpression }
                })
            {
                return false;
            }

            existingInitializer = objectCreation.Initializer;

            if (!IsLegalInitializer(objectCreation.Initializer))
                return false;

            if (!IsListLike(current))
                return false;

            if (objectCreation.ArgumentList is { Arguments.Count: 1 })
            {
                // Can take a single argument if that argument is itself a collection.
                var argumentType = semanticModel.GetTypeInfo(objectCreation.ArgumentList.Arguments[0].Expression, cancellationToken).Type;
                if (argumentType is null)
                    return false;

                if (!Equals(argumentType.OriginalDefinition, ienumerableOfTType) &&
                    !argumentType.AllInterfaces.Any(i => Equals(i.OriginalDefinition, ienumerableOfTType)))
                {
                    return false;
                }

                // Add the arguments to the pre-matches.  They will execute before the initializer values are added.
                AddArgumentsInReverse(preMatchesInReverse, objectCreation.ArgumentList.Arguments, useSpread: true);
                return true;
            }
            else if (objectCreation.ArgumentList is null or { Arguments.Count: 0 })
            {
                // Otherwise, we have to have an empty argument list.
                return true;
            }

            return false;
        }

        // Forms like `ImmutableArray.Create(...)` or `ImmutableArray.CreateRange(...)` are fine base cases.
        if (current is InvocationExpressionSyntax currentInvocationExpression &&
            IsCollectionFactoryCreate(semanticModel, currentInvocationExpression, out var factoryMemberAccess, out var unwrapArgument, out var useSpread, cancellationToken))
        {
            if (!IsListLike(current))
                return false;

            AddArgumentsInReverse(postMatchesInReverse, GetArguments(currentInvocationExpression.ArgumentList, unwrapArgument), useSpread);
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
        if (!isAdditionMatch && IsIterable(semanticModel, current, cancellationToken))
        {
            AddFinalMatch(current);
            return true;
        }

        // Something we didn't understand.
        return false;

        void AddFinalMatch(ExpressionSyntax expression)
        {
            if (postMatchesInReverse is null)
                return;

            // We're only adding one item to the final collection.  So we're ending up with `[.. <expr>]`.  If this
            // originally was wrapped over multiple lines in a fluent fashion, and we're down to just a single wrapped
            // line, then unwrap.
            if (postMatchesInReverse.Count == 0 &&
                expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } innerInvocation &&
                text.Lines.GetLineFromPosition(expression.SpanStart).LineNumber + 1 == text.Lines.GetLineFromPosition(expression.Span.End).LineNumber &&
                memberAccess.Expression.GetTrailingTrivia()
                    .Concat(memberAccess.OperatorToken.GetAllTrivia())
                    .Concat(memberAccess.Name.GetLeadingTrivia())
                    .All(static t => t.IsWhitespaceOrEndOfLine()))
            {
                // Remove any whitespace around the `.`, making the singly-wrapped fluent expression into a single line.
                postMatchesInReverse.Add(new CollectionMatch<ArgumentSyntax>(
                    Argument(innerInvocation.WithExpression(
                        memberAccess.Update(
                            memberAccess.Expression.WithoutTrailingTrivia(),
                            memberAccess.OperatorToken.WithoutTrivia(),
                            memberAccess.Name.WithoutLeadingTrivia()))),
                    UseSpread: true));
                return;
            }

            postMatchesInReverse.Add(new CollectionMatch<ArgumentSyntax>(Argument(expression), UseSpread: true));
        }

        // We only want to offer this feature when the original collection was list-like (as opposed to being something
        // like a hash-set).  For example: `new List<int> { x, y, z }.ToImmutableArray()` produces different results
        // than `new HashSet<int> { x, y, z }.ToImmutableArray()` in the presence of duplicates.
        bool IsListLike(ExpressionSyntax expression)
        {
            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (type is null or IErrorTypeSymbol)
                return false;

            return
                EqualsOrImplements(type, compilation.IListOfTType()) ||
                EqualsOrImplements(type, compilation.IReadOnlyListOfTType());
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
        ArrayBuilder<CollectionMatch<ArgumentSyntax>>? matchesInReverse,
        SeparatedSyntaxList<ArgumentSyntax> arguments,
        bool useSpread)
    {
        Contract.ThrowIfTrue(useSpread && arguments.Count != 1);

        if (matchesInReverse is null)
            return;

        for (var i = arguments.Count - 1; i >= 0; i--)
            matchesInReverse.Add(new(arguments[i], useSpread));
    }

    /// <summary>
    /// Tests if this single `expr.SomeInvocation(...)` syntactically matches one of the allowed forms
    /// (ToList/AsSpan/etc.).  That includes that the arguments present to the invocation are acceptable for that
    /// particular method call.  If <paramref name="matchesInReverse"/> is provided, the arguments to the method will be
    /// appropriately extracted so that they can be placed in the final collection expression.
    /// </summary>
    private static bool IsMatch(
        FluentState state,
        MemberAccessExpressionSyntax memberAccess,
        InvocationExpressionSyntax invocation,
        bool allowLinq,
        ArrayBuilder<CollectionMatch<ArgumentSyntax>>? matchesInReverse,
        out bool isAdditionMatch,
        CancellationToken cancellationToken)
    {
        // Check for syntactic match first.
        if (!IsMatchWorker(out isAdditionMatch))
            return false;

        // Check to make sure we're not calling something banned because it would change semantics. First check if the
        // method itself comes from a banned type (like with an extension method).
        var member = state.SemanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
        if (BannedTypes.Contains(member?.ContainingType.Name))
            return false;

        // Next, check if we're invoking this on a banned type.
        var type = state.SemanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        if (BannedTypes.Contains(type?.Name))
            return false;

        return true;

        bool IsMatchWorker(out bool isAdditionMatch)
        {
            isAdditionMatch = false;
            if (memberAccess.Kind() != SyntaxKind.SimpleMemberAccessExpression)
                return false;

            var name = memberAccess.Name.Identifier.ValueText;

            // Check for Add/AddRange/Concat
            if (state.TryAnalyzeInvocationForCollectionExpression(invocation, allowLinq, cancellationToken, out _, out var useSpread))
            {
                AddArgumentsInReverse(matchesInReverse, invocation.ArgumentList.Arguments, useSpread);

                isAdditionMatch = true;
                return true;
            }

            // Now check for ToXXX/AsXXX.  All of these need no args.
            if (invocation.ArgumentList.Arguments.Count > 0)
                return false;

            return IsAnyNameMatch(name);
        }

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
    /// <param name="PreMatches">The arguments being added to the collection that will be converted into elements in
    /// the final collection expression *before* the existing initializer elements.</param>
    /// <param name="PostMatches">The arguments being added to the collection that will be converted into elements in
    /// the final collection expression *after* the existing initializer elements.</param>
    public readonly record struct AnalysisResult(
        // Location DiagnosticLocation,
        InitializerExpressionSyntax? ExistingInitializer,
        InvocationExpressionSyntax CreationExpression,
        ImmutableArray<CollectionMatch<ArgumentSyntax>> PreMatches,
        ImmutableArray<CollectionMatch<ArgumentSyntax>> PostMatches,
        bool ChangesSemantics);
}
