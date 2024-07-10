// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal abstract class AbstractImplementInterfaceCodeFixProvider<TTypeSyntax> : CodeFixProvider
    where TTypeSyntax : SyntaxNode
{
    protected abstract bool IsTypeInInterfaceBaseList(TTypeSyntax type);

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

        var service = document.GetRequiredLanguageService<IImplementInterfaceService>();
        var options = context.Options.GetImplementTypeGenerationOptions(document.Project.Services);
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var type in token.Parent.GetAncestorsOrThis<TTypeSyntax>())
        {
            if (this.IsTypeInInterfaceBaseList(type))
            {
                var generators = service.GetGenerators(
                    document, options, model, type, cancellationToken);
                context.RegisterFixes(generators.SelectAsArray(
                    g => CodeAction.Create(
                        g.Title,
                        cancellationToken => g.ImplementInterfaceAsync(cancellationToken),
                        g.EquivalenceKey)), context.Diagnostics);
                break;
            }
        }
    }
}
