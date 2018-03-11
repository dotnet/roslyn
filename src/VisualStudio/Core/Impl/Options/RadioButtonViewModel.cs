// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class RadioButtonViewModel<TOption> : AbstractRadioButtonViewModel
    {
        private readonly Option<TOption> _option;
        private readonly TOption _value;

        public RadioButtonViewModel(string description, string preview, string group, TOption value, Option<TOption> option, AbstractOptionPreviewViewModel info, OptionSet options)
            : base(description, preview, info, options, isChecked: options.GetOption(option).Equals(value), group: group)
        {
            _value = value;
            _option = option;
        }

        internal override void SetOptionAndUpdatePreview(AbstractOptionPreviewViewModel info, string preview)
        {
            info.SetOptionAndUpdatePreview(_value, _option, preview);
        }
    }
}
