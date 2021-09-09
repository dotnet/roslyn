// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.CallstackExplorer;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.CallstackExplorer
{
    internal class CallstackExplorerViewModel : ViewModelBase
    {
        private readonly IThreadingContext _threadingContext;
        private readonly Workspace _workspace;

        public ObservableCollection<CallstackLineViewModel> CallstackLines { get; } = new();

        private bool _isLoading;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMap _formatMap;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private CallstackLineViewModel? _selection;
        public CallstackLineViewModel? Selection
        {
            get => _selection;
            set => SetProperty(ref _selection, value);
        }

        public CallstackExplorerViewModel(IThreadingContext threadingContext, Workspace workspace, ClassificationTypeMap classificationTypeMap, IClassificationFormatMap formatMap)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;

            workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
            _classificationTypeMap = classificationTypeMap;
            _formatMap = formatMap;
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            if (e.Kind == WorkspaceChangeKind.SolutionChanged)
            {
                Selection = null;
                CallstackLines.Clear();
            }
        }

        internal void OnPaste()
        {
            CallstackLines.Clear();
            var textObject = Clipboard.GetData(DataFormats.Text);

            if (textObject is string text)
            {
                OnPaste(text);
            }
        }

        internal void OnPaste(string text)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var result = await CallstackAnalyzer.AnalyzeAsync(text, _threadingContext.DisposalToken).ConfigureAwait(false);
                    var viewModels = result.ParsedLines.Select(l => new CallstackLineViewModel(l, _threadingContext, _workspace, _formatMap, _classificationTypeMap));

                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    Selection = null;
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
