// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal abstract class AbstractCheckBoxViewModel : AbstractNotifyPropertyChanged
    {
        private readonly string _truePreview;
        private readonly string _falsePreview;
        protected bool _isChecked;

        protected AbstractOptionPreviewViewModel Info { get; }
        public IOption Option { get; }
        public string Description { get; set; }

        internal virtual string GetPreview() => _isChecked ? _truePreview : _falsePreview;

        public AbstractCheckBoxViewModel(IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options)
            : this(option, description, preview, preview, info, options)
        {
        }

        public AbstractCheckBoxViewModel(IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionSet options)
        {
            _truePreview = truePreview;
            _falsePreview = falsePreview;

            Info = info;
            Option = option;
            Description = description;
        }

        public abstract bool IsChecked { get; set; }
    }
}
