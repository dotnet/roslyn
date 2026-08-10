// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed partial class WorkspaceProjectDiscoveryService
{
    internal readonly struct TestAccessor(WorkspaceProjectDiscoveryService instance)
    {
        internal void Initialize(ImmutableArray<string> workspaceFolders)
        {
            lock (instance._gate)
                instance._workspaceFolders = workspaceFolders.SelectAsArray(NormalizePath);
        }

        internal void AddWorkspaceFolder(string workspaceFolder)
            => instance.AddWorkspaceFolder(workspaceFolder);

        internal void RemoveWorkspaceFolder(string workspaceFolder)
            => instance.RemoveWorkspaceFolder(workspaceFolder);

        internal void NotifyProjectFileChanged(string projectFilePath)
            => instance.OnProjectFileChanged(sender: null, projectFilePath);

        internal ValueTask<ImmutableArray<string>> GetCandidateProjectsAsync(string filePath, CancellationToken cancellationToken)
            => instance.GetCandidateProjectsAsync(filePath, cancellationToken);

        internal ValueTask<ImmutableArray<string>> GetProjectsInDirectoryAsync(
            string directory, string workspaceFolder, CancellationToken cancellationToken)
            => instance.GetProjectsInDirectoryAsync(directory, workspaceFolder, cancellationToken);

        internal int ProjectDirectoryCount
        {
            get
            {
                lock (instance._gate)
                    return instance._projectDirectories.Count;
            }
        }

        internal int WorkspaceFolderCount
        {
            get
            {
                lock (instance._gate)
                    return instance._workspaceFolders.Length;
            }
        }
    }
}
