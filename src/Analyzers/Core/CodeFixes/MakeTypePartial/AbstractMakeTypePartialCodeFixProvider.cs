// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeTypePartial;

internal abstract class AbstractMakeTypePartialCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider(supportsFixAll: false)
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CodeFixesResources.Make_type_partial, nameof(CodeFixesResources.Make_type_partial));
    }

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var syntaxRoot = editor.OriginalRoot;
        var generator = editor.Generator;
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics)
        {
            var declaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);

            if (symbol is null)
            {
                Debug.Fail("Declared symbol must never be null here");
                continue;
            }

            foreach (var reference in symbol.DeclaringSyntaxReferences)
            {
                var node = await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var modifiers = generator.GetModifiers(node);

                if (!modifiers.IsPartial)
                {
                    var fixedModifiers = modifiers.WithPartial(true);
                    editor.ReplaceNode(node, generator.WithModifiers(node, fixedModifiers));
                }
            }
        }
    }
}
