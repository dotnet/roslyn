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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;
using static SyntaxFactory;
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
        nameof(Span<int>),
        nameof(ReadOnlySpan<int>),
        nameof(System.Collections.Generic.List<int>),
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
        nameof(System.Collections.Immutable),
    ];

    /// <summary>
    /// Set of type-names that are blocked from moving over to collection expressions because the semantics of them are
    /// known to be specialized, and thus could change semantics in undesirable ways if the compiler emitted its own
    /// code as an replacement.
    /// </summary>
    private static readonly ImmutableHashSet<string?> s_bannedTypes = [
        nameof(ParallelEnumerable),
        nameof(ParallelQuery),
        // Special internal runtime interface that is optimized for fast path conversions of collections.
        "IIListProvider"];

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
            additionalLocations: ImmutableArray.Create(invocation.GetLocation()),
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
        using var _ = ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>.GetInstance(out var matchesInReverse);
        if (!AnalyzeInvocation(text, state, invocation, addMatches ? matchesInReverse : null, out var existingInitializer, cancellationToken))
            return null;

        if (!CanReplaceWithCollectionExpression(
                state.SemanticModel, invocation, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out var changesSemantics))
        {
            return null;
        }

        matchesInReverse.ReverseContents();
        return new AnalysisResult(existingInitializer, invocation, matchesInReverse.ToImmutable(), changesSemantics);
    }

    private static bool AnalyzeInvocation(
        SourceText text,
        FluentState state,
        InvocationExpressionSyntax invocation,
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>? matchesInReverse,
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
        if (!IsMatch(state, memberAccess, invocation, allowLinq: false, matchesInReverse, out var isAdditionMatch, cancellationToken))
            return false;

        // We don't want to offer this feature on top of some builder-type.  They will commonly end with something like
        // `builder.ToImmutable()`.  We want that case to be handled by the 'ForBuilder' analyzer instead.
        var expressionType = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        if (expressionType is null || expressionType.Name.EndsWith("Builder", StringComparison.Ordinal))
            return false;

        var ienumerableOfTType = compilation.IEnumerableOfTType();

        using var _1 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var stack);
        stack.Push(memberAccess.Expression);

        var copiedData = false;

        while (stack.TryPop(out var current))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Methods of the form Add(...)/AddRange(...) or `ToXXX()` count as something to continue recursing down the
            // left hand side of the expression.  In the inner expressions we can have things like `.Concat/.Append`
            // calls as the outer expressions will realize the collection.
            if (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax currentMemberAccess } currentInvocation &&
                IsMatch(state, currentMemberAccess, currentInvocation, allowLinq: true, matchesInReverse, out _, cancellationToken))
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
                        Initializer: null or { RawKind: (int)SyntaxKind.CollectionInitializerExpression }
                    })
                {
                    return false;
                }

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

                    if (!argumentType.AllInterfaces.Any(i => Equals(i.OriginalDefinition, ienumerableOfTType)))
                        return false;

                    if (matchesInReverse != null)
                    {
                        // Need to push any initializer values to the matchesInReverse first, as they need to execute prior
                        // to the argument to the object creation itself executing.

                        if (objectCreation.Initializer != null)
                            AddArgumentsInReverse(matchesInReverse, ArgumentList(SeparatedList(objectCreation.Initializer.Expressions.Select(Argument))).Arguments, useSpread: false);

                        AddArgumentsInReverse(matchesInReverse, objectCreation.ArgumentList.Arguments, useSpread: true);
                    }

                    return true;
                }
                else if (objectCreation.ArgumentList is null or { Arguments.Count: 0 })
                {
                    // Otherwise, we have to have an empty argument list.
                    existingInitializer = objectCreation.Initializer;
                    return true;
                }

                return false;
            }

            // Forms like `ImmutableArray.Create(...)` or `ImmutableArray.CreateRange(...)` are fine base cases.
            if (current is InvocationExpressionSyntax currentInvocationExpression &&
                IsCollectionFactoryCreate(semanticModel, currentInvocationExpression, out var factoryMemberAccess, out var unwrapArgument, cancellationToken))
            {
                if (!IsListLike(current))
                    return false;

                AddArgumentsInReverse(matchesInReverse, GetArguments(currentInvocationExpression, unwrapArgument), useSpread: false);
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
                AddFinalMatch(current);
                return true;
            }

            // Something we didn't understand.
            return false;
        }

        return false;

        void AddFinalMatch(ExpressionSyntax expression)
        {
            if (matchesInReverse is null)
                return;

            // We're only adding one item to the final collection.  So we're ending up with `[.. <expr>]`.  If this
            // originally was wrapped over multiple lines in a fluent fashion, and we're down to just a single wrapped
            // line, then unwrap.
            if (matchesInReverse.Count == 0 &&
                expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } innerInvocation &&
                text.Lines.GetLineFromPosition(expression.SpanStart).LineNumber + 1 == text.Lines.GetLineFromPosition(expression.Span.End).LineNumber &&
                memberAccess.Expression.GetTrailingTrivia()
                    .Concat(memberAccess.OperatorToken.GetAllTrivia())
                    .Concat(memberAccess.Name.GetLeadingTrivia())
                    .All(static t => t.IsWhitespaceOrEndOfLine()))
            {
                // Remove any whitespace around the `.`, making the singly-wrapped fluent expression into a single line.
                matchesInReverse.Add(new CollectionExpressionMatch<ArgumentSyntax>(
                    Argument(innerInvocation.WithExpression(
                        memberAccess.Update(
                            memberAccess.Expression.WithoutTrailingTrivia(),
                            memberAccess.OperatorToken.WithoutTrivia(),
                            memberAccess.Name.WithoutLeadingTrivia()))),
                    UseSpread: true));
                return;
            }

            matchesInReverse.Add(new CollectionExpressionMatch<ArgumentSyntax>(Argument(expression), UseSpread: true));
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
                Implements(type, compilation.IListOfTType()) ||
                Implements(type, compilation.IReadOnlyListOfTType());
        }

        bool IsIterable(ExpressionSyntax expression)
        {
            var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (type is null or IErrorTypeSymbol)
                return false;

            if (s_bannedTypes.Contains(type.Name))
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
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>? matchesInReverse,
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
        ArrayBuilder<CollectionExpressionMatch<ArgumentSyntax>>? matchesInReverse,
        out bool isAdditionMatch,
        CancellationToken cancellationToken)
    {
        // Check for syntactic match first.
        if (!IsMatchWorker(out isAdditionMatch))
            return false;

        // Check to make sure we're not calling something banned because it would change semantics. First check if the
        // method itself comes from a banned type (like with an extension method).
        var member = state.SemanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
        if (s_bannedTypes.Contains(member?.ContainingType.Name))
            return false;

        // Next, check if we're invoking this on a banned type.
        var type = state.SemanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
        if (s_bannedTypes.Contains(type?.Name))
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
    /// <param name="Matches">The arguments being added to the collection that will be converted into elements in the
    /// final collection expression.</param>
    public readonly record struct AnalysisResult(
        // Location DiagnosticLocation,
        InitializerExpressionSyntax? ExistingInitializer,
        InvocationExpressionSyntax CreationExpression,
        ImmutableArray<CollectionExpressionMatch<ArgumentSyntax>> Matches,
        bool ChangesSemantics);
}
