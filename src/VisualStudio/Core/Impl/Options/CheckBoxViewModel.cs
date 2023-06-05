// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class CheckBoxOptionViewModel : AbstractCheckBoxViewModel
    {
        public CheckBoxOptionViewModel(IOption2 option, string description, string preview, AbstractOptionPreviewViewModel info, OptionStore optionStore)
            : this(option, description, preview, preview, info, optionStore)
        {
        }

        public CheckBoxOptionViewModel(IOption2 option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionStore optionStore)
            : base(option, description, truePreview, falsePreview, info)
        {
            SetProperty(ref _isChecked, optionStore.GetOption<bool>(option, option.IsPerLanguage ? info.Language : null));
        }

        public override bool IsChecked
        {
            get
            {
                return _isChecked;
            }

            set
            {
                SetProperty(ref _isChecked, value);
                Info.SetOptionAndUpdatePreview(_isChecked, Option, GetPreview());
            }
        }
    }
}
