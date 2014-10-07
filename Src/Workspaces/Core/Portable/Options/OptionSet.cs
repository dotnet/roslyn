// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    public sealed class OptionSet
    {
        private readonly IOptionService service;

        private readonly object gate = new object();
        private ImmutableDictionary<OptionKey, object> values;

        internal OptionSet(IOptionService service)
        {
            this.service = service;
            this.values = ImmutableDictionary.Create<OptionKey, object>();
        }

        private OptionSet(IOptionService service, ImmutableDictionary<OptionKey, object> values)
        {
            this.service = service;
            this.values = values;
        }

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
        /// Gets the value of the option.
        /// </summary>
        public object GetOption(OptionKey optionKey)
        {
            lock (gate)
            {
                object value;

                if (!values.TryGetValue(optionKey, out value))
                {
                    value = this.service != null ? this.service.GetOption(optionKey) : optionKey.Option.DefaultValue;
                    values = values.Add(optionKey, value);
                }

                return value;
            }
        }

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

        /// <summary>
        /// Creates a new <see cref="OptionSet" /> that contains the changed value.
        /// </summary>
        public OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
        {
            // make sure we first load this in current optionset
            this.GetOption(optionAndLanguage);

            return new OptionSet(this.service, this.values.SetItem(optionAndLanguage, value));
        }

        /// <summary>
        /// Gets a list of all the options that were accessed.
        /// </summary>
        internal IEnumerable<OptionKey> GetAccessedOptions()
        {
            var optionSet = this.service.GetOptions();
            return GetChangedOptions(optionSet);
        }

        internal IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
        {
            foreach (var kvp in this.values)
            {
                var currentValue = optionSet.GetOption(kvp.Key);
                if (!object.Equals(currentValue, kvp.Value))
                {
                    yield return kvp.Key;
                }
            }
        }
    }
}
