// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An implementation of <see cref="OptionSet"/> that fetches values it doesn't know about to the workspace's option service. It ensures a contract
    /// that values are immutable from this instance once observed.
    /// </summary>
    internal sealed class WorkspaceOptionSet : OptionSet
    {
        private readonly IOptionService _service;

        private readonly object _gate = new object();
        private ImmutableDictionary<OptionKey, object> _values;

        internal WorkspaceOptionSet(IOptionService service)
        {
            _service = service;
            _values = ImmutableDictionary.Create<OptionKey, object>();
        }

        private WorkspaceOptionSet(IOptionService service, ImmutableDictionary<OptionKey, object> values)
        {
            _service = service;
            _values = values;
        }

        public override T GetOption<T>(Option<T> option)
        {
            return (T)GetOption(new OptionKey(option, language: null));
        }

        public override T GetOption<T>(PerLanguageOption<T> option, string language)
        {
            return (T)GetOption(new OptionKey(option, language));
        }

        public override object GetOption(OptionKey optionKey)
        {
            lock (_gate)
            {
                object value;

                if (!_values.TryGetValue(optionKey, out value))
                {
                    value = _service != null ? _service.GetOption(optionKey) : optionKey.Option.DefaultValue;
                    _values = _values.Add(optionKey, value);
                }

                return value;
            }
        }

        public override OptionSet WithChangedOption<T>(Option<T> option, T value)
        {
            return WithChangedOption(new OptionKey(option, language: null), value);
        }

        public override OptionSet WithChangedOption<T>(PerLanguageOption<T> option, string language, T value)
        {
            return WithChangedOption(new OptionKey(option, language), value);
        }

        public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
        {
            // make sure we first load this in current optionset
            this.GetOption(optionAndLanguage);

            return new WorkspaceOptionSet(_service, _values.SetItem(optionAndLanguage, value));
        }

        /// <summary>
        /// Gets a list of all the options that were accessed.
        /// </summary>
        internal IEnumerable<OptionKey> GetAccessedOptions()
        {
            var optionSet = _service.GetOptions();
            return GetChangedOptions(optionSet);
        }

        internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
        {
            foreach (var kvp in _values)
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
