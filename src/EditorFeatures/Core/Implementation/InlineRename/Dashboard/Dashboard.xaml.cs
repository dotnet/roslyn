// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class Dashboard : UserControl, IDisposable
    {
        private readonly DashboardViewModel _model;
        private readonly IWpfTextView _textView;
        private readonly IAdornmentLayer _findAdornmentLayer;
        private PresentationSource _presentationSource;
        private DependencyObject _rootDependencyObject;
        private IInputElement _rootInputElement;
        private UIElement _focusedElement = null;
        private readonly List<UIElement> _tabNavigableChildren;
        internal bool ShouldReceiveKeyboardNavigation { get; set; }

        private IEnumerable<string> _renameAccessKeys = new[]
            {
                RenameShortcutKey.RenameOverloads,
                RenameShortcutKey.SearchInComments,
                RenameShortcutKey.SearchInStrings,
                RenameShortcutKey.Apply,
                RenameShortcutKey.PreviewChanges
            };

        public Dashboard(
            DashboardViewModel model,
            IWpfTextView textView)
        {
            _model = model;
            InitializeComponent();

            _tabNavigableChildren = new UIElement[] { this.OverloadsCheckbox, this.CommentsCheckbox, this.StringsCheckbox, this.PreviewChangesCheckbox, this.ApplyButton, this.CloseButton }.ToList();

            _textView = textView;
            this.DataContext = model;

            this.Visibility = textView.HasAggregateFocus ? Visibility.Visible : Visibility.Collapsed;

            _textView.GotAggregateFocus += OnTextViewGotAggregateFocus;
            _textView.LostAggregateFocus += OnTextViewLostAggregateFocus;
            _textView.VisualElement.SizeChanged += OnElementSizeChanged;
            this.SizeChanged += OnElementSizeChanged;

            PresentationSource.AddSourceChangedHandler(this, OnPresentationSourceChanged);

            try
            {
                _findAdornmentLayer = textView.GetAdornmentLayer("FindUIAdornmentLayer");
                ((UIElement)_findAdornmentLayer).LayoutUpdated += FindAdornmentCanvas_LayoutUpdated;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Find UI doesn't exist in ETA.
            }

            this.Focus();
            textView.Caret.IsHidden = false;
            ShouldReceiveKeyboardNavigation = false;
        }

        private void ShowCaret()
        {
            // We actually want the caret visible even though the view isn't explicitly focused.
            ((UIElement)_textView.Caret).Visibility = Visibility.Visible;
        }

        private void FocusElement(UIElement firstElement, Func<int, int> selector)
        {
            if (_focusedElement == null)
            {
                _focusedElement = firstElement;
            }
            else
            {
                var current = _tabNavigableChildren.IndexOf(_focusedElement);
                current = selector(current);
                _focusedElement = _tabNavigableChildren[current];
            }

            _focusedElement.Focus();
            ShowCaret();
        }

        internal void FocusNextElement()
        {
            FocusElement(_tabNavigableChildren.First(), i => i == _tabNavigableChildren.Count - 1 ? 0 : i + 1);
        }

        internal void FocusPreviousElement()
        {
            FocusElement(_tabNavigableChildren.Last(), i => i == 0 ? _tabNavigableChildren.Count - 1 : i - 1);
        }

        private void OnPresentationSourceChanged(object sender, SourceChangedEventArgs args)
        {
            if (args.NewSource == null)
            {
                this.DisconnectFromPresentationSource();
            }
            else
            {
                this.ConnectToPresentationSource(args.NewSource);
            }
        }

        private void ConnectToPresentationSource(PresentationSource presentationSource)
        {
            if (presentationSource == null)
            {
                throw new ArgumentNullException(nameof(presentationSource));
            }

            _presentationSource = presentationSource;

            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                _rootDependencyObject = Application.Current.MainWindow as DependencyObject;
            }
            else
            {
                _rootDependencyObject = _presentationSource.RootVisual as DependencyObject;
            }

            _rootInputElement = _rootDependencyObject as IInputElement;

            if (_rootDependencyObject != null && _rootInputElement != null)
            {
                foreach (string accessKey in _renameAccessKeys)
                {
                    AccessKeyManager.Register(accessKey, _rootInputElement);
                }

                AccessKeyManager.AddAccessKeyPressedHandler(_rootDependencyObject, OnAccessKeyPressed);
            }
        }

        private void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs args)
        {
            foreach (string accessKey in _renameAccessKeys)
            {
                if (string.Compare(accessKey, args.Key, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    args.Target = this;
                    args.Handled = true;
                    return;
                }
            }
        }

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            if (e != null)
            {
                if (string.Equals(e.Key, RenameShortcutKey.RenameOverloads, StringComparison.OrdinalIgnoreCase))
                {
                    this.OverloadsCheckbox.IsChecked = !this.OverloadsCheckbox.IsChecked;
                }
                else if (string.Equals(e.Key, RenameShortcutKey.SearchInComments, StringComparison.OrdinalIgnoreCase))
                {
                    this.CommentsCheckbox.IsChecked = !this.CommentsCheckbox.IsChecked;
                }
                else if (string.Equals(e.Key, RenameShortcutKey.SearchInStrings, StringComparison.OrdinalIgnoreCase))
                {
                    this.StringsCheckbox.IsChecked = !this.StringsCheckbox.IsChecked;
                }
                else if (string.Equals(e.Key, RenameShortcutKey.PreviewChanges, StringComparison.OrdinalIgnoreCase))
                {
                    this.PreviewChangesCheckbox.IsChecked = !this.PreviewChangesCheckbox.IsChecked;
                }
                else if (string.Equals(e.Key, RenameShortcutKey.Apply, StringComparison.OrdinalIgnoreCase))
                {
                    this.Commit();
                }
            }
        }

        private void DisconnectFromPresentationSource()
        {
            if (_rootInputElement != null)
            {
                foreach (string registeredKey in _renameAccessKeys)
                {
                    AccessKeyManager.Unregister(registeredKey, _rootInputElement);
                }

                AccessKeyManager.RemoveAccessKeyPressedHandler(_rootDependencyObject, OnAccessKeyPressed);
            }

            _presentationSource = null;
            _rootDependencyObject = null;
            _rootInputElement = null;
        }

        private void FindAdornmentCanvas_LayoutUpdated(object sender, EventArgs e)
        {
            PositionDashboard();
        }

        public string RenameOverloads { get { return EditorFeaturesResources.RenameOverloads; } }
        public Visibility RenameOverloadsVisibility { get { return _model.RenameOverloadsVisibility; } }
        public bool IsRenameOverloadsEditable { get { return _model.IsRenameOverloadsEditable; } }
        public string SearchInComments { get { return EditorFeaturesResources.SearchInComments; } }
        public string SearchInStrings { get { return EditorFeaturesResources.SearchInStrings; } }
        public string ApplyRename { get { return EditorFeaturesResources.ApplyRename; } }
        public string PreviewChanges { get { return EditorFeaturesResources.RenamePreviewChanges; } }
        public string RenameInstructions {  get { return EditorFeaturesResources.InlineRenameInstructions; } }
        public string ApplyToolTip { get { return EditorFeaturesResources.RenameApplyToolTip + " (Enter)"; } }
        public string CancelToolTip { get { return EditorFeaturesResources.RenameCancelToolTip + " (Esc)"; } }

        private void OnElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                PositionDashboard();
            }
        }

        private void PositionDashboard()
        {
            var top = _textView.ViewportTop;
            if (_findAdornmentLayer != null && _findAdornmentLayer.Elements.Count != 0)
            {
                var adornment = _findAdornmentLayer.Elements[0].Adornment;
                top += adornment.RenderSize.Height;
            }

            Canvas.SetTop(this, top);
            Canvas.SetLeft(this, _textView.ViewportLeft + _textView.VisualElement.RenderSize.Width - this.RenderSize.Width);
        }

        private void OnTextViewGotAggregateFocus(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
            PositionDashboard();
        }

        private void OnTextViewLostAggregateFocus(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _model.Session.Cancel();
            _textView.VisualElement.Focus();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Commit();
        }

        private void Commit()
        {
            _model.Session.Commit();
            _textView.VisualElement.Focus();
        }

        public void Dispose()
        {
            _textView.GotAggregateFocus -= OnTextViewGotAggregateFocus;
            _textView.LostAggregateFocus -= OnTextViewLostAggregateFocus;
            _textView.VisualElement.SizeChanged -= OnElementSizeChanged;
            this.SizeChanged -= OnElementSizeChanged;

            if (_findAdornmentLayer != null)
            {
                ((UIElement)_findAdornmentLayer).LayoutUpdated -= FindAdornmentCanvas_LayoutUpdated;
            }

            _model.Dispose();
            PresentationSource.RemoveSourceChangedHandler(this, OnPresentationSourceChanged);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = false;
            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = true;
            e.Handled = true;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = true;
            e.Handled = true;
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = false;
            e.Handled = true;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            // Don't send clicks into the text editor below.
            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            ShouldReceiveKeyboardNavigation = (bool)e.NewValue;
        }
    }
}
