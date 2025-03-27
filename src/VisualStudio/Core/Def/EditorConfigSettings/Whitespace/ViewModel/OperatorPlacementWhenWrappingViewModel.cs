// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel;

[Export(typeof(IEnumSettingViewModelFactory)), Shared]
internal sealed class OperatorPlacementWhenWrappingViewModelFactory : IEnumSettingViewModelFactory
{
    private readonly OptionKey2 _key;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OperatorPlacementWhenWrappingViewModelFactory()
    {
        _key = new OptionKey2(CodeStyleOptions2.OperatorPlacementWhenWrapping);
    }

    public IEnumSettingViewModel CreateViewModel(Setting setting)
    {
        return new OperatorPlacementWhenWrappingViewModel(setting);
    }

    public bool IsSupported(OptionKey2 key) => _key == key;

    private sealed class OperatorPlacementWhenWrappingViewModel : EnumSettingViewModel<OperatorPlacementWhenWrappingPreference>
    {
        private readonly Setting _setting;

        public OperatorPlacementWhenWrappingViewModel(Setting setting)
        {
            _setting = setting;
        }

        protected override void ChangePropertyTo(OperatorPlacementWhenWrappingPreference newValue)
        {
            switch (newValue)
            {
                case OperatorPlacementWhenWrappingPreference.BeginningOfLine:
                    _setting.SetValue(OperatorPlacementWhenWrappingPreference.BeginningOfLine);
                    break;
                case OperatorPlacementWhenWrappingPreference.EndOfLine:
                    _setting.SetValue(OperatorPlacementWhenWrappingPreference.EndOfLine);
                    break;
                default:
                    break;
            }
        }

        protected override OperatorPlacementWhenWrappingPreference GetCurrentValue()
        {
            return _setting.GetValue() switch
            {
                OperatorPlacementWhenWrappingPreference.BeginningOfLine => OperatorPlacementWhenWrappingPreference.BeginningOfLine,
                _ => OperatorPlacementWhenWrappingPreference.EndOfLine,
            };
        }

        protected override IReadOnlyDictionary<string, OperatorPlacementWhenWrappingPreference> GetValuesAndDescriptions()
        {
            return EnumerateOptions().ToDictionary(x => x.description, x => x.value);

            static IEnumerable<(string description, OperatorPlacementWhenWrappingPreference value)> EnumerateOptions()
            {
                yield return (ServicesVSResources.Beginning_of_line, OperatorPlacementWhenWrappingPreference.BeginningOfLine);
                yield return (ServicesVSResources.End_of_line, OperatorPlacementWhenWrappingPreference.EndOfLine);
            }
        }
    }
}
