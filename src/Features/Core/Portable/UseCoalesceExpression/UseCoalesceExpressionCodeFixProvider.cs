// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal class UseCoalesceExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseCoalesceExpressionDiagnosticId);

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ApplyEdit(editor, syntaxFacts, diagnostic);
            }

            return SpecializedTasks.EmptyTask;
        }

        private static void ApplyEdit(SyntaxEditor editor, ISyntaxFactsService syntaxFacts, Diagnostic diagnostic)
        {
            var root = editor.OriginalRoot;
            var conditionalExpression = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
            var conditionalPartHigh = root.FindNode(diagnostic.AdditionalLocations[1].SourceSpan);
            var whenPart = root.FindNode(diagnostic.AdditionalLocations[2].SourceSpan);
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out var condition, out var whenTrue, out var whenFalse);

            var conditionalPartLow = syntaxFacts.WalkDownParentheses(conditionalPartHigh);
            editor.ReplaceNode(conditionalExpression,
                (c, g) =>
                {
                    syntaxFacts.GetPartsOfConditionalExpression(
                        c, out var currentCondition, out var currentWhenTrue, out var currentWhenFalse);

                    var coalesceExpression = GetCoalesceExpression(
                        syntaxFacts, g, whenPart, whenTrue, conditionalPartLow, 
                        currentWhenTrue, currentWhenFalse);

                    return coalesceExpression.WithAdditionalAnnotations(Formatter.Annotation);
                });
        }

        private static SyntaxNode GetCoalesceExpression(
            ISyntaxFactsService syntaxFacts, SyntaxGenerator generator, 
            SyntaxNode whenPart, SyntaxNode whenTrue, 
            SyntaxNode conditionalPartLow, 
            SyntaxNode currentWhenTrue, SyntaxNode currentWhenFalse)
        {
            return whenPart == whenTrue
                ? generator.CoalesceExpression(conditionalPartLow, syntaxFacts.WalkDownParentheses(currentWhenTrue))
                : generator.CoalesceExpression(conditionalPartLow, syntaxFacts.WalkDownParentheses(currentWhenFalse));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_coalesce_expression, createChangedDocument)
            {

            }
        }
    }
}