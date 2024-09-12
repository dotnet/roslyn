// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

/// <summary>
/// Interaction logic for RenameUserInputTextBox.xaml
/// </summary>
internal sealed partial class RenameUserInputTextBox : TextBox, IRenameUserInput
{
    internal RenameUserInputTextBox(RenameFlyoutViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public int TextSelectionStart
    {
        get => SelectionStart;
        set => SelectionStart = value;
    }

    public int TextSelectionLength
    {
        get => SelectionLength;
        set => SelectionLength = value;
    }

    public event RoutedEventHandler? TextSelectionChanged
    {
        add
        {
            AddHandler(SelectionChangedEvent, value, handledEventsToo: false);
        }
        remove
        {
            RemoveHandler(SelectionChangedEvent, value);
        }
    }

    event KeyEventHandler? IRenameUserInput.PreviewKeyDown
    {
        add
        {
            AddHandler(PreviewKeyDownEvent, value, handledEventsToo: false);
        }
        remove
        {
            RemoveHandler(PreviewKeyDownEvent, value);
        }
    }

    public void SelectText(int start, int length)
    {
        Select(start, length);
    }

    public void SelectAllText()
    {
        SelectAll();
    }

    void IRenameUserInput.Focus()
    {
        this.Focus();
    }
}
