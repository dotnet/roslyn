// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class PerLanguageOptionBinding<T>
    {
        private readonly IOptionService _optionService;
        private readonly PerLanguageOption<T> _key;
        private readonly string _languageName;

        public PerLanguageOptionBinding(IOptionService optionService, PerLanguageOption<T> key, string languageName)
        {
            _optionService = optionService;
            _key = key;
            _languageName = languageName;
        }

        public T Value
        {
            get
            {
                return _optionService.GetOption(_key, _languageName);
            }

            set
            {
                var oldOptions = _optionService.GetOptions();
                var newOptions = oldOptions.WithChangedOption(_key, _languageName, value);

                _optionService.SetOptions(newOptions);
                OptionLogger.Log(oldOptions, newOptions);
            }
        }
    }
}
