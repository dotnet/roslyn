// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    public abstract class OptionSet
    {
        /// <summary>
        /// Gets the value of the option.
        /// </summary>
        public abstract object GetOption(OptionKey optionKey);

        /// <summary>
        /// Gets the value of the option.
        /// </summary>
        public T GetOption<T>(Option<T> option)
        {
            return (T)GetOption(new OptionKey(option, language: null));
        }

        /// <summary>
        /// Gets the value of the option.
        /// </summary>
        public T GetOption<T>(PerLanguageOption<T> option, string language)
        {
            return (T)GetOption(new OptionKey(option, language));
        }

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public abstract OptionSet WithChangedOption(OptionKey optionAndLanguage, object value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(Option<T> option, T value)
        {
            return WithChangedOption(new OptionKey(option, language: null), value);
        }

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(PerLanguageOption<T> option, string language, T value)
        {
            return WithChangedOption(new OptionKey(option, language), value);
        }

        internal abstract IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet);
    }
}