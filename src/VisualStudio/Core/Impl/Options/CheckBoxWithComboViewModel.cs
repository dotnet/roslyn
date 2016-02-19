// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    // BALAJIK TODO: Refactor this and push to an abstract base class.
    // Make this a generic class. While instantiating generic type parameter will be CodeStyleOption
    internal class CheckBoxWithComboOptionViewModel : AbstractNotifyPropertyChanged
    {
        private readonly AbstractOptionPreviewViewModel _info;
        //private readonly Option<TOption> _option;
        //private readonly TOption _value;

        internal string GetPreview()
        {
            return _isChecked ? _truePreview : _falsePreview;
        }

        private bool _isChecked;
        private readonly string _truePreview;
        private readonly string _falsePreview;

        public IOption Option { get; }

        public string Description { get; set; }

        // keep this as abstract - IList<object>?
        public IList<CodeStyleOptionsViewModel> Items { get; }
        private CodeStyleOptionsViewModel _selectedItem;

        public CheckBoxWithComboOptionViewModel(IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options, IList<CodeStyleOptionsViewModel> items)
        {
            this.Option = option;

            Description = description;
            _truePreview = preview;
            _falsePreview = preview;
            _info = info;

            Items = items;

            var opt = ((CodeStyleOption)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
            SetProperty(ref _isChecked, opt.IsChecked);

            var notificationViewModel = items.Where(i => i.Notification == opt.Notification).Single();
            SetProperty(ref _selectedItem, notificationViewModel);
        }

        public CheckBoxWithComboOptionViewModel(IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionSet options, IList<CodeStyleOptionsViewModel> items)
        {
            this.Option = option;

            Description = description;
            _truePreview = truePreview;
            _falsePreview = falsePreview;
            _info = info;
            Items = items;

            var opt = ((CodeStyleOption)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
            SetProperty(ref _isChecked, opt.IsChecked);

            var notificationViewModel = items.Where(i => i.Notification == opt.Notification).Single();
            SetProperty(ref _selectedItem, notificationViewModel);
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
                _info.SetOptionAndUpdatePreview(new CodeStyleOption(_isChecked, _selectedItem.Notification), Option, GetPreview());
            }
        }

        public CodeStyleOptionsViewModel SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                SetProperty(ref _selectedItem, value);
                _info.SetOptionAndUpdatePreview(new CodeStyleOption(_isChecked, _selectedItem.Notification), Option, GetPreview());
            }
        }
    }
}
