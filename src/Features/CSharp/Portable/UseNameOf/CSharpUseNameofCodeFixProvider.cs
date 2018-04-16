using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.UseNameOf
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpUseNameofCodeFixProvider)), Shared]
    internal class CSharpUseNameofCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDEDiagnosticIds.UseNameofDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    new CodeAction.DocumentChangeAction(
                        "Use nameof.",
                        cancellationToken => CreateChangedDocumentAsync(context.Document, diagnostic, cancellationToken),
                        "Use nameof."),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
                                           .ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is LiteralExpressionSyntax literal)
                {
                    var nameOf = diagnostic.Properties.ContainsKey(nameof(SyntaxGenerator.ThisExpression))
                        ? SyntaxFactory.ParseExpression($"nameof(this.{literal.Token.ValueText})").WithAdditionalAnnotations(Simplifier.Annotation)
                        : SyntaxFactory.ParseExpression($"nameof({literal.Token.ValueText})");
                    editor.ReplaceNode(literal, nameOf);
                }
            }
        }

        private static async Task<Document> CreateChangedDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken)
                                           .ConfigureAwait(false);

            if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is LiteralExpressionSyntax literal)
            {
                var nameOf = diagnostic.Properties.ContainsKey(nameof(SyntaxGenerator.ThisExpression))
                    ? SyntaxFactory.ParseExpression($"nameof(this.{literal.Token.ValueText})").WithAdditionalAnnotations(Simplifier.Annotation)
                    : SyntaxFactory.ParseExpression($"nameof({literal.Token.ValueText})");
                return document.WithSyntaxRoot(
                    syntaxRoot.ReplaceNode(
                        literal,
                        nameOf));
            }

            return document;
        }
    }
}
