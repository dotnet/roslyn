// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel;

[Export(typeof(IEnumSettingViewModelFactory)), Shared]
internal sealed class NewLineViewModelFactory : IEnumSettingViewModelFactory
{
    private readonly OptionKey2 _key;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NewLineViewModelFactory()
    {
        _key = new OptionKey2(FormattingOptions2.NewLine, LanguageNames.CSharp);
    }

    public IEnumSettingViewModel CreateViewModel(Setting setting)
    {
        return new NewLineViewModel(setting);
    }

    public bool IsSupported(OptionKey2 key) => _key == key;
}

internal enum NewLineSetting
{
    Newline,
    CarriageReturn,
    CarriageReturnNewline,
    NotSet
}

internal sealed class NewLineViewModel : EnumSettingViewModel<NewLineSetting>
{
    private readonly Setting _setting;

    public NewLineViewModel(Setting setting)
    {
        _setting = setting;
    }

    protected override void ChangePropertyTo(NewLineSetting newValue)
    {
        switch (newValue)
        {
            case NewLineSetting.Newline:
                _setting.SetValue("\n");
                break;
            case NewLineSetting.CarriageReturn:
                _setting.SetValue("\r");
                break;
            case NewLineSetting.CarriageReturnNewline:
                _setting.SetValue("\r\n");
                break;
            case NewLineSetting.NotSet:
            default:
                break;
        }
    }

    protected override NewLineSetting GetCurrentValue()
    {
        return _setting.GetValue() switch
        {
            "\n" => NewLineSetting.Newline,
            "\r" => NewLineSetting.CarriageReturn,
            "\r\n" => NewLineSetting.CarriageReturnNewline,
            _ => NewLineSetting.NotSet,
        };
    }

    protected override IReadOnlyDictionary<string, NewLineSetting> GetValuesAndDescriptions()
    {
        return EnumerateOptions().ToDictionary(x => x.description, x => x.value);

        static IEnumerable<(string description, NewLineSetting value)> EnumerateOptions()
        {
            yield return (ServicesVSResources.Newline_n, NewLineSetting.Newline);
            yield return (ServicesVSResources.Carriage_Return_r, NewLineSetting.CarriageReturn);
            yield return (ServicesVSResources.Carriage_Return_Newline_rn, NewLineSetting.CarriageReturnNewline);
        }
    }
}
