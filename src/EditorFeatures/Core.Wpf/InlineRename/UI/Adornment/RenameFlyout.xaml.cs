// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    /// <summary>
    /// Interaction logic for InlineRenameAdornment.xaml
    /// </summary>
    internal partial class RenameFlyout : InlineRenameAdornment
    {
        private readonly RenameFlyoutViewModel _viewModel;
        private readonly IWpfTextView _textView;
        private readonly IAsyncQuickInfoBroker _asyncQuickInfoBroker;
        private readonly IAsynchronousOperationListener _listener;

        public RenameFlyout(
            RenameFlyoutViewModel viewModel,
            IWpfTextView textView,
            IWpfThemeService? themeService,
            IAsyncQuickInfoBroker asyncQuickInfoBroker,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            DataContext = _viewModel = viewModel;
            _textView = textView;
            _asyncQuickInfoBroker = asyncQuickInfoBroker;
            _textView.LayoutChanged += TextView_LayoutChanged;
            _textView.ViewportHeightChanged += TextView_ViewPortChanged;
            _textView.ViewportWidthChanged += TextView_ViewPortChanged;
            _listener = listenerProvider.GetListener(FeatureAttribute.InlineRenameFlyout);

            // On load focus the first tab target
            Loaded += (s, e) =>
            {
                // Wait until load to position adornment for space negotiation
                PositionAdornment();

                IdentifierTextBox.Focus();
                IdentifierTextBox.Select(_viewModel.StartingSelection.Start, _viewModel.StartingSelection.Length);
                IdentifierTextBox.SelectionChanged += IdentifierTextBox_SelectionChanged;
            };

            InitializeComponent();

            if (themeService is not null)
            {
                Outline.BorderBrush = new SolidColorBrush(themeService.GetThemeColor(EnvironmentColors.AccentBorderColorKey));
                Background = new SolidColorBrush(themeService.GetThemeColor(EnvironmentColors.ToolWindowBackgroundColorKey));
            }

            // Dismiss any current tooltips. Note that this does not disable tooltips
            // from showing up again, so if a user has the mouse unmoved another
            // tooltip will pop up. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1611398
            // tracks when we can handle this with IFeaturesService in VS
            var token = _listener.BeginAsyncOperation(nameof(DismissToolTipsAsync));
            _ = DismissToolTipsAsync().CompletesAsyncOperation(token);
        }

        private async Task DismissToolTipsAsync()
        {
            var infoSession = _asyncQuickInfoBroker.GetSession(_textView);
            if (infoSession is null)
            {
                return;
            }

            await infoSession.DismissAsync().ConfigureAwait(false);
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

        private void TextView_ViewPortChanged(object sender, EventArgs e)
            => PositionAdornment();

        private void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // Since the textview will update for the buffer being updated, we only want to reposition
            // in cases where there was an actual view translation instead of EVERY time it updates. Otherwise
            // the user will see the flyout jumping as they type
            if (e.VerticalTranslation || e.HorizontalTranslation)
            {
                PositionAdornment();
            }
        }

        private void PositionAdornment()
        {
            var span = _viewModel.InitialTrackingSpan.GetSpan(_textView.TextSnapshot);
            var line = _textView.GetTextViewLineContainingBufferPosition(span.Start);
            var charBounds = line.GetCharacterBounds(span.Start);

            var height = DesiredSize.Height;
            var width = DesiredSize.Width;

            var desiredTop = charBounds.TextBottom + 5;
            var desiredLeft = charBounds.Left;

            var top = (desiredTop + height) > _textView.ViewportBottom
                ? _textView.ViewportBottom - height
                : desiredTop;

            var left = (desiredLeft + width) > _textView.ViewportRight
                ? _textView.ViewportRight - width
                : desiredLeft;

            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, left);
        }

        public override void Dispose()
        {
            _viewModel.Dispose();

            _textView.LayoutChanged -= TextView_LayoutChanged;
            _textView.ViewportHeightChanged -= TextView_ViewPortChanged;
            _textView.ViewportWidthChanged -= TextView_ViewPortChanged;

            // Restore focus back to the textview
            _textView.VisualElement.Focus();
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
                    _viewModel.PreviewChangesFlag = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
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

        /// <summary>
        /// Respond to selection/cursor changes in the textbox the user is editing by
        /// applying the same selection to the textview that initiated the command
        /// </summary>
        private void IdentifierTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            // When user is editing the text or make selection change in the text box, sync the selection with text view
            if (!this.IdentifierTextBox.IsFocused)
            {
                return;
            }

            var start = IdentifierTextBox.SelectionStart;
            var length = IdentifierTextBox.SelectionLength;

            var buffer = _viewModel.InitialTrackingSpan.TextBuffer;
            var startPoint = _viewModel.InitialTrackingSpan.GetStartPoint(buffer.CurrentSnapshot);
            _textView.SetSelection(new SnapshotSpan(startPoint + start, length));
        }
    }
}
