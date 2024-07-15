// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel;

[Export(typeof(IEnumSettingViewModelFactory)), Shared]
internal class TabSizeViewModelFactory : IEnumSettingViewModelFactory
{
    private readonly OptionKey2 _key;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TabSizeViewModelFactory()
    {
        _key = new OptionKey2(FormattingOptions2.TabSize, LanguageNames.CSharp);
    }

    public IEnumSettingViewModel CreateViewModel(Setting setting)
    {
        return new TabSizeViewModel(setting);
    }

    public bool IsSupported(OptionKey2 key) => _key == key;
}
