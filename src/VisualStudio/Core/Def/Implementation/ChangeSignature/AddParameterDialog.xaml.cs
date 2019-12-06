// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for AddParameterDialog.xaml
    /// </summary>
    internal partial class AddParameterDialog : DialogWindow
    {
        private readonly AddParameterDialogViewModel _viewModel;
        private readonly IVsTextBuffer _textBuffer;
        private readonly IVsTextView _textView;
        private readonly IWpfTextView _wpfTextView;

        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        public string TypeNameLabel { get { return ServicesVSResources.Type_Name; } }

        public string ParameterNameLabel { get { return ServicesVSResources.Parameter_Name; } }

        public string CallsiteValueLabel { get { return ServicesVSResources.Callsite_Value; } }

        public string AddParameterDialogTitle { get { return ServicesVSResources.Add_Parameter; } }

        public AddParameterDialog(
            AddParameterDialogViewModel viewModel,
            IVsTextBuffer textBuffer,
            IVsTextView vsTextView,
            IWpfTextView wpfTextView)
        {
            _viewModel = viewModel;
            _textBuffer = textBuffer;
            _textView = vsTextView;
            _wpfTextView = wpfTextView;
            this.Loaded += AddParameterDialog_Loaded;

            InitializeComponent();
        }

        private void AddParameterDialog_Loaded(object sender, RoutedEventArgs e)
        {
            IntellisenseTextBox typeNameTextBox = new IntellisenseTextBox(
                _textBuffer as IVsTextLines, _textView, _wpfTextView, TypeNameContentControl);
            this.TypeNameContentControl.Content = typeNameTextBox;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TypeNameContentControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            if (elementWithFocus is IWpfTextView)
            {
                IntellisenseTextBox typeNameTextBox = elementWithFocus.GetParentOfType<IntellisenseTextBox>();

                if (typeNameTextBox != null)
                {
                    if (e.Key == Key.Escape && !typeNameTextBox.HasActiveIntellisenseSession)
                    {
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Enter && !typeNameTextBox.HasActiveIntellisenseSession)
                    {
                        // Do nothing. This case is handled in parent control KeyDown events.
                    }

                    else if (e.Key == Key.Tab && !typeNameTextBox.HasActiveIntellisenseSession)
                    {
                        // Do nothing. This case is handled in parent control KeyDown events.
                    }
                    else
                    {
                        // Let the editor control handle the keystrokes
                        System.Windows.Interop.MSG msg = ComponentDispatcher.CurrentKeyboardMessage;

                        var oleInteropMsg = new OLE.Interop.MSG();

                        oleInteropMsg.hwnd = msg.hwnd;
                        oleInteropMsg.message = (uint)msg.message;
                        oleInteropMsg.wParam = msg.wParam;
                        oleInteropMsg.lParam = msg.lParam;
                        oleInteropMsg.pt.x = msg.pt_x;
                        oleInteropMsg.pt.y = msg.pt_y;

                        e.Handled = typeNameTextBox.HandleKeyDown(oleInteropMsg);
                    }
                }
            }
        }
    }
}
