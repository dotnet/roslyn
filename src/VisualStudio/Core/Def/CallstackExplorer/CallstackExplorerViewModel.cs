// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.CallstackExplorer;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CallstackExplorer
{
    internal class CallstackExplorerViewModel : ViewModelBase
    {
        private readonly IThreadingContext _threadingContext;
        private readonly Workspace _workspace;

        public ObservableCollection<CallstackLineViewModel> CallstackLines { get; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public CallstackExplorerViewModel(IThreadingContext threadingContext, Workspace workspace)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;

            workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionChanged)
            {
                // If the workspace changes we want to clear out the current stack trace
                CallstackLines.Clear();
            }
        }

        internal void OnPaste(string text)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var result = await CallstackAnalyzer.AnalyzeAsync(text, _threadingContext.DisposalToken).ConfigureAwait(false);
                    var viewModels = result.ParsedLines.Select(l => new CallstackLineViewModel(l, _threadingContext, _workspace));

                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    CallstackLines.Clear();

                    foreach (var vm in viewModels)
                    {
                        CallstackLines.Add(vm);
                    }
                }
                finally
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    IsLoading = false;
                }
            }, _threadingContext.DisposalToken);
        }
    }
}
