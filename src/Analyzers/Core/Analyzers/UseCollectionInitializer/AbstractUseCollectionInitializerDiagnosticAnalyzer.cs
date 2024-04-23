// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

/// <summary>
/// Represents statements following an object initializer that should be converted into
/// collection-initializer/expression elements.
/// </summary>
/// <param name="Statement">The statement that follows that contains the values to add to the new
/// collection-initializer or collection-expression</param>
/// <param name="UseSpread">Whether or not a spread (<c>.. x</c>) element should be created for this statement. This
/// is needed as the statement could be cases like <c>expr.Add(x)</c> vs. <c>expr.AddRange(x)</c>. This property
/// indicates that the latter should become a spread, without the consumer having to reexamine the statement to see
/// what form it is.</param>
internal readonly record struct Match<TStatementSyntax>(
    TStatementSyntax Statement,
    bool UseSpread) where TStatementSyntax : SyntaxNode;

internal abstract partial class AbstractUseCollectionInitializerDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TStatementSyntax,
    TObjectCreationExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TInvocationExpressionSyntax,
    TExpressionStatementSyntax,
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
    where TLocalDeclarationStatementSyntax : TStatementSyntax
    where TVariableDeclaratorSyntax : SyntaxNode
    where TAnalyzer : AbstractUseCollectionInitializerAnalyzer<
        TExpressionSyntax,
        TStatementSyntax,
        TObjectCreationExpressionSyntax,
        TMemberAccessExpressionSyntax,
        TInvocationExpressionSyntax,
        TExpressionStatementSyntax,
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

    protected AbstractUseCollectionInitializerDiagnosticAnalyzer()
        : base(
            [
                (s_descriptor, CodeStyleOptions2.PreferCollectionInitializer)
            ])
    {
    }

    protected abstract ISyntaxFacts SyntaxFacts { get; }

    protected abstract bool AreCollectionInitializersSupported(Compilation compilation);
    protected abstract bool AreCollectionExpressionsSupported(Compilation compilation);
    protected abstract bool CanUseCollectionExpression(
        SemanticModel semanticModel, TObjectCreationExpressionSyntax objectCreationExpression, INamedTypeSymbol? expressionType, bool allowSemanticsChange, CancellationToken cancellationToken, out bool changesSemantics);

    protected abstract TAnalyzer GetAnalyzer();

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

        // not point in analyzing if both options are off.
        if (!preferInitializerOption.Value
            && preferExpressionOption.Value == Shared.CodeStyle.CollectionExpressionPreference.Never
            && !ShouldSkipAnalysis(context.FilterTree, context.Options, context.Compilation.Options,
                    [preferInitializerOption.Notification, preferExpressionOption.Notification],
                    context.CancellationToken))
        {
            return;
        }

        // Object creation can only be converted to collection initializer if it implements the IEnumerable type.
        var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
        if (objectType.Type == null || !objectType.Type.AllInterfaces.Contains(ienumerableType))
            return;

        // Analyze the surrounding statements. First, try a broader set of statements if the language supports
        // collection expressions. 
        var syntaxFacts = this.SyntaxFacts;
        using var analyzer = GetAnalyzer();

        var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();

        var collectionExpressionMatches = GetCollectionExpressionMatches();
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

        var nodes = containingStatement is null
            ? ImmutableArray<SyntaxNode>.Empty
            : [containingStatement];
        nodes = nodes.AddRange(matches.Select(static m => m.Statement));
        if (syntaxFacts.ContainsInterleavedDirective(nodes, cancellationToken))
            return;

        var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

        var notification = shouldUseCollectionExpression ? preferExpressionOption.Notification : preferInitializerOption.Notification;
        var properties = shouldUseCollectionExpression ? UseCollectionInitializerHelpers.UseCollectionExpressionProperties : ImmutableDictionary<string, string?>.Empty;
        if (changesSemantics)
            properties = properties.Add(UseCollectionInitializerHelpers.ChangesSemanticsName, "");

        context.ReportDiagnostic(DiagnosticHelper.Create(
            s_descriptor,
            objectCreationExpression.GetFirstToken().GetLocation(),
            notification,
            context.Options,
            additionalLocations: locations,
            properties));

        FadeOutCode(context, matches, locations, properties);

        return;

        (ImmutableArray<Match<TStatementSyntax>> matches, bool shouldUseCollectionExpression, bool changesSemantics)? GetCollectionInitializerMatches()
        {
            if (containingStatement is null)
                return null;

            if (!preferInitializerOption.Value)
                return null;

            var matches = analyzer.Analyze(semanticModel, syntaxFacts, objectCreationExpression, analyzeForCollectionExpression: false, cancellationToken);

            // If analysis failed, we can't change this, no matter what.
            if (matches.IsDefault)
                return null;

            return (matches, shouldUseCollectionExpression: false, changesSemantics: false);
        }

        (ImmutableArray<Match<TStatementSyntax>> matches, bool shouldUseCollectionExpression, bool changesSemantics)? GetCollectionExpressionMatches()
        {
            if (preferExpressionOption.Value == CollectionExpressionPreference.Never)
                return null;

            // Don't bother analyzing for the collection expression case if the lang/version doesn't even support it.
            if (!this.AreCollectionExpressionsSupported(context.Compilation))
                return null;

            var matches = analyzer.Analyze(semanticModel, syntaxFacts, objectCreationExpression, analyzeForCollectionExpression: true, cancellationToken);

            // If analysis failed, we can't change this, no matter what.
            if (matches.IsDefault)
                return null;

            // Check if it would actually be legal to use a collection expression here though.
            var allowSemanticsChange = preferExpressionOption.Value == CollectionExpressionPreference.WhenTypesLooselyMatch;
            if (!CanUseCollectionExpression(semanticModel, objectCreationExpression, expressionType, allowSemanticsChange, cancellationToken, out var changesSemantics))
                return null;

            return (matches, shouldUseCollectionExpression: true, changesSemantics);
        }
    }

    private void FadeOutCode(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<Match<TStatementSyntax>> matches,
        ImmutableArray<Location> locations,
        ImmutableDictionary<string, string?>? properties)
    {
        var syntaxFacts = this.SyntaxFacts;

        foreach (var match in matches)
        {
            var additionalUnnecessaryLocations = UseCollectionInitializerHelpers.GetLocationsToFade(
                syntaxFacts, match);
            if (additionalUnnecessaryLocations.IsDefaultOrEmpty)
                continue;

            // Report the diagnostic at the first unnecessary location. This is the location where the code fix
            // will be offered.
            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                s_unnecessaryCodeDescriptor,
                additionalUnnecessaryLocations[0],
                NotificationOption2.ForSeverity(s_unnecessaryCodeDescriptor.DefaultSeverity),
                context.Options,
                additionalLocations: locations,
                additionalUnnecessaryLocations: additionalUnnecessaryLocations,
                properties));
        }
    }
}
