// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    internal class InheritanceMarginGlyph : Button
    {
        private const string ToolTipStyleKey = "ToolTipStyle";

        private static readonly object s_toolTipPlaceholder = new();

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

            Background = Brushes.Transparent;
            BorderBrush = Brushes.Transparent;

            Click += InheritanceMargin_OnClick;
            MouseEnter += InheritanceMargin_OnMouseEnter;
            MouseLeave += InheritanceMargin_OnMouseLeave;
            KeyUp += OnKeyUp;

            Resources.Add(ToolTipStyleKey, new Style(typeof(ToolTip))
            {
                Setters =
                {
                    new Setter(BackgroundProperty, new DynamicResourceExtension(EnvironmentColors.ToolTipBrushKey)),
                    new Setter(BorderBrushProperty, new DynamicResourceExtension(EnvironmentColors.ToolTipBorderBrushKey)),
                    new Setter(ForegroundProperty, new DynamicResourceExtension(EnvironmentColors.ToolTipTextBrushKey)),
                },
            });

            var viewModel = InheritanceMarginGlyphViewModel.Create(classificationTypeMap, classificationFormatMap, tag, textView.ZoomLevel);
            SetValue(AutomationProperties.NameProperty, viewModel.AutomationName);

            // Control template only shows the image
            var templateBorder = new FrameworkElementFactory(typeof(Border), "Border");
            templateBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            templateBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(BackgroundProperty));
            templateBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(BorderBrushProperty));

            var templateImage = new FrameworkElementFactory(typeof(CrispImage));
            templateImage.SetValue(CrispImage.MonikerProperty, viewModel.ImageMoniker);
            templateImage.SetValue(CrispImage.ScaleFactorProperty, viewModel.ScaleFactor);
            templateBorder.AppendChild(templateImage);

            Template = new ControlTemplate { VisualTree = templateBorder };
            DataContext = viewModel;

            // Add the context menu and tool tip as placeholders
            ContextMenu = new ContextMenu();
            ToolTip = s_toolTipPlaceholder;
        }

        protected override void OnToolTipOpening(ToolTipEventArgs e)
        {
            if (ToolTip == s_toolTipPlaceholder)
            {
                var viewModel = (InheritanceMarginGlyphViewModel)DataContext;
                ToolTip = new ToolTip { Content = viewModel.ToolTipTextBlock, Style = (Style)FindResource(ToolTipStyleKey) };
            }

            base.OnToolTipOpening(e);
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            LazyInitializeContextMenu();
            base.OnContextMenuOpening(e);
        }

        private void LazyInitializeContextMenu()
        {
            if (ContextMenu is not InheritanceMarginContextMenu)
            {
                var viewModel = (InheritanceMarginGlyphViewModel)DataContext;

                ContextMenu = new InheritanceMarginContextMenu(_threadingContext, _streamingFindUsagesPresenter, _operationExecutor, _workspace, _listener);
                ContextMenu.DataContext = viewModel;
                ContextMenu.ItemsSource = viewModel.MenuItemViewModels;
                ContextMenu.Opened += ContextMenu_OnOpen;
                ContextMenu.Closed += ContextMenu_OnClose;
            }
        }

        private void InheritanceMargin_OnClick(object sender, RoutedEventArgs e)
        {
            LazyInitializeContextMenu();
            ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void ChangeBorderToHoveringColor()
        {
            SetResourceReference(BackgroundProperty, VsBrushes.CommandBarMenuBackgroundGradientKey);
            SetResourceReference(BorderBrushProperty, VsBrushes.CommandBarMenuBorderKey);
        }

        private void InheritanceMargin_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ChangeBorderToHoveringColor();
        }

        private void InheritanceMargin_OnMouseLeave(object sender, MouseEventArgs e)
        {
            // If the context menu is open, then don't reset the color of the button because we need
            // the margin looks like being pressed.
            if (!ContextMenu.IsOpen)
            {
                ResetBorderToInitialColor();
            }
        }

        private void ContextMenu_OnClose(object sender, RoutedEventArgs e)
        {
            // If mouse is still hovering. Don't reset the color. The context menu might be closed because user clicks within the margin
            if (!IsMouseOver)
            {
                ResetBorderToInitialColor();
            }
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

        private void ResetBorderToInitialColor()
        {
            this.Background = Brushes.Transparent;
            this.BorderBrush = Brushes.Transparent;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            // Move the focus back to textView when the Esc is pressed
            // It ensures the focus won't be left at the margin
            if (e.Key == Key.Escape)
            {
                ResetFocus();
            }
        }

        private void ResetFocus()
        {
            if (!_textView.HasAggregateFocus)
            {
                var visualElement = _textView.VisualElement;
                if (visualElement.Focusable)
                {
                    Keyboard.Focus(visualElement);
                }
            }
        }
    }
}
