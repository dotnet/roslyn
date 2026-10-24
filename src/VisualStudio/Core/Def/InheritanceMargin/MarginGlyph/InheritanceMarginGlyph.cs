// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

internal sealed partial class InheritanceMarginGlyph : UserControl
{
    private readonly IThreadingContext _threadingContext;
    private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
    private readonly IUIThreadOperationExecutor _operationExecutor;
    private readonly Workspace _workspace;
    private readonly IWpfTextView _textView;
    private readonly IAsynchronousOperationListener _listener;

    public InheritanceMarginGlyph(
        Workspace workspace,
        IThreadingContext threadingContext,
        IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
        ClassificationTypeMap classificationTypeMap,
        IClassificationFormatMap classificationFormatMap,
        IUIThreadOperationExecutor operationExecutor,
        InheritanceMarginTag tag,
        IWpfTextView textView,
        IAsynchronousOperationListener listener)
    {
        _threadingContext = threadingContext;
        _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
        _workspace = workspace;
        _operationExecutor = operationExecutor;
        _textView = textView;
        _listener = listener;

        InitializeComponent();
        // Scope the hover style to this surface, not its nested menu flyouts.
        HoverPopup.Child.SetValue(StyleProperty, HoverPopup.Resources["HoverFlyoutStyle"]);
        DataContext = InheritanceMarginGlyphViewModel.Create(classificationTypeMap, classificationFormatMap, tag, textView.ZoomLevel);
        HoverPopup.CustomPopupPlacementCallback = PlaceHoverPopup;
    }

    private CustomPopupPlacement[] PlaceHoverPopup(Size popupSize, Size targetSize, Point offset)
    {
        // Match image origins and leave device-pixel rounding to the popup.
        var imageOrigin = HoverImage.TranslatePoint(default, HoverPopup.Child);
        var scaleX = popupSize.Width / HoverPopup.Child.RenderSize.Width;
        var scaleY = popupSize.Height / HoverPopup.Child.RenderSize.Height;
        return [new CustomPopupPlacement(
            new Point(-imageOrigin.X * scaleX, -imageOrigin.Y * scaleY),
            PopupPrimaryAxis.None)];
    }

    private void OpenHoverPopup()
    {
        HoverPopup.PlacementRectangle = new Rect(
            GlyphImage.TranslatePoint(default, this),
            default(Size));
        HoverPopup.IsOpen = true;
    }

    private void InheritanceMargin_OnMouseEnter(object sender, MouseEventArgs e)
    {
        OpenHoverPopup();
    }

    private void InheritanceMargin_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is MouseButton.Left or MouseButton.Right)
        {
            OpenHoverPopup();
            HoverButton.ShowDropDown();
            e.Handled = true;
        }
    }

    private void InheritanceMargin_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (e.NewFocus == this)
        {
            OpenHoverPopup();
            if (HoverButton.IsVisible)
                HoverButton.Focus();
        }
    }

    private void InheritanceMargin_OnIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue)
            CloseHoverPopupIfInactive();
    }

    private void InheritanceMargin_OnMouseLeave(object sender, MouseEventArgs e)
        => CloseHoverPopupIfInactive();

    private void CloseHoverPopupIfInactive()
    {
        // Let mouse and focus transitions across the popup's presentation source finish before checking them.
        var token = _listener.BeginAsyncOperation(nameof(CloseHoverPopupIfInactive));
        var joinableTaskFactory = _threadingContext.JoinableTaskFactory.WithPriority(Dispatcher, DispatcherPriority.Input);
        joinableTaskFactory.RunAsync(async () =>
        {
            await joinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _threadingContext.DisposalToken);

            if (!InheritanceMenu.IsOpen
                && !IsKeyboardFocusWithin
                && !HoverButton.IsKeyboardFocusWithin
                && !IsMouseOver
                && !HoverButton.IsMouseOver)
            {
                CloseHoverPopup();
            }
        }).Task.CompletesAsyncOperation(token);
    }

    private void ContextMenu_OnClose(object sender, RoutedEventArgs e)
    {
        if (IsKeyboardFocusWithin || HoverButton.IsKeyboardFocusWithin || InheritanceMenu.IsKeyboardFocusWithin || Keyboard.IsKeyDown(Key.Escape))
            ResetFocus();

        CloseHoverPopupIfInactive();
    }

    private void ContextMenu_OnOpen(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ContextMenu { DataContext: InheritanceMarginGlyphViewModel inheritanceMarginViewModel }
            && inheritanceMarginViewModel.MenuItemViewModels.Any(static vm => vm is TargetMenuItemViewModel))
        {
            // We have two kinds of context menu. e.g.
            // 1. [margin] -> Header
            //                Target1
            //                Target2
            //                Target3
            //
            // 2. [margin] -> method Bar -> Header
            //                           -> Target1
            //                           -> Target2
            //             -> method Foo -> Header
            //                           -> Target3
            //                           -> Target4
            // If the first level of the context menu contains a TargetMenuItemViewModel, it means here it is case 1,
            // user is viewing the targets menu.
            InheritanceMarginLogger.LogInheritanceTargetsMenuOpen();
        }
    }

    private void InheritanceMargin_OnUnloaded(object sender, RoutedEventArgs e)
    {
        InheritanceMenu.IsOpen = false;
        CloseHoverPopup();
    }

    private void CloseHoverPopup()
    {
        HoverPopup.IsOpen = false;
    }

    private void TargetMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is MenuItem { DataContext: TargetMenuItemViewModel viewModel })
        {
            InheritanceMarginLogger.LogNavigateToTarget();

            var token = _listener.BeginAsyncOperation(nameof(TargetMenuItem_OnClick));
            TargetMenuItem_OnClickAsync(viewModel).CompletesAsyncOperation(token);
        }
    }

    private async Task TargetMenuItem_OnClickAsync(TargetMenuItemViewModel viewModel)
    {
        using var context = _operationExecutor.BeginExecute(
            title: EditorFeaturesResources.Navigating,
            defaultDescription: string.Format(ServicesVSResources.Navigate_to_0, viewModel.DisplayContent),
            allowCancellation: true,
            showProgress: false);

        var cancellationToken = context.UserCancellationToken;
        var rehydrated = await viewModel.DefinitionItem.TryRehydrateAsync(_workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);
        if (rehydrated == null)
            return;

        await _streamingFindUsagesPresenter.TryPresentLocationOrNavigateIfOneAsync(
            _threadingContext,
            _workspace,
            string.Format(CultureInfo.InvariantCulture, EditorFeaturesResources._0_declarations, viewModel.DisplayContent),
            [rehydrated],
            cancellationToken).ConfigureAwait(false);
    }

    private void HoverButton_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseHoverPopup();
            ResetFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Tab && (e.KeyboardDevice.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == 0)
        {
            // Tab navigation cannot cross the popup's separate presentation source.
            var direction = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0
                ? FocusNavigationDirection.Previous
                : FocusNavigationDirection.Next;
            CloseHoverPopup();
            MoveFocus(new TraversalRequest(direction));
            e.Handled = true;
        }
    }

    private void ResetFocus()
    {
        var visualElement = _textView.VisualElement;
        if (visualElement.Focusable)
        {
            Keyboard.Focus(visualElement);
        }
    }
}
