// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal abstract class AbstractRadioButtonViewModel : AbstractNotifyPropertyChanged
    {
        private readonly AbstractOptionPreviewViewModel _info;
        internal readonly string Preview;
        private bool _isChecked;
        private string _groupName;

        public string Description { get; }
        public string GroupName { get; }

        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }

            set
            {
                SetProperty(ref _isChecked, value);

                if (_isChecked)
                {
                    SetOptionAndUpdatePreview(_info, Preview);
                }
            }
        }

        public AbstractRadioButtonViewModel(string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options, bool isChecked, string group)
        {
            Description = description;
            this.Preview = preview;
            _info = info;
            this.GroupName = group;

            SetProperty(ref _isChecked, isChecked);
        }

        internal abstract void SetOptionAndUpdatePreview(AbstractOptionPreviewViewModel info, string preview);
    }
}
