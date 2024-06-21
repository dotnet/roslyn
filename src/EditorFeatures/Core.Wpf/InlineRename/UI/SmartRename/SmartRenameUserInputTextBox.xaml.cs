// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

/// <summary>
/// Interaction logic for SmartRenameUserInputTextBox.xaml
/// </summary>
internal sealed partial class SmartRenameUserInputTextBox : TextBox, IRenameUserInput
{
    private readonly SmartRenameViewModel _smartRenameViewModel;

    internal SmartRenameUserInputTextBox(RenameFlyoutViewModel viewModel)
    {
        Requires.NotNull(viewModel, nameof(viewModel));
        Requires.NotNull(viewModel.SmartRenameViewModel!, nameof(viewModel.SmartRenameViewModel));

        InitializeComponent();
        DataContext = viewModel.SmartRenameViewModel;

        _smartRenameViewModel = viewModel.SmartRenameViewModel;
        _smartRenameViewModel.SuggestedNames.CollectionChanged += SuggestedNames_CollectionChanged;
    }

    public int TextSelectionStart
    {
        get => this.SelectionStart;
        set => this.SelectionStart = value;
    }

    public int TextSelectionLength
    {
        get => this.SelectionLength;
        set => this.SelectionLength = value;
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

    void IRenameUserInput.Focus()
    {
        this.Focus();
    }

    private void GetSuggestionsButtonClick(object sender, RoutedEventArgs e)
    {
        if (_smartRenameViewModel.SupportsAutomaticSuggestions)
        {
            _smartRenameViewModel.ToggleAutomaticSuggestions();
            if (_smartRenameViewModel.IsAutomaticSuggestionsEnabled)
            {
                _smartRenameViewModel.GetSuggestionsCommand.Execute(null);
            }
        }
        else
        {
            _smartRenameViewModel.GetSuggestionsCommand.Execute(null);
        }
    }

    private void Control_Unloaded(object sender, RoutedEventArgs e)
    {
        _smartRenameViewModel.SuggestedNames.CollectionChanged -= SuggestedNames_CollectionChanged;
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            var identifierName = e.AddedItems[0].ToString();
            _smartRenameViewModel.SelectedSuggestedName = identifierName;
        }
    }

    private void SuggestedNames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Focus();
    }

    public void SelectText(int start, int length)
    {
        this.Select(start, length);
    }

    public void SelectAllText()
    {
        this.SelectAll();
    }
}
