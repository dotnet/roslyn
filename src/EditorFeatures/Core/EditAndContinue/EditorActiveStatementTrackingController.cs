// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[Shared]
[Export(typeof(IActiveStatementTrackingController))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorActiveStatementTrackingController(Lazy<IHostWorkspaceProvider> workspaceProvider) : IActiveStatementTrackingController
{
    private readonly IActiveStatementTrackingService _service
        = workspaceProvider.Value.Workspace.Services.SolutionServices.GetRequiredService<IActiveStatementTrackingService>();

    public void StartTracking(Solution solution, IActiveStatementSpanFactory spanProvider)
        => _service.StartTracking(solution, spanProvider);

    public ActiveStatementSpanProvider GetSpanProvider(Solution solution)
        => new((documentId, filePath, cancellationToken) => _service.GetSpansAsync(solution, documentId, filePath, cancellationToken));

    public void EndTracking()
        => _service.EndTracking();
}
