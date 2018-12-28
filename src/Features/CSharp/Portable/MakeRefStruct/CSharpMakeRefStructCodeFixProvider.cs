using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.MakeRefStruct
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class CSharpMakeRefStructCodeFixProvider : CodeFixProvider
    {
        // Error CS8345: Field or auto-implemented property cannot be of certain type unless it is an instance member of a ref struct.
        private const string CS8345 = nameof(CS8345);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS8345);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var span = context.Span;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var structDeclaration = FindContainingStruct(root, span);

            if (!structDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword)))
            {
                var gen = SyntaxGenerator.GetGenerator(document);
                var refStructDeclaration = gen.WithModifiers(structDeclaration,
                    gen.GetModifiers(structDeclaration).WithIsRef(true));

                context.RegisterCodeFix(
                    new MyCodeAction(
                        c => FixCodeAsync(context.Document, context.Span, refStructDeclaration, c),
                        CSharpFeaturesResources.Make_ref_struct),
                    context.Diagnostics.First());
            }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private async Task<Document> FixCodeAsync(Document document, TextSpan span, SyntaxNode refStructDeclaration, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);

            var structDeclaration = FindContainingStruct(root, span);
            root = root.ReplaceNode(structDeclaration, refStructDeclaration);

            return document.WithSyntaxRoot(root);
        }

        private StructDeclarationSyntax FindContainingStruct(SyntaxNode root, TextSpan span)
        {
            var member = root.FindNode(span);
            return member.FirstAncestorOrSelf<StructDeclarationSyntax>();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string title)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
