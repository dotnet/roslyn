// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class CheckBoxOptionViewModel : AbstractNotifyPropertyChanged
    {
        private readonly AbstractOptionPreviewViewModel _info;

        internal string GetPreview()
        {
            return _isChecked ? _truePreview : _falsePreview;
        }

        private bool _isChecked;
        private readonly string _truePreview;
        private readonly string _falsePreview;

        public IOption Option { get; }
        public string Description { get; set; }

        public CheckBoxOptionViewModel(IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options)
        {
            this.Option = option;
            Description = description;
            _truePreview = preview;
            _falsePreview = preview;
            _info = info;
            SetProperty(ref _isChecked, (bool)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
        }

        public CheckBoxOptionViewModel(IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionSet options)
        {
            this.Option = option;
            Description = description;
            _truePreview = truePreview;
            _falsePreview = falsePreview;
            _info = info;
            SetProperty(ref _isChecked, (bool)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
        }

        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }

            set
            {
                SetProperty(ref _isChecked, value);
                _info.SetOptionAndUpdatePreview(_isChecked, Option, GetPreview());
            }
        }
    }
}
