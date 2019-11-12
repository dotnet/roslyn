// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for AddParameterDialog.xaml
    /// </summary>
    internal partial class AddParameterDialog : DialogWindow
    {
        private readonly AddParameterDialogViewModel _viewModel;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly ITextBufferFactoryService _textBufferFactoryService;
        private IEditorCommandHandlerServiceFactory _commandServiceFactory;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IContentType _contentType;

        private IWpfTextView _wpfView;
        private IEditorCommandHandlerService _commandService;

        private Action Noop { get; } = new Action(() => { });

        private Func<CommandState> Available { get; } = () => CommandState.Available;

        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        public string TypeNameLabel { get { return ServicesVSResources.Type_Name; } }

        public string ParameterNameLabel { get { return ServicesVSResources.Parameter_Name; } }

        public string CallsiteValueLabel { get { return ServicesVSResources.Callsite_Value; } }

        public string AddParameterDialogTitle { get { return ServicesVSResources.Add_Parameter; } }

        public AddParameterDialog(
            AddParameterDialogViewModel viewModel,
            ITextEditorFactoryService textEditorFactoryService,
            ITextBufferFactoryService textBufferFactoryService,
            IEditorCommandHandlerServiceFactory commandServiceFactory,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IContentType contentType)
        {
            _viewModel = viewModel;
            _textEditorFactoryService = textEditorFactoryService;
            _textBufferFactoryService = textBufferFactoryService;
            _commandServiceFactory = commandServiceFactory;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _contentType = contentType;
            this.Loaded += AddParameterDialog_Loaded;

            InitializeComponent();
        }

        private void AddParameterDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var buffer = _textBufferFactoryService.CreateTextBuffer(_contentType);
            _wpfView = _textEditorFactoryService.CreateTextView(buffer, _textEditorFactoryService.AllPredefinedRoles); // DefaultRoles might be ok
            var viewHost = _textEditorFactoryService.CreateTextViewHost(_wpfView, setFocus: true).HostControl;
            this.TypeControl = viewHost;
            _commandService = _commandServiceFactory.GetService(_wpfView, buffer);

            var editorOperations = _editorOperationsFactoryService.GetEditorOperations(_wpfView);

            this.TypeControl.Focus();
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

        private void TypeControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // TODO shift/alt
            InsertChar(GetCharFromKey(e.Key));
        }

        // --- Get char from Key, courtesy of https://stackoverflow.com/a/5826175/879243

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
        {
            char ch = '\0';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }

        public void InsertChar(char character)
        {
            QueryAndExecute((v, b) => new TypeCharCommandArgs(v, b, character));
        }

        public void QueryAndExecute<T>(Func<ITextView, ITextBuffer, T> argsFactory) where T : EditorCommandArgs
        {
            var state = _commandService.GetCommandState(argsFactory, Available);
            if (state.IsAvailable)
            {
                _commandService.Execute(argsFactory, Noop);
            }
        }
    }
}
