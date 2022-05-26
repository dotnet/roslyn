// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [Export(typeof(IGlobalOptionService)), Shared]
    internal sealed class GlobalOptionService : IGlobalOptionService
    {
        private static readonly ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> s_emptyEditorConfigKeysToOptions
            = ImmutableDictionary.Create<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)>(AnalyzerConfigOptions.KeyComparer);

        private readonly IWorkspaceThreadingService? _workspaceThreadingService;
        private readonly Lazy<ImmutableHashSet<IOption>> _lazyAllOptions;
        private readonly ImmutableArray<Lazy<IOptionPersisterProvider>> _optionPersisterProviders;
        private readonly ImmutableDictionary<string, Lazy<ImmutableHashSet<IOption>>> _serializableOptionsByLanguage;
        private readonly HashSet<string> _forceComputedLanguages = new();

        // access is interlocked
        private ImmutableArray<Workspace> _registeredWorkspaces;

        private readonly object _gate = new();

        #region Guarded by _gate

#pragma warning disable IDE0044 // Add readonly modifier - https://github.com/dotnet/roslyn/issues/46785
        private ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> _neutralEditorConfigKeysToOptions = s_emptyEditorConfigKeysToOptions;
        private ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> _csharpEditorConfigKeysToOptions = s_emptyEditorConfigKeysToOptions;
        private ImmutableDictionary<string, (IOption? option, IEditorConfigStorageLocation2? storageLocation)> _visualBasicEditorConfigKeysToOptions = s_emptyEditorConfigKeysToOptions;
#pragma warning restore IDE0044 // Add readonly modifier

        private ImmutableArray<IOptionPersister> _lazyOptionPersisters;

        private ImmutableDictionary<OptionKey, object?> _currentValues;
        private ImmutableHashSet<OptionKey> _changedOptionKeys;

        #endregion

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public GlobalOptionService(
            [Import(AllowDefault = true)] IWorkspaceThreadingService? workspaceThreadingService,
            [ImportMany] IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders,
            [ImportMany] IEnumerable<Lazy<IOptionPersisterProvider>> optionPersisters)
        {
            _workspaceThreadingService = workspaceThreadingService;
            _lazyAllOptions = new Lazy<ImmutableHashSet<IOption>>(() => optionProviders.SelectMany(p => p.Value.Options).ToImmutableHashSet());
            _optionPersisterProviders = optionPersisters.ToImmutableArray();
            _serializableOptionsByLanguage = CreateLazySerializableOptionsByLanguage(optionProviders);
            _registeredWorkspaces = ImmutableArray<Workspace>.Empty;

            _currentValues = ImmutableDictionary.Create<OptionKey, object?>();
            _changedOptionKeys = ImmutableHashSet<OptionKey>.Empty;
        }

        private static ImmutableDictionary<string, Lazy<ImmutableHashSet<IOption>>> CreateLazySerializableOptionsByLanguage(IEnumerable<Lazy<IOptionProvider, LanguageMetadata>> optionProviders)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, Lazy<ImmutableHashSet<IOption>>>();

            foreach (var (language, lazyProvidersAndMetadata) in optionProviders.ToPerLanguageMap())
            {
                builder.Add(language, new Lazy<ImmutableHashSet<IOption>>(() => ComputeSerializableOptionsFromProviders(lazyProvidersAndMetadata)));
            }

            return builder.ToImmutable();

            // Local functions
            static ImmutableHashSet<IOption> ComputeSerializableOptionsFromProviders(ImmutableArray<Lazy<IOptionProvider, LanguageMetadata>> lazyProvidersAndMetadata)
            {
                var builder = ImmutableHashSet.CreateBuilder<IOption>();

                foreach (var lazyProviderAndMetadata in lazyProvidersAndMetadata)
                {
                    var provider = lazyProviderAndMetadata.Value;
                    if (!IsSolutionOptionProvider(provider))
                    {
                        continue;
                    }

                    builder.AddRange(provider.Options);
                }

                return builder.ToImmutable();
            }
        }

        // We only consider the options defined in the the DefaultAssemblies (Workspaces and Features) as serializable.
        // This is due to the fact that other layers above are VS specific and do not execute in OOP.
        internal static bool IsSolutionOptionProvider(IOptionProvider provider)
            => MefHostServices.IsDefaultAssembly(provider.GetType().Assembly);

        private ImmutableArray<IOptionPersister> GetOptionPersisters()
        {
            if (_lazyOptionPersisters.IsDefault)
            {
                // Option persisters cannot be initialized while holding the global options lock
                // https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1353715
                Debug.Assert(!Monitor.IsEntered(_gate));

                ImmutableInterlocked.InterlockedInitialize(
                    ref _lazyOptionPersisters,
                    GetOptionPersistersSlow(_workspaceThreadingService, _optionPersisterProviders, CancellationToken.None));
            }

            return _lazyOptionPersisters;

            // Local functions
            static ImmutableArray<IOptionPersister> GetOptionPersistersSlow(
                IWorkspaceThreadingService? workspaceThreadingService,
                ImmutableArray<Lazy<IOptionPersisterProvider>> persisterProviders,
                CancellationToken cancellationToken)
            {
                if (workspaceThreadingService is not null)
                {
                    return workspaceThreadingService.Run(() => GetOptionPersistersAsync(persisterProviders, cancellationToken));
                }
                else
                {
                    return GetOptionPersistersAsync(persisterProviders, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
                }
            }

            static async Task<ImmutableArray<IOptionPersister>> GetOptionPersistersAsync(
                ImmutableArray<Lazy<IOptionPersisterProvider>> persisterProviders,
                CancellationToken cancellationToken)
            {
                return await persisterProviders.SelectAsArrayAsync(
                    static (lazyProvider, cancellationToken) => lazyProvider.Value.GetOrCreatePersisterAsync(cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private static object? LoadOptionFromPersisterOrGetDefault(OptionKey optionKey, ImmutableArray<IOptionPersister> persisters)
        {
            foreach (var persister in persisters)
            {
                if (persister.TryFetch(optionKey, out var persistedValue))
                {
                    return persistedValue;
                }
            }

            // Just use the default. We will still cache this so we aren't trying to deserialize
            // over and over.
            return optionKey.Option.DefaultValue;
        }

        public IEnumerable<IOption> GetRegisteredOptions()
            => _lazyAllOptions.Value;

        public bool TryMapEditorConfigKeyToOption(string key, string? language, [NotNullWhen(true)] out IEditorConfigStorageLocation2? storageLocation, out OptionKey optionKey)
        {
            var temporaryOptions = s_emptyEditorConfigKeysToOptions;
            ref var editorConfigToOptionsStorage = ref temporaryOptions;
            switch (language)
            {
                case LanguageNames.CSharp:
                    editorConfigToOptionsStorage = ref _csharpEditorConfigKeysToOptions!;
                    break;

                case LanguageNames.VisualBasic:
                    editorConfigToOptionsStorage = ref _visualBasicEditorConfigKeysToOptions!;
                    break;

                case null:
                case "":
                    editorConfigToOptionsStorage = ref _neutralEditorConfigKeysToOptions!;
                    break;
            }

            var (option, storage) = ImmutableInterlocked.GetOrAdd(
                ref editorConfigToOptionsStorage,
                key,
                (key, arg) => MapToOptionIgnorePerLanguage(arg.self, key, arg.language),
                (self: this, language));

            if (option is object)
            {
                RoslynDebug.AssertNotNull(storage);
                storageLocation = storage;
                optionKey = option.IsPerLanguage ? new OptionKey(option, language) : new OptionKey(option);
                return true;
            }

            storageLocation = null;
            optionKey = default;
            return false;

            // Local function
            static (IOption? option, IEditorConfigStorageLocation2? storageLocation) MapToOptionIgnorePerLanguage(GlobalOptionService service, string key, string? language)
            {
                // Use GetRegisteredSerializableOptions instead of GetRegisteredOptions to avoid loading assemblies for
                // inactive languages.
                foreach (var option in service.GetRegisteredSerializableOptions(ImmutableHashSet.Create(language ?? "")))
                {
                    foreach (var storage in option.StorageLocations)
                    {
                        if (storage is not IEditorConfigStorageLocation2 editorConfigStorage)
                            continue;

                        if (!AnalyzerConfigOptions.KeyComparer.Equals(key, editorConfigStorage.KeyName))
                            continue;

                        return (option, editorConfigStorage);
                    }
                }

                return (null, null);
            }
        }

        public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages)
        {
            if (languages.IsEmpty)
            {
                return ImmutableHashSet<IOption>.Empty;
            }

            var builder = ImmutableHashSet.CreateBuilder<IOption>();

            // "string.Empty" for options from language agnostic option providers.
            builder.AddRange(GetSerializableOptionsForLanguage(string.Empty));

            foreach (var language in languages)
            {
                builder.AddRange(GetSerializableOptionsForLanguage(language));
            }

            return builder.ToImmutable();

            // Local functions.
            ImmutableHashSet<IOption> GetSerializableOptionsForLanguage(string language)
            {
                if (_serializableOptionsByLanguage.TryGetValue(language, out var lazyOptions))
                {
                    return lazyOptions.Value;
                }

                return ImmutableHashSet<IOption>.Empty;
            }
        }

        /// <summary>
        /// Gets force computed serializable options with prefetched values for all the registered options applicable to the given <paramref name="languages"/> by quering the option persisters.
        /// </summary>
        public SerializableOptionSet GetSerializableOptionsSnapshot(ImmutableHashSet<string> languages, IOptionService optionService)
        {
            Debug.Assert(languages.All(RemoteSupportedLanguages.IsSupported));
            var serializableOptions = GetRegisteredSerializableOptions(languages);
            var serializableOptionValues = GetSerializableOptionValues(serializableOptions, languages);
            var changedOptionsKeysSerializable = _changedOptionKeys
                .Where(key => serializableOptions.Contains(key.Option) && (!key.Option.IsPerLanguage || languages.Contains(key.Language!)))
                .ToImmutableHashSet();
            return new SerializableOptionSet(optionService, serializableOptionValues, changedOptionsKeysSerializable);
        }

        private ImmutableDictionary<OptionKey, object?> GetSerializableOptionValues(ImmutableHashSet<IOption> optionKeys, ImmutableHashSet<string> languages)
        {
            if (optionKeys.IsEmpty)
            {
                return ImmutableDictionary<OptionKey, object?>.Empty;
            }

            // Ensure the option persisters are available before taking the global lock
            var persisters = GetOptionPersisters();

            lock (_gate)
            {
                // Force compute the option values for languages, if required.
                if (!languages.All(_forceComputedLanguages.Contains))
                {
                    foreach (var option in optionKeys)
                    {
                        if (!option.IsPerLanguage)
                        {
                            var key = new OptionKey(option);
                            var _ = GetOption_NoLock(key, persisters);
                            continue;
                        }

                        foreach (var language in languages)
                        {
                            var key = new OptionKey(option, language);
                            var _ = GetOption_NoLock(key, persisters);
                        }
                    }

                    _forceComputedLanguages.AddRange(languages);
                }

                return ImmutableDictionary.CreateRange(_currentValues
                    .Where(kvp => optionKeys.Contains(kvp.Key.Option) &&
                                   (!kvp.Key.Option.IsPerLanguage ||
                                    languages.Contains(kvp.Key.Language!))));
            }
        }

        public T GetOption<T>(Option<T> option)
            => OptionsHelpers.GetOption(option, GetOption);

        public T GetOption<T>(Option2<T> option)
            => OptionsHelpers.GetOption(option, GetOption);

        public T GetOption<T>(PerLanguageOption<T> option, string? language)
            => OptionsHelpers.GetOption(option, language, GetOption);

        public T GetOption<T>(PerLanguageOption2<T> option, string? language)
            => OptionsHelpers.GetOption(option, language, GetOption);

        public object? GetOption(OptionKey optionKey)
        {
            // Ensure the option persisters are available before taking the global lock
            var persisters = GetOptionPersisters();

            lock (_gate)
            {
                return GetOption_NoLock(optionKey, persisters);
            }
        }

        public ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey> optionKeys)
        {
            // Ensure the option persisters are available before taking the global lock
            var persisters = GetOptionPersisters();
            using var values = TemporaryArray<object?>.Empty;

            lock (_gate)
            {
                foreach (var optionKey in optionKeys)
                {
                    values.Add(GetOption_NoLock(optionKey, persisters));
                }
            }

            return values.ToImmutableAndClear();
        }

        private object? GetOption_NoLock(OptionKey optionKey, ImmutableArray<IOptionPersister> persisters)
        {
            if (_currentValues.TryGetValue(optionKey, out var value))
            {
                return value;
            }

            value = LoadOptionFromPersisterOrGetDefault(optionKey, persisters);

            _currentValues = _currentValues.Add(optionKey, value);

            // Track options with non-default values from serializers as changed options.
            if (!object.Equals(value, optionKey.Option.DefaultValue))
            {
                _changedOptionKeys = _changedOptionKeys.Add(optionKey);
            }

            return value;
        }

        private void SetOptionCore(OptionKey optionKey, object? newValue)
        {
            _currentValues = _currentValues.SetItem(optionKey, newValue);
            _changedOptionKeys = _changedOptionKeys.Add(optionKey);
        }

        public void SetGlobalOption(OptionKey optionKey, object? value)
            => SetGlobalOptions(ImmutableArray.Create(optionKey), ImmutableArray.Create(value));

        public void SetGlobalOptions(ImmutableArray<OptionKey> optionKeys, ImmutableArray<object?> values)
        {
            Contract.ThrowIfFalse(optionKeys.Length == values.Length);

            var changedOptions = new List<OptionChangedEventArgs>();
            var persisters = GetOptionPersisters();

            lock (_gate)
            {
                for (var i = 0; i < optionKeys.Length; i++)
                {
                    var optionKey = optionKeys[i];
                    var value = values[i];

                    var existingValue = GetOption_NoLock(optionKey, persisters);
                    if (Equals(value, existingValue))
                    {
                        continue;
                    }

                    // not updating _changedOptionKeys since that's only relevant for serializable options, not global ones
                    _currentValues = _currentValues.SetItem(optionKey, value);
                    changedOptions.Add(new OptionChangedEventArgs(optionKey, value));
                }
            }

            for (var i = 0; i < optionKeys.Length; i++)
            {
                PersistOption(persisters, optionKeys[i], values[i]);
            }

            RaiseOptionChangedEvent(changedOptions);
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
                    var newValue = optionSet.GetOption(optionKey);
                    var currentValue = this.GetOption(optionKey);

                    if (object.Equals(currentValue, newValue))
                    {
                        // Identical, so nothing is changing
                        continue;
                    }

                    // The value is actually changing, so update
                    changedOptions.Add(new OptionChangedEventArgs(optionKey, newValue));

                    SetOptionCore(optionKey, newValue);
                }
            }

            var persisters = GetOptionPersisters();
            foreach (var changedOption in changedOptions)
            {
                PersistOption(persisters, changedOption.OptionKey, changedOption.Value);
            }

            // Outside of the lock, raise the events on our task queue.
            UpdateRegisteredWorkspacesAndRaiseEvents(changedOptions);
        }

        private static void PersistOption(ImmutableArray<IOptionPersister> persisters, OptionKey optionKey, object? value)
        {
            foreach (var persister in persisters)
            {
                if (persister.TryPersist(optionKey, value))
                {
                    break;
                }
            }
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

                SetOptionCore(optionKey, newValue);
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
            foreach (var workspace in _registeredWorkspaces)
            {
                workspace.UpdateCurrentSolutionOnOptionsChanged();
            }

            RaiseOptionChangedEvent(changedOptions);
        }

        private void RaiseOptionChangedEvent(List<OptionChangedEventArgs> changedOptions)
        {
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
            => ImmutableInterlocked.Update(ref _registeredWorkspaces, (workspaces, workspace) => workspaces.Add(workspace), workspace);

        public void UnregisterWorkspace(Workspace workspace)
            => ImmutableInterlocked.Update(ref _registeredWorkspaces, (workspaces, workspace) => workspaces.Remove(workspace), workspace);

        public event EventHandler<OptionChangedEventArgs>? OptionChanged;
    }
}
