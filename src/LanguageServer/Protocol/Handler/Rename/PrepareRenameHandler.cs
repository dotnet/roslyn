// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(PrepareRenameHandler)), Shared]
[Method(LSP.Methods.TextDocumentPrepareRenameName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class PrepareRenameHandler() : ILspServiceDocumentRequestHandler<LSP.PrepareRenameParams, LSP.Range?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.PrepareRenameParams request)
        => request.TextDocument;

    public Task<LSP.Range?> HandleRequestAsync(LSP.PrepareRenameParams request, RequestContext context, CancellationToken cancellationToken)
        => GetRenameRangeAsync(context.GetRequiredDocument(), ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken);

    internal static async Task<LSP.Range?> GetRenameRangeAsync(Document document, LinePosition linePosition, CancellationToken cancellationToken)
    {
        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);

        var symbolicRenameInfo = await SymbolicRenameInfo.GetRenameInfoAsync(
            document, position, cancellationToken).ConfigureAwait(false);
        if (symbolicRenameInfo.IsError)
            return null;

        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return ProtocolConversions.TextSpanToRange(symbolicRenameInfo.TriggerToken.Span, text);
    }
}
