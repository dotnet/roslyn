﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [Export(typeof(IOptionService))]
    [Shared]
    internal class OptionService : IOptionService
    {
        private readonly Lazy<HashSet<IOption>> _options;
        private readonly ImmutableDictionary<string, ImmutableArray<Lazy<IOptionSerializer, OptionSerializerMetadata>>> _featureNameToOptionSerializers =
            ImmutableDictionary.Create<string, ImmutableArray<Lazy<IOptionSerializer, OptionSerializerMetadata>>>();

        private readonly object _gate = new object();
        private ImmutableDictionary<OptionKey, object> _currentValues;

        [ImportingConstructor]
        public OptionService(
            [ImportMany] IEnumerable<Lazy<IOptionProvider>> optionProviders,
            [ImportMany] IEnumerable<Lazy<IOptionSerializer, OptionSerializerMetadata>> optionSerializers)
        {
            _options = new Lazy<HashSet<IOption>>(() =>
            {
                var options = new HashSet<IOption>();

                foreach (var provider in optionProviders)
                {
                    options.AddRange(provider.Value.GetOptions());
                }

                return options;
            });

            foreach (var optionSerializerAndMetadata in optionSerializers)
            {
                foreach (var featureName in optionSerializerAndMetadata.Metadata.Features)
                {
                    ImmutableArray<Lazy<IOptionSerializer, OptionSerializerMetadata>> existingSerializers;
                    if (!_featureNameToOptionSerializers.TryGetValue(featureName, out existingSerializers))
                    {
                        existingSerializers = ImmutableArray.Create<Lazy<IOptionSerializer, OptionSerializerMetadata>>();
                    }

                    _featureNameToOptionSerializers = _featureNameToOptionSerializers.SetItem(featureName, existingSerializers.Add(optionSerializerAndMetadata));
                }
            }

            _currentValues = ImmutableDictionary.Create<OptionKey, object>();
        }

        private object LoadOptionFromSerializerOrGetDefault(OptionKey optionKey)
        {
            lock (_gate)
            {
                ImmutableArray<Lazy<IOptionSerializer, OptionSerializerMetadata>> optionSerializers;
                if (_featureNameToOptionSerializers.TryGetValue(optionKey.Option.Feature, out optionSerializers))
                {
                    foreach (var serializer in optionSerializers)
                    {
                        // There can be options (ex, formatting) that only exist in only one specific language. In those cases,
                        // feature's serializer should exist in only that language.
                        if (!SupportedSerializer(optionKey, serializer.Metadata))
                        {
                            continue;
                        }

                        // We have a deserializer, so deserialize and use that value.
                        object deserializedValue;
                        if (serializer.Value.TryFetch(optionKey, out deserializedValue))
                        {
                            return deserializedValue;
                        }
                    }
                }

                // Just use the default. We will still cache this so we aren't trying to deserialize
                // over and over.
                return optionKey.Option.DefaultValue;
            }
        }

        public IEnumerable<IOption> GetRegisteredOptions()
        {
            return _options.Value;
        }

        public OptionSet GetOptions()
        {
            return new OptionSet(this);
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
                object value;

                if (_currentValues.TryGetValue(optionKey, out value))
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

            var changedOptions = new List<OptionChangedEventArgs>();

            lock (_gate)
            {
                foreach (var optionKey in optionSet.GetAccessedOptions())
                {
                    var setValue = optionSet.GetOption(optionKey);
                    object currentValue = this.GetOption(optionKey);

                    if (object.Equals(currentValue, setValue))
                    {
                        // Identical, so nothing is changing
                        continue;
                    }

                    // The value is actually changing, so update
                    changedOptions.Add(new OptionChangedEventArgs(optionKey, setValue));

                    _currentValues = _currentValues.SetItem(optionKey, setValue);

                    ImmutableArray<Lazy<IOptionSerializer, OptionSerializerMetadata>> optionSerializers;
                    if (_featureNameToOptionSerializers.TryGetValue(optionKey.Option.Feature, out optionSerializers))
                    {
                        foreach (var serializer in optionSerializers)
                        {
                            // There can be options (ex, formatting) that only exist in only one specific language. In those cases,
                            // feature's serializer should exist in only that language.
                            if (!SupportedSerializer(optionKey, serializer.Metadata))
                            {
                                continue;
                            }

                            if (serializer.Value.TryPersist(optionKey, setValue))
                            {
                                break;
                            }
                        }
                    }
                }
            }

            // Outside of the lock, raise events
            var optionChanged = OptionChanged;
            if (optionChanged != null)
            {
                foreach (var changedOption in changedOptions)
                {
                    optionChanged(this, changedOption);
                }
            }
        }

        private static bool SupportedSerializer(OptionKey optionKey, OptionSerializerMetadata metadata)
        {
            return optionKey.Language == null || optionKey.Language == metadata.Language;
        }

        public event EventHandler<OptionChangedEventArgs> OptionChanged;
    }
}
