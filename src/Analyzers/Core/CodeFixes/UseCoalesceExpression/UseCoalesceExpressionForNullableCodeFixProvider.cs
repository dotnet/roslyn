// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class UseCoalesceExpressionForNullableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseCoalesceExpressionForNullableCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseCoalesceExpressionForNullableDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var expressionTypeOpt = semanticModel.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
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

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Use_coalesce_expression, createChangedDocument)
            {

            }
        }
    }
}
