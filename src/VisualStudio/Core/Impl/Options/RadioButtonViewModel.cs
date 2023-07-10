// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class RadioButtonViewModel<TOptionValue> : AbstractRadioButtonViewModel
    {
        private readonly Option2<TOptionValue> _option;
        private readonly TOptionValue _value;

        public RadioButtonViewModel(string description, string preview, string group, TOptionValue value, Option2<TOptionValue> option, AbstractOptionPreviewViewModel info, OptionStore optionStore)
            : base(description, preview, info, isChecked: optionStore.GetOption<TOptionValue>(option).Equals(value), group: group)
        {
            _value = value;
            _option = option;
        }

        internal override void SetOptionAndUpdatePreview(AbstractOptionPreviewViewModel info, string preview)
            => info.SetOptionAndUpdatePreview(_value, _option, preview);
    }
}
