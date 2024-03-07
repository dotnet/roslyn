// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.View;

/// <summary>
/// Interaction logic for FormattingSettingsView.xaml
/// </summary>
internal partial class WhitespaceSettingsView : UserControl, ISettingsEditorView
{
    private readonly IWpfSettingsEditorViewModel _viewModel;

    public WhitespaceSettingsView(IWpfSettingsEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        TableControl = _viewModel.GetTableControl();
        WhitespaceTable.Content = TableControl.Control;
    }

    public UserControl SettingControl => this;
    public IWpfTableControl TableControl { get; }
    public Task<SourceText> UpdateEditorConfigAsync(SourceText sourceText) => _viewModel.UpdateEditorConfigAsync(sourceText);
    public void OnClose() => _viewModel.ShutDown();
}
