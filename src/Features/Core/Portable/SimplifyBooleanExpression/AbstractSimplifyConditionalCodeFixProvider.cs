// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

namespace Microsoft.CodeAnalysis.SimplifyBooleanExpression
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal sealed class SimplifyConditionalCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public const string Negate = nameof(Negate);
        public const string Or = nameof(Or);
        public const string And = nameof(And);
        public const string WhenTrue = nameof(WhenTrue);
        public const string WhenFalse = nameof(WhenFalse);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SimplifyConditionalCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory
            => CodeFixCategory.CodeQuality;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected sealed override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var generatorInternal = document.GetLanguageService<SyntaxGeneratorInternal>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var expr = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                generator.SyntaxFacts.GetPartsOfConditionalExpression(expr, out var condition, out var whenTrue, out var whenFalse);

                if (diagnostic.Properties.ContainsKey(Negate))
                    condition = generator.Negate(generatorInternal, condition, semanticModel, cancellationToken);

                var replacement = condition;
                if (diagnostic.Properties.ContainsKey(Or))
                {
                    var right = diagnostic.Properties.ContainsKey(WhenTrue) ? whenTrue : whenFalse;
                    replacement = generator.LogicalOrExpression(condition, right);
                }
                else if (diagnostic.Properties.ContainsKey(And))
                {
                    var right = diagnostic.Properties.ContainsKey(WhenTrue) ? whenTrue : whenFalse;
                    replacement = generator.LogicalAndExpression(condition, right);
                };

                editor.ReplaceNode(
                    expr, generator.AddParentheses(replacement.WithTriviaFrom(expr)));
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
