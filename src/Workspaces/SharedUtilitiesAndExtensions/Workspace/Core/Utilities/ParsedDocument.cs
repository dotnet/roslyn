// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis;

internal readonly record struct ParsedDocument(DocumentId Id, SourceText Text, SyntaxNode Root)
{
    public SyntaxTree SyntaxTree => Root.SyntaxTree;

    public static async ValueTask<ParsedDocument> CreateAsync(Document document, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return new ParsedDocument(document.Id, text, root);
    }

#if !CODE_STYLE
    public static ParsedDocument CreateSynchronously(Document document, CancellationToken cancellationToken)
    {
        var text = document.GetTextSynchronously(cancellationToken);
        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        return new ParsedDocument(document.Id, text, root);
    }
#endif
}
