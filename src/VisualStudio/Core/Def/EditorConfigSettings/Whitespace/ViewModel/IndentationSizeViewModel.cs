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

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Whitespace.ViewModel
{
    [Export(typeof(IEnumSettingViewModelFactory)), Shared]
    internal class IndentationSizeVViewModelFactory : IEnumSettingViewModelFactory
    {
        private readonly OptionKey2 _key;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IndentationSizeVViewModelFactory()
        {
            _key = new OptionKey2(FormattingOptions2.IndentationSize, LanguageNames.CSharp);
        }

        public IEnumSettingViewModel CreateViewModel(Setting setting)
        {
            return new IndentationSizeViewModel(setting);
        }

        public bool IsSupported(OptionKey2 key) => _key == key;
    }

    internal enum IndentationSizeSetting
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

    internal class IndentationSizeViewModel : EnumSettingViewModel<IndentationSizeSetting>
    {
        private readonly Setting _setting;

        public IndentationSizeViewModel(Setting setting)
        {
            _setting = setting;
        }

        protected override void ChangePropertyTo(IndentationSizeSetting newValue)
        {
            switch (newValue)
            {
                case IndentationSizeSetting._1:
                    _setting.SetValue(1);
                    break;
                case IndentationSizeSetting._2:
                    _setting.SetValue(2);
                    break;
                case IndentationSizeSetting._3:
                    _setting.SetValue(3);
                    break;
                case IndentationSizeSetting._4:
                    _setting.SetValue(4);
                    break;
                case IndentationSizeSetting._5:
                    _setting.SetValue(5);
                    break;
                case IndentationSizeSetting._6:
                    _setting.SetValue(6);
                    break;
                case IndentationSizeSetting._7:
                    _setting.SetValue(7);
                    break;
                case IndentationSizeSetting._8:
                    _setting.SetValue(8);
                    break;
                default:
                    break;
            }
        }

        protected override IndentationSizeSetting GetCurrentValue()
        {
            return _setting.GetValue() switch
            {
                1 => IndentationSizeSetting._1,
                2 => IndentationSizeSetting._2,
                3 => IndentationSizeSetting._3,
                4 => IndentationSizeSetting._4,
                5 => IndentationSizeSetting._5,
                6 => IndentationSizeSetting._6,
                7 => IndentationSizeSetting._7,
                _ => IndentationSizeSetting._8,
            };
        }

        protected override IReadOnlyDictionary<string, IndentationSizeSetting> GetValuesAndDescriptions()
        {
            return EnumerateOptions().ToDictionary(x => x.description, x => x.value);

            static IEnumerable<(string description, IndentationSizeSetting value)> EnumerateOptions()
            {
                yield return ("1", IndentationSizeSetting._1);
                yield return ("2", IndentationSizeSetting._2);
                yield return ("3", IndentationSizeSetting._3);
                yield return ("4", IndentationSizeSetting._4);
                yield return ("5", IndentationSizeSetting._5);
                yield return ("6", IndentationSizeSetting._6);
                yield return ("7", IndentationSizeSetting._7);
                yield return ("8", IndentationSizeSetting._8);
            }
        }
    }
}
