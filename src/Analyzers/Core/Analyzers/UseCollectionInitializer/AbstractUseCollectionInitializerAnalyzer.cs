// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionExpression;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

/// <summary>
/// Pass 3 of the IDE0017+IDE0028 unification: this single walk produces matches for every
/// foldable kind — member assignments, <c>Add</c> / <c>AddRange</c> invocations, indexer
/// assignments, and collection-expression-only shapes (<c>foreach</c> spreads). Replaces the
/// formerly-separate <c>AbstractUseNamedMemberInitializerAnalyzer</c>. Shape-lock applies
/// for languages that don't support the mixed object/collection initializer feature
/// (csharplang#10185): the first match's kind locks the rest of the walk to the same
/// "family" (member-init OR collection-element), so legacy IDE0017 / IDE0028 behavior on
/// pre-Preview C# and on VB is preserved exactly.
/// </summary>
internal abstract class AbstractUseCollectionInitializerAnalyzer<
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TInvocationExpressionSyntax,
    TExpressionStatementSyntax,
    TAssignmentStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer> : AbstractObjectCreationExpressionAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        InitializerMatch<SyntaxNode>, TAnalyzer>
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TExpressionStatementSyntax : TStatementSyntax
    where TAssignmentStatementSyntax : TStatementSyntax
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractUseCollectionInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
        TAssignmentStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax,
        TAnalyzer>, new()
{
    protected bool _analyzeForCollectionExpression;

    protected abstract bool IsComplexElementInitializer(SyntaxNode expression, out int initializerElementCount);

    protected abstract bool HasExistingInvalidInitializerForCollection();
    protected abstract bool AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
        ArrayBuilder<InitializerMatch<SyntaxNode>> preMatches,
        ArrayBuilder<InitializerMatch<SyntaxNode>> postMatches,
        out bool mayChangeSemantics,
        CancellationToken cancellationToken);

    protected abstract IUpdateExpressionSyntaxHelper<TExpressionSyntax, TStatementSyntax> SyntaxHelper { get; }

    /// <summary>
    /// Returns true when the language (and the target's parse options) admit compound
    /// assignment forms (<c>+=</c>, <c>-=</c>, <c>??=</c>, …) as <em>member initializers</em>
    /// inside an object initializer body. Member-init folding under this walk uses the same
    /// gate the legacy IDE0017 walk used.
    /// </summary>
    protected abstract bool SupportsCompoundAssignmentInInitializer(ParseOptions options);

    /// <summary>
    /// Returns true when the language (and the target's parse options) admit the mixed
    /// object/collection initializer feature (csharplang#10185). Under that feature a single
    /// <c>{ … }</c> initializer may contain both member-init and Add-element children; the
    /// walk relaxes its per-statement shape-lock so member and Add matches can interleave.
    /// On VB and on pre-Preview C# this returns false and the walk falls back to the legacy
    /// rule that a single object creation folds into <em>either</em> member-init or
    /// collection-element form, never both.
    /// </summary>
    protected abstract bool SupportsMixedObjectAndCollectionInitializers(ParseOptions options);

    protected override void Clear()
    {
        base.Clear();
        _analyzeForCollectionExpression = false;
    }

    public AnalysisResult Analyze(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TObjectCreationExpressionSyntax objectCreationExpression,
        bool analyzeForCollectionExpression,
        CancellationToken cancellationToken)
    {
        _analyzeForCollectionExpression = analyzeForCollectionExpression;
        var state = TryInitializeState(semanticModel, syntaxFacts, objectCreationExpression, cancellationToken);

        // If we didn't find something we're assigned to, then we normally can't continue.  However, we always support
        // converting a `new List<int>()` collection over to a collection expression.  We just won't analyze later
        // statements.  
        if (state.ValuePattern == default && !analyzeForCollectionExpression)
            return default;

        this.Initialize(state, objectCreationExpression);
        var (preMatches, postMatches, mayChangeSemantics) = this.AnalyzeWorker(cancellationToken);

        // If analysis failed entirely, immediately bail out.
        if (preMatches.IsDefault || postMatches.IsDefault)
            return default;

        // Analysis succeeded, but the result may be empty or non empty.
        //
        // For collection expressions, it's fine for this result to be empty.  In other words, it's ok to offer
        // changing `new List<int>() { 1 }` (on its own) to `[1]`.
        //
        // However, for collection initializers we always want at least one element to add to the initializer.  In
        // other words, we don't want to suggest changing `new List<int>()` to `new List<int>() { }` as that's just
        // noise.  So convert empty results to an invalid result here.
        if (analyzeForCollectionExpression)
            return new(preMatches, postMatches, mayChangeSemantics);

        // Downgrade an empty result to a failure for the normal collection-initializer case.
        return postMatches.IsEmpty ? default : new(preMatches, postMatches, mayChangeSemantics);
    }

    protected sealed override bool TryAddMatches(
        ArrayBuilder<InitializerMatch<SyntaxNode>> preMatches,
        ArrayBuilder<InitializerMatch<SyntaxNode>> postMatches,
        out bool mayChangeSemantics,
        CancellationToken cancellationToken)
    {
        mayChangeSemantics = false;
        var seenInvocation = false;
        var seenIndexAssignment = false;
        var seenMemberInit = false;

        // Per-name state for the member-init duplicate-target rule. `target = { ... }` is
        // exclusive (no further initializer for that target is permitted, per the spec's
        // nested-init exclusivity rule); every other shape (`=` with non-init RHS, `+=`,
        // `-=`, `??=`, event `+=`/`-=`, …) is "set but not exclusive" and admits one or more
        // compound follow-ups. Pre-Pass-3 this lived in the deleted
        // `AbstractUseNamedMemberInitializerAnalyzer` walk.
        using var _1 = PooledDictionary<string, bool>.GetInstance(out var seenNames);

        var initializer = this.SyntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
        if (initializer != null)
        {
            // Pre-populate the member-init duplicate-target map from the existing initializer
            // (e.g. `new C { X = 1 }`) so subsequent member-shape follow-ups respect the
            // exclusivity / ordering rules. Also surface any element-access children
            // (`{ [k] = v }`) into `seenIndexAssignment`: the pre-Pass-3 IDE0028 walk relied
            // on `HasExistingInvalidInitializerForCollection` short-circuiting `ShouldAnalyze`
            // for these shapes, but the unified walk now runs whenever EITHER form could fire
            // (member-fold may legitimately apply when collection-fold can't), and without
            // this pre-population a subsequent `c.Add(...)` would falsely match a fold that
            // can't represent the existing element-access children.
            foreach (var existingChild in this.SyntaxFacts.GetInitializersOfObjectMemberInitializer(initializer))
            {
                if (this.SyntaxFacts.IsNamedMemberInitializer(existingChild))
                {
                    this.SyntaxFacts.GetPartsOfNamedMemberInitializer(existingChild, out var name, out var rhs);
                    var nameText = this.SyntaxFacts.GetIdentifierOfIdentifierName(name).ValueText;
                    var isExclusiveNested = this.SyntaxFacts.IsObjectMemberInitializer(rhs)
                        || this.SyntaxFacts.IsObjectCollectionInitializer(rhs);
                    seenNames[nameText] = isExclusiveNested;
                    seenMemberInit = true;
                }
                else if (this.SyntaxFacts.IsElementAccessInitializer(existingChild))
                {
                    seenIndexAssignment = true;
                }
            }

            var initializerExpressions = this.SyntaxFacts.GetExpressionsOfObjectCollectionInitializer(initializer);
            if (initializerExpressions is [var firstInit, ..])
            {
                // if we have an object creation, and it *already* has an initializer in it (like `new T { { x, y } }`)
                // this can't legally become a collection expression.  Unless there are exactly two elements in the
                // initializer, and we support k:v elements.
                if (_analyzeForCollectionExpression && this.IsComplexElementInitializer(firstInit, out var initializerElementCount))
                {
                    if (initializerElementCount != 2 || !this.SyntaxFacts.SupportsKeyValuePairElement(_objectCreationExpression.SyntaxTree.Options))
                        return false;
                }

                seenIndexAssignment = this.SyntaxFacts.IsElementAccessInitializer(firstInit);
                seenInvocation = !seenIndexAssignment;

                // An indexer can't be used with a collection expression (except for dictionary expressions).  So fail
                // out immediately if we see that.
                if (_analyzeForCollectionExpression && seenIndexAssignment && !this.SyntaxFacts.SupportsKeyValuePairElement(_objectCreationExpression.SyntaxTree.Options))
                    return false;
            }
        }

        // Member-init folding is initializer-only — there's no `[…]` collection-expression
        // representation for `{ Prop = value }`, so the collection-expression mode of this
        // walk never produces member-init matches and never consults the compound/mixed
        // hooks. Both flags safely default to false in that mode.
        var supportsCompoundMemberInit = !_analyzeForCollectionExpression &&
            this.SupportsCompoundAssignmentInInitializer(_objectCreationExpression.SyntaxTree.Options);
        var supportsMixedInit = !_analyzeForCollectionExpression &&
            this.SupportsMixedObjectAndCollectionInitializers(_objectCreationExpression.SyntaxTree.Options);

        // Collection-init folding requires the target type expose a public `Add` method via
        // standard lookup; explicit-interface-implemented `Add` doesn't qualify (binding to
        // `c.Add(x)` works for the user, but the synthesized `{ x }` element initializer
        // wouldn't bind without an applicable `Add` on the type itself). Pre-Pass-3 the
        // IDE0028 walk's `ShouldAnalyze` short-circuited on `GetAddMethods().Any() == false`;
        // the unified walk now gates each per-statement Add attempt instead so the member-init
        // path can still fire on types without an accessible Add.
        var addMethodsAvailable = GetAddMethods(cancellationToken).Any();

        if (State.ValuePattern != default)
        {
            foreach (var statement in this.State.GetSubsequentStatements())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Per-statement try-order: member-init → Add/AddRange → index-assignment →
                // (collection-expression-only shapes). The shape-lock for languages that don't
                // support mixed initializers is enforced inside each helper: a member-shape
                // statement is rejected when an Add/index has already been collected and the
                // language can't mix, and vice versa. Under the mixed object/collection
                // initializer feature member-init and Add matches may interleave freely.
                var memberMatch = TryAnalyzeMemberInitStatement(
                    statement, supportsCompoundMemberInit, supportsMixedInit,
                    seenInvocation, seenIndexAssignment, seenNames, cancellationToken);
                if (memberMatch is not null)
                {
                    seenMemberInit = true;
                    postMatches.Add(memberMatch.Value);
                    continue;
                }

                // Pre-Preview / VB shape-lock: once member-init matches are committed, an
                // Add or index match here would produce an output that the language can't
                // represent (`{ X = 1, 2 }` only binds with the mixed-init feature). Bail
                // before consuming this statement so the diagnostic analyzer reports the
                // pure-member subset.
                if (seenMemberInit && !supportsMixedInit)
                    break;

                var collectionMatch = TryAnalyzeStatement(
                    statement, addMethodsAvailable, ref seenInvocation, ref seenIndexAssignment, cancellationToken);
                if (collectionMatch is null)
                    break;

                postMatches.Add(collectionMatch.Value);
            }
        }

        if (_analyzeForCollectionExpression)
        {
            return AnalyzeMatchesAndCollectionConstructorForCollectionExpression(
                preMatches, postMatches, out mayChangeSemantics, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Member-initializer match per statement (formerly in the deleted
    /// <c>AbstractUseNamedMemberInitializerAnalyzer</c>). Returns <see langword="null"/> when
    /// the statement isn't a foldable member assignment under the current language settings
    /// (e.g. compound forms are rejected pre-C#15; static targets, explicit-interface
    /// implementations, and value-pattern self-references are always rejected), when the
    /// duplicate-target rule excludes the member, or when the per-statement shape-lock for
    /// pre-mixed-init languages forbids a member follow-up after a collection element.
    /// </summary>
    private InitializerMatch<SyntaxNode>? TryAnalyzeMemberInitStatement(
        TStatementSyntax statement,
        bool supportsCompound,
        bool supportsMixed,
        bool seenInvocation,
        bool seenIndexAssignment,
        Dictionary<string, bool> seenNames,
        CancellationToken cancellationToken)
    {
        // Shape-lock: pre-mixed-init languages can't follow a collected Add or index match
        // with a member match (would produce a syntactically invalid `{ 1, X = 2 }` output).
        if (!supportsMixed && (seenInvocation || seenIndexAssignment))
            return null;

        if (statement is not TAssignmentStatementSyntax assignmentStatement)
            return null;

        // Compound forms (`+=`, `??=`, …) only fold on languages with the compound-assignment-
        // in-initializer feature; otherwise only simple `=` qualifies.
        var matchesAssignmentShape = supportsCompound
            ? this.SyntaxFacts.IsAnyAssignmentStatement(assignmentStatement)
            : this.SyntaxFacts.IsSimpleAssignmentStatement(assignmentStatement);
        if (!matchesAssignmentShape)
            return null;

        this.SyntaxFacts.GetPartsOfAssignmentStatement(
            assignmentStatement, out var left, out var right);

        if (left is not TMemberAccessExpressionSyntax leftMemberAccess ||
            !this.SyntaxFacts.IsSimpleMemberAccessExpression(leftMemberAccess))
        {
            return null;
        }

        if (this.SyntaxFacts.GetExpressionOfMemberAccessExpression(leftMemberAccess) is not TExpressionSyntax instance)
            return null;

        if (!this.State.ValuePatternMatches(instance))
            return null;

        // Static members aren't initializable through an object initializer; explicitly-
        // implemented interface members likewise. Either disqualifies the fold.
        var leftSymbol = this.SemanticModel.GetSymbolInfo(leftMemberAccess, cancellationToken).GetAnySymbol();
        if (leftSymbol?.IsStatic is true)
            return null;

        var targetType = this.SemanticModel.GetTypeInfo(_objectCreationExpression, cancellationToken).Type;
        if (targetType is null)
            return null;

        if (IsExplicitlyImplemented(targetType, leftSymbol))
            return null;

        // Reject if the RHS references the value being initialized: under `var v = new X();`
        // the `v` binding isn't observable inside the initializer body, so synthesizing
        // `new X { … = v… }` would either fail to bind or change semantics relative to the
        // original `v.Prop = v.Prop…;` pattern.
        if (this.State.NodeContainsValuePatternOrReferencesInitializedSymbol((TExpressionSyntax)right, cancellationToken))
            return null;

        // Reject if the RHS contains an implicit member-access whose target sits before the
        // object creation: moving the expression inside the initializer would silently
        // re-bind that implicit target to the new instance.
        if (ImplicitMemberAccessWouldBeAffected((TExpressionSyntax)right))
            return null;

        // Duplicate-target rule: a second `=` for the same member is always rejected (would
        // duplicate or violate `=` ordering). A compound (`+=`, etc.) is rejected when the
        // prior occurrence was the exclusive `target = { ... }` nested-init form.
        var name = this.SyntaxFacts.GetNameOfMemberAccessExpression(leftMemberAccess);
        var identifier = this.SyntaxFacts.GetIdentifierOfSimpleName(name);
        if (seenNames.TryGetValue(identifier.ValueText, out var priorIsExclusiveNested))
        {
            var subsequentIsCompound = !this.SyntaxFacts.IsSimpleAssignmentStatement(assignmentStatement);
            if (priorIsExclusiveNested || !subsequentIsCompound)
                return null;
        }

        seenNames[identifier.ValueText] = false;
        return new InitializerMatch<SyntaxNode>(
            Node: assignmentStatement,
            Kind: InitializerMatchKind.MemberInitializer);
    }

    private static bool IsExplicitlyImplemented(ITypeSymbol classOrStructType, ISymbol? member)
    {
        if (member is null || !member.ContainingType.IsInterfaceType())
            return false;

        return classOrStructType.FindImplementationForInterfaceMember(member) is IPropertySymbol
        {
            DeclaredAccessibility: Accessibility.Private,
            ExplicitInterfaceImplementations.Length: > 0,
        };
    }

    private bool ImplicitMemberAccessWouldBeAffected(SyntaxNode? node)
    {
        if (node is null)
            return false;

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (child.AsNode(out var childNode) && ImplicitMemberAccessWouldBeAffected(childNode))
                return true;
        }

        if (this.SyntaxFacts.IsSimpleMemberAccessExpression(node))
        {
            var expression = this.SyntaxFacts.GetExpressionOfMemberAccessExpression(node, allowImplicitTarget: true);
            // If the implicit target sits before the object creation, moving the node inside
            // the new initializer body silently re-binds it.
            if (expression is not null && expression.SpanStart < _objectCreationExpression.SpanStart)
                return true;
        }

        return false;
    }

    private InitializerMatch<SyntaxNode>? TryAnalyzeStatement(
        TStatementSyntax statement, bool addMethodsAvailable, ref bool seenInvocation, ref bool seenIndexAssignment, CancellationToken cancellationToken)
    {
        if (_analyzeForCollectionExpression)
        {
            // Collection-expression-mode analysis (foreach, AddRange spreads, etc.) is still
            // implemented on `UpdateExpressionState` in terms of the legacy `CollectionMatch`
            // shape; translate to the unified `InitializerMatch` shape here. Pass 3 of the
            // IDE0017+IDE0028 unification will fold this into a single walk that natively
            // produces `InitializerMatch` for all kinds.
            var collectionMatch = State.TryAnalyzeStatementForCollectionExpression(this.SyntaxHelper, statement, cancellationToken);
            if (collectionMatch is null)
                return null;

            var node = collectionMatch.Value.Node;
            // The collection-expression walk emits matches whose source node is either an
            // expression-statement (Add / AddRange invocation) or a `foreach` statement; pick
            // the discriminator based on shape so the fixer can dispatch without re-inspecting.
            var kind = node is TExpressionStatementSyntax
                ? InitializerMatchKind.AddInvocation
                : InitializerMatchKind.ForEach;
            return new InitializerMatch<SyntaxNode>(
                Node: node,
                Kind: kind,
                UseSpread: collectionMatch.Value.UseSpread,
                UseCast: collectionMatch.Value.UseCast,
                UseKeyValue: collectionMatch.Value.UseKeyValue);
        }

        return TryAnalyzeStatementForCollectionInitializer(statement, addMethodsAvailable, ref seenInvocation, ref seenIndexAssignment, cancellationToken);
    }

    private InitializerMatch<SyntaxNode>? TryAnalyzeStatementForCollectionInitializer(
        TStatementSyntax statement, bool addMethodsAvailable, ref bool seenInvocation, ref bool seenIndexAssignment, CancellationToken cancellationToken)
    {
        // At least one of these has to be false.
        Contract.ThrowIfTrue(seenInvocation && seenIndexAssignment);

        if (statement is not TExpressionStatementSyntax expressionStatement)
            return null;

        // Can't mix Adds and indexing. Add matching also requires the target type expose an
        // accessible `Add` method via standard lookup — `TryAnalyzeAddInvocation` resolves
        // through `GetSymbolInfo` which would otherwise match explicit-interface-implemented
        // `Add` calls (see the legacy `ShouldAnalyze` precondition the unified walk replaces).
        if (!seenIndexAssignment && addMethodsAvailable)
        {
            // Look for a call to Add or AddRange
            if (this.State.TryAnalyzeAddInvocation(
                    (TExpressionSyntax)this.SyntaxFacts.GetExpressionOfExpressionStatement(expressionStatement),
                    requiredArgumentName: null,
                    forCollectionExpression: false,
                    cancellationToken,
                    out var instance,
                    out var useKeyValue) &&
                this.State.ValuePatternMatches(instance))
            {
                seenInvocation = true;
                return new InitializerMatch<SyntaxNode>(
                    Node: expressionStatement,
                    Kind: InitializerMatchKind.AddInvocation,
                    UseSpread: false,
                    UseKeyValue: useKeyValue);
            }
        }

        if (!seenInvocation)
        {
            if (this.State.TryAnalyzeIndexAssignment(expressionStatement, cancellationToken, out var instance) &&
                this.State.ValuePatternMatches(instance))
            {
                seenIndexAssignment = true;
                return new InitializerMatch<SyntaxNode>(
                    Node: expressionStatement,
                    Kind: InitializerMatchKind.IndexAssignment,
                    UseSpread: false,
                    UseKeyValue: this.State.SyntaxFacts.SupportsKeyValuePairElement(statement.SyntaxTree.Options));
            }
        }

        return null;
    }

    protected sealed override bool ShouldAnalyze(CancellationToken cancellationToken)
    {
        // Pre-Pass-3 IDE0028's `ShouldAnalyze` short-circuited both before the walk and (via
        // its precondition on the collection-expression synthesis) before the
        // `CanUseCollectionExpression` check could see a problematic existing initializer
        // like `{ [k] = v }`. The unified walk produces member-init matches when the
        // language admits them, but to keep IDE0028's strict pre-checks intact this gate is
        // unchanged from the legacy behavior — pure-member-init scenarios on types without
        // an accessible `Add` continue to be handled by the unmerged
        // <see cref="AbstractUseNamedMemberInitializerAnalyzer"/> walk reachable from
        // <c>AbstractUseObjectInitializerDiagnosticAnalyzer</c>. Full walk consolidation
        // (deleting the member-init walk and routing pure-member from this analyzer too)
        // requires also tightening the collection-expression synthesis path to skip
        // problematic existing initializers; that follow-on work is intentionally not done
        // in this pass.
        if (this.HasExistingInvalidInitializerForCollection())
            return false;

        return GetAddMethods(cancellationToken).Any();
    }

    protected ImmutableArray<IMethodSymbol> GetAddMethods(CancellationToken cancellationToken)
    {
        var type = this.SemanticModel.GetTypeInfo(_objectCreationExpression, cancellationToken).Type;
        if (type == null)
            return [];

        var addMethods = this.SemanticModel.LookupSymbols(
            _objectCreationExpression.SpanStart,
            container: type,
            name: WellKnownMemberNames.CollectionInitializerAddMethodName,
            includeReducedExtensionMethods: true);
        return addMethods.SelectAsArray(s => s is IMethodSymbol { Parameters: [_, ..] }, s => (IMethodSymbol)s);
    }
}
