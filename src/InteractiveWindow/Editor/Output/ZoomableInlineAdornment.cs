// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    internal sealed class ZoomableInlineAdornment : ContentControl
    {
        private readonly ITextView _parent;
        private readonly double _minimizedZoom;
        private readonly double _zoomStep;
        private readonly double _widthRatio;
        private readonly double _heightRatio;

        private ResizingAdorner _adorner;
        private bool _isResizing;
        private double _zoom;

        public ZoomableInlineAdornment(UIElement content, ITextView parent)
        {
            _parent = parent;
            Debug.Assert(parent is IInputElement);
            this.Content = new Border { BorderThickness = new Thickness(1), Child = content, Focusable = true };

            _zoom = 1.0;             // config.GetConfig().Repl.InlineMedia.MaximizedZoom
            _zoomStep = 0.25;        // config.GetConfig().Repl.InlineMedia.ZoomStep
            _minimizedZoom = 0.25;   // config.GetConfig().Repl.InlineMedia.MinimizedZoom
            _widthRatio = 0.67;      // config.GetConfig().Repl.InlineMedia.WidthRatio
            _heightRatio = 0.5;      // config.GetConfig().Repl.InlineMedia.HeightRatio

            _isResizing = false;
            UpdateSize();

            this.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (MyContent.IsFocused)
                {
                    Keyboard.Focus(MyParent);
                    e.Handled = true;
                }
            };

            this.MouseRightButtonDown += (s, e) =>
            {
                // The editor doesn't support context menus, so even for an
                // adornment we have to open it explicitly
                ContextMenu.IsOpen = true;
            };

            this.GotFocus += OnGotFocus;
            this.LostFocus += OnLostFocus;

            ContextMenu = MakeContextMenu();

            var trigger = new Trigger { Property = IsFocusedProperty, Value = true };
            var setter = new Setter { Property = Border.BorderBrushProperty, Value = SystemColors.ActiveBorderBrush };
            trigger.Setters.Add(setter);

            var style = new Style();
            style.Triggers.Add(trigger);
            this.MyContent.Style = style;
        }

        private ContextMenu MakeContextMenu()
        {
            var result = new ContextMenu();
            AddMenuItem(result, "Zoom In", "Ctrl+OemPlus", (s, e) => OnZoomIn());
            AddMenuItem(result, "Zoom Out", "Ctrl+OemMinus", (s, e) => OnZoomOut());
            result.Items.Add(new Separator());
            AddMenuItem(result, "150%", null, (s, e) => Zoom(1.5));
            AddMenuItem(result, "100%", null, (s, e) => Zoom(1.0));
            AddMenuItem(result, "75%", null, (s, e) => Zoom(0.75));
            AddMenuItem(result, "50%", null, (s, e) => Zoom(0.50));
            AddMenuItem(result, "25%", null, (s, e) => Zoom(0.25));
            return result;
        }

        private static void AddMenuItem(ContextMenu menu, string text, string shortcut, EventHandler handler)
        {
            var item = new MenuItem();
            item.Header = text;
            item.Click += (s, e) => handler(s, e);
            menu.Items.Add(item);
        }

        private Border MyContent
        {
            get { return Content as Border; }
        }

        private IInputElement MyParent
        {
            get { return _parent as IInputElement; }
        }

        private void OnGotFocus(object sender, RoutedEventArgs args)
        {
            MyParent.PreviewMouseLeftButtonDown += OnPreviewParentMouseDown;
            PreviewKeyDown += OnPreviewKeyDown;
            _adorner = new ResizingAdorner(MyContent);
            _adorner.ResizeStarted += OnResizeStarted;
            _adorner.ResizeCompleted += OnResizeCompleted;

            var adornerLayer = AdornerLayer.GetAdornerLayer(MyContent);
            adornerLayer.Add(_adorner);
        }

        private void OnLostFocus(object sender, RoutedEventArgs args)
        {
            MyParent.PreviewMouseLeftButtonDown -= OnPreviewParentMouseDown;
            PreviewKeyDown -= OnPreviewKeyDown;
            _adorner.ResizeStarted -= OnResizeStarted;
            _adorner.ResizeCompleted -= OnResizeCompleted;

            var adornerLayer = AdornerLayer.GetAdornerLayer(MyContent);
            adornerLayer.Remove(_adorner);
            _adorner = null;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            var modifiers = args.KeyboardDevice.Modifiers & ModifierKeys.Control;
            if (modifiers == ModifierKeys.Control && args.Key == Key.OemPlus)
            {
                OnZoomIn();
                args.Handled = true;
            }
            else if (modifiers == ModifierKeys.Control && args.Key == Key.OemMinus)
            {
                OnZoomOut();
                args.Handled = true;
            }
        }

        private void OnPreviewParentMouseDown(object sender, RoutedEventArgs args)
        {
            if (MyContent.IsFocused)
            {
                Keyboard.Focus(MyParent);
            }
        }

        private void OnResizeStarted(object sender, RoutedEventArgs args)
        {
            _isResizing = true;
        }

        private void OnResizeCompleted(object sender, RoutedEventArgs args)
        {
            _isResizing = false;
            UpdateSize();
        }

        internal void Zoom(double zoomFactor)
        {
            _zoom = zoomFactor;
            UpdateSize();
        }

        private void OnZoomIn()
        {
            _zoom += _zoomStep;
            UpdateSize();
        }

        private void OnZoomOut()
        {
            if (_zoom - _zoomStep > 0.1)
            {
                _zoom -= _zoomStep;
                UpdateSize();
            }
        }

        internal void UpdateSize()
        {
            if (_isResizing)
            {
                return;
            }

            double width = _parent.ViewportWidth * _widthRatio * _zoom;
            double height = _parent.ViewportHeight * _heightRatio * _zoom;
            MyContent.MaxWidth = width;
            MyContent.MaxHeight = height;
            MyContent.Measure(new Size(width, height));
        }

        internal double MinimizedZoom
        {
            get { return _minimizedZoom; }
        }
    }
}
