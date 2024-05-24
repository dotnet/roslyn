// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.StackTraceExplorer;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

internal class StackTraceExplorerRootViewModel : ViewModelBase
{
    private readonly VisualStudioWorkspace _workspace;
    private readonly IClassificationFormatMap _formatMap;
    private readonly ClassificationTypeMap _typeMap;
    private readonly IThreadingContext _threadingContext;

    public StackTraceExplorerRootViewModel(IThreadingContext threadingContext, VisualStudioWorkspace workspace, IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
    {
        _threadingContext = threadingContext;
        _workspace = workspace;
        _formatMap = formatMap;
        _typeMap = typeMap;
    }

    public ObservableCollection<StackTraceExplorerTab> Tabs { get; } = [];

    private StackTraceExplorerTab? _selectedTab;
    public StackTraceExplorerTab? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    /// <summary>
    /// Returns true if there's a tab that already matches the text
    /// </summary>
    public bool ContainsTab(string text)
        => Tabs.Any(tab => tab.Content.ViewModel.Matches(text));

    public async Task AddNewTabAsync(StackTraceAnalysisResult? result, string originalText, CancellationToken cancellationToken)
    {
        // Name will always have an index. Use the highest index opened + 1.
        var highestIndex = Tabs.Count == 0
            ? 0
            : Tabs.Max(t => t.NameIndex);

        var newTab = new StackTraceExplorerTab(_threadingContext, _workspace, _formatMap, _typeMap, highestIndex + 1);
        Tabs.Add(newTab);

        SelectedTab = newTab;

        newTab.OnClosed += Tab_Closed;
        if (result.HasValue)
        {
            await newTab.Content.ViewModel.SetStackTraceResultAsync(result.Value, originalText, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task DoPasteAsync(CancellationToken cancellationToken)
    {
        var text = Clipboard.GetText();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var result = await StackTraceAnalyzer.AnalyzeAsync(text, cancellationToken).ConfigureAwait(false);
        if (SelectedTab is { IsEmpty: true })
        {
            // Paste in the SelectedTab instead of opening a new tab
            // for cases where there are no contents in the current tab
            await SelectedTab.Content.ViewModel.SetStackTraceResultAsync(result, text, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await AddNewTabAsync(result, text, cancellationToken).ConfigureAwait(false);
        }
    }

    public void DoPasteSynchronously(CancellationToken cancellationToken)
    {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
        _threadingContext.JoinableTaskFactory.Run(() => DoPasteAsync(cancellationToken));
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
    }

    private void Tab_Closed(object sender, System.EventArgs e)
    {
        var tab = (StackTraceExplorerTab)sender;
        Tabs.Remove(tab);
        tab.OnClosed -= Tab_Closed;
    }
}
