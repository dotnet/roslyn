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

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.ViewModel
{
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

        public IEnumSettingViewModel CreateViewModel(FormattingSetting setting)
        {
            return new TabSizeViewModel(setting);
        }

        public bool IsSupported(OptionKey2 key) => _key == key;
    }

    internal enum TabSizeSettings
    {
        _1,
        _2,
        _3,
        _4,
        _5,
        _6,
        _7,
        _8,
    }

    internal class TabSizeViewModel : EnumSettingViewModel<TabSizeSettings>
    {
        private readonly FormattingSetting _setting;

        public TabSizeViewModel(FormattingSetting setting)
        {
            _setting = setting;
        }

        protected override void ChangePropertyTo(TabSizeSettings newValue)
        {
            switch (newValue)
            {
                case TabSizeSettings._1:
                    _setting.SetValue(1);
                    break;
                case TabSizeSettings._2:
                    _setting.SetValue(2);
                    break;
                case TabSizeSettings._3:
                    _setting.SetValue(3);
                    break;
                case TabSizeSettings._4:
                    _setting.SetValue(4);
                    break;
                case TabSizeSettings._5:
                    _setting.SetValue(5);
                    break;
                case TabSizeSettings._6:
                    _setting.SetValue(6);
                    break;
                case TabSizeSettings._7:
                    _setting.SetValue(7);
                    break;
                case TabSizeSettings._8:
                    _setting.SetValue(8);
                    break;
                default:
                    break;
            }
        }

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
}
