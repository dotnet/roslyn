// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

internal class StackTraceExplorerTab
{
    private readonly StackTraceExplorerViewModel _stackExplorerVM;
    public int NameIndex { get; }
    public string Header => string.Format(ServicesVSResources.Stack_trace_0, NameIndex);
    public string CloseTab => ServicesVSResources.Close_tab;
    public StackTraceExplorer Content { get; }
    public ICommand CloseClick { get; }
    public event EventHandler? OnClosed;
    public bool IsEmpty => _stackExplorerVM.Frames.Count == 0;

    public StackTraceExplorerTab(IThreadingContext threadingContext, VisualStudioWorkspace workspace, IClassificationFormatMap formatMap, ClassificationTypeMap typeMap, int nameIndex)
    {
        NameIndex = nameIndex;

        _stackExplorerVM = new StackTraceExplorerViewModel(threadingContext, workspace, typeMap, formatMap);
        Content = new StackTraceExplorer(_stackExplorerVM);

        CloseClick = new DelegateCommand(_ =>
        {
            OnClosed?.Invoke(this, null);
        });
    }
}
