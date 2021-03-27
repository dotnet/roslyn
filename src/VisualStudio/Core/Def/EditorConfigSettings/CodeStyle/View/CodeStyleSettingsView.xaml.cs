// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View
{
    /// <summary>
    /// Interaction logic for CodeStyleView.xaml
    /// </summary>
    internal partial class CodeStyleSettingsView : UserControl, ISettingsEditorView
    {
        private readonly IVsTextLines _vsTextLines;
        private readonly IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private readonly IThreadingContext _threadingContext;
        private readonly IWpfSettingsEditorViewModel _viewModel;

        public CodeStyleSettingsView(IVsTextLines vsTextLines,
                                     IVsEditorAdaptersFactoryService vsEditorAdaptersFactoryService,
                                     IThreadingContext threadingContext,
                                     IWpfSettingsEditorViewModel viewModel)
        {
            InitializeComponent();
            _vsTextLines = vsTextLines;
            _vsEditorAdaptersFactoryService = vsEditorAdaptersFactoryService;
            _threadingContext = threadingContext;
            _viewModel = viewModel;
            TableControl = _viewModel.GetTableControl();
            CodeStyleTable.Child = TableControl.Control;
            DataContext = viewModel;
        }

        public UserControl SettingControl => this;
        public IWpfTableControl TableControl { get; }

        public void OnClose()
        {
            _viewModel.ShutDown();
        }

        public void Synchronize()
        {
            if (IsKeyboardFocusWithin)
            {
                EditorTextUpdater.UpdateText(_threadingContext, _vsEditorAdaptersFactoryService, _vsTextLines, _viewModel);
            }
        }
    }
}
