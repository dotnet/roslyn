// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeDeclarationPartial
{
    internal abstract class AbstractMakeDeclarationPartialCodeFixProvider : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declarationNode = syntaxRoot.FindNode(context.Span);

            context.RegisterCodeFix(
                CodeAction.Create(
                    CodeFixesResources.Make_partial,
                    c => MakeDeclarationPartialAsync(document, declarationNode, c)),
                context.Diagnostics);
        }

        private static async Task<Document> MakeDeclarationPartialAsync(Document document, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Services);
            var generator = editor.Generator;

            var modifiers = generator.GetModifiers(declaration);
            var newDeclaration = generator.WithModifiers(declaration, modifiers.WithPartial(true));

            editor.ReplaceNode(declaration, newDeclaration);

            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }
    }
}
