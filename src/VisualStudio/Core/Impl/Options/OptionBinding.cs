// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class OptionBinding<T>
    {
        private readonly IOptionService _optionService;
        private readonly Option<T> _key;

        public OptionBinding(IOptionService optionService, Option<T> key)
        {
            _optionService = optionService;
            _key = key;
        }

        public T Value
        {
            get
            {
                return _optionService.GetOption(_key);
            }

            set
            {
                var oldOptions = _optionService.GetOptions();
                var newOptions = oldOptions.WithChangedOption(_key, value);

                _optionService.SetOptions(newOptions);
                OptionLogger.Log(oldOptions, newOptions);
            }
        }
    }
}
