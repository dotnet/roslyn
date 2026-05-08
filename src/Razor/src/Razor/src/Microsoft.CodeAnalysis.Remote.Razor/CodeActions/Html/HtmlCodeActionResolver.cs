// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IHtmlCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class HtmlCodeActionResolver(IRazorEditService razorEditService) : IHtmlCodeActionResolver
{
    private readonly IRazorEditService _razorEditService = razorEditService;

    public string Action => LanguageServerConstants.CodeActions.Default;

    Task<CodeAction> IHtmlCodeActionResolver.ResolveAsync(
        RemoteDocumentContext documentContext,
        CodeAction codeAction,
        CancellationToken cancellationToken)
        => ResolveAsync(documentContext, codeAction, cancellationToken);

    public async Task<CodeAction> ResolveAsync(
        RemoteDocumentContext documentContext,
        CodeAction codeAction,
        CancellationToken cancellationToken)
    {
        await HtmlCodeActionHelpers.MapAndFixHtmlCodeActionEditAsync(_razorEditService, documentContext.Snapshot, codeAction, cancellationToken).ConfigureAwait(false);

        return codeAction;
    }
}
