// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.UseCoalesceExpressionForNullableTernaryConditionalCheck), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class UseCoalesceExpressionForNullableTernaryConditionalCheckCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticId];

    protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
        => !diagnostic.Descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary);

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_coalesce_expression, nameof(AnalyzersResources.Use_coalesce_expression));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var expressionTypeOpt = semanticModel.Compilation.ExpressionOfTType();

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
        var generator = editor.Generator;
        var root = editor.OriginalRoot;

        foreach (var diagnostic in diagnostics)
        {
            var conditionalExpression = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            var conditionExpression = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
            var whenPart = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out var condition, out var whenTrue, out var whenFalse);

            editor.ReplaceNode(conditionalExpression,
                (c, g) =>
                {
                    syntaxFacts.GetPartsOfConditionalExpression(
                        c, out var currentCondition, out var currentWhenTrue, out var currentWhenFalse);

                    var coalesceExpression = whenPart == whenTrue
                        ? g.CoalesceExpression(conditionExpression, syntaxFacts.WalkDownParentheses(currentWhenTrue))
                        : g.CoalesceExpression(conditionExpression, syntaxFacts.WalkDownParentheses(currentWhenFalse));

                    // We may be moving from `a == null ? b : a` to `a ?? b`.  In this case, we want to ensure that the
                    // space after the 'b' can be cleaned up if needed.
                    coalesceExpression = coalesceExpression.WithAppendedTrailingTrivia(syntaxFacts.ElasticMarker);

                    if (semanticFacts.IsInExpressionTree(
                            semanticModel, conditionalExpression, expressionTypeOpt, cancellationToken))
                    {
                        coalesceExpression = coalesceExpression.WithAdditionalAnnotations(
                            WarningAnnotation.Create(AnalyzersResources.Changes_to_expression_trees_may_result_in_behavior_changes_at_runtime));
                    }

                    return coalesceExpression;
                });
        }
    }
}
