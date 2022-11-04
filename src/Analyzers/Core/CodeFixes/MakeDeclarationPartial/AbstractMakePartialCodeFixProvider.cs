// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.MakeDeclarationPartial
{
    internal abstract class AbstractMakeDeclarationPartialCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        // This code fix addresses a very specific compiler error. It's unlikely there will be more than 1 of them at a time.
        protected AbstractMakeDeclarationPartialCodeFixProvider()
            : base(supportsFixAll: false)
        {
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CodeFixesResources.Make_partial, nameof(CodeFixesResources.Make_partial));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var syntaxRoot = editor.OriginalRoot;
            var generator = editor.Generator;

            foreach (var diagnostic in diagnostics)
            {
                var declaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                var fixedModifiers = generator.GetModifiers(declaration).WithPartial(true);
                editor.ReplaceNode(declaration, generator.WithModifiers(declaration, fixedModifiers));
            }

            return Task.CompletedTask;
        }
    }
}
