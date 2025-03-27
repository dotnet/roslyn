// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings;

[Export(typeof(IEnumSettingViewModelFactory)), Shared]
internal sealed class LabelPositionOptionsViewModelFactory : IEnumSettingViewModelFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LabelPositionOptionsViewModelFactory()
    {
    }

    public IEnumSettingViewModel CreateViewModel(Setting setting)
    {
        return new LabelPositionOptionsViewModel(setting);
    }

    public bool IsSupported(OptionKey2 key)
        => key.Option.Type == typeof(LabelPositionOptions);
}

internal sealed class LabelPositionOptionsViewModel : EnumSettingViewModel<LabelPositionOptions>
{
    private readonly Setting _setting;

    public LabelPositionOptionsViewModel(Setting setting)
    {
        _setting = setting;
    }

    protected override void ChangePropertyTo(LabelPositionOptions newValue)
    {
        _setting.SetValue(newValue);
    }

    protected override LabelPositionOptions GetCurrentValue()
    {
        return (LabelPositionOptions)_setting.GetValue()!;
    }

    protected override IReadOnlyDictionary<string, LabelPositionOptions> GetValuesAndDescriptions()
    {
        return EnumerateOptions().ToDictionary(x => x.description, x => x.value);

        static IEnumerable<(string description, LabelPositionOptions value)> EnumerateOptions()
        {
            yield return (CSharpVSResources.Place_goto_labels_in_leftmost_column, LabelPositionOptions.LeftMost);
            yield return (CSharpVSResources.Indent_labels_normally, LabelPositionOptions.NoIndent);
            yield return (CSharpVSResources.Place_goto_labels_one_indent_less_than_current, LabelPositionOptions.OneLess);
        }
    }
}
