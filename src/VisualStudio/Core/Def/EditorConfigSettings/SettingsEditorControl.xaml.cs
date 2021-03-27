// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Controls;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings
{
    /// <summary>
    /// Interaction logic for SettingsEditorControl.xaml
    /// </summary>
    internal partial class SettingsEditorControl : UserControl
    {
        private readonly ISettingsEditorView _formattingView;
        private readonly ISettingsEditorView _codeStyleView;
        private readonly ISettingsEditorView _analyzerSettingsView;

        public static string Formatting => ServicesVSResources.Formatting;
        public static string CodeStyle => ServicesVSResources.Code_Style;
        public static string Analyzers => ServicesVSResources.Analyzers;

        public SettingsEditorControl(ISettingsEditorView formattingView,
                                     ISettingsEditorView codeStyleView,
                                     ISettingsEditorView analyzerSettingsView)
        {
            InitializeComponent();
            DataContext = this;
            _formattingView = formattingView;
            FormattingTab.Content = _formattingView.SettingControl;
            _codeStyleView = codeStyleView;
            CodeStyleTab.Content = _codeStyleView.SettingControl;
            _analyzerSettingsView = analyzerSettingsView;
            AnalyzersTab.Content = _analyzerSettingsView.SettingControl;
        }

        internal void SynchronizeSettings()
        {
            _formattingView.Synchronize();
            _codeStyleView.Synchronize();
            _analyzerSettingsView.Synchronize();
        }

        internal IWpfTableControl[] GetTableControls()
            => new[]
            {
                _formattingView.TableControl,
                _codeStyleView.TableControl,
                _analyzerSettingsView.TableControl,
            };

        internal void OnClose()
        {
            _formattingView.OnClose();
            _codeStyleView.OnClose();
            _analyzerSettingsView.OnClose();
        }
    }
}
