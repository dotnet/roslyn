// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.UseInitializer;

/// <summary>
/// Pass 3 of the IDE0017+IDE0028 unification: one fix provider class handles the three
/// related diagnostics (<see cref="IDEDiagnosticIds.UseObjectInitializerDiagnosticId"/>
/// = IDE0017, <see cref="IDEDiagnosticIds.UseCollectionInitializerDiagnosticId"/> = IDE0028,
/// and <see cref="IDEDiagnosticIds.UseMixedObjectAndCollectionInitializerDiagnosticId"/>
/// = IDE0400), backed by a single walk (<see cref="AbstractUseCollectionInitializerAnalyzer{
/// TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax,
/// TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax,
/// TAssignmentStatementSyntax, TLocalDeclarationStatementSyntax, TVariableDeclaratorSyntax,
/// TAnalyzer}"/>). The diagnostic's property bag tells the fixer which synthesis path
/// to take (<see cref="UseCollectionInitializerHelpers.UseMemberInitializerName"/> →
/// member-initializer synthesis, <see cref="UseCollectionInitializerHelpers.UseCollectionExpressionName"/>
/// → collection-expression synthesis, neither → collection-initializer synthesis).
/// </summary>
/// <remarks>
/// The replaced single-purpose providers and walks
/// (<c>AbstractUseObjectInitializerCodeFixProvider</c>, <c>AbstractUseCollectionInitializerCodeFixProvider</c>,
/// <c>AbstractUseNamedMemberInitializerAnalyzer</c>, and the per-language concretes) are
/// all deleted by this pass; per-language concretes consolidate into a single
/// <c>*UseInitializerCodeFixProvider</c> each. Test verifier aliases switch to the new
/// concrete type name.
/// </remarks>
internal abstract class AbstractUseInitializerCodeFixProvider<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TAssignmentStatementSyntax,
    TInvocationExpressionSyntax,
    TExpressionStatementSyntax,
    TLocalDeclarationStatementSyntax,
    TVariableDeclaratorSyntax,
    TAnalyzer>
    : ForkingSyntaxEditorBasedCodeFixProvider<TObjectCreationExpressionSyntax>
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStatementSyntax : SyntaxNode
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TAssignmentStatementSyntax : TStatementSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TExpressionStatementSyntax : TStatementSyntax
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
    private static readonly string s_collectionInitTitle = AnalyzersResources.Collection_initialization_can_be_simplified;
    private static readonly string s_collectionInitTitleChangesSemantics = string.Format(CodeFixesResources._0_may_change_semantics, s_collectionInitTitle);
    private static readonly string s_collectionInitEquivalenceKey = nameof(AnalyzersResources.Collection_initialization_can_be_simplified);
    private static readonly string s_collectionInitEquivalenceKeyChangesSemantics = s_collectionInitEquivalenceKey + "_ChangesSemantics";

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => [
            IDEDiagnosticIds.UseObjectInitializerDiagnosticId,
            IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
            IDEDiagnosticIds.UseMixedObjectAndCollectionInitializerDiagnosticId,
        ];

    protected abstract TAnalyzer GetAnalyzer();

    protected abstract ISyntaxKinds SyntaxKinds { get; }
    protected abstract ISyntaxFormatting SyntaxFormatting { get; }

    protected abstract SyntaxTrivia Whitespace(string text);

    /// <summary>
    /// Synthesizes the new statement when the triggering diagnostic was IDE0017 or IDE0400 —
    /// i.e., the matches the unified walk produced should be folded as member initializers
    /// (pure-member for IDE0017; mixed member + Add for IDE0400). The per-language fixer
    /// wraps the matches into the language's member-initializer syntax. Matches are typed
    /// as <see cref="InitializerMatch{TNode}"/> over <see cref="SyntaxNode"/> because the
    /// unified walk reports across the broader collection-init match shapes; the fixer
    /// narrows back to per-statement nodes using <see cref="InitializerMatch{TNode}.Node"/>.
    /// </summary>
    protected abstract TStatementSyntax GetNewStatementForMemberInit(
        TStatementSyntax statement,
        TObjectCreationExpressionSyntax objectCreation,
        SyntaxFormattingOptions options,
        ImmutableArray<InitializerMatch<SyntaxNode>> matches);

    /// <summary>
    /// Synthesizes the replacement when the triggering diagnostic was IDE0028 — either the
    /// collection-initializer form (<paramref name="useCollectionExpression"/> false) or the
    /// collection-expression form (true). The per-language fixer produces both forms; the
    /// dispatch lives in the concrete override.
    /// </summary>
    protected abstract Task<(SyntaxNode oldNode, SyntaxNode newNode)> GetReplacementNodesForCollectionInitAsync(
        Document document,
        TObjectCreationExpressionSyntax objectCreation,
        bool useCollectionExpression,
        ImmutableArray<InitializerMatch<SyntaxNode>> preMatches,
        ImmutableArray<InitializerMatch<SyntaxNode>> postMatches,
        CancellationToken cancellationToken);

    protected sealed override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
    {
        // The lightbulb title (and the equivalence key that drives fix-all and the user-
        // visible action menu) depends on which of the three diagnostics surfaced the fix.
        // The IDE0028 path additionally toggles a "may change semantics" suffix when the
        // analyzer set the corresponding property — mirrors the behavior the previous
        // `AbstractUseCollectionExpressionCodeFixProvider` constructor provided automatically.
        var diagnostic = context.Diagnostics[0];
        switch (diagnostic.Id)
        {
            case IDEDiagnosticIds.UseMixedObjectAndCollectionInitializerDiagnosticId:
                return (
                    AnalyzersResources.Object_and_collection_initialization_can_be_merged,
                    nameof(AnalyzersResources.Object_and_collection_initialization_can_be_merged));

            case IDEDiagnosticIds.UseCollectionInitializerDiagnosticId:
                return UseCollectionInitializerHelpers.ChangesSemantics(diagnostic)
                    ? (s_collectionInitTitleChangesSemantics, s_collectionInitEquivalenceKeyChangesSemantics)
                    : (s_collectionInitTitle, s_collectionInitEquivalenceKey);

            case IDEDiagnosticIds.UseObjectInitializerDiagnosticId:
            default:
                return (
                    AnalyzersResources.Object_initialization_can_be_simplified,
                    nameof(AnalyzersResources.Object_initialization_can_be_simplified));
        }
    }

    protected sealed override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string? equivalenceKey, CancellationToken cancellationToken)
    {
        // Faded-secondary diagnostics (the "unnecessary code" peers used only to underline
        // statements that are about to be removed) should never participate in fix-all —
        // they share the primary's ID but carry the `Unnecessary` tag.
        if (diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary))
            return false;

        // For IDE0028, the "may change semantics" suffix on the equivalence key explicitly
        // opts in to fixing semantic-changing diagnostics; the safer-default equivalence key
        // skips them.
        if (diagnostic.Id == IDEDiagnosticIds.UseCollectionInitializerDiagnosticId)
        {
            if (equivalenceKey == s_collectionInitEquivalenceKeyChangesSemantics)
                return true;

            return !UseCollectionInitializerHelpers.ChangesSemantics(diagnostic);
        }

        // IDE0017 / IDE0400 don't have a changes-semantics variant today.
        return true;
    }

    protected sealed override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        TObjectCreationExpressionSyntax objectCreation,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // `ForkingSyntaxEditorBasedCodeFixProvider.FixAsync` only receives the diagnostic's
        // property bag, not the diagnostic itself, so the analyzer attaches a property tag
        // to each diagnostic identifying which fix path the fixer should take:
        //
        //   * `UseMemberInitializerName` → IDE0017 or IDE0400. Routes to the member-
        //     initializer synthesis path; the unified walk reports member + (mixed) Add
        //     matches in this mode.
        //   * `UseCollectionExpressionName` → IDE0028 in collection-expression mode (set
        //     when the user prefers `[ … ]` and the lang supports it). Routes to the
        //     collection-expression synthesis path.
        //   * Neither → IDE0028 in collection-initializer mode. Routes to the collection-
        //     initializer synthesis path.
        //
        // Under the mixed object/collection initializer feature (csharplang#10185) the same
        // walk reports both member and Add matches, so the analyzer-side property tag is the
        // only stable signal: probing the walk and inspecting kinds would mis-route when
        // both kinds are present (the user's intent — member-init or pure-collection — was
        // already decided when the diagnostic ID was picked).
        if (properties.ContainsKey(UseCollectionInitializerHelpers.UseMemberInitializerName))
        {
            await FixMemberInitAsync(document, editor, objectCreation, semanticModel, syntaxFacts, cancellationToken).ConfigureAwait(false);
            return;
        }

        var useCollectionExpression = properties.ContainsKey(UseCollectionInitializerHelpers.UseCollectionExpressionName);
        await FixCollectionInitAsync(document, editor, objectCreation, semanticModel, syntaxFacts,
            useCollectionExpression, cancellationToken).ConfigureAwait(false);
    }

    private async Task FixMemberInitAsync(
        Document document,
        SyntaxEditor editor,
        TObjectCreationExpressionSyntax objectCreation,
        SemanticModel semanticModel,
        ISyntaxFactsService syntaxFacts,
        CancellationToken cancellationToken)
    {
        // Re-run the unified walk in initializer mode (`analyzeForCollectionExpression: false`)
        // — collection-expression mode never produces member matches and would silently drop
        // the IDE0017/IDE0400 fold. The walk's matches arrive as
        // `InitializerMatch<SyntaxNode>` carrying both member-init and Add matches; the
        // per-language `GetNewStatementForMemberInit` routes per `match.Kind`.
        using var analyzer = GetAnalyzer();
        var (preMatches, postMatches, _) = analyzer.Analyze(
            semanticModel, syntaxFacts, objectCreation, analyzeForCollectionExpression: false, cancellationToken);

        if (preMatches.IsDefault || postMatches.IsDefault)
            return;

        // Member-init mode never produces pre-matches (constructor arguments are only
        // synthesized by collection-expression mode); guard against accidental regressions.
        Contract.ThrowIfFalse(preMatches.IsEmpty);

        var matches = postMatches;

        // Mirror the diagnostic analyzer's IDE0400 IEnumerable gate: the unified walk emits
        // Add matches whenever `GetAddMethods().Any()` regardless of IEnumerable status, so
        // for non-IEnumerable targets the synthesized `new C { X = 1, 10 }` would not bind.
        // The diagnostic analyzer drops the Add matches and routes to IDE0017 in that case;
        // the fixer must apply the same drop or the synthesized output diverges from what
        // the diagnostic title promised. (TestSubsequentAddInvocation_NoIEnumerable_NotFolded
        // pins this end-to-end through the iterative fix verifier.)
        var hasAdd = false;
        var hasMember = false;
        foreach (var match in matches)
        {
            if (match.Kind == InitializerMatchKind.MemberInitializer)
                hasMember = true;
            else
                hasAdd = true;
        }

        if (hasAdd && hasMember)
        {
            var ienumerableType = semanticModel.Compilation.IEnumerableType();
            var targetType = semanticModel.GetTypeInfo(objectCreation, cancellationToken).Type;
            if (ienumerableType is null || targetType is null || !targetType.AllInterfaces.Contains(ienumerableType))
            {
                matches = matches.WhereAsArray(static m => m.Kind == InitializerMatchKind.MemberInitializer);
            }
        }

        if (matches.IsEmpty)
            return;

        var statement = objectCreation.FirstAncestorOrSelf<TStatementSyntax>();
        Contract.ThrowIfNull(statement);

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var newStatement = GetNewStatementForMemberInit(statement, objectCreation, formattingOptions, matches)
            .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(statement, newStatement);
        foreach (var match in matches)
            editor.RemoveNode(match.Node, SyntaxRemoveOptions.KeepUnbalancedDirectives);
    }

    private async Task FixCollectionInitAsync(
        Document document,
        SyntaxEditor editor,
        TObjectCreationExpressionSyntax objectCreation,
        SemanticModel semanticModel,
        ISyntaxFactsService syntaxFacts,
        bool useCollectionExpression,
        CancellationToken cancellationToken)
    {
        using var collectionAnalyzer = GetAnalyzer();
        var (preMatches, postMatches, _) = collectionAnalyzer.Analyze(
            semanticModel, syntaxFacts, objectCreation, useCollectionExpression, cancellationToken);

        if (preMatches.IsDefault || postMatches.IsDefault)
            return;

        var (oldNode, newNode) = await GetReplacementNodesForCollectionInitAsync(
            document, objectCreation, useCollectionExpression, preMatches, postMatches, cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(oldNode, newNode);

        // Only the post-matches map back to statements that should be removed — pre-matches
        // are arguments inside the object creation, which itself was just replaced.
        foreach (var match in postMatches)
            editor.RemoveNode(match.Node, SyntaxRemoveOptions.KeepUnbalancedDirectives);
    }

    /// <summary>
    /// Re-indents <paramref name="expression"/> by one indentation level. Used by the
    /// per-language member-initializer synthesizers when promoting an existing RHS into a
    /// brace-bound initializer body.
    /// </summary>
    protected TExpressionSyntax Indent(TExpressionSyntax expression, SyntaxFormattingOptions options)
    {
        var endOfLineKind = this.SyntaxKinds.EndOfLineTrivia;
        var whitespaceTriviaKind = this.SyntaxKinds.WhitespaceTrivia;
        return expression.ReplaceTokens(
            expression.DescendantTokens(),
            (currentToken, _) =>
            {
                if (currentToken.LeadingTrivia is [.., var whitespace1] &&
                    whitespace1.RawKind == whitespaceTriviaKind)
                {
                    // This is a token on its own line.  With whitespace at the start of the line.
                    var leadingTrivia = currentToken.LeadingTrivia.Replace(
                        whitespace1,
                        IncreaseIndent(whitespace1, options));

                    currentToken = currentToken.WithLeadingTrivia(leadingTrivia);
                }

                if (currentToken.TrailingTrivia is [.., var endOfLine, var whitespace2] &&
                    endOfLine.RawKind == endOfLineKind &&
                    whitespace2.RawKind == whitespaceTriviaKind)
                {
                    // This is a VB line continuation case (`_`), with indentation before the next token
                    var trailingTrivia = currentToken.TrailingTrivia.Replace(
                        whitespace2,
                        IncreaseIndent(whitespace2, options));

                    currentToken = currentToken.WithTrailingTrivia(trailingTrivia);
                }

                return currentToken;
            });
    }

    private SyntaxTrivia IncreaseIndent(SyntaxTrivia whitespaceTrivia, SyntaxFormattingOptions options)
    {
        var existingWhitespace = whitespaceTrivia.ToString();
        var spaceCount = existingWhitespace.ConvertTabToSpace(
            options.TabSize,
            initialColumn: 0,
            endPosition: existingWhitespace.Length);

        var desiredSpaceCount = spaceCount + options.IndentationSize;

        var desiredWhitespace = desiredSpaceCount.CreateIndentationString(options.UseTabs, options.TabSize);
        return Whitespace(desiredWhitespace);
    }
}
