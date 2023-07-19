// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.NamingStyle.View
{
    /// <summary>
    /// Interaction logic for NamingStyleSettingsView.xaml
    /// </summary>
    internal partial class NamingStyleSettingsView : UserControl, ISettingsEditorView
    {
        private readonly IWpfSettingsEditorViewModel _viewModel;

        public NamingStyleSettingsView(IWpfSettingsEditorViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            TableControl = _viewModel.GetTableControl();
            NamingStyleTable.Content = TableControl.Control;
            DataContext = viewModel;
        }

        public UserControl SettingControl => this;
        public IWpfTableControl TableControl { get; }
        public Task<SourceText> UpdateEditorConfigAsync(SourceText sourceText) => _viewModel.UpdateEditorConfigAsync(sourceText);
        public void OnClose() => _viewModel.ShutDown();
    }
}
