// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel;

internal class TabSizeViewModel : EnumSettingViewModel<TabSizeSettings>
{
    private readonly Setting _setting;

    public TabSizeViewModel(Setting setting)
    {
        _setting = setting;
    }

    protected override void ChangePropertyTo(TabSizeSettings newValue)
        => _setting.SetValue((int)newValue);

    protected override TabSizeSettings GetCurrentValue()
    {
        return _setting.GetValue() switch
        {
            1 => TabSizeSettings._1,
            2 => TabSizeSettings._2,
            3 => TabSizeSettings._3,
            4 => TabSizeSettings._4,
            5 => TabSizeSettings._5,
            6 => TabSizeSettings._6,
            7 => TabSizeSettings._7,
            _ => TabSizeSettings._8,
        };
    }

    protected override IReadOnlyDictionary<string, TabSizeSettings> GetValuesAndDescriptions()
    {
        return EnumerateOptions().ToDictionary(x => x.description, x => x.value);

        static IEnumerable<(string description, TabSizeSettings value)> EnumerateOptions()
        {
            yield return ("1", TabSizeSettings._1);
            yield return ("2", TabSizeSettings._2);
            yield return ("3", TabSizeSettings._3);
            yield return ("4", TabSizeSettings._4);
            yield return ("5", TabSizeSettings._5);
            yield return ("6", TabSizeSettings._6);
            yield return ("7", TabSizeSettings._7);
            yield return ("8", TabSizeSettings._8);
        }
    }
}
