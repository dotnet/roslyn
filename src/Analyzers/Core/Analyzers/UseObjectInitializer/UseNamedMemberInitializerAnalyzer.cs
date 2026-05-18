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
        var state = TryInitializeState(semanticModel, syntaxFacts, objectCreationExpression, cancellationToken);

        // If we didn't find something we're assigned to, then we can't continue.  
        if (state.ValuePattern == default)
            return default;

        this.Initialize(state, objectCreationExpression);
        return this.AnalyzeWorker(cancellationToken).PostMatches;
    }

    protected sealed override bool ShouldAnalyze(CancellationToken cancellationToken)
    {
        // Can't add member initializers if the object already has a collection initializer attached to it.
        return !this.SyntaxFacts.IsObjectCollectionInitializer(this.SyntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression));
    }

    /// <summary>
    /// Returns true if the language (and its current version) admits compound assignment forms
    /// (`+=`, `-=`, `??=`, …) as <em>member initializers</em> in object/<c>with</c> initializers.
    /// When true, a subsequent compound expression-statement targeting the initialized object is a
    /// candidate for being folded into the initializer; when false, only `=` statements qualify.
    /// </summary>
    protected abstract bool SupportsCompoundAssignmentInInitializer(ParseOptions options);

    /// <summary>
    /// Returns true if the language (and its current version) admits the mixed object/collection
    /// initializer form (dotnet/csharplang#10185), where a `{ ... }` initializer may contain both
    /// member-shape assignments and bare-expression element initializers in the same list. When
    /// true, a subsequent `instance.Add(value)` expression-statement targeting the initialized
    /// object is a candidate for being folded into the initializer as an element initializer.
    /// </summary>
    protected abstract bool SupportsMixedObjectAndCollectionInitializers(ParseOptions options);

    protected sealed override bool TryAddMatches(
        ArrayBuilder<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> preMatches,
        ArrayBuilder<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> postMatches,
        out bool changesSemantics,
        CancellationToken cancellationToken)
    {
        changesSemantics = false;

        // Per-name state for repeat-target handling. `target = { ... }` is exclusive (no further
        // initializer for that target is permitted, per the spec's nested-init exclusivity rule);
        // every other shape (`=` with non-init RHS, `+=`, `-=`, `??=`, event `+=`/`-=`, …) is
        // "set but not exclusive" and admits one or more compound follow-ups (`= → +=`, `+= → +=`,
        // event `+= h1 → += h2`, etc.). It does NOT admit a follow-up `=` (would either duplicate
        // an `=` or violate the "= before any compound" ordering rule).
        using var _1 = PooledDictionary<string, bool>.GetInstance(out var seenNames);

        var initializer = this.SyntaxFacts.GetInitializerOfBaseObjectCreationExpression(_objectCreationExpression);
        if (initializer != null)
        {
            foreach (var init in this.SyntaxFacts.GetInitializersOfObjectMemberInitializer(initializer))
            {
                if (this.SyntaxFacts.IsNamedMemberInitializer(init))
                {
                    this.SyntaxFacts.GetPartsOfNamedMemberInitializer(init, out var name, out var rhs);
                    var nameText = this.SyntaxFacts.GetIdentifierOfIdentifierName(name).ValueText;
                    var isExclusiveNested = this.SyntaxFacts.IsObjectMemberInitializer(rhs)
                        || this.SyntaxFacts.IsObjectCollectionInitializer(rhs);
                    seenNames[nameText] = isExclusiveNested;
                }
            }
        }

        var supportsCompound = this.SupportsCompoundAssignmentInInitializer(_objectCreationExpression.SyntaxTree.Options);

        // Mixed object/collection initializer (dotnet/csharplang#10185) requires the same
        // precondition as IDE0028's pure-collection fold: the target type must implement
        // IEnumerable. Without it, the synthesized `new C { X = 1, 10 }` would fail to bind
        // (ERR_CollectionInitRequiresIEnumerable from the binder). Computed once per analysis,
        // gated behind the language-version check so we never pay for the symbol lookup on
        // languages that can't take the Add-fold anyway.
        var supportsMixed =
            this.SupportsMixedObjectAndCollectionInitializers(_objectCreationExpression.SyntaxTree.Options) &&
            TargetImplementsIEnumerable(cancellationToken);

        foreach (var subsequentStatement in this.State.GetSubsequentStatements())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Mixed object/collection initializer (dotnet/csharplang#10185): a subsequent
            // `instance.Add(value)` expression-statement is foldable as an element initializer
            // inside the same `{ ... }`. Member-shape statements continue through the existing
            // assignment path below; the duplicate-target rules don't apply to element
            // initializers (multiple Add calls are always permitted), so the Add-fold doesn't
            // touch the `seenNames` map.
            if (supportsMixed && TryMatchAddInvocation(subsequentStatement, cancellationToken) is { } addMatch)
            {
                postMatches.Add(addMatch);
                continue;
            }

            if (subsequentStatement is not TAssignmentStatementSyntax statement)
                break;

            // Subsequent compound expression-statements (`c.x += 1;`) are foldable only on languages
            // that admit compound member initializers; otherwise the produced initializer would be
            // syntactically invalid for the target language version.
            var matches = supportsCompound
                ? this.SyntaxFacts.IsAnyAssignmentStatement(statement)
                : this.SyntaxFacts.IsSimpleAssignmentStatement(statement);
            if (!matches)
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
            // For repeat-target folds: a subsequent `=` is never foldable (would either duplicate
            // an `=` or violate the "= before any compound" ordering rule), and a subsequent
            // compound (`+=`, `??=`, event `+=`/`-=`, …) is foldable iff the prior occurrence is
            // not the exclusive `target = { ... }` form. Subsequent statements never produce a
            // nested `= { ... }` (it isn't a statement form), so newly-folded entries are always
            // recorded as non-exclusive.
            var name = this.SyntaxFacts.GetNameOfMemberAccessExpression(leftMemberAccess);
            var identifier = this.SyntaxFacts.GetIdentifierOfSimpleName(name);

            if (seenNames.TryGetValue(identifier.ValueText, out var priorIsExclusiveNested))
            {
                var subsequentIsCompound = !this.SyntaxFacts.IsSimpleAssignmentStatement(statement);
                if (priorIsExclusiveNested || !subsequentIsCompound)
                    break;
            }

            seenNames[identifier.ValueText] = false;

            postMatches.Add(new Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>(
                statement, leftMemberAccess, rightExpression, typeMember?.Name ?? identifier.ValueText));
        }

        return true;
    }

    private bool TargetImplementsIEnumerable(CancellationToken cancellationToken)
    {
        var enumerableType = this.SemanticModel.Compilation.IEnumerableType();
        if (enumerableType is null)
            return false;

        var type = this.SemanticModel.GetTypeInfo(_objectCreationExpression, cancellationToken).Type;
        return type is not null && type.AllInterfaces.Contains(enumerableType);
    }

    /// <summary>
    /// Single-argument <c>instance.Add(value)</c> match for the mixed object/collection initializer
    /// feature (dotnet/csharplang#10185). Multi-argument Add (which would correspond to the
    /// <c>{ a, b }</c> brace-list element-initializer shape) is intentionally not folded by this
    /// pass — a later extension can add it if needed. Named arguments (<c>Add(item: v)</c>),
    /// <c>ref</c>/<c>out</c>/<c>in</c> arguments, and non-member-access invocations are rejected
    /// earlier inside <see cref="UpdateExpressionState{TExpressionSyntax, TStatementSyntax}.TryAnalyzeAddInvocation"/>
    /// via the <c>IsSimpleArgument</c> / <c>IsSimpleMemberAccessExpression</c> gates.
    /// </summary>
    private Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>? TryMatchAddInvocation(
        TStatementSyntax subsequentStatement,
        CancellationToken cancellationToken)
    {
        // The Add-fold only applies to expression-statement-wrapped invocations; the pattern
        // match below bails before any invocation-shape helper is invoked on shapes (assignment
        // statements, block statements, etc.) that don't carry an inner expression in the right
        // place. In C#, `TAssignmentStatementSyntax` resolves to `ExpressionStatementSyntax`,
        // which is the parent of both `x = 1;` and `x.Add(1);`; in VB the type resolves to the
        // narrower `AssignmentStatementSyntax`, which never wraps an invocation — but VB always
        // returns false from `SupportsMixedObjectAndCollectionInitializers`, so this helper is
        // unreachable from VB.
        if (subsequentStatement is not TAssignmentStatementSyntax expressionStatement)
            return null;

        if (this.SyntaxFacts.GetExpressionOfExpressionStatement(expressionStatement) is not TExpressionSyntax invocation)
            return null;

        if (!this.SyntaxFacts.IsInvocationExpression(invocation))
            return null;

        if (!this.State.TryAnalyzeAddInvocation(
                invocation,
                requiredArgumentName: null,
                forCollectionExpression: false,
                cancellationToken,
                out var instance,
                out _))
        {
            return null;
        }

        if (!this.State.ValuePatternMatches(instance))
            return null;

        if (this.SyntaxFacts.GetExpressionOfInvocationExpression(invocation) is not TMemberAccessExpressionSyntax memberAccess)
            return null;

        // Reject multi-argument Add here; brace-list (`{ a, b }`) element-initializer synthesis
        // is intentionally out of scope for this pass. (Named/ref/out args are already rejected
        // by `IsSimpleArgument` inside `TryAnalyzeAddInvocation`, so reaching this point with
        // `arguments.Count != 1` always means an additional positional argument.)
        var arguments = this.SyntaxFacts.GetArgumentsOfInvocationExpression(invocation);
        if (arguments.Count != 1)
            return null;

        var argument = (TExpressionSyntax)this.SyntaxFacts.GetExpressionOfArgument(arguments[0]);

        // Folding would change semantics if the argument expression refers to the value being
        // initialized — the initializer body runs before the user's `var x = ...` binding is
        // observable to subsequent code.
        if (this.State.NodeContainsValuePatternOrReferencesInitializedSymbol(argument, cancellationToken))
            return null;

        if (ImplicitMemberAccessWouldBeAffected(argument))
            return null;

        return new Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>(
            expressionStatement,
            memberAccess,
            argument,
            memberName: string.Empty,
            isAddInvocation: true);
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
                if (child.AsNode(out var childNode) &&
                    ImplicitMemberAccessWouldBeAffected(childNode))
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

/// <summary>
/// A statement matched by IDE0017 that is a candidate for folding into the initializer of the
/// enclosing object creation. Two shapes are supported:
/// <list type="bullet">
/// <item><description>
/// A member-initializer match (the existing IDE0017 shape): an assignment statement
/// <c>x.Member = value</c> (or any compound form, when
/// <see cref="AbstractUseNamedMemberInitializerAnalyzer{TExpressionSyntax,TStatementSyntax,TObjectCreationExpressionSyntax,TMemberAccessExpressionSyntax,TAssignmentStatementSyntax,TLocalDeclarationStatementSyntax,TVariableDeclaratorSyntax,TAnalyzer}.SupportsCompoundAssignmentInInitializer"/>
/// is true). <see cref="IsAddInvocation"/> is <see langword="false"/>; the fixer emits
/// <c>MemberName = Initializer</c> (or the matching compound form) inside the resulting initializer.
/// </description></item>
/// <item><description>
/// An element-initializer match (added by the mixed object/collection initializer feature,
/// dotnet/csharplang#10185): an expression statement <c>x.Add(value)</c>.
/// <see cref="IsAddInvocation"/> is <see langword="true"/> and the fixer emits
/// <see cref="Initializer"/> as a bare element initializer. <see cref="MemberName"/> is set to
/// the empty string (Add-invocation matches never participate in the duplicate-target rules).
/// </description></item>
/// </list>
/// </summary>
internal readonly struct Match<
    TExpressionSyntax,
    TStatementSyntax,
    TMemberAccessExpressionSyntax,
    TAssignmentStatementSyntax>(
    TAssignmentStatementSyntax statement,
    TMemberAccessExpressionSyntax memberAccessExpression,
    TExpressionSyntax initializer,
    string memberName,
    bool isAddInvocation = false)
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TAssignmentStatementSyntax : TStatementSyntax
{
    public readonly TAssignmentStatementSyntax Statement = statement;
    public readonly TMemberAccessExpressionSyntax MemberAccessExpression = memberAccessExpression;
    public readonly TExpressionSyntax Initializer = initializer;
    public readonly string MemberName = memberName;
    public readonly bool IsAddInvocation = isAddInvocation;
}
