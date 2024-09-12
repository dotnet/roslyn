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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SimplifyBooleanExpression;

using static SimplifyBooleanExpressionConstants;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.SimplifyConditionalExpression), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class SimplifyConditionalCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId];

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Simplify_conditional_expression, nameof(AnalyzersResources.Simplify_conditional_expression));
        return Task.CompletedTask;
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var generator = SyntaxGenerator.GetGenerator(document);
        var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // Walk the diagnostics in descending position order so that we process innermost conditionals before
        // outermost ones. Also, use ApplyExpressionLevelSemanticEditsAsync so that we can appropriately understand
        // the semantics of conditional nodes if we changed what was inside of them.

        await editor.ApplyExpressionLevelSemanticEditsAsync(
            document,
            diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start).ToImmutableArray(),
            d => d.Location.FindNode(getInnermostNodeForTie: true, cancellationToken),
            canReplace: (_, _, _) => true,
            (semanticModel, root, diagnostic, current) => root.ReplaceNode(current, SimplifyConditional(semanticModel, diagnostic, current)),
            cancellationToken).ConfigureAwait(false);

        return;

        SyntaxNode SimplifyConditional(SemanticModel semanticModel, Diagnostic diagnostic, SyntaxNode expr)
        {
            if (!syntaxFacts.IsConditionalExpression(expr))
                return expr;

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

            return generatorInternal.AddParentheses(replacement.WithTriviaFrom(expr));
        }
    }
}
