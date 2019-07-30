// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Text.Classification;
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
        private readonly IEditorFormatMap _textFormattingMap;

        internal bool ShouldReceiveKeyboardNavigation { get; set; }

        private readonly IEnumerable<string> _renameAccessKeys = new[]
            {
                RenameShortcutKey.RenameOverloads,
                RenameShortcutKey.SearchInComments,
                RenameShortcutKey.SearchInStrings,
                RenameShortcutKey.Apply,
                RenameShortcutKey.PreviewChanges
            };

        public Dashboard(
            DashboardViewModel model,
            IEditorFormatMapService editorFormatMapService,
            IWpfTextView textView)
        {
            _model = model;
            InitializeComponent();

            _tabNavigableChildren = new UIElement[] { this.OverloadsCheckbox, this.CommentsCheckbox, this.StringsCheckbox, this.FileRenameCheckbox, this.PreviewChangesCheckbox, this.ApplyButton, this.CloseButton }.ToList();

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

            // Once the Dashboard is loaded, the visual tree is completely created and the 
            // UIAutomation system has discovered and connected the AutomationPeer to the tree,
            // allowing us to raise the AutomationFocusChanged event and have it process correctly.
            // for us to set up the AutomationPeer
            this.Loaded += Dashboard_Loaded;

            if (editorFormatMapService != null)
            {
                _textFormattingMap = editorFormatMapService.GetEditorFormatMap("text");
                _textFormattingMap.FormatMappingChanged += UpdateBorderColors;
                UpdateBorderColors(this, eventArgs: null);
            }

            ResolvableConflictBorder.StrokeThickness = RenameFixupTagDefinition.StrokeThickness;
            ResolvableConflictBorder.StrokeDashArray = new DoubleCollection(RenameFixupTagDefinition.StrokeDashArray);

            UnresolvableConflictBorder.StrokeThickness = RenameConflictTagDefinition.StrokeThickness;
            UnresolvableConflictBorder.StrokeDashArray = new DoubleCollection(RenameConflictTagDefinition.StrokeDashArray);

            this.Focus();
            textView.Caret.IsHidden = false;
            ShouldReceiveKeyboardNavigation = false;
        }

        private void UpdateBorderColors(object sender, FormatItemsEventArgs eventArgs)
        {
            var resolvableConflictBrush = GetEditorTagBorderBrush(RenameFixupTag.TagId);
            ResolvableConflictBorder.Stroke = resolvableConflictBrush;
            ResolvableConflictText.Foreground = resolvableConflictBrush;

            var unresolvableConflictBrush = GetEditorTagBorderBrush(RenameConflictTag.TagId);
            UnresolvableConflictBorder.Stroke = unresolvableConflictBrush;
            UnresolvableConflictText.Foreground = unresolvableConflictBrush;

            ErrorText.Foreground = unresolvableConflictBrush;
        }

        private Brush GetEditorTagBorderBrush(string tagId)
        {
            var properties = _textFormattingMap.GetProperties(tagId);
            return (Brush)(properties["Foreground"] ?? ((Pen)properties["MarkerFormatDefinition/BorderId"]).Brush);
        }

        private void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            // Move automation focus to the Dashboard so that screenreaders will announce that the
            // session has begun.
            if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
            {
                UIElementAutomationPeer.CreatePeerForElement(this)?.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
            }
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
            _presentationSource = presentationSource ?? throw new ArgumentNullException(nameof(presentationSource));

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
                foreach (var accessKey in _renameAccessKeys)
                {
                    AccessKeyManager.Register(accessKey, _rootInputElement);
                }

                AccessKeyManager.AddAccessKeyPressedHandler(_rootDependencyObject, OnAccessKeyPressed);
            }
        }

        private void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs args)
        {
            foreach (var accessKey in _renameAccessKeys)
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
                else if (string.Equals(e.Key, RenameShortcutKey.RenameFile, StringComparison.OrdinalIgnoreCase))
                {
                    this.FileRenameCheckbox.IsChecked = !this.FileRenameCheckbox.IsChecked;
                }
                else if (string.Equals(e.Key, RenameShortcutKey.Apply, StringComparison.OrdinalIgnoreCase))
                {
                    this.Commit();
                }
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DashboardAutomationPeer(this, _model.GetOriginalName());
        }

        private void DisconnectFromPresentationSource()
        {
            if (_rootInputElement != null)
            {
                foreach (var registeredKey in _renameAccessKeys)
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

        public string RenameOverloads => EditorFeaturesResources.Include_overload_s;
        public Visibility RenameOverloadsVisibility => _model.RenameOverloadsVisibility;
        public bool IsRenameOverloadsEditable => _model.IsRenameOverloadsEditable;
        public string SearchInComments => EditorFeaturesResources.Include_comments;
        public string SearchInStrings => EditorFeaturesResources.Include_strings;
        public string ApplyRename => EditorFeaturesResources.Apply1;
        public string CancelRename => EditorFeaturesResources.Cancel;
        public string PreviewChanges => EditorFeaturesResources.Preview_changes1;
        public string RenameInstructions => EditorFeaturesResources.Modify_any_highlighted_location_to_begin_renaming;
        public string ApplyToolTip { get { return EditorFeaturesResources.Apply3 + " (Enter)"; } }
        public string CancelToolTip { get { return EditorFeaturesResources.Cancel + " (Esc)"; } }

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
            try
            {
                _model.Session.Commit();
                _textView.VisualElement.Focus();
            }
            catch (NotSupportedException ex)
            {
                // Session.Commit can throw if it can't commit
                // rename operation.
                // handle that case gracefully
                var notificationService = _model.Session.Workspace.Services.GetService<INotificationService>();
                notificationService.SendNotification(ex.Message, title: EditorFeaturesResources.Rename, severity: NotificationSeverity.Error);
            }
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

            if (_textFormattingMap != null)
            {
                _textFormattingMap.FormatMappingChanged -= UpdateBorderColors;
            }

            this.Loaded -= Dashboard_Loaded;

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
