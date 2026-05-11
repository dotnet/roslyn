// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions;
using Microsoft.CodeAnalysis.Razor.CodeActions.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.CodeActions;

[Export(typeof(IRazorCodeActionResolver)), Shared]
[method: ImportingConstructor]
internal sealed class OOPGenerateEventHandlerCodeActionResolver(
    IRoslynCodeActionHelpers roslynCodeActionHelpers,
    IRazorFormattingService razorFormattingService,
    RemoteSnapshotManager snapshotManager)
    : GenerateEventHandlerCodeActionResolver(roslynCodeActionHelpers, razorFormattingService)
{
    private readonly RemoteSnapshotManager _snapshotManager = snapshotManager;

    protected override async Task<SyntaxTree?> GetCodeBehindSyntaxTreeAsync(DocumentContext documentContext, string codeBehindPath, CancellationToken cancellationToken)
    {
        if (documentContext is not RemoteDocumentContext remoteDocumentContext)
        {
            throw new InvalidOperationException($"{nameof(OOPGenerateEventHandlerCodeActionResolver)} can only be used with {nameof(RemoteDocumentContext)} instances.");
        }

        var razorDocumentSnapshot = _snapshotManager.GetSnapshot(remoteDocumentContext.TextDocument);
        var solution = razorDocumentSnapshot.TextDocument.Project.Solution;
        var projectId = razorDocumentSnapshot.TextDocument.Project.Id;

        if (solution.GetDocumentIdsWithFilePath(codeBehindPath).FirstOrDefault(id => id.ProjectId == projectId) is not { } codeBehindDocumentId)
        {
            return null;
        }

        if (!solution.TryGetDocument(codeBehindDocumentId, out var codeBehindDocument))
        {
            return null;
        }

        return await codeBehindDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
    }
}
