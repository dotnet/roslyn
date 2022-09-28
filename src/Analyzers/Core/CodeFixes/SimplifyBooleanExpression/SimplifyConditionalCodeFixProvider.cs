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

namespace Microsoft.CodeAnalysis.SimplifyBooleanExpression
{
    using static SimplifyBooleanExpressionConstants;

    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.SimplifyConditionalExpression), Shared]
    internal sealed class SimplifyConditionalCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SimplifyConditionalCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId);

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, AnalyzersResources.Simplify_conditional_expression, nameof(AnalyzersResources.Simplify_conditional_expression));
            return Task.CompletedTask;
        }

        protected sealed override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var expr = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                syntaxFacts.GetPartsOfConditionalExpression(expr, out var condition, out var whenTrue, out var whenFalse);

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
                }

                editor.ReplaceNode(
                    expr, generatorInternal.AddParentheses(replacement.WithTriviaFrom(expr)));
            }
        }
    }
}
