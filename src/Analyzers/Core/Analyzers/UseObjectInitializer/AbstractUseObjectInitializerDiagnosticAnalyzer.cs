// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

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

        var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Select(m => m.Statement));
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
            if (match.IsAddInvocation)
                hasAddMatch = true;
            else
                hasMemberMatch = true;
        }

        // Yield to IDE0028 when the synthesis would be a pure collection initializer — the
        // existing initializer is empty (or null) of member assignments and every new match is
        // an Add-fold. IDE0028's existing pipeline produces the exact same shape and remains
        // the canonical owner of pure-collection suggestions.
        if (hasAddMatch && !hasMemberMatch && !existingHasMemberInit)
            return;

        // Synthesis is mixed iff at least one Add-shape element coexists with at least one
        // member-shape element across either the existing initializer or the matches.
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

        context.ReportDiagnostic(DiagnosticHelper.Create(
            primaryDescriptor,
            objectCreationExpression.GetFirstToken().GetLocation(),
            notification,
            context.Options,
            locations,
            properties: null));

        FadeOutCode(context, unnecessaryDescriptor, matches, locations);
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
        ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches,
        ImmutableArray<Location> locations)
    {
        var syntaxTree = context.Node.SyntaxTree;

        var syntaxFacts = GetSyntaxFacts();

        foreach (var match in matches)
        {
            using var additionalUnnecessaryLocations = TemporaryArray<Location>.Empty;

            var end = FadeOutOperatorToken
                ? syntaxFacts.GetOperatorTokenOfMemberAccessExpression(match.MemberAccessExpression).Span.End
                : syntaxFacts.GetExpressionOfMemberAccessExpression(match.MemberAccessExpression)!.Span.End;

            var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                match.MemberAccessExpression.SpanStart, end));
            additionalUnnecessaryLocations.Add(location1);

            if (match.Statement.Span.End > match.Initializer.FullSpan.End)
            {
                locations.Add(syntaxTree.GetLocation(TextSpan.FromBounds(match.Initializer.FullSpan.End, match.Statement.Span.End)));
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
                properties: null));
        }
    }
}
