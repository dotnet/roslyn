// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeDeclarationPartial
{
    internal abstract class AbstractMakeDeclarationPartialCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CodeFixesResources.Make_partial, nameof(CodeFixesResources.Make_partial));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            foreach (var diagnostic in diagnostics)
            {
                var declaration = root.FindNode(diagnostic.Location.SourceSpan);
                var fixedModifiers = generator.GetModifiers(declaration).WithPartial(true);
                editor.ReplaceNode(declaration, generator.WithModifiers(declaration, fixedModifiers));
            }
        }
    }
}
