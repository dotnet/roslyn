// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IHtmlCodeActionProvider)), Shared]
[method: ImportingConstructor]
internal sealed class HtmlCodeActionProvider(IRazorEditService razorEditService) : IHtmlCodeActionProvider
{
    private readonly IRazorEditService _razorEditService = razorEditService;

    public async Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(
        RazorCodeActionContext context,
        ImmutableArray<RazorVSInternalCodeAction> codeActions,
        CancellationToken cancellationToken)
    {
        using var results = new PooledArrayBuilder<RazorVSInternalCodeAction>(codeActions.Length);
        foreach (var codeAction in codeActions)
        {
            if (codeAction.Edit is not null)
            {
                await HtmlCodeActionHelpers.MapAndFixHtmlCodeActionEditAsync(_razorEditService, context.DocumentSnapshot, codeAction, cancellationToken).ConfigureAwait(false);

                results.Add(codeAction);
            }
            else
            {
                results.Add(codeAction.WrapResolvableCodeAction(context, language: RazorLanguageKind.Html));
            }
        }

        return results.ToImmutable();
    }
}
