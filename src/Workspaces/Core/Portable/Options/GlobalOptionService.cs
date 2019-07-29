// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [Export(typeof(IGlobalOptionService)), Shared]
    internal class GlobalOptionService : IGlobalOptionService
    {
        private readonly Lazy<HashSet<IOption>> _options;
        private readonly ImmutableArray<Lazy<IOptionPersister>> _optionSerializers;

        private readonly object _gate = new object();

        private ImmutableDictionary<OptionKey, object> _currentValues;

        [ImportingConstructor]
        public GlobalOptionService(
            [ImportMany] IEnumerable<Lazy<IOptionProvider>> optionProviders,
            [ImportMany] IEnumerable<Lazy<IOptionPersister>> optionSerializers)
        {
            _options = new Lazy<HashSet<IOption>>(() =>
            {
                var options = new HashSet<IOption>();

                foreach (var provider in optionProviders)
                {
                    options.AddRange(provider.Value.Options);
                }

                return options;
            });

            _optionSerializers = optionSerializers.ToImmutableArray();
            _currentValues = ImmutableDictionary.Create<OptionKey, object>();
        }

        private object LoadOptionFromSerializerOrGetDefault(OptionKey optionKey)
        {
            foreach (var serializer in _optionSerializers)
            {
                // We have a deserializer, so deserialize and use that value.
                if (serializer.Value.TryFetch(optionKey, out var deserializedValue))
                {
                    return deserializedValue;
                }
            }

            // Just use the default. We will still cache this so we aren't trying to deserialize
            // over and over.
            return optionKey.Option.DefaultValue;
        }

        public IEnumerable<IOption> GetRegisteredOptions()
        {
            return _options.Value;
        }

        public T GetOption<T>(Option<T> option)
        {
            return (T)GetOption(new OptionKey(option, language: null));
        }

        public T GetOption<T>(PerLanguageOption<T> option, string language)
        {
            return (T)GetOption(new OptionKey(option, language));
        }

        public object GetOption(OptionKey optionKey)
        {
            lock (_gate)
            {
                if (_currentValues.TryGetValue(optionKey, out var value))
                {
                    return value;
                }

                value = LoadOptionFromSerializerOrGetDefault(optionKey);

                _currentValues = _currentValues.Add(optionKey, value);

                return value;
            }
        }

        public void SetOptions(OptionSet optionSet)
        {
            if (optionSet == null)
            {
                throw new ArgumentNullException(nameof(optionSet));
            }

            var workspaceOptionSet = optionSet as WorkspaceOptionSet;

            if (workspaceOptionSet == null)
            {
                throw new ArgumentException(WorkspacesResources.Options_did_not_come_from_Workspace, paramName: nameof(optionSet));
            }

            var changedOptions = new List<OptionChangedEventArgs>();

            lock (_gate)
            {
                foreach (var optionKey in workspaceOptionSet.GetAccessedOptions())
                {
                    var setValue = optionSet.GetOption(optionKey);
                    var currentValue = this.GetOption(optionKey);

                    if (object.Equals(currentValue, setValue))
                    {
                        // Identical, so nothing is changing
                        continue;
                    }

                    // The value is actually changing, so update
                    changedOptions.Add(new OptionChangedEventArgs(optionKey, setValue));

                    _currentValues = _currentValues.SetItem(optionKey, setValue);

                    foreach (var serializer in _optionSerializers)
                    {
                        if (serializer.Value.TryPersist(optionKey, setValue))
                        {
                            break;
                        }
                    }
                }
            }

            // Outside of the lock, raise the events on our task queue.
            RaiseEvents(changedOptions);
        }

        public void RefreshOption(OptionKey optionKey, object newValue)
        {
            lock (_gate)
            {
                if (_currentValues.TryGetValue(optionKey, out var oldValue))
                {
                    if (object.Equals(oldValue, newValue))
                    {
                        // Value is still the same, no reason to raise events
                        return;
                    }
                }

                _currentValues = _currentValues.SetItem(optionKey, newValue);
            }

            RaiseEvents(new List<OptionChangedEventArgs> { new OptionChangedEventArgs(optionKey, newValue) });
        }

        private void RaiseEvents(List<OptionChangedEventArgs> changedOptions)
        {
            var optionChanged = OptionChanged;
            if (optionChanged != null)
            {
                foreach (var changedOption in changedOptions)
                {
                    optionChanged(this, changedOption);
                }
            }
        }

        public event EventHandler<OptionChangedEventArgs> OptionChanged;
    }
}
