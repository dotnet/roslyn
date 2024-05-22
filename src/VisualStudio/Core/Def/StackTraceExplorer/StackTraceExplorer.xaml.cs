// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.StackTraceExplorer;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer;

/// <summary>
/// Interaction logic for CallstackExplorer.xaml
/// </summary>
internal partial class StackTraceExplorer : UserControl
{
    public readonly StackTraceExplorerViewModel ViewModel;

    public StackTraceExplorer(StackTraceExplorerViewModel viewModel)
    {
        DataContext = ViewModel = viewModel;
        InitializeComponent();

        DataObject.AddPastingHandler(this, OnPaste);
    }

    private void OnPaste(object sender, DataObjectPastingEventArgs e)
        => OnPaste();

    private void CommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        => OnPaste();

    public void OnPaste()
    {
        var text = Clipboard.GetText();
        ViewModel.OnPaste_CallOnUIThread(text);
    }

    private void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ViewModel.Selection is StackFrameViewModel stackFrameViewModel)
        {
            stackFrameViewModel.NavigateToSymbol();
        }
    }

    internal void OnClear()
    {
        ViewModel.OnClear();
    }

    private void TextBlock_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var textBlock = (TextBlock)sender;

        if (textBlock.IsVisible)
        {
            var peer = FrameworkElementAutomationPeer.FromElement(textBlock);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
    }
}
