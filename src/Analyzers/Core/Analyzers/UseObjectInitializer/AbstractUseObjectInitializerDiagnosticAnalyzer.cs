// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

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

    protected abstract bool FadeOutOperatorToken { get; }
    protected abstract TAnalyzer GetAnalyzer();

    protected AbstractUseObjectInitializerDiagnosticAnalyzer()
        : base(
            [
                (s_descriptor, CodeStyleOptions2.PreferObjectInitializer)
            ])
    {
    }

    protected abstract ISyntaxFacts GetSyntaxFacts();

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
        var language = objectCreationExpression.Language;
        var option = context.GetAnalyzerOptions().PreferObjectInitializer;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
        {
            // not point in analyzing if the option is off.
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

        context.ReportDiagnostic(DiagnosticHelper.Create(
            s_descriptor,
            objectCreationExpression.GetFirstToken().GetLocation(),
            option.Notification,
            context.Options,
            locations,
            properties: null));

        FadeOutCode(context, matches, locations);
    }

    private void FadeOutCode(
        SyntaxNodeAnalysisContext context,
        ImmutableArray<Match<TExpressionSyntax, TStatementSyntax, TMemberAccessExpressionSyntax, TAssignmentStatementSyntax>> matches,
        ImmutableArray<Location> locations)
    {
        var syntaxTree = context.Node.SyntaxTree;

        var syntaxFacts = GetSyntaxFacts();

        foreach (var match in matches)
        {
            var end = FadeOutOperatorToken
                ? syntaxFacts.GetOperatorTokenOfMemberAccessExpression(match.MemberAccessExpression).Span.End
                : syntaxFacts.GetExpressionOfMemberAccessExpression(match.MemberAccessExpression)!.Span.End;

            var location1 = Location.Create(syntaxTree, TextSpan.FromBounds(
                match.MemberAccessExpression.SpanStart, end));

            if (match.Statement.Span.End > match.Initializer.FullSpan.End)
            {
                context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                    s_unnecessaryCodeDescriptor,
                    location1,
                    NotificationOption2.ForSeverity(s_unnecessaryCodeDescriptor.DefaultSeverity),
                    context.Options,
                    additionalLocations: locations,
                    additionalUnnecessaryLocations: [syntaxTree.GetLocation(TextSpan.FromBounds(match.Initializer.FullSpan.End, match.Statement.Span.End))]));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_unnecessaryCodeDescriptor, location1, additionalLocations: locations));
            }
        }
    }

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
}
