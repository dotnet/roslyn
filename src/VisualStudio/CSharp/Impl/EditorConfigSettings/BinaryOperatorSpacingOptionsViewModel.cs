// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.EditorConfigSettings
{
    internal class BinaryOperatorSpacingOptionsViewModel : EnumSettingViewModel<BinaryOperatorSpacingOptions>
    {
        private readonly Setting _setting;

        public BinaryOperatorSpacingOptionsViewModel(Setting setting)
        {
            _setting = setting;
        }

        protected override void ChangePropertyTo(BinaryOperatorSpacingOptions newValue)
        {
            _setting.SetValue(newValue);
        }

        protected override BinaryOperatorSpacingOptions GetCurrentValue()
        {
            return (BinaryOperatorSpacingOptions)_setting.GetValue()!;
        }

        protected override IReadOnlyDictionary<string, BinaryOperatorSpacingOptions> GetValuesAndDescriptions()
        {
            return EnumerateOptions().ToDictionary(x => x.description, x => x.value);

            static IEnumerable<(string description, BinaryOperatorSpacingOptions value)> EnumerateOptions()
            {
                yield return (CSharpVSResources.Ignore_spaces_around_binary_operators, BinaryOperatorSpacingOptions.Ignore);
                yield return (CSharpVSResources.Remove_spaces_before_and_after_binary_operators, BinaryOperatorSpacingOptions.Remove);
                yield return (CSharpVSResources.Insert_space_before_and_after_binary_operators, BinaryOperatorSpacingOptions.Single);
            }
        }
    }
}
