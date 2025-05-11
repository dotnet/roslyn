// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class SignatureHelp
{
    public static Task<LSP.SignatureHelp?> GetSignatureHelpAsync(Document document, LinePosition linePosition, bool supportsVisualStudioExtensions, CancellationToken cancellationToken)
    {
        var signatureHelpService = document.Project.Solution.Services.ExportProvider.GetService<SignatureHelpService>();

        return SignatureHelpHandler.GetSignatureHelpAsync(signatureHelpService, document, linePosition, supportsVisualStudioExtensions, cancellationToken);
    }
}
