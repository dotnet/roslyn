// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class CheckBoxOptionViewModel : AbstractCheckBoxViewModel
    {
        public CheckBoxOptionViewModel(IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options)
            : this(option, description, preview, preview, info, options)
        {
        }

        public CheckBoxOptionViewModel(IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionSet options)
            : base(option, description, truePreview, falsePreview, info, options)
        {
            SetProperty(ref _isChecked, (bool)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
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
