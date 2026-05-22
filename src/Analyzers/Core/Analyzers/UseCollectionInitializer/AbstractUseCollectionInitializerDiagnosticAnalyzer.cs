// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

internal abstract partial class AbstractUseCollectionInitializerDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TInvocationExpressionSyntax,
    TExpressionStatementSyntax,
    TAssignmentStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct
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

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
        EnforceOnBuildValues.UseCollectionInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
        EnforceOnBuildValues.UseCollectionInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    // Pass 3b of the IDE0017+IDE0028 unification: this analyzer now also reports the
    // IDE0017 (UseObjectInitializer) and IDE0400 (UseMixedObjectAndCollectionInitializer)
    // diagnostics, replacing the deleted `AbstractUseObjectInitializerDiagnosticAnalyzer`.
    // The routing in `AnalyzeNode` picks the right descriptor based on the kinds of matches
    // the unified walk produces (member-init / Add / index / spread).

    private static readonly DiagnosticDescriptor s_objectInitDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
        EnforceOnBuildValues.UseObjectInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_object_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Object_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_objectInitUnnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
        EnforceOnBuildValues.UseObjectInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_object_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Object_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    private static readonly DiagnosticDescriptor s_mixedDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseMixedObjectAndCollectionInitializerDiagnosticId,
        EnforceOnBuildValues.UseMixedObjectAndCollectionInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Use_mixed_object_and_collection_initializer), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Object_and_collection_initialization_can_be_merged), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_mixedUnnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseMixedObjectAndCollectionInitializerDiagnosticId,
        EnforceOnBuildValues.UseMixedObjectAndCollectionInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Use_mixed_object_and_collection_initializer), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Object_and_collection_initialization_can_be_merged), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    protected AbstractUseCollectionInitializerDiagnosticAnalyzer()
        : base([
            (s_descriptor, ImmutableHashSet.Create<IOption2>(CodeStyleOptions2.PreferCollectionInitializer)),
            (s_objectInitDescriptor, ImmutableHashSet.Create<IOption2>(CodeStyleOptions2.PreferObjectInitializer)),
            // IDE0400 depends on BOTH preferences; runtime check in `TryGetMixedInitializerNotification`
            // suppresses the report when either is disabled. Register both so editorconfig severity
            // and "configure code style" UI reflect the same dependency.
            (s_mixedDescriptor, ImmutableHashSet.Create<IOption2>(
                CodeStyleOptions2.PreferObjectInitializer,
                CodeStyleOptions2.PreferCollectionInitializer)),
        ])
    {
    }

    protected abstract ISyntaxFacts SyntaxFacts { get; }

    protected abstract bool AreCollectionInitializersSupported(Compilation compilation);
    protected abstract bool AreCollectionExpressionsSupported(Compilation compilation);

    /// <summary>
    /// Returns true when <paramref name="objectCreationExpression"/> already has an attached
    /// initializer whose existing children can't be represented as a collection-expression
    /// element. Mirrors the walk-level <c>HasExistingInvalidInitializerForCollection</c>
    /// precondition that pre-Pass-3 short-circuited via the (now-loosened) walk-level
    /// `ShouldAnalyze`. Used by the diagnostic analyzer to skip collection-expression
    /// synthesis paths that would feed an unsupported existing initializer (`{ X = ... }`,
    /// `{ [k] = v }` when key-value elements aren't supported) into the binder.
    /// </summary>
    protected abstract bool HasExistingInvalidInitializerForCollectionExpression(TObjectCreationExpressionSyntax objectCreationExpression);

    protected abstract bool CanUseCollectionExpression(
        SemanticModel semanticModel,
        TObjectCreationExpressionSyntax objectCreationExpression,
        INamedTypeSymbol? expressionType,
        ImmutableArray<InitializerMatch<SyntaxNode>> preMatches,
        bool allowSemanticsChange,
        CancellationToken cancellationToken,
        out bool changesSemantics);

    protected abstract TAnalyzer GetAnalyzer();

    protected abstract bool IsValidContainingStatement(TStatementSyntax node);

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(OnCompilationStart);

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (!AreCollectionInitializersSupported(context.Compilation))
            return;

        var ienumerableType = context.Compilation.IEnumerableType();
        if (ienumerableType is null)
            return;

        var syntaxKinds = this.SyntaxFacts.SyntaxKinds;

        using var matchKinds = TemporaryArray<TSyntaxKind>.Empty;
        matchKinds.Add(syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ObjectCreationExpression));
        if (syntaxKinds.ImplicitObjectCreationExpression != null)
            matchKinds.Add(syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ImplicitObjectCreationExpression.Value));
        var matchKindsArray = matchKinds.ToImmutableAndClear();

        // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
        // get callbacks for object creation expression nodes, but analyze nodes across the entire code block
        // and eventually report fading diagnostics with location outside this node.
        // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
        // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
        var expressionType = context.Compilation.ExpressionOfTType();
        context.RegisterCodeBlockStartAction<TSyntaxKind>(blockStartContext =>
            blockStartContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeNode(nodeContext, ienumerableType, expressionType),
                matchKindsArray));
    }

    private void AnalyzeNode(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol ienumerableType,
        INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
        var language = objectCreationExpression.Language;
        var cancellationToken = context.CancellationToken;

        var preferInitializerOption = context.GetAnalyzerOptions().PreferCollectionInitializer;
        var preferExpressionOption = context.GetAnalyzerOptions().PreferCollectionExpression;
        var preferObjectOption = context.GetAnalyzerOptions().PreferObjectInitializer;

        // Pass 3b of the IDE0017+IDE0028 unification: this analyzer is the single canonical
        // reporter for IDE0017 (object initializer), IDE0028 (collection initializer +
        // collection expression), and IDE0400 (mixed object/collection initializer).
        // Bail only if ALL three relevant user preferences are off — any one being on means
        // the routing below might pick the corresponding diagnostic ID.
        var anyPreferenceEnabled =
            preferInitializerOption.Value ||
            preferExpressionOption.Value != Shared.CodeStyle.CollectionExpressionPreference.Never ||
            preferObjectOption.Value;
        if (!anyPreferenceEnabled
            && !ShouldSkipAnalysis(context.FilterTree, context.Options, context.Compilation.Options,
                    [preferInitializerOption.Notification, preferExpressionOption.Notification, preferObjectOption.Notification],
                    context.CancellationToken))
        {
            return;
        }

        // The walk needs to run when EITHER member-init OR collection-init folding could
        // apply. Collection-init / collection-expression both require the target type
        // implement IEnumerable (legacy IDE0028 precondition). Member-init does not — but
        // the walk's `ShouldAnalyze` already gates the member-init path on the right
        // existing-initializer conditions, so we only bail upfront when the type doesn't
        // implement IEnumerable AND we don't have a non-collection target for the
        // member-init path either.
        var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
        var implementsIEnumerable = objectType.Type != null && objectType.Type.AllInterfaces.Contains(ienumerableType);

        var syntaxFacts = this.SyntaxFacts;
        using var analyzer = GetAnalyzer();

        var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
        if (containingStatement != null && !IsValidContainingStatement(containingStatement))
            return;

        // Two-mode walk: initializer-mode produces member + Add/index matches; collection-
        // expression-mode produces Add/AddRange spread / foreach matches. The walk itself
        // gates the per-statement Add detection on `GetAddMethods().Any()`, but the type-
        // level IEnumerable check still belongs here — only the collection-fold paths
        // require it; member-fold doesn't. Probing both modes is safe regardless of the
        // routing decision below.
        var collectionExpressionMatches = implementsIEnumerable ? GetCollectionExpressionMatches() : null;
        var collectionInitializerMatches = GetCollectionInitializerMatches();

        // if both fail, we have nothing to offer.
        if (collectionExpressionMatches is null && collectionInitializerMatches is null)
            return;

        // if one fails, prefer the other.  If both succeed, prefer the one with more matches.
        var (matches, shouldUseCollectionExpression, changesSemantics) =
            collectionExpressionMatches is null ? collectionInitializerMatches!.Value :
            collectionInitializerMatches is null ? collectionExpressionMatches!.Value :
            collectionExpressionMatches.Value.matches.Length >= collectionInitializerMatches.Value.matches.Length
                ? collectionExpressionMatches.Value
                : collectionInitializerMatches.Value;

        // Classify the synthesized initializer's final shape by combining (existing
        // initializer children) with (match-produced children). The same routing logic
        // pre-Pass-3 lived in the deleted `AbstractUseObjectInitializerDiagnosticAnalyzer`
        // but only looked at the new matches against the existing member shape; the
        // unified analyzer must also consider whether the existing initializer already
        // carries collection-element shape, because the walk's `ShouldAnalyze` now permits
        // member-fold against ObjectInitializer existing inits regardless of whether
        // their children are member-shape, element-shape, or both (under csharplang#10185).
        var newHasMember = false;
        var newHasCollectionElement = false;
        foreach (var match in matches)
        {
            if (match.Kind == InitializerMatchKind.MemberInitializer)
                newHasMember = true;
            else
                newHasCollectionElement = true;
        }

        ClassifyExistingInitializerChildren(
            objectCreationExpression,
            out var existingHasMember,
            out var existingHasCollectionElement);

        var hasMember = newHasMember || existingHasMember;
        var hasCollectionElement = newHasCollectionElement || existingHasCollectionElement;

        var nodes = containingStatement is null
            ? ImmutableArray<SyntaxNode>.Empty
            : [containingStatement];
        nodes = nodes.AddRange(matches.Select(static m => m.Node));
        if (syntaxFacts.ContainsInterleavedDirective(nodes, cancellationToken))
            return;

        var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

        // Route to the right diagnostic ID + fade behavior based on the final shape of the
        // synthesized initializer:
        //
        //   * has member shape only → IDE0017 (pure-member fold).
        //   * has collection-element shape only → IDE0028 (pure-collection fold).
        //   * has both → IDE0400 (mixed, csharplang#10185).
        //   * neither → no synthesis to do.
        //
        // Each route owns its own option gate and notification source; IDE0400 additionally
        // requires both PreferObjectInitializer and PreferCollectionInitializer be enabled
        // (the less-severe notification wins) so a user who silenced one pure form via
        // severity downgrade isn't surprised by a louder mixed-form suggestion.
        DiagnosticDescriptor primaryDescriptor;
        DiagnosticDescriptor unnecessaryDescriptor;
        NotificationOption2 notification;
        ImmutableDictionary<string, string?> properties;

        if (shouldUseCollectionExpression)
        {
            // Collection-expression conversion (`new T()` / `new T() { … }` → `[ … ]`) is
            // legitimate even with an empty match list — the rewrite is the conversion
            // itself, not the folded statements. Route to IDE0028 unconditionally on this
            // branch; the classification feeds only the initializer-mode routing below.
            // The walk already gated this branch on `implementsIEnumerable`.
            if (preferExpressionOption.Value == Shared.CodeStyle.CollectionExpressionPreference.Never)
                return;

            primaryDescriptor = s_descriptor;
            unnecessaryDescriptor = s_unnecessaryCodeDescriptor;
            notification = preferExpressionOption.Notification;
            properties = UseCollectionInitializerHelpers.UseCollectionExpressionProperties;
        }
        else if (hasMember && hasCollectionElement)
        {
            // IDE0400's collection-element side inherits IDE0028's IEnumerable requirement:
            // the synthesized `new C { X = 1, 10 }` wouldn't bind unless the target type
            // implements IEnumerable. Pre-Pass-3 this gate lived inside the deleted
            // `AbstractUseNamedMemberInitializerAnalyzer.TryMatchAddInvocation` (which
            // refused to emit Add matches without IEnumerable). The unified walk drops that
            // per-statement gate so the member-init route can still fire on non-IEnumerable
            // types; routing-time IEnumerable enforcement here keeps the binder-valid
            // contract for IDE0400 specifically. (TestSubsequentAddInvocation_NoIEnumerable_NotFolded
            // pins this.)
            if (!implementsIEnumerable)
            {
                if (!preferObjectOption.Value)
                    return;

                // Strip the Add-shape matches and fall through to the member-init route.
                // The synthesized initializer becomes pure-member (legacy IDE0017) — the
                // user-visible behavior the deleted walk produced when its Add-fold helper
                // returned null.
                matches = matches.WhereAsArray(static m => m.Kind == InitializerMatchKind.MemberInitializer);
                if (matches.IsEmpty)
                    return;

                primaryDescriptor = s_objectInitDescriptor;
                unnecessaryDescriptor = s_objectInitUnnecessaryCodeDescriptor;
                notification = preferObjectOption.Notification;
                properties = UseCollectionInitializerHelpers.UseMemberInitializerProperties;
            }
            else
            {
                var mixedNotification = TryGetMixedInitializerNotification(context);
                if (mixedNotification is null)
                    return;

                primaryDescriptor = s_mixedDescriptor;
                unnecessaryDescriptor = s_mixedUnnecessaryCodeDescriptor;
                notification = mixedNotification.Value;
                properties = UseCollectionInitializerHelpers.UseMemberInitializerProperties;
            }
        }
        else if (hasMember)
        {
            // Pure member-init fold (legacy IDE0017).
            if (!preferObjectOption.Value)
                return;

            primaryDescriptor = s_objectInitDescriptor;
            unnecessaryDescriptor = s_objectInitUnnecessaryCodeDescriptor;
            notification = preferObjectOption.Notification;
            properties = UseCollectionInitializerHelpers.UseMemberInitializerProperties;
        }
        else if (hasCollectionElement)
        {
            // Pure collection-init fold (legacy IDE0028 initializer mode). Requires the
            // target type implement IEnumerable; without it the synthesized initializer
            // wouldn't bind. Member-init route above doesn't have this requirement (member
            // assignments work on any type).
            if (!implementsIEnumerable)
                return;

            if (!preferInitializerOption.Value)
                return;

            primaryDescriptor = s_descriptor;
            unnecessaryDescriptor = s_unnecessaryCodeDescriptor;
            notification = preferInitializerOption.Notification;
            properties = ImmutableDictionary<string, string?>.Empty;
        }
        else
        {
            return;
        }

        if (changesSemantics)
            properties = properties.Add(UseCollectionInitializerHelpers.ChangesSemanticsName, "");

        // Pick the fade strategy by route, not by per-match kind: IDE0017 / IDE0400 use the
        // legacy member-init fade (`c.` prefix); IDE0028 uses the legacy collection-init
        // fade (`c.Add(` + `)`). Under IDE0400, Add matches use the member-init fade too
        // (matching what the deleted `AbstractUseObjectInitializerDiagnosticAnalyzer.FadeOutCode`
        // produced under PR 7 — the unification preserves user-visible fade behavior).
        var useMemberInitFade = ReferenceEquals(primaryDescriptor, s_objectInitDescriptor)
            || ReferenceEquals(primaryDescriptor, s_mixedDescriptor);

        context.ReportDiagnostic(DiagnosticHelper.Create(
            primaryDescriptor,
            objectCreationExpression.GetFirstToken().GetLocation(),
            notification,
            context.Options,
            additionalLocations: locations,
            properties));

        FadeOutCode(context, unnecessaryDescriptor, matches, locations, properties, useMemberInitFade);

        return;

        (ImmutableArray<InitializerMatch<SyntaxNode>> matches, bool shouldUseCollectionExpression, bool changesSemantics)? GetCollectionInitializerMatches()
        {
            if (containingStatement is null)
                return null;

            if (!preferInitializerOption.Value)
                return null;

            var (_, matches, changesSemantics) = analyzer.Analyze(semanticModel, syntaxFacts, objectCreationExpression, analyzeForCollectionExpression: false, cancellationToken);

            // If analysis failed, we can't change this, no matter what.
            if (matches.IsDefault)
                return null;

            return (matches, shouldUseCollectionExpression: false, changesSemantics);
        }

        (ImmutableArray<InitializerMatch<SyntaxNode>> matches, bool shouldUseCollectionExpression, bool changesSemantics)? GetCollectionExpressionMatches()
        {
            if (preferExpressionOption.Value == CollectionExpressionPreference.Never)
                return null;

            // Don't bother analyzing for the collection expression case if the lang/version doesn't even support it.
            if (!this.AreCollectionExpressionsSupported(context.Compilation))
                return null;

            // Pass 3 of the IDE0017+IDE0028 unification loosened `ShouldAnalyze` (which used
            // to block on `HasExistingInvalidInitializerForCollection`) so the unified walk
            // can also fire on pure-member-init scenarios. The collection-expression synthesis
            // path can't represent those problematic existing initializer shapes
            // (`{ X = ... }`, `{ [k] = v }` when `k:v` elements aren't supported), and
            // attempting the synthesis would feed an unsupported expression into the binder.
            // Restore the equivalent gate here so this branch keeps the legacy preconditions
            // it depends on.
            if (this.HasExistingInvalidInitializerForCollectionExpression(objectCreationExpression))
                return null;

            var (preMatches, postMatches, changesSemantics1) = analyzer.Analyze(semanticModel, syntaxFacts, objectCreationExpression, analyzeForCollectionExpression: true, cancellationToken);

            // If analysis failed, we can't change this, no matter what.
            if (preMatches.IsDefault || postMatches.IsDefault)
                return null;

            // Check if it would actually be legal to use a collection expression here though.
            var allowSemanticsChange = preferExpressionOption.Value == CollectionExpressionPreference.WhenTypesLooselyMatch;
            if (!CanUseCollectionExpression(semanticModel, objectCreationExpression, expressionType, preMatches, allowSemanticsChange, cancellationToken, out var changesSemantics2))
                return null;

            return ([.. preMatches, .. postMatches], shouldUseCollectionExpression: true, changesSemantics1 || changesSemantics2);
        }
    }

    private void FadeOutCode(
        SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor unnecessaryDescriptor,
        ImmutableArray<InitializerMatch<SyntaxNode>> matches,
        ImmutableArray<Location> locations,
        ImmutableDictionary<string, string?>? properties,
        bool useMemberInitFade)
    {
        var syntaxFacts = this.SyntaxFacts;
        var syntaxTree = context.Node.SyntaxTree;

        // Pass 3b of the IDE0017+IDE0028 unification: route-driven fade dispatch. Member-init
        // matches use the legacy IDE0017 fade strategy (highlight the `c.` prefix up through
        // the member-access operator token); Add/AddRange/index/spread matches use the
        // legacy IDE0028 fade strategy (`UseCollectionInitializerHelpers.GetLocationsToFade`,
        // which highlights `c.Add(` / `)` around the argument list, etc.). For IDE0400 mixed
        // synthesis the *route* selects member-init fade for both kinds — matching the
        // pre-Pass-3 behavior where IDE0017's walk owned Add matches under mixed mode.
        foreach (var match in matches)
        {
            ImmutableArray<Location> additionalUnnecessaryLocations;
            if (useMemberInitFade)
            {
                additionalUnnecessaryLocations = GetMemberInitFadeLocations(syntaxFacts, syntaxTree, match);
            }
            else
            {
                additionalUnnecessaryLocations = UseCollectionInitializerHelpers.GetLocationsToFade(syntaxFacts, match);
            }

            if (additionalUnnecessaryLocations.IsDefaultOrEmpty)
                continue;

            // Report the diagnostic at the first unnecessary location. This is the location where the code fix
            // will be offered.
            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                unnecessaryDescriptor,
                additionalUnnecessaryLocations[0],
                NotificationOption2.ForSeverity(unnecessaryDescriptor.DefaultSeverity),
                context.Options,
                additionalLocations: locations,
                additionalUnnecessaryLocations: additionalUnnecessaryLocations,
                properties));
        }
    }

    /// <summary>
    /// Returns the fade-out locations for a member-init-style match (mirrors what the deleted
    /// <c>AbstractUseObjectInitializerDiagnosticAnalyzer.FadeOutCode</c> produced). The
    /// inputs are the same shapes the legacy walk handled: an assignment expression-statement
    /// or an <c>x.Add(value)</c> invocation; the recovery here re-derives the member-access
    /// and right-hand-value anchors needed to compute the fade spans.
    /// </summary>
    private ImmutableArray<Location> GetMemberInitFadeLocations(
        ISyntaxFacts syntaxFacts,
        SyntaxTree syntaxTree,
        InitializerMatch<SyntaxNode> match)
    {
        if (!TryGetMemberInitFadeAnchors(match, syntaxFacts, out var memberAccess, out var initializer))
            return [];

        var end = FadeOutOperatorToken
            ? syntaxFacts.GetOperatorTokenOfMemberAccessExpression(memberAccess).Span.End
            : syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess)!.Span.End;

        var fadeSpan = Location.Create(syntaxTree, TextSpan.FromBounds(memberAccess.SpanStart, end));
        return [fadeSpan];
    }

    /// <summary>
    /// Recovers the (member-access, initializer) anchor pair from a member-init-style match.
    /// Both supported kinds wrap an expression statement: assignment (`x.Member = value`,
    /// possibly compound) or single-arg <c>x.Add(value)</c>. Returns false for shapes the
    /// fade-out can't compute spans for; the caller skips fading those matches.
    /// </summary>
    private static bool TryGetMemberInitFadeAnchors(
        InitializerMatch<SyntaxNode> match,
        ISyntaxFacts syntaxFacts,
        [NotNullWhen(true)] out SyntaxNode? memberAccess,
        [NotNullWhen(true)] out SyntaxNode? initializer)
    {
        memberAccess = null;
        initializer = null;

        var statement = match.Node;

        switch (match.Kind)
        {
            case InitializerMatchKind.MemberInitializer:
                if (!syntaxFacts.IsAnyAssignmentStatement(statement))
                    return false;

                syntaxFacts.GetPartsOfAssignmentStatement(statement, out var left, out _, out var right);
                if (!syntaxFacts.IsSimpleMemberAccessExpression(left))
                    return false;

                memberAccess = left;
                initializer = right;
                return true;

            case InitializerMatchKind.AddInvocation:
                var statementExpression = syntaxFacts.GetExpressionOfExpressionStatement(statement);
                if (statementExpression is null || !syntaxFacts.IsInvocationExpression(statementExpression))
                    return false;

                var invoked = syntaxFacts.GetExpressionOfInvocationExpression(statementExpression);
                if (!syntaxFacts.IsSimpleMemberAccessExpression(invoked))
                    return false;

                var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(statementExpression);
                if (arguments.Count == 0)
                    return false;

                memberAccess = invoked;
                initializer = syntaxFacts.GetExpressionOfArgument(arguments[0]);
                return initializer is not null;

            default:
                // IndexAssignment / ForEach / ConstructorArgument never participate in
                // member-init fade — the caller routes those to the collection-init fade.
                return false;
        }
    }

    /// <summary>
    /// Inspects the existing initializer (if any) on <paramref name="objectCreationExpression"/>
    /// and reports whether its children include named-member assignments (e.g.
    /// <c>X = 1</c>), collection-element initializers (e.g. <c>1</c>, <c>{ key, value }</c>,
    /// <c>[k] = v</c>), or both. Used by `AnalyzeNode` to compute the final shape of the
    /// synthesized initializer (existing children + new matches) when routing between IDE0017,
    /// IDE0028, and IDE0400.
    /// </summary>
    private void ClassifyExistingInitializerChildren(
        TObjectCreationExpressionSyntax objectCreationExpression,
        out bool hasMember,
        out bool hasCollectionElement)
    {
        hasMember = false;
        hasCollectionElement = false;

        var syntaxFacts = this.SyntaxFacts;
        var initializer = syntaxFacts.GetInitializerOfBaseObjectCreationExpression(objectCreationExpression);
        if (initializer is null)
            return;

        foreach (var element in syntaxFacts.GetInitializersOfObjectMemberInitializer(initializer))
        {
            // Under csharplang#10185 (mixed object/collection initializers) an
            // `ObjectInitializerExpression` body can mix member-assignment children with
            // collection-element children. Walk every child rather than short-circuiting so
            // both shapes are detected on the first pass through this initializer.
            if (syntaxFacts.IsNamedMemberInitializer(element))
                hasMember = true;
            else
                hasCollectionElement = true;
        }
    }

    /// <summary>
    /// Returns the notification severity to use when reporting the mixed-initializer
    /// diagnostic (IDE0400), or <see langword="null"/> if mixed suggestions should be
    /// suppressed because either user preference (object-initializer or collection-
    /// initializer) is disabled. When both are enabled, the less severe of the two
    /// notifications wins so that a user who silenced one of the pure forms via severity
    /// downgrade is not surprised by a louder mixed-form suggestion.
    /// </summary>
    private static NotificationOption2? TryGetMixedInitializerNotification(SyntaxNodeAnalysisContext context)
    {
        var preferObject = context.GetAnalyzerOptions().PreferObjectInitializer;
        var preferCollection = context.GetAnalyzerOptions().PreferCollectionInitializer;
        if (!preferObject.Value || !preferCollection.Value)
            return null;

        return preferObject.Notification.Severity <= preferCollection.Notification.Severity
            ? preferObject.Notification
            : preferCollection.Notification;
    }

    /// <summary>
    /// True when the per-language member-init fade should extend through the operator token
    /// (`c.` for C# — both `c` and the dot are faded) vs only the receiver expression (`c`
    /// for VB — only the receiver is faded). Mirrors the legacy
    /// <c>AbstractUseObjectInitializerDiagnosticAnalyzer.FadeOutOperatorToken</c>.
    /// </summary>
    protected abstract bool FadeOutOperatorToken { get; }
}
