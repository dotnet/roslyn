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
/// Interaction logic for SmartRenameUserInputComboBox.xaml
/// </summary>
[TemplatePart(Name = InnerTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = DropDownPopup, Type = typeof(Popup))]
internal sealed partial class SmartRenameUserInputComboBox : ComboBox, IRenameUserInput
{
    private const string InnerTextBox = "PART_EditableTextBox";
    private const string DropDownPopup = "PART_Popup";

    private readonly SmartRenameViewModel _smartRenameViewModel;
    private readonly Lazy<TextBox> _innerTextBox;
    private Popup? _dropDownPopup;

    internal SmartRenameUserInputComboBox(RenameFlyoutViewModel viewModel)
    {
        Requires.NotNull(viewModel, nameof(viewModel));
        Requires.NotNull(viewModel.SmartRenameViewModel!, nameof(viewModel.SmartRenameViewModel));

        InitializeComponent();
        DataContext = viewModel.SmartRenameViewModel;

        _smartRenameViewModel = viewModel.SmartRenameViewModel;
        _innerTextBox = new Lazy<TextBox>(() =>
        {
            ApplyTemplate();
            return (TextBox)GetTemplateChild(InnerTextBox)!;
        });

        _smartRenameViewModel.SuggestedNames.CollectionChanged += SuggestedNames_CollectionChanged;
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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _dropDownPopup = (Popup)GetTemplateChild(DropDownPopup)!;
    }

    private void ComboBox_Unloaded(object sender, RoutedEventArgs e)
    {
        _smartRenameViewModel.SuggestedNames.CollectionChanged -= SuggestedNames_CollectionChanged;
    }

    private void ComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        e.Handled = true;
    }

    private void ComboBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        Assumes.NotNull(_dropDownPopup);
        _dropDownPopup.IsOpen = false;
    }

    private void ComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if ((e.Key is Key.Up or Key.Down) && Items.Count > 0)
        {
            Assumes.NotNull(_dropDownPopup);
            _dropDownPopup.IsOpen = true;
        }
    }

    private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0)
        {
            var identifierName = e.AddedItems[0].ToString();
            _smartRenameViewModel.SelectedSuggestedName = identifierName;
        }
    }

    private void ItemsPresenter_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        Assumes.NotNull(_dropDownPopup);
        _dropDownPopup.IsOpen = false;
    }

    private void InnerTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (Items.Count > 0)
        {
            Assumes.NotNull(_dropDownPopup);
            _dropDownPopup.IsOpen = true;
        }
    }

    private void InnerTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        Assumes.NotNull(_dropDownPopup);
        _dropDownPopup.IsOpen = false;
    }

    private void InnerTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        Assumes.NotNull(_dropDownPopup);
        if ((e.Key is Key.Escape or Key.Space or Key.Enter) && _dropDownPopup.IsOpen)
        {
            _dropDownPopup.IsOpen = false;
            SelectAllText();
            e.Handled = true;
        }
    }

    private void SuggestedNames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        Focus();
    }
}
