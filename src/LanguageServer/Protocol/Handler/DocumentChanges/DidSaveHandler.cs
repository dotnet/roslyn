// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;

[ExportCSharpVisualBasicStatelessLspService(typeof(DidSaveHandler)), Shared]
[Method(Methods.TextDocumentDidSaveName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class DidSaveHandler() : ILspServiceNotificationHandler<DidSaveTextDocumentParams>, ITextDocumentIdentifierHandler<DidSaveTextDocumentParams, TextDocumentIdentifier>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(DidSaveTextDocumentParams request) => request.TextDocument;

    public async Task HandleNotificationAsync(DidSaveTextDocumentParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.GetRequiredDocument();
        var workspace = document.Project.Solution.Workspace;
        context.TraceDebug($"RefreshSourceGenerators for {document.Project.Id.DebugName}");

        workspace.EnqueueUpdateSourceGeneratorVersion(document.Project.Id, forceRegeneration: false);
    }
}