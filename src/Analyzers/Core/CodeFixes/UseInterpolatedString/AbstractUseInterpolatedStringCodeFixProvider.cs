// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseInterpolatedString;

internal abstract partial class AbstractUseInterpolatedStringCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in diagnostics)
        {
            await this.FixOneAsync(document, semanticModel, diagnostic, editor, cancellationToken).ConfigureAwait(false);
        }
    }

    protected abstract Task FixOneAsync(Document document, SemanticModel semanticModel, Diagnostic diagnostic, SyntaxEditor editor, CancellationToken cancellationToken);
}
