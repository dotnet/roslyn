// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseInterpolatedString;

internal abstract class AbstractUseInterpolatedStringCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    protected abstract Task FixOneAsync(Document document, Diagnostic diagnostic, SyntaxEditor editor, CancellationToken cancellationToken);

    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = [IDEDiagnosticIds.UseInterpolatedStringDiagnosticId];

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            if (root.FindNode(diagnostic.Location.SourceSpan) != null)
            {
                RegisterCodeFix(context, AnalyzersResources.Use_interpolated_string, nameof(AnalyzersResources.String_can_be_converted_to_interpolated_string));
            }
        }
    }

    protected sealed override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            await this.FixOneAsync(document, diagnostic, editor, cancellationToken).ConfigureAwait(false);
        }
    }
}
