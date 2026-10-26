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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using ShellControls = Microsoft.VisualStudio.Shell.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

internal sealed partial class InheritanceMarginGlyph : UserControl
{
    private readonly IThreadingContext _threadingContext;
    private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
    private readonly IUIThreadOperationExecutor _operationExecutor;
    private readonly Workspace _workspace;
    private readonly IWpfTextView _textView;
    private readonly IAsynchronousOperationListener _listener;
    private ShellControls.Popup? _hoverPopup;
    private bool _openMenuWhenHoverOpens;

    private ShellControls.DropDownButton HoverButton
    {
        get
        {
            Contract.ThrowIfNull(_hoverPopup);
            return (ShellControls.DropDownButton)((Border)_hoverPopup.Child).Child;
        }
    }

    private InheritanceMarginContextMenu InheritanceMenu => (InheritanceMarginContextMenu)HoverButton.ContextMenu;

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
        DataContext = InheritanceMarginGlyphViewModel.Create(classificationTypeMap, classificationFormatMap, tag, textView.ZoomLevel);
        var textFormattingMode = (TextFormattingMode?)textView.VisualElement.GetValue(ShellControls.TextBlock.PreferredTextFormattingModeProperty)
            ?? TextOptions.GetTextFormattingMode(textView.VisualElement);
        TextOptions.SetTextFormattingMode(this, textFormattingMode);
    }

    private CustomPopupPlacement[] PlaceHoverPopup(Size popupSize, Size targetSize, Point offset)
    {
        Contract.ThrowIfNull(_hoverPopup);
        var button = HoverButton;
        // Callback coordinates already include device scaling and editor zoom.
        var image = (FrameworkElement)button.Content;
        var translation = (TranslateTransform)image.RenderTransform;
        var imageOrigin = image.TranslatePoint(default, _hoverPopup.Child);
        imageOrigin.Y -= translation.Y;
        var scaleX = popupSize.Width / _hoverPopup.Child.RenderSize.Width;
        var scaleY = popupSize.Height / _hoverPopup.Child.RenderSize.Height;
        var position = new Point(
            -Math.Round(imageOrigin.X * scaleX, MidpointRounding.AwayFromZero),
            -Math.Round(imageOrigin.Y * scaleY, MidpointRounding.AwayFromZero));
        var preferredTop = position.Y;
        var buttonOrigin = button.TranslatePoint(default, _hoverPopup.Child);
        var view = _textView.VisualElement;
        if (PresentationSource.FromVisual(view) is not null && view.RenderSize.Height > 0)
        {
            var anchor = GlyphImage.PointToScreen(default);
            var viewTop = view.PointToScreen(default).Y;
            var viewBottom = view.PointToScreen(new Point(0, view.RenderSize.Height)).Y;
            var buttonTop = position.Y + buttonOrigin.Y * scaleY;
            var minimumTop = viewTop - anchor.Y;
            var maximumTop = Math.Max(minimumTop, viewBottom - anchor.Y - button.ActualHeight * scaleY);
            position.Y += Math.Min(Math.Max(buttonTop, minimumTop), maximumTop) - buttonTop;
        }

        // Keep the image anchored to the resting glyph when the frame is moved inside the viewport.
        var imageOffset = position.Y == preferredTop ? 0 : -position.Y / scaleY - imageOrigin.Y;
        if (translation.Y != imageOffset)
            translation.Y = imageOffset;
        if (imageOffset == 0)
        {
            image.Clip = null;
        }
        else
        {
            var visibleImage = new Rect(
                0,
                buttonOrigin.Y + button.BorderThickness.Top - imageOrigin.Y - imageOffset,
                image.ActualWidth,
                Math.Max(0, button.ActualHeight - button.BorderThickness.Top - button.BorderThickness.Bottom));
            visibleImage.Intersect(new Rect(image.RenderSize));
            image.Clip = new RectangleGeometry(visibleImage);
        }

        return [new CustomPopupPlacement(position, PopupPrimaryAxis.None)];
    }

    private void OpenHoverPopup(bool openMenu = false)
    {
        if (_hoverPopup is null)
        {
            _hoverPopup = (ShellControls.Popup)((DataTemplate)Resources["HoverPopupTemplate"]).LoadContent();
            _hoverPopup.PlacementTarget = this;
            _hoverPopup.CustomPopupPlacementCallback = PlaceHoverPopup;
            TextOptions.SetTextFormattingMode(HoverButton, TextOptions.GetTextFormattingMode(this));
            ((FrameworkElement)HoverButton.Content).RenderTransform = new TranslateTransform();
            HoverButton.DropDown = InheritanceMenu;
            // The menu has its own compiled XAML; connect its events after loading the template.
            InheritanceMenu.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(TargetMenuItem_OnClick));
            InheritanceMenu.Closed += ContextMenu_OnClose;
            InheritanceMenu.Opened += ContextMenu_OnOpen;
            GlyphImage.Style = new Style(typeof(CrispImage))
            {
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding(nameof(_hoverPopup.IsOpen)) { Source = _hoverPopup },
                        Value = true,
                        Setters = { new Setter(OpacityProperty, 0.0) },
                    },
                },
            };
            ((Grid)Content).Children.Add(_hoverPopup);
        }

        _hoverPopup.PlacementRectangle = new Rect(
            GlyphImage.TranslatePoint(default, this),
            default(Size));
        _hoverPopup.IsOpen = true;
        if (openMenu)
        {
            if (PresentationSource.FromVisual(HoverButton) is not null)
                HoverButton.ShowDropDown();
            else
                _openMenuWhenHoverOpens = true;
        }
    }

    private void HoverPopup_OnOpened(object sender, EventArgs e)
    {
        if (_openMenuWhenHoverOpens)
        {
            _openMenuWhenHoverOpens = false;
            HoverButton.ShowDropDown();
        }
    }

    private void InheritanceMargin_OnMouseEnter(object sender, MouseEventArgs e)
    {
        OpenHoverPopup();
    }

    private void InheritanceMargin_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is MouseButton.Left or MouseButton.Right)
        {
            OpenHoverPopup(openMenu: true);
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
        if (_hoverPopup is null)
            return;

        if (!InheritanceMenu.IsOpen
            && !IsKeyboardFocusWithin
            && !HoverButton.IsKeyboardFocusWithin
            && !IsMouseOver
            && !IsMouseOverHoverButton())
        {
            CloseHoverPopup();
        }
    }

    private bool IsMouseOverHoverButton()
    {
        if (_hoverPopup is not { IsOpen: true })
            return false;

        var position = Mouse.GetPosition(HoverButton);
        return position.X >= 0 && position.Y >= 0
            && position.X < HoverButton.ActualWidth && position.Y < HoverButton.ActualHeight;
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
        if (_hoverPopup is not null)
        {
            InheritanceMenu.IsOpen = false;
            CloseHoverPopup();
        }
    }

    private void CloseHoverPopup()
    {
        _openMenuWhenHoverOpens = false;
        if (_hoverPopup is not null)
            _hoverPopup.IsOpen = false;
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
