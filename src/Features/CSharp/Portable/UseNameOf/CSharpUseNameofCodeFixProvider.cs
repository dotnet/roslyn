namespace Microsoft.CodeAnalysis.CSharp.UseNameOf
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Editing;
    using Microsoft.CodeAnalysis.Simplification;

    [Shared]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpUseNameofCodeFixProvider))]
    internal class CSharpUseNameofCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
            IDEDiagnosticIds.UseNameofDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    new CodeAction.DocumentChangeAction(
                        "Use nameof.",
                        _ => CreateChangedDocument(diagnostic),
                        "Use nameof."),
                    diagnostic);
            }

            return Task.CompletedTask;

            async Task<Document> CreateChangedDocument(Diagnostic diagnostic)
            {
                var syntaxRoot = await context.Document
                                              .GetSyntaxRootAsync(context.CancellationToken)
                                              .ConfigureAwait(false);

                if (syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true) is LiteralExpressionSyntax literal)
                {
                    var nameOf = diagnostic.Properties.ContainsKey(nameof(SyntaxGenerator.ThisExpression))
                        ? SyntaxFactory.ParseExpression($"nameof(this.{literal.Token.ValueText})").WithAdditionalAnnotations(Simplifier.Annotation)
                        : SyntaxFactory.ParseExpression($"nameof({literal.Token.ValueText})");
                    return context.Document.WithSyntaxRoot(
                        syntaxRoot.ReplaceNode(
                            literal,
                            nameOf));
                }

                return context.Document;
            }
        }
    }
}
