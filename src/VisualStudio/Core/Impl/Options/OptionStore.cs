// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public T GetOption<T>(OptionKey optionKey) => _optionSet.GetOption<T>(optionKey);
        public T GetOption<T>(Option<T> option) => _optionSet.GetOption(option);
        internal T GetOption<T>(Option2<T> option) => _optionSet.GetOption(option);
        public T GetOption<T>(PerLanguageOption<T> option, string language) => _optionSet.GetOption(option, language);
        internal T GetOption<T>(PerLanguageOption2<T> option, string language) => _optionSet.GetOption(option, language);
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

        internal void SetOption<T>(Option2<T> option, T value)
        {
            _optionSet = _optionSet.WithChangedOption(option, value);

            OnOptionChanged(new OptionKey(option));
        }

        public void SetOption<T>(PerLanguageOption<T> option, string language, T value)
        {
            _optionSet = _optionSet.WithChangedOption(option, language, value);

            OnOptionChanged(new OptionKey(option, language));
        }

        internal void SetOption<T>(PerLanguageOption2<T> option, string language, T value)
        {
            _optionSet = _optionSet.WithChangedOption(option, language, value);

            OnOptionChanged(new OptionKey(option, language));
        }

        public IEnumerable<IOption> GetRegisteredOptions()
            => _registeredOptions;

        public void SetOptions(OptionSet optionSet)
            => _optionSet = optionSet;

        public void SetRegisteredOptions(IEnumerable<IOption> registeredOptions)
            => _registeredOptions = registeredOptions;

        private void OnOptionChanged(OptionKey optionKey)
            => OptionChanged?.Invoke(this, optionKey);
    }
}
