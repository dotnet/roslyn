// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    // unfortunately, we can't implement this on LanguageServerClient since this uses MEF v2 and
    // ILanguageClient requires MEF v1 and two can't be mixed exported in 1 class.
    [Export]
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class LanguageServerClientEventListener : IEventListener<object>
    {
        private readonly TaskCompletionSource<object> _taskCompletionSource;

        public Task WorkspaceStarted => _taskCompletionSource.Task;

        public LanguageServerClientEventListener()
        {
            _taskCompletionSource = new TaskCompletionSource<object>();
        }

        public void StartListening(Workspace workspace, object serviceOpt)
        {
            // mark that roslyn solution is added
            _taskCompletionSource.SetResult(null);
        }
    }
}
