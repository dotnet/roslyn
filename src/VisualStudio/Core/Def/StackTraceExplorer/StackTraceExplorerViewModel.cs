// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

internal sealed class StackTraceExplorerViewModel : ViewModelBase
{
    private readonly IThreadingContext _threadingContext;
    private readonly Workspace _workspace;
    public ObservableCollection<FrameViewModel> Frames { get; } = [];

    private readonly ClassificationTypeMap _classificationTypeMap;
    private readonly IClassificationFormatMap _formatMap;

    public bool IsLoading
    {
        get;
        set => SetProperty(ref field, value);
    }
    public FrameViewModel? Selection
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsListVisible => Frames.Count > 0;
    public bool IsInstructionTextVisible => Frames.Count == 0;

    private int _id;

    internal void OnClear()
    {
        Frames.Clear();
    }

    public string InstructionText => ServicesVSResources.Paste_valid_stack_trace;
    public string StackTrace => ServicesVSResources.Stack_Trace;

    public StackTraceExplorerViewModel(IThreadingContext threadingContext, Workspace workspace, ClassificationTypeMap classificationTypeMap, IClassificationFormatMap formatMap)
    {
        _threadingContext = threadingContext;
        _workspace = workspace;

        _classificationTypeMap = classificationTypeMap;
        _formatMap = formatMap;

        Frames.CollectionChanged += CallstackLines_CollectionChanged;
    }

    public bool Matches(string text) => text.GetHashCode() == _id;

    public void OnPaste_CallOnUIThread(string text)
    {
        IsLoading = true;
        Frames.Clear();
        var cancellationToken = _threadingContext.DisposalToken;
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var result = await StackTraceAnalyzer.AnalyzeAsync(text, cancellationToken).ConfigureAwait(false);
                await SetStackTraceResultAsync(result, text, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                IsLoading = false;
            }
        }, cancellationToken);
    }

    public async System.Threading.Tasks.Task SetStackTraceResultAsync(StackTraceAnalysisResult result, string originalText, System.Threading.CancellationToken cancellationToken)
    {
        _id = originalText.GetHashCode();
        var viewModels = result.ParsedFrames.Select(l => GetViewModel(l));

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        Selection = null;
        Frames.Clear();

        foreach (var vm in viewModels)
        {
            Frames.Add(vm);
        }
    }

    private void CallstackLines_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        NotifyPropertyChanged(nameof(IsListVisible));
        NotifyPropertyChanged(nameof(IsInstructionTextVisible));
    }

    private FrameViewModel GetViewModel(ParsedFrame frame)
        => frame switch
        {
            IgnoredFrame ignoredFrame => new IgnoredFrameViewModel(ignoredFrame, _formatMap, _classificationTypeMap),
            ParsedStackFrame stackFrame => new StackFrameViewModel(stackFrame, _threadingContext, _workspace, _formatMap, _classificationTypeMap),
            _ => throw ExceptionUtilities.UnexpectedValue(frame)
        };
}
