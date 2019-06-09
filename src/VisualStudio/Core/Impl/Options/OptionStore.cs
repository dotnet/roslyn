// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// This class is intended to be used by Option pages. It will provide access to an options
    /// from an optionset but will not persist changes automatically.
    /// </summary>
    public class OptionStore
    {
        public event EventHandler<OptionKey> OptionChanged;

        private OptionSet _optionSet;
        private IEnumerable<IOption> _registeredOptions;

        public OptionStore(OptionSet optionSet, IEnumerable<IOption> registeredOptions)
        {
            _optionSet = optionSet;
            _registeredOptions = registeredOptions;
        }

        public object GetOption(OptionKey optionKey) => _optionSet.GetOption(optionKey);
        public T GetOption<T>(Option<T> option) => _optionSet.GetOption(option);
        public T GetOption<T>(PerLanguageOption<T> option, string language) => _optionSet.GetOption(option, language);
        public OptionSet GetOptions() => _optionSet;

        public void SetOption(OptionKey optionKey, object value)
        {
            _optionSet = _optionSet.WithChangedOption(optionKey, value);

            OnOptionChanged(optionKey);
        }

        public void SetOption<T>(Option<T> option, T value)
        {
            _optionSet = _optionSet.WithChangedOption(option, value);

            OnOptionChanged(new OptionKey(option));
        }

        public void SetOption<T>(PerLanguageOption<T> option, string language, T value)
        {
            _optionSet = _optionSet.WithChangedOption(option, language, value);

            OnOptionChanged(new OptionKey(option, language));
        }

        public IEnumerable<IOption> GetRegisteredOptions()
        {
            return _registeredOptions;
        }

        public void SetOptions(OptionSet optionSet)
        {
            _optionSet = optionSet;
        }

        public void SetRegisteredOptions(IEnumerable<IOption> registeredOptions)
        {
            _registeredOptions = registeredOptions;
        }

        private void OnOptionChanged(OptionKey optionKey)
        {
            OptionChanged?.Invoke(this, optionKey);
        }
    }
}
