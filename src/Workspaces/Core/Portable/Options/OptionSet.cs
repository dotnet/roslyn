// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Options
{
    public abstract class OptionSet
    {
        /// <summary>
        /// Gets the value of the option.
        /// </summary>
        public abstract object? GetOption(OptionKey optionKey);

        /// <summary>
        /// Gets the value of the option.
        /// </summary>
        [return: MaybeNull]
        public T GetOption<T>(Option<T> option)
        {
            return (T)GetOption(new OptionKey(option, language: null))!;
        }

        /// <summary>
        /// Gets the value of the option.
        /// </summary>
        [return: MaybeNull]
        public T GetOption<T>(PerLanguageOption<T> option, string? language)
        {
            return (T)GetOption(new OptionKey(option, language))!;
        }

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public abstract OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value);

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(Option<T> option, [MaybeNull] T value)
        {
            return WithChangedOption(new OptionKey(option), value);
        }

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption<T>(PerLanguageOption<T> option, string? language, [MaybeNull] T value)
        {
            return WithChangedOption(new OptionKey(option, language), value);
        }

        internal abstract IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet);
    }
}
