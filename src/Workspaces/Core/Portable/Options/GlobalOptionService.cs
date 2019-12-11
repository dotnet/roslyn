// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [Export(typeof(IGlobalOptionService)), Shared]
    internal class GlobalOptionService : IGlobalOptionService
    {
        private readonly Lazy<ImmutableHashSet<IOption>> _options;
        private readonly Lazy<ImmutableHashSet<IOption>> _serializableOptions;
        private readonly ImmutableArray<Lazy<IOptionPersister>> _optionSerializers;

        private readonly object _gate = new object();

        private ImmutableDictionary<OptionKey, object?> _currentValues;
        private readonly HashSet<string> _forceComputedLanguages;

        [ImportingConstructor]
        public GlobalOptionService(
            [ImportMany] IEnumerable<Lazy<IOptionProvider>> optionProviders,
            [ImportMany] IEnumerable<Lazy<IOptionPersister>> optionSerializers)
        {
            _options = GetLazyOptions(optionProviders, onlySerializable: false);
            _serializableOptions = GetLazyOptions(optionProviders, onlySerializable: true);
            _optionSerializers = optionSerializers.ToImmutableArray();
            _currentValues = ImmutableDictionary.Create<OptionKey, object?>();
            _forceComputedLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static Lazy<ImmutableHashSet<IOption>> GetLazyOptions(IEnumerable<Lazy<IOptionProvider>> optionProviders, bool onlySerializable)
        {
            return new Lazy<ImmutableHashSet<IOption>>(() =>
            {
                var builder = ImmutableHashSet.CreateBuilder<IOption>();

                foreach (var lazyProvider in optionProviders)
                {
                    var provider = lazyProvider.Value;
                    if (onlySerializable &&
                        !MefHostServices.DefaultAssemblies.Contains(provider.GetType().Assembly))
                    {
                        continue;
                    }

                    builder.AddRange(provider.Options);
                }

                return builder.ToImmutable();
            });
        }

        private object? LoadOptionFromSerializerOrGetDefault(OptionKey optionKey)
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

        public ImmutableHashSet<IOption> GetRegisteredSerializableOptions()
        {
            return _serializableOptions.Value;
        }

        /// <summary>
        /// Gets force computed serializable options with prefetched values for all the registered options by quering the option persisters.
        /// </summary>
        public ImmutableDictionary<OptionKey, object?> GetForceComputedRegisteredSerializableOptionValues(IEnumerable<string> languages)
        {
            ForceComputeOptions(languages);
            lock (_gate)
            {
                return ImmutableDictionary.CreateRange(_currentValues
                    .Where(kvp => _serializableOptions.Value.Contains(kvp.Key.Option) &&
                                   (!kvp.Key.Option.IsPerLanguage ||
                                    languages.Contains(kvp.Key.Language!))));
            }
        }

        private void ForceComputeOptions(IEnumerable<string> languages)
        {
            lock (_gate)
            {
                if (languages.All(_forceComputedLanguages.Contains))
                {
                    return;
                }
            }

            foreach (var option in GetRegisteredSerializableOptions())
            {
                if (!option.IsPerLanguage)
                {
                    var key = new OptionKey(option);
                    var _ = GetOption(key);
                    continue;
                }

                foreach (var language in languages)
                {
                    var key = new OptionKey(option, language);
                    var _ = GetOption(key);
                }
            }

            lock (_gate)
            {
                _forceComputedLanguages.AddRange(languages);
            }
        }

        [return: MaybeNull]
        public T GetOption<T>(Option<T> option)
        {
            return (T)GetOption(new OptionKey(option))!;
        }

        [return: MaybeNull]
        public T GetOption<T>(PerLanguageOption<T> option, string? language)
        {
            return (T)GetOption(new OptionKey(option, language))!;
        }

        public object? GetOption(OptionKey optionKey)
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

            var changedOptionKeys = (optionSet as WorkspaceOptionSet)?.GetChangedOptions() ?? (optionSet as SolutionOptionSet)?.GetChangedOptions();

            if (changedOptionKeys == null)
            {
                throw new ArgumentException(WorkspacesResources.Options_did_not_come_from_Workspace, paramName: nameof(optionSet));
            }

            var changedOptions = new List<OptionChangedEventArgs>();

            lock (_gate)
            {
                foreach (var optionKey in changedOptionKeys)
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

        public void RefreshOption(OptionKey optionKey, object? newValue)
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
            if (changedOptions.Count == 0)
            {
                return;
            }

            var optionChanged = OptionChanged;
            if (optionChanged != null)
            {
                foreach (var changedOption in changedOptions)
                {
                    optionChanged(this, changedOption);
                }
            }

            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<OptionChangedEventArgs>? OptionChanged;
        public event EventHandler<EventArgs>? OptionsChanged;
    }
}
