// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

/// <summary>
/// Interaction logic for InlineRenameAdornment.xaml
/// </summary>
internal partial class RenameFlyout : InlineRenameAdornment
{
    private const int DefaultMinWidth = 200;

    private readonly RenameFlyoutViewModel _viewModel;
    private readonly IWpfTextView _textView;
    private readonly IAsyncQuickInfoBroker _asyncQuickInfoBroker;
    private readonly IAsynchronousOperationListener _listener;
    private readonly IThreadingContext _threadingContext;

    public RenameFlyout(
        RenameFlyoutViewModel viewModel,
        IWpfTextView textView,
        IAsyncQuickInfoBroker asyncQuickInfoBroker,
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        DataContext = _viewModel = viewModel;
        _textView = textView;
        _asyncQuickInfoBroker = asyncQuickInfoBroker;
        _textView.LayoutChanged += TextView_LayoutChanged;
        _textView.ViewportHeightChanged += TextView_ViewPortChanged;
        _textView.ViewportWidthChanged += TextView_ViewPortChanged;
        _listener = listenerProvider.GetListener(FeatureAttribute.InlineRenameFlyout);
        _threadingContext = threadingContext;

        RenameUserInput = _viewModel.SmartRenameViewModel is null ? new RenameUserInputTextBox(_viewModel) : new SmartRenameUserInputComboBox(_viewModel);

        if (RenameUserInput is Control renameControl)
        {
            renameControl.IsTabStop = true;
            renameControl.Focusable = true;
        }

        // On load focus the first tab target
        var token1 = _listener.BeginAsyncOperation(nameof(RenameUserInput.GotFocus));
        Loaded += (s, e) =>
        {
            // Wait until load to position adornment for space negotiation
            PositionAdornment();

            RenameUserInput.Focus();
            RenameUserInput.SelectText(_viewModel.StartingSelection.Start, _viewModel.StartingSelection.Length);
            RenameUserInput.TextSelectionChanged += RenameUserInput_TextSelectionChanged;
            RenameUserInput.GotFocus += RenameUserInput_GotFocus;
            token1.Dispose();
        };

        InitializeComponent();

        RenameUserInputPresenter.Content = RenameUserInput;
        RenameUserInput.PreviewKeyDown += RenameUserInput_PreviewKeyDown;

        // If smart rename is available, insert the control after the identifier text box.
        if (viewModel.SmartRenameViewModel is not null)
        {
            var smartRenameControl = new SmartRenameStatusControl(viewModel.SmartRenameViewModel);
            var index = MainPanel.Children.IndexOf(IdentifierAndExpandButtonGrid);
            MainPanel.Children.Insert(index + 1, smartRenameControl);
        }

        // Dismiss any current tooltips. Note that this does not disable tooltips
        // from showing up again, so if a user has the mouse unmoved another
        // tooltip will pop up. https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1611398
        // tracks when we can handle this with IFeaturesService in VS
        var token2 = _listener.BeginAsyncOperation(nameof(DismissToolTipsAsync));
        _ = DismissToolTipsAsync().CompletesAsyncOperation(token2);
    }

    internal IRenameUserInput RenameUserInput { get; }

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
    public string SubmitText
        => _viewModel.SmartRenameViewModel is not null
        ? _viewModel.SmartRenameViewModel.SubmitTextOverride
        : EditorFeaturesWpfResources.Enter_to_rename_shift_enter_to_preview;
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
        try
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

            MaxWidth = _textView.ViewportRight;
            MinWidth = Math.Min(DefaultMinWidth, _textView.ViewportWidth);

            // Top can be negative if the viewport is scrolled up, but not left
            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, Math.Max(0, left));
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
            _viewModel.Cancel();
        }
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

            case Key.Space:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    // If smart rename is available, trigger or toggle it.
                    if (_viewModel.SmartRenameViewModel is not null)
                    {
                        _viewModel.SmartRenameViewModel.ToggleOrTriggerSuggestions();
                        e.Handled = true;
                    }
                }

                break;
        }
    }

    private void Adornment_ConsumeMouseEvent(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void ToggleExpand(object sender, RoutedEventArgs e)
    {
        _viewModel.IsExpanded = !_viewModel.IsExpanded;
    }

    private void RenameUserInput_GotFocus(object sender, RoutedEventArgs e)
    {
        this.RenameUserInput.SelectAllText();
    }

    /// <summary>
    /// Respond to selection/cursor changes in the textbox the user is editing by
    /// applying the same selection to the textview that initiated the command
    /// </summary>
    private void RenameUserInput_TextSelectionChanged(object sender, RoutedEventArgs e)
    {
        // When user is editing the text or make selection change in the text box, sync the selection with text view
        if (!this.RenameUserInput.IsFocused)
        {
            return;
        }

        var start = RenameUserInput.TextSelectionStart;
        var length = RenameUserInput.TextSelectionLength;

        var buffer = _viewModel.InitialTrackingSpan.TextBuffer;
        var startPoint = _viewModel.InitialTrackingSpan.GetStartPoint(buffer.CurrentSnapshot);
        _textView.SetSelection(new SnapshotSpan(startPoint + start, length));
    }

    private void RenameUserInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // When smart rename is available, allow the user choose the suggestions using the up/down keys.
        _threadingContext.ThrowIfNotOnUIThread();
        var smartRenameViewModel = _viewModel.SmartRenameViewModel;
        if (smartRenameViewModel is not null)
        {
            var currentIdentifier = RenameUserInput.Text;
            if (e.Key is Key.Down or Key.Up)
            {
                var newIdentifier = smartRenameViewModel.ScrollSuggestions(currentIdentifier, down: e.Key == Key.Down);
                if (newIdentifier is not null)
                {
                    _viewModel.IdentifierText = newIdentifier;
                    // Place the cursor at the end of the input text box.
                    RenameUserInput.SelectText(newIdentifier.Length, 0);
                    e.Handled = true;
                }
            }
        }
    }
}
