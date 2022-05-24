// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnsealClass
{
    internal abstract class AbstractUnsealClassCodeFixProvider : CodeFixProvider
    {
        protected abstract string TitleFormat { get; }

        public override FixAllProvider GetFixAllProvider()
        {
            // This code fix addresses a very specific compiler error. It's unlikely there will be more than 1 of them at a time.
            return null;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var node = syntaxRoot.FindNode(context.Span, getInnermostNodeForTie: true);

            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is INamedTypeSymbol type &&
                type.TypeKind == TypeKind.Class && type.IsSealed && !type.IsStatic)
            {
                var definition = await SymbolFinder.FindSourceDefinitionAsync(
                    type, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                if (definition != null && definition.DeclaringSyntaxReferences.Length > 0)
                {
                    var title = string.Format(TitleFormat, type.Name);
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title,
                            c => UnsealDeclarationsAsync(document.Project.Solution, definition.DeclaringSyntaxReferences, c),
                            title),
                        context.Diagnostics);
                }
            }
        }

        private static async Task<Solution> UnsealDeclarationsAsync(
            Solution solution, ImmutableArray<SyntaxReference> declarationReferences, CancellationToken cancellationToken)
        {
            foreach (var (documentId, syntaxReferences) in
                declarationReferences.GroupBy(reference => solution.GetDocumentId(reference.SyntaxTree)))
            {
                var document = solution.GetDocument(documentId);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);
                var generator = editor.Generator;

                foreach (var syntaxReference in syntaxReferences)
                {
                    var declaration = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

                    var modifiers = generator.GetModifiers(declaration);
                    if (modifiers.IsSealed)
                    {
                        var newDeclaration = generator.WithModifiers(declaration, modifiers.WithIsSealed(false));

                        editor.ReplaceNode(declaration, newDeclaration);
                    }
                }

                solution = solution.WithDocumentSyntaxRoot(documentId, editor.GetChangedRoot());
            }

            return solution;
        }
    }
}
