// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseObjectInitializer;

internal abstract partial class AbstractUseObjectInitializerDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
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
    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
        EnforceOnBuildValues.UseObjectInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_object_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Object_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
        EnforceOnBuildValues.UseObjectInitializer,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_object_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Object_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    // IDE0400. Surfaces when the analyzer's matches synthesize a *mixed* object/collection
    // initializer (dotnet/csharplang#10185) — i.e., the suggested `{ ... }` would contain both
    // member-shape children (`Prop = v`, `Prop += v`) and bare-element children. Gated on
    // *both* `PreferObjectInitializer` and `PreferCollectionInitializer` being enabled (see
    // `TryGetMixedInitializerNotification`), since disabling either user preference should
    // suppress mixed suggestions just as it suppresses the corresponding pure form.
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

    protected abstract bool FadeOutOperatorToken { get; }
    protected abstract TAnalyzer GetAnalyzer();

    protected AbstractUseObjectInitializerDiagnosticAnalyzer()
        : base([
            (s_descriptor, ImmutableHashSet.Create<IOption2>(CodeStyleOptions2.PreferObjectInitializer)),
            // IDE0400 (mixed object/collection initializer) depends on BOTH preferences being
            // enabled — the analyzer's runtime check in `TryGetMixedInitializerNotification`
            // suppresses the mixed suggestion if either is off. Register both options so
            // editorconfig severity wiring, "configure code style" UI, and consumers of the
            // descriptor-to-option mapping reflect the same dependency as the runtime gate.
            (s_mixedDescriptor, ImmutableHashSet.Create<IOption2>(
                CodeStyleOptions2.PreferObjectInitializer,
                CodeStyleOptions2.PreferCollectionInitializer)),
        ])
    {
    }

    protected abstract ISyntaxFacts GetSyntaxFacts();

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (!AreObjectInitializersSupported(context.Compilation))
                return;

            var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
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
            context.RegisterCodeBlockStartAction<TSyntaxKind>(blockStartContext =>
                blockStartContext.RegisterSyntaxNodeAction(AnalyzeNode, matchKindsArray));
        });
    }

    protected abstract bool AreObjectInitializersSupported(Compilation compilation);

    protected abstract bool IsValidContainingStatement(TStatementSyntax node);

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
        var preferObjectOption = context.GetAnalyzerOptions().PreferObjectInitializer;
        if (!preferObjectOption.Value || ShouldSkipAnalysis(context, preferObjectOption.Notification))
        {
            // No point in analyzing if the object-initializer preference is off — both the
            // legacy IDE0017 path and the new mixed IDE0400 path require it.
            return;
        }

        var syntaxFacts = GetSyntaxFacts();
        using var analyzer = GetAnalyzer();
        var matches = analyzer.Analyze(semanticModel, syntaxFacts, objectCreationExpression, context.CancellationToken);

        if (matches.IsDefaultOrEmpty)
            return;

        var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
        if (containingStatement == null)
            return;

        if (!IsValidContainingStatement(containingStatement))
            return;

        var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Select(static m => (SyntaxNode)m.Node));
        if (syntaxFacts.ContainsInterleavedDirective(nodes, context.CancellationToken))
            return;

        var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

        // Route the diagnostic based on the *shape of the synthesized initializer* (existing
        // initializer contents + the new matches), not just the matches alone:
        //
        //   * Synthesis would be pure member-initializer → legacy IDE0017.
        //   * Synthesis would be pure collection-initializer (no existing member init either)
        //     → yield to `IDE0028` (UseCollectionInitializer), which analyzes the same
        //     object-creation site and already handles this case.
        //   * Synthesis would mix member-shape and element-shape children → IDE0400
        //     (UseMixedObjectAndCollectionInitializer), the new diagnostic for the mixed
        //     object/collection initializer language feature (dotnet/csharplang#10185).
        //
        // Pre-Preview language versions never produce Add-shape matches (the underlying
        // `TryMatchAddInvocation` is gated on `SupportsMixedObjectAndCollectionInitializers`),
        // so the routing collapses to the legacy IDE0017 path and observable behavior is
        // unchanged for those versions.
        var existingHasMemberInit = ExistingInitializerHasMemberAssignment(objectCreationExpression);
        var hasAddMatch = false;
        var hasMemberMatch = false;
        foreach (var match in matches)
        {
            switch (match.Kind)
            {
                case InitializerMatchKind.AddInvocation:
                    hasAddMatch = true;
                    break;
                case InitializerMatchKind.MemberInitializer:
                    hasMemberMatch = true;
                    break;
                default:
                    // IDE0017's walk currently never emits IndexAssignment / ForEach kinds; if
                    // a future change adds them they'd need explicit routing here. Failing fast
                    // is safer than silently misclassifying the synthesized shape.
                    throw ExceptionUtilities.UnexpectedValue(match.Kind);
            }
        }

        // Yield to IDE0028 when the synthesis would be a pure collection initializer — the
        // existing initializer is empty (or null) of member assignments and every new match is
        // an Add-fold. IDE0028's existing pipeline produces the exact same shape and remains
        // the canonical owner of pure-collection suggestions.
        if (hasAddMatch && !hasMemberMatch && !existingHasMemberInit)
            return;

        // Synthesis is mixed iff at least one Add-shape element coexists with at least one
        // member-shape element across either the existing initializer or the matches.
        // After Pass 1 of the IDE0017+IDE0028 unification, matches are typed as
        // `InitializerMatch<TStatementSyntax>` with a `Kind` discriminator; the IDE0017 walk
        // never emits IndexAssignment/ForEach today, so this enum check is exhaustive.
        var isMixed = hasAddMatch && (hasMemberMatch || existingHasMemberInit);

        DiagnosticDescriptor primaryDescriptor;
        DiagnosticDescriptor unnecessaryDescriptor;
        NotificationOption2 notification;
        if (isMixed)
        {
            var mixedNotification = TryGetMixedInitializerNotification(context);
            if (mixedNotification is null)
                return;

            primaryDescriptor = s_mixedDescriptor;
            unnecessaryDescriptor = s_mixedUnnecessaryCodeDescriptor;
            notification = mixedNotification.Value;
        }
        else
        {
            primaryDescriptor = s_descriptor;
            unnecessaryDescriptor = s_unnecessaryCodeDescriptor;
            notification = preferObjectOption.Notification;
        }

        // Pass 2 of the IDE0017+IDE0028 unification routed both IDs (and IDE0400) through a
        // single shared fix provider; the property tag below tells that provider to use the
        // member-initializer synthesis path. Without it the fixer would have to re-probe both
        // walks and could mis-route when the same object creation is foldable by either.
        var memberInitProperties = UseCollectionInitializerHelpers.UseMemberInitializerProperties;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            primaryDescriptor,
            objectCreationExpression.GetFirstToken().GetLocation(),
            notification,
            context.Options,
            locations,
            properties: memberInitProperties));

        FadeOutCode(context, unnecessaryDescriptor, matches, locations, memberInitProperties);
    }

    /// <summary>
    /// Returns true if <paramref name="objectCreationExpression"/> already has an attached
    /// initializer with at least one assignment-shape child (e.g. <c>new C { X = 1 }</c>).
    /// Used to detect when even an all-Add subsequent-statement match list would still
    /// synthesize a mixed initializer because the existing list already supplies the
    /// member-shape side.
    /// </summary>
    private bool ExistingInitializerHasMemberAssignment(TObjectCreationExpressionSyntax objectCreationExpression)
    {
        var syntaxFacts = GetSyntaxFacts();
        var initializer = syntaxFacts.GetInitializerOfBaseObjectCreationExpression(objectCreationExpression);
        if (initializer is null)
            return false;

        foreach (var element in syntaxFacts.GetInitializersOfObjectMemberInitializer(initializer))
        {
            if (syntaxFacts.IsNamedMemberInitializer(element))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the notification severity to use when reporting the mixed-initializer
    /// diagnostic, or <see langword="null"/> if mixed suggestions should be suppressed because
    /// either user preference (object-initializer or collection-initializer) is disabled. When
    /// both are enabled, the less severe of the two notifications wins so that a user who
    /// silenced one of the pure forms via severity downgrade is not surprised by a louder
    /// mixed-form suggestion.
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

    private void FadeOutCode(
        SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor unnecessaryDescriptor,
        ImmutableArray<InitializerMatch<TStatementSyntax>> matches,
        ImmutableArray<Location> locations,
        ImmutableDictionary<string, string?> properties)
    {
        var syntaxTree = context.Node.SyntaxTree;
        var syntaxFacts = GetSyntaxFacts();

        foreach (var match in matches)
        {
            using var additionalUnnecessaryLocations = TemporaryArray<Location>.Empty;

            // Pass 1 of unification dropped the rich (MemberAccessExpression, Initializer) data
            // from the match struct in favor of just (Statement, Kind). Recover them from the
            // statement here so the fade spans stay identical to what the pre-Pass-1 code
            // produced. Both supported kinds wrap an expression statement; the inner expression
            // is an assignment (member-init) or invocation (Add-fold).
            if (!TryGetFadeAnchors(match, syntaxFacts,
                    out TMemberAccessExpressionSyntax? memberAccess,
                    out TExpressionSyntax? initializer))
            {
                continue;
            }

            var end = FadeOutOperatorToken
                ? syntaxFacts.GetOperatorTokenOfMemberAccessExpression(memberAccess).Span.End
                : syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess)!.Span.End;

            var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                memberAccess.SpanStart, end));
            additionalUnnecessaryLocations.Add(location1);

            if (match.Node.Span.End > initializer.FullSpan.End)
            {
                locations.Add(syntaxTree.GetLocation(TextSpan.FromBounds(initializer.FullSpan.End, match.Node.Span.End)));
            }

            if (additionalUnnecessaryLocations.Count == 0)
                continue;

            // Report the diagnostic at the first unnecessary location. This is the location where the code fix
            // will be offered.
            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                unnecessaryDescriptor,
                additionalUnnecessaryLocations[0],
                NotificationOption2.ForSeverity(unnecessaryDescriptor.DefaultSeverity),
                context.Options,
                additionalLocations: locations,
                additionalUnnecessaryLocations: additionalUnnecessaryLocations.ToImmutableAndClear(),
                properties: properties));
        }
    }

    /// <summary>
    /// Recovers the member-access and value-expression anchors for fade-out from an
    /// <see cref="InitializerMatch{TStatementSyntax}"/>. Both supported kinds (member-init,
    /// Add-invocation) wrap an expression statement; the inner expression's shape depends on
    /// the kind. Returns false if the statement doesn't have the expected shape — defensive
    /// guard against future walk extensions that might emit a kind without a recoverable
    /// member-access anchor.
    /// </summary>
    private static bool TryGetFadeAnchors(
        InitializerMatch<TStatementSyntax> match,
        ISyntaxFacts syntaxFacts,
        [NotNullWhen(true)] out TMemberAccessExpressionSyntax? memberAccess,
        [NotNullWhen(true)] out TExpressionSyntax? initializer)
    {
        memberAccess = null;
        initializer = null;

        switch (match.Kind)
        {
            case InitializerMatchKind.MemberInitializer:
                // `x.Member = value` (or `x.Member += value` etc.). `IsAnyAssignmentStatement`
                // is the cross-language predicate covering simple and all compound assignment
                // statement shapes. The parts helper then splits the assignment into
                // (left, operator, right); member-access = LHS, initializer = RHS.
                if (!syntaxFacts.IsAnyAssignmentStatement(match.Node))
                    return false;

                syntaxFacts.GetPartsOfAssignmentStatement(match.Node, out var left, out _, out var right);
                memberAccess = left as TMemberAccessExpressionSyntax;
                initializer = right as TExpressionSyntax;
                return memberAccess is not null && initializer is not null;

            case InitializerMatchKind.AddInvocation:
                // `x.Add(value)` (single-arg, per `TryMatchAddInvocation`). Member-access =
                // `x.Add`, initializer = first argument's expression.
                var statementExpression = syntaxFacts.GetExpressionOfExpressionStatement(match.Node);
                if (statementExpression is null || !syntaxFacts.IsInvocationExpression(statementExpression))
                    return false;

                memberAccess = syntaxFacts.GetExpressionOfInvocationExpression(statementExpression) as TMemberAccessExpressionSyntax;
                if (memberAccess is null)
                    return false;

                var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(statementExpression);
                if (arguments.Count == 0)
                    return false;

                initializer = syntaxFacts.GetExpressionOfArgument(arguments[0]) as TExpressionSyntax;
                return initializer is not null;

            default:
                // IDE0017's walk doesn't emit any other kinds today (see exhaustive switch in
                // AnalyzeNode). Anything else means a walk extension forgot to extend fade-out.
                return false;
        }
    }
}
