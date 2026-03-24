// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[Shared]
[Export(typeof(ISolutionSnapshotProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorHostSolutionProvider(Lazy<IHostWorkspaceProvider> workspaceProvider) : ISolutionSnapshotProvider
{
    public ValueTask<Solution> GetCurrentSolutionAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(workspaceProvider.Value.Workspace.CurrentSolution);
}
