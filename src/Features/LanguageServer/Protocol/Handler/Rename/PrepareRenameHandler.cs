// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(PrepareRenameHandler)), Shared]
[Method(Methods.TextDocumentPrepareRenameName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class PrepareRenameHandler() : ILspServiceDocumentRequestHandler<PrepareRenameParams, DefaultBehaviorPrepareRename?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(PrepareRenameParams request)
        => request.TextDocument;

    public async Task<DefaultBehaviorPrepareRename?> HandleRequestAsync(PrepareRenameParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        Contract.ThrowIfNull(document);

        var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

        var symbolicRenameInfo = await SymbolicRenameInfo.GetRenameInfoAsync(
            document, position, cancellationToken).ConfigureAwait(false);
        if (symbolicRenameInfo.IsError)
            return null;

        return new DefaultBehaviorPrepareRename
        {
            DefaultBehavior = true,
        };
    }
}
