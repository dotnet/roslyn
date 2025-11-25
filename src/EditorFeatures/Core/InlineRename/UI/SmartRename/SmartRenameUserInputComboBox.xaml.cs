// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

/// <summary>
/// Interaction logic for SmartRenameUserInputComboBox.xaml
/// </summary>
[TemplatePart(Name = InnerTextBox, Type = typeof(TextBox))]
internal sealed partial class SmartRenameUserInputComboBox : ComboBox, IRenameUserInput
{
    private const string InnerTextBox = "PART_EditableTextBox";

    private readonly SmartRenameViewModel _smartRenameViewModel;
    private readonly RenameFlyoutViewModel _baseViewModel;
    private readonly Lazy<TextBox> _innerTextBox;
    private bool _userChangedTextSelection;

    internal SmartRenameUserInputComboBox(RenameFlyoutViewModel viewModel)
    {
        Requires.NotNull(viewModel, nameof(viewModel));
        Requires.NotNull(viewModel.SmartRenameViewModel!, nameof(viewModel.SmartRenameViewModel));

        InitializeComponent();
        DataContext = viewModel.SmartRenameViewModel;
        _baseViewModel = viewModel;
        _smartRenameViewModel = viewModel.SmartRenameViewModel;
        _innerTextBox = new Lazy<TextBox>(() =>
        {
            ApplyTemplate();
            var textBox = (TextBox)GetTemplateChild(InnerTextBox)!;
            textBox.SelectionChanged += (sender, e) => _userChangedTextSelection = true;
            return textBox;
        });

        _smartRenameViewModel.SuggestedNames.CollectionChanged += SuggestedNames_CollectionChanged;
        AddHandler(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(OnItemGotKeyboardFocus), handledEventsToo: true);
    }

    public int TextSelectionStart
    {
        get => _innerTextBox.Value.SelectionStart;
        set => _innerTextBox.Value.SelectionStart = value;
    }

    public int TextSelectionLength
    {
        get => _innerTextBox.Value.SelectionLength;
        set => _innerTextBox.Value.SelectionLength = value;
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
        _innerTextBox.Value.Select(start, length);
    }

    public void SelectAllText()
    {
        _innerTextBox.Value.SelectAll();
    }

    void IRenameUserInput.Focus()
    {
        this.Focus();
    }

    private void ComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Handle the event to avoid stack overflow through passing execution back RenameFlyout.Adornment_GotKeyboardFocus
        e.Handled = true;
    }

    private void OnItemGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // When a ComboBoxItem receives keyboard focus (e.g., via Tab navigation),
        // update the ComboBox selection to match the focused item
        if (e.NewFocus is ComboBoxItem comboBoxItem && comboBoxItem.DataContext != null)
        {
            SelectedItem = comboBoxItem.DataContext;
        }
    }

    private void SuggestionsPanelScrollViewer_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        _baseViewModel.Submit();
    }

    private void GetSuggestionsButtonClick(object sender, RoutedEventArgs e)
    {
        _smartRenameViewModel.ToggleOrTriggerSuggestions();
    }

    private void ComboBox_Unloaded(object sender, RoutedEventArgs e)
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
        // RenameFlyout handles GotFocus event by calling SelectAllText, which selects all text in the input text box.
        // If user changed selection (e.g. by moving the caret or selecting text in the input text box we don't want
        // to step on it once rename suggestions became available.
        if (!_userChangedTextSelection)
        {
            Focus();
        }
    }
}
