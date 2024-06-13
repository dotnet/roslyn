// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild.Logging;

namespace Microsoft.CodeAnalysis.Workspaces.MSBuild.BuildHost;

internal class RemoteProjectFile : IRemoteProjectFile
{
    private readonly IProjectFile _projectFile;

    public RemoteProjectFile(IProjectFile projectFile)
    {
        _projectFile = projectFile;
    }

    public void Dispose()
    {
    }

    public Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken)
        => _projectFile.GetProjectFileInfosAsync(cancellationToken);

    public Task<ImmutableArray<DiagnosticLogItem>> GetDiagnosticLogItemsAsync(CancellationToken cancellationToken)
        => Task.FromResult(_projectFile.Log.ToImmutableArray());

}
