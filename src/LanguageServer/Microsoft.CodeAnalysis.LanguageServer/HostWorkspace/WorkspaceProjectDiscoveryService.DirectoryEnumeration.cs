// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed partial class WorkspaceProjectDiscoveryService
{
    private sealed class DirectoryEnumeration(string workspaceFolder, IFileChangeContext watcher)
    {
        private int _started;
        private int _watcherDisposed;

        public string WorkspaceFolder { get; } = workspaceFolder;
        public IFileChangeContext Watcher { get; } = watcher;
        public Dictionary<string, bool> Changes { get; } = new(PathUtilities.Comparer);
        public TaskCompletionSource<ImmutableArray<string>> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool TryStart()
            => Interlocked.Exchange(ref _started, 1) == 0;

        public void DisposeWatcher()
        {
            if (Interlocked.Exchange(ref _watcherDisposed, 1) == 0)
                Watcher.Dispose();
        }
    }
}
