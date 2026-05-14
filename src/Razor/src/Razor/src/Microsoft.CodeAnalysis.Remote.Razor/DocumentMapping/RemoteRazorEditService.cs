// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

[Export(typeof(IRazorEditService)), Shared]
[method: ImportingConstructor]
internal sealed class RemoteRazorEditService(
    IDocumentMappingService documentMappingService,
    IClientSettingsManager clientSettingsManager,
    IFilePathService filePathService,
    RemoteSnapshotManager snapshotManager,
    ITelemetryReporter telemetryReporter)
    : RazorEditService(documentMappingService, clientSettingsManager, filePathService, telemetryReporter)
{
    private readonly RemoteSnapshotManager _snapshotManager = snapshotManager;

    protected override bool TryGetDocumentContext(IDocumentSnapshot contextDocumentSnapshot, Uri razorDocumentUri, VSProjectContext? projectContext, [NotNullWhen(true)] out DocumentContext? documentContext)
    {
        if (contextDocumentSnapshot is not RemoteDocumentSnapshot originSnapshot)
        {
            throw new InvalidOperationException("RemoteRazorEditService can only be used with RemoteDocumentSnapshot instances.");
        }

        var solution = originSnapshot.TextDocument.Project.Solution;
        if (!solution.TryGetRazorDocument(razorDocumentUri, out var razorDocument))
        {
            documentContext = null;
            return false;
        }

        var razorDocumentSnapshot = _snapshotManager.GetSnapshot(razorDocument);

        documentContext = new RemoteDocumentContext(razorDocumentUri, razorDocumentSnapshot);
        return true;
    }

    protected override async Task<Uri?> GetRazorDocumentUriAsync(IDocumentSnapshot contextDocumentSnapshot, Uri generatedDocumentUri, CancellationToken cancellationToken)
    {
        if (contextDocumentSnapshot is not RemoteDocumentSnapshot originSnapshot)
        {
            throw new InvalidOperationException("RemoteRazorEditService can only be used with RemoteDocumentSnapshot instances.");
        }

        var solution = originSnapshot.TextDocument.Project.Solution;
        var razorCodeDocument = await _snapshotManager.TryGetRazorCodeDocumentAsync(solution, generatedDocumentUri, cancellationToken).ConfigureAwait(false);
        if (razorCodeDocument is null)
        {
            return null;
        }

        return solution.GetRazorDocumentUri(razorCodeDocument);
    }
}
