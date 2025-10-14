// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.CodeAnalysis.Options;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class CodeLens
{
    public static Task<LSP.CodeLens[]?> GetCodeLensAsync(LSP.TextDocumentIdentifier textDocumentIdentifier, Document document, CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();

        return CodeLensHandler.GetCodeLensAsync(textDocumentIdentifier, document, globalOptions, cancellationToken);
    }

    public static Task<LSP.CodeLens> ResolveCodeLensAsync(LSP.CodeLens codeLens, Document document, CancellationToken cancellationToken)
    {
        return CodeLensResolveHandler.ResolveCodeLensAsync(codeLens, document, cancellationToken);
    }
}
