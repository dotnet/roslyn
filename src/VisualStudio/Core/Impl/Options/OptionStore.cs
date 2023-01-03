// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        public OptionStore(OptionSet optionSet)
        {
            _optionSet = optionSet;
        }

        public object GetOption(OptionKey optionKey) => _optionSet.GetOption(optionKey);
        public T GetOption<T>(OptionKey optionKey) => _optionSet.GetOption<T>(optionKey);
        internal T GetOption<T>(Option2<T> option) => _optionSet.GetOption(option);
        internal T GetOption<T>(PerLanguageOption2<T> option, string language) => _optionSet.GetOption(option, language);
        public OptionSet GetOptions() => _optionSet;

        public void SetOption(OptionKey optionKey, object value)
        {
            _optionSet = _optionSet.WithChangedOption(optionKey, value);

            OnOptionChanged(optionKey);
        }

        internal void SetOption<T>(Option2<T> option, T value)
            => SetOption(new OptionKey(option), value);

        internal void SetOption<T>(PerLanguageOption2<T> option, string language, T value)
            => SetOption(new OptionKey(option, language), value);

        public void SetOptions(OptionSet optionSet)
            => _optionSet = optionSet;

        private void OnOptionChanged(OptionKey optionKey)
            => OptionChanged?.Invoke(this, optionKey);
    }
}
