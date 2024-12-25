// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;

internal interface IWpfSettingsEditorViewModel : ISettingsEditorViewModel
{
    IWpfTableControl4 GetTableControl();
    void ShutDown();
}
