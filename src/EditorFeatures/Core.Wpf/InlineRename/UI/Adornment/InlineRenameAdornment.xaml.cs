// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.InlineRename.Adornment
{
    /// <summary>
    /// Interaction logic for InlineRenameAdornment.xaml
    /// </summary>
    internal partial class InlineRenameAdornment : UserControl, IDisposable
    {
        private readonly InlineRenameAdornmentViewModel _viewModel;
        private readonly ITextView _textView;

        public InlineRenameAdornment(InlineRenameAdornmentViewModel viewModel, ITextView textView)
        {
            DataContext = _viewModel = viewModel;
            _textView = textView;

            _textView.LayoutChanged += TextView_LayoutChanged;
            _textView.ViewportHeightChanged += TextView_ViewPortChanged;
            _textView.ViewportWidthChanged += TextView_ViewPortChanged;
            _textView.LostAggregateFocus += TextView_LostFocus;
            _textView.Caret.PositionChanged += TextView_CursorChanged;

            // On initialization focus the first tab target
            Initialized += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.First));

            InitializeComponent();
            PositionAdornment();
        }

#pragma warning disable CA1822 // Mark members as static - used in xaml
        public string RenameOverloads => EditorFeaturesResources.Include_overload_s;
        public string SearchInComments => EditorFeaturesResources.Include_comments;
        public string SearchInStrings => EditorFeaturesResources.Include_strings;
        public string ApplyRename => EditorFeaturesResources.Apply1;
        public string CancelRename => EditorFeaturesResources.Cancel;
        public string PreviewChanges => EditorFeaturesResources.Preview_changes1;
        public string SubmitText => EditorFeaturesWpfResources.Enter_to_rename_shift_enter_to_preview;
#pragma warning restore CA1822 // Mark members as static

        private void TextView_CursorChanged(object sender, CaretPositionChangedEventArgs e)
            => _viewModel.Cancel();

        private void TextView_LostFocus(object sender, EventArgs e)
            => _viewModel.Cancel();

        private void TextView_ViewPortChanged(object sender, EventArgs e)
            => PositionAdornment();

        private void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
            => PositionAdornment();

        private void PositionAdornment()
        {
            var top = _textView.Caret.Bottom + 5;
            var left = _textView.Caret.Left - 5;

            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
        }

        public void Dispose()
        {
            _viewModel.Dispose();

            _textView.LayoutChanged -= TextView_LayoutChanged;
            _textView.ViewportHeightChanged -= TextView_ViewPortChanged;
            _textView.ViewportWidthChanged -= TextView_ViewPortChanged;
            _textView.LostAggregateFocus -= TextView_LostFocus;
            _textView.Caret.PositionChanged -= TextView_CursorChanged;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Submit();
        }

        private void Adornment_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    _viewModel.Submit();
                    break;

                case Key.Escape:
                    e.Handled = true;
                    _viewModel.Cancel();
                    break;

                case Key.Tab:
                    // We don't want tab to lose focus for the adornment, so manually 
                    // loop focus back to the first item that is focusable.
                    FrameworkElement lastItem = _viewModel.IsExpanded
                        ? FileRenameCheckbox
                        : IdentifierTextBox;

                    if (lastItem.IsFocused)
                    {
                        e.Handled = true;
                        MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                    }

                    break;
            }
        }

        private void IdentifierTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            IdentifierTextBox.SelectAll();
        }

        private void Adornment_ConsumeMouseEvent(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Adornment_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OldFocus == this)
            {
                return;
            }

            IdentifierTextBox.Focus();
            e.Handled = true;
        }

        private void ToggleExpand(object sender, RoutedEventArgs e)
        {
            _viewModel.IsExpanded = !_viewModel.IsExpanded;
        }
    }
}
