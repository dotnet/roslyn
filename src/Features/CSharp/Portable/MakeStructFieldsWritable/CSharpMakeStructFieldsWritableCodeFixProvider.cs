using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructFieldsWritable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.MakeStructFieldsWritable), Shared]
    internal class CSharpMakeStructFieldsWritableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.MakeStructFieldsWritable);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {

            var emptyToken = SyntaxFactory.Token(SyntaxKind.None);

            foreach (var diagnostic in diagnostics)
            {
                var diagnosticNode = diagnostic.Location.FindNode(cancellationToken);
                var structDeclaration = diagnosticNode.GetAncestors()
                    .OfType<StructDeclarationSyntax>()
                    .FirstOrDefault();

                var fieldDeclarations = structDeclaration.ChildNodes()
                    .OfType<FieldDeclarationSyntax>();

                foreach (var fieldDeclaration in fieldDeclarations)
                {
                    var readonlySyntaxToken = fieldDeclaration.ChildTokens()
                        .FirstOrDefault(token => token.IsKind(SyntaxKind.ReadOnlyKeyword));

                    if (readonlySyntaxToken != default)
                    {
                        var fieldWithoutReadonly = fieldDeclaration.ReplaceToken(readonlySyntaxToken, emptyToken);
                        editor.ReplaceNode(fieldDeclaration, fieldWithoutReadonly);
                    }
                }
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Make_readonly_fields_writable, createChangedDocument)
            {
            }
        }
    }

}
