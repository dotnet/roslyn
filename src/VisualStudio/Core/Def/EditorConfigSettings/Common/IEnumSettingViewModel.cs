// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditorConfigSettings
{
    internal interface IEnumSettingViewModel
    {
        string[] EnumValues { get; }
        string SelectedEnumValue { get; set; }
        string ToolTip { get; }
        string AutomationName { get; }
        void ChangeProperty(string v);
    }
}
