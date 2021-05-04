// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings
{
    internal interface ISettingsEditorView
    {
        UserControl SettingControl { get; }
        IWpfTableControl TableControl { get; }
        Task<SourceText> UpdateEditorConfigAsync(SourceText sourceText);
        void OnClose();
    }
}
