// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    internal partial class InheritanceMargin
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly IWaitIndicator _waitIndicator;
        private readonly Workspace _workspace;
        private readonly IWpfTextView _textView;

        public InheritanceMargin(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMap classificationFormatMap,
            IWaitIndicator waitIndicator,
            InheritanceMarginTag tag,
            IWpfTextView textView)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _workspace = tag.Workspace;
            _waitIndicator = waitIndicator;
            _textView = textView;
            InitializeComponent();

            var viewModel = InheritanceMarginViewModel.Create(classificationTypeMap, classificationFormatMap, tag, textView.ZoomLevel);
            DataContext = viewModel;
            ContextMenu.DataContext = viewModel;
            ToolTip = new ToolTip { Content = viewModel.ToolTipTextBlock, Style = (Style)FindResource("ToolTipStyle") };
        }

        private void InheritanceMargin_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.ContextMenu != null)
            {
                this.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private void TargetMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem { DataContext: TargetMenuItemViewModel viewModel })
            {
                Logger.Log(FunctionId.InheritanceMargin_NavigateToTarget, KeyValueLogMessage.Create(LogType.UserAction));
                _waitIndicator.Wait(
                    title: EditorFeaturesResources.Navigating,
                    message: string.Format(ServicesVSResources.Navigate_to_0, viewModel.DisplayContent),
                    allowCancel: true,
                    context => GoToDefinitionHelpers.TryGoToDefinition(
                        ImmutableArray.Create(viewModel.DefinitionItem),
                        _workspace,
                        string.Format(EditorFeaturesResources._0_declarations, viewModel.DisplayContent),
                        _threadingContext,
                        _streamingFindUsagesPresenter,
                        context.CancellationToken));
            }
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
            ResetBorderToInitialColor();
            // Move the focus back to textView when the context menu is closed.
            // It ensures the focus won't be left at the margin
            ResetFocus();
        }

        private void ContextMenu_OnOpen(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is ContextMenu { DataContext: InheritanceMarginViewModel inheritanceMarginViewModel }
                && inheritanceMarginViewModel.MenuItemViewModels.Any(vm => vm is TargetMenuItemViewModel))
            {
                // We have two kinds of context menu. e.g.
                // 1. [margin] -> Target1
                //                Target2
                //                Target3
                //
                // 2. [margin] -> method Bar -> Target1
                //                           -> Target2
                //             -> method Foo -> Target3
                //                           -> Target4
                // If the first level of the context menu contains a TargetMenuItemViewModel, it means here it is case 1,
                // user is viewing the targets menu.
                Logger.Log(FunctionId.InheritanceMargin_TargetsMenuOpen, KeyValueLogMessage.Create(LogType.UserAction));
            }
        }

        private void TargetsSubmenu_OnOpen(object sender, RoutedEventArgs e)
        {
            Logger.Log(FunctionId.InheritanceMargin_TargetsMenuOpen, KeyValueLogMessage.Create(LogType.UserAction));
        }

        private void ResetBorderToInitialColor()
        {
            this.Background = Brushes.Transparent;
            this.BorderBrush = Brushes.Transparent;
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
