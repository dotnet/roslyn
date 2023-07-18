// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common
{
    internal interface IEnumSettingViewModelFactory
    {
        bool IsSupported(OptionKey2 key);
        IEnumSettingViewModel CreateViewModel(Setting setting);
    }
}
