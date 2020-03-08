// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyConditional
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpSimplifyConditionalCodeFixProvider :
        SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpSimplifyConditionalCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId);

        internal override CodeFixCategory CodeFixCategory
            => CodeFixCategory.CodeQuality;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var expr = (ConditionalExpressionSyntax)diagnostic.Location.FindNode(
                    getInnermostNodeForTie: true, cancellationToken);

                var replacement = expr.Condition;
                if (diagnostic.Properties.ContainsKey(CSharpSimplifyConditionalDiagnosticAnalyzer.Negate))
                    replacement = (ExpressionSyntax)generator.Negate(replacement, semanticModel, cancellationToken);

                editor.ReplaceNode(expr, replacement.WithTriviaFrom(expr).Parenthesize());
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Simplify_conditional_expression, createChangedDocument, FeaturesResources.Simplify_conditional_expression)
            {
            }
        }
    }
}
