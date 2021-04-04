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

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings
{

    [Export(typeof(IEnumSettingViewModelFactory)), Shared]
    internal class LabelPositionOptionsViewModelFactory : IEnumSettingViewModelFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LabelPositionOptionsViewModelFactory()
        {
        }

        public IEnumSettingViewModel CreateViewModel(FormattingSetting setting)
        {
            return new LabelPositionOptionsViewModel(setting);
        }

        public bool IsSupported(OptionKey2 key)
            => key.Option.Type == typeof(LabelPositionOptions);
    }

    internal class LabelPositionOptionsViewModel : EnumSettingViewModel<LabelPositionOptions>
    {
        private readonly FormattingSetting _setting;

        public LabelPositionOptionsViewModel(FormattingSetting setting)
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
                yield return ("place goto labels in leftmost column", LabelPositionOptions.LeftMost);
                yield return ("indent labels normally", LabelPositionOptions.NoIndent);
                yield return ("place goto labels one indent less than current", LabelPositionOptions.OneLess);
            }
        }
    }
}
