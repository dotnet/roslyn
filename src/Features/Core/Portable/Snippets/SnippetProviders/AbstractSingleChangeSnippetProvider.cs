// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal abstract class AbstractSingleChangeSnippetProvider<TSnippetSyntax> : AbstractSnippetProvider<TSnippetSyntax>
    where TSnippetSyntax : SyntaxNode
{
    protected abstract Task<TextChange> GenerateSnippetTextChangeAsync(Document document, int position, CancellationToken cancellationToken);

    protected sealed override async Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var change = await GenerateSnippetTextChangeAsync(document, position, cancellationToken).ConfigureAwait(false);
        return [change];
    }
}
