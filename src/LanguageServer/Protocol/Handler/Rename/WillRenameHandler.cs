// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(WillRenameHandler)), Shared]
[Method(LSP.Methods.WorkspaceWillRenameFilesName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WillRenameHandler(
    [ImportMany] IEnumerable<Lazy<ILspWillRenameListener, ILspWillRenameListenerMetadata>> renameListeners) : ILspServiceRequestHandler<LSP.RenameFilesParams, WorkspaceEdit?>
{
    public bool MutatesSolutionState => true;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(RenameParams request) => request.TextDocument;

    public async Task<WorkspaceEdit?> HandleRequestAsync(RenameFilesParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        using var _1 = PooledDictionary<string, ArrayBuilder<TextEdit>>.GetInstance(out var changesBuilder);
        using var _2 = ArrayBuilder<SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>>.GetInstance(out var documentChangesBuilder);

        foreach (var listener in renameListeners)
        {
            var edit = await listener.Value.HandleWillRenameAsync(request, requestContext, cancellationToken).ConfigureAwait(false);

            if (edit is null)
            {
                continue;
            }

            if (edit.Changes is { } changes)
            {
                foreach (var (path, edits) in changes)
                {
                    if (!changesBuilder.TryGetValue(path, out var existingEdits))
                    {
                        existingEdits = ArrayBuilder<TextEdit>.GetInstance();
                        changesBuilder.Add(path, existingEdits);
                    }

                    existingEdits.AddRange(edits);
                }
            }
            else if (edit.DocumentChanges is { } documentChanges)
            {
                if (documentChanges.TryGetFirst(out var textDocumentEdits))
                {
                    foreach (var textDocumentEdit in textDocumentEdits)
                    {
                        documentChangesBuilder.Add(textDocumentEdit);
                    }
                }
                else if (documentChanges.TryGetSecond(out var sumTypes))
                {
                    foreach (var sumType in sumTypes)
                    {
                        documentChangesBuilder.Add(sumType);
                    }
                }
            }
        }

        if (changesBuilder.Count == 0 && documentChangesBuilder.Count == 0)
        {
            return null;
        }

        Contract.ThrowIfTrue(changesBuilder.Count > 0 && documentChangesBuilder.Count > 0, "Cannot have both changes and documentChanges in a WorkspaceEdit. Please honour the client capabilities.");

        if (changesBuilder.Count > 0)
        {
            var changes = new Dictionary<string, TextEdit[]>();
            foreach (var (path, editsBuilder) in changesBuilder)
            {
                changes[path] = editsBuilder.ToArrayAndFree();
            }

            return new WorkspaceEdit
            {
                Changes = changes
            };
        }

        return new WorkspaceEdit
        {
            DocumentChanges = documentChangesBuilder.ToArray()
        };
    }
}
