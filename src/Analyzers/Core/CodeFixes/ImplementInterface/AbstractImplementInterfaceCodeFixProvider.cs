// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal abstract class AbstractImplementInterfaceCodeFixProvider<TTypeSyntax> : CodeFixProvider
    where TTypeSyntax : SyntaxNode
{
    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var span = context.Span;
        var cancellationToken = context.CancellationToken;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(span.Start);
        if (!token.Span.IntersectsWith(span))
            return;

        var type = token.Parent.GetAncestorsOrThis<TTypeSyntax>().LastOrDefault();

        var service = document.GetRequiredLanguageService<IImplementInterfaceService>();
        var codeActions = await service.GetCodeActionsAsync(document, type, cancellationToken).ConfigureAwait(false);

        context.RegisterFixes(codeActions, context.Diagnostics);
    }
}
