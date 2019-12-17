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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [Export(typeof(IGlobalOptionService)), Shared]
    internal class GlobalOptionService : IGlobalOptionService
    {
        private readonly Lazy<ImmutableHashSet<IOption>> _lazyAllOptions;
        private readonly ImmutableArray<Lazy<IOptionPersister>> _optionSerializers;
        private readonly ImmutableDictionary<string, ImmutableArray<Lazy<IOptionProvider, LanguageMetadata>>> _languageToLazyProvidersMap;
        private readonly Dictionary<string, ImmutableHashSet<IOption>> _languageToSerializableOptionsMap;
        private readonly HashSet<string> _forceComputedLanguages;
        private readonly ImmutableHashSet<Workspace>.Builder _registeredWorkspacesBuilder;

        private readonly object _gate = new object();

        private ImmutableDictionary<OptionKey, object?> _currentValues;

        [ImportingConstructor]
        public GlobalOptionService(
            [ImportMany] IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders,
            [ImportMany] IEnumerable<Lazy<IOptionPersister>> optionSerializers)
        {
            _lazyAllOptions = new Lazy<ImmutableHashSet<IOption>>(() => optionProviders.SelectMany(p => p.Value.Options).ToImmutableHashSet());
            _optionSerializers = optionSerializers.ToImmutableArray();
            _languageToLazyProvidersMap = optionProviders.ToPerLanguageMap();
            _languageToSerializableOptionsMap = new Dictionary<string, ImmutableHashSet<IOption>>();
            _forceComputedLanguages = new HashSet<string>();
            _registeredWorkspacesBuilder = ImmutableHashSet.CreateBuilder<Workspace>();

            _currentValues = ImmutableDictionary.Create<OptionKey, object?>();
        }

        private ImmutableHashSet<IOption> GetOrComputeSerializableOptionsForLanguage(string language)
        {
            ImmutableHashSet<IOption> options;
            lock (_gate)
            {
                if (_languageToSerializableOptionsMap.TryGetValue(language, out options))
                {
                    return options;
                }
            }

            options = ComputeSerializableOptionsForLanguage();

            lock (_gate)
            {
                if (!_languageToSerializableOptionsMap.TryGetValue(language, out var savedOptions))
                {
                    savedOptions = options;
                    _languageToSerializableOptionsMap.Add(language, savedOptions);
                }

                return savedOptions;
            }

            ImmutableHashSet<IOption> ComputeSerializableOptionsForLanguage()
            {
                var builder = ImmutableHashSet.CreateBuilder<IOption>();

                if (!_languageToLazyProvidersMap.TryGetValue(language, out var lazyProvidersAndMetadata))
                {
                    return ImmutableHashSet<IOption>.Empty;
                }

                foreach (var lazyProviderAndMetadata in lazyProvidersAndMetadata)
                {
                    // We only consider the options defined in the the DefaultAssemblies (Workspaces and Features) as serializable.
                    // This is due to the fact that other layers above are VS specific and do not execute in OOP.
                    var provider = lazyProviderAndMetadata.Value;
                    if (!MefHostServices.IsDefaultAssembly(provider.GetType().Assembly))
                    {
                        continue;
                    }

                    builder.AddRange(provider.Options);
                }

                return builder.ToImmutable();
            }
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
            return _lazyAllOptions.Value;
        }

        public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages)
        {
            if (languages.IsEmpty)
            {
                return ImmutableHashSet<IOption>.Empty;
            }

            var builder = ImmutableHashSet.CreateBuilder<IOption>();

            // "string.Empty" for options from language agnostic option providers.
            builder.AddRange(GetOrComputeSerializableOptionsForLanguage(string.Empty));

            foreach (var language in languages)
            {
                builder.AddRange(GetOrComputeSerializableOptionsForLanguage(language));
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Gets force computed serializable options with prefetched values for all the registered options applicable to the given <paramref name="languages"/> by quering the option persisters.
        /// </summary>
        public SerializableOptionSet GetForceComputedOptions(ImmutableHashSet<string> languages, IOptionService optionService)
        {
            var serializableOptionKeys = GetRegisteredSerializableOptions(languages);
            var serializableOptionValues = GetSerializableOptionValues(serializableOptionKeys, languages);
            return new SerializableOptionSet(languages, optionService, serializableOptionKeys, serializableOptionValues);
        }

        private ImmutableDictionary<OptionKey, object?> GetSerializableOptionValues(ImmutableHashSet<IOption> optionKeys, ImmutableHashSet<string> languages)
        {
            if (optionKeys.IsEmpty)
            {
                return ImmutableDictionary<OptionKey, object?>.Empty;
            }

            ForceComputeOptionValues(optionKeys, languages);

            lock (_gate)
            {
                return ImmutableDictionary.CreateRange(_currentValues
                    .Where(kvp => optionKeys.Contains(kvp.Key.Option) &&
                                   (!kvp.Key.Option.IsPerLanguage ||
                                    languages.Contains(kvp.Key.Language!))));
            }

            // Local functions
            void ForceComputeOptionValues(ImmutableHashSet<IOption> options, ImmutableHashSet<string> languages)
            {
                lock (_gate)
                {
                    if (languages.All(_forceComputedLanguages.Contains))
                    {
                        return;
                    }
                }

                foreach (var option in options)
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
            var changedOptionKeys = optionSet switch
            {
                null => throw new ArgumentNullException(nameof(optionSet)),
                SerializableOptionSet serializableOptionSet => serializableOptionSet.GetChangedOptions(),
                _ => throw new ArgumentException(WorkspacesResources.Options_did_not_come_from_specified_Solution, paramName: nameof(optionSet))
            };

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
            UpdateRegisteredWorkspacesAndRaiseEvents(changedOptions);
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

            UpdateRegisteredWorkspacesAndRaiseEvents(new List<OptionChangedEventArgs> { new OptionChangedEventArgs(optionKey, newValue) });
        }

        private void UpdateRegisteredWorkspacesAndRaiseEvents(List<OptionChangedEventArgs> changedOptions)
        {
            if (changedOptions.Count == 0)
            {
                return;
            }

            // Ensure that the Workspace's CurrentSolution snapshot is updated with new options for all registered workspaces
            // prior to raising option changed event handlers.
            ImmutableHashSet<Workspace> registeredWorkspaces;
            lock (_gate)
            {
                registeredWorkspaces = _registeredWorkspacesBuilder.ToImmutable();
            }

            foreach (var workspace in registeredWorkspaces)
            {
                workspace.UpdateCurrentSolutionOnOptionsChanged();
            }

            // Raise option changed events.
            var optionChanged = OptionChanged;
            if (optionChanged != null)
            {
                foreach (var changedOption in changedOptions)
                {
                    optionChanged(this, changedOption);
                }
            }
        }

        public void RegisterWorkspace(Workspace workspace)
        {
            lock (_gate)
            {
                _registeredWorkspacesBuilder.Add(workspace);
            }
        }

        public void UnregisterWorkspace(Workspace workspace)
        {
            lock (_gate)
            {
                _registeredWorkspacesBuilder.Remove(workspace);
            }
        }

        public event EventHandler<OptionChangedEventArgs>? OptionChanged;
    }
}
