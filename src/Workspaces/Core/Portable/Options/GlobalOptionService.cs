﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    [Export(typeof(IGlobalOptionService)), Shared]
    internal sealed class GlobalOptionService : IGlobalOptionService
    {
        private readonly IWorkspaceThreadingService? _workspaceThreadingService;
        private readonly ImmutableArray<Lazy<IOptionPersisterProvider>> _optionPersisterProviders;

        private readonly object _gate = new();

        #region Guarded by _gate

        private ImmutableArray<IOptionPersister> _lazyOptionPersisters;
        private ImmutableDictionary<OptionKey2, object?> _currentValues;

        #endregion

        public event EventHandler<OptionChangedEventArgs>? OptionChanged;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public GlobalOptionService(
            [Import(AllowDefault = true)] IWorkspaceThreadingService? workspaceThreadingService,
            [ImportMany] IEnumerable<Lazy<IOptionPersisterProvider>> optionPersisters)
        {
            _workspaceThreadingService = workspaceThreadingService;
            _optionPersisterProviders = optionPersisters.ToImmutableArray();

            _currentValues = ImmutableDictionary.Create<OptionKey2, object?>();
        }

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

        private static object? LoadOptionFromPersisterOrGetDefault(OptionKey2 optionKey, ImmutableArray<IOptionPersister> persisters)
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

        bool IOptionsReader.TryGetOption<T>(OptionKey2 optionKey, out T value)
        {
            value = GetOption<T>(optionKey);
            return true;
        }

        public T GetOption<T>(Option2<T> option)
            => GetOption<T>(new OptionKey2(option));

        public T GetOption<T>(PerLanguageOption2<T> option, string language)
            => GetOption<T>(new OptionKey2(option, language));

        public T GetOption<T>(OptionKey2 optionKey)
        {
            // Ensure the option persisters are available before taking the global lock
            var persisters = GetOptionPersisters();

            lock (_gate)
            {
                return (T)GetOption_NoLock(optionKey, persisters)!;
            }
        }

        public ImmutableArray<object?> GetOptions(ImmutableArray<OptionKey2> optionKeys)
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

        private object? GetOption_NoLock(OptionKey2 optionKey, ImmutableArray<IOptionPersister> persisters)
        {
            // The option must be internally defined and it can't be a legacy option whose value is mapped to another option:
            Debug.Assert(optionKey.Option is IOption2 { Definition.StorageMapping: null });

            if (_currentValues.TryGetValue(optionKey, out var value))
            {
                return value;
            }

            value = LoadOptionFromPersisterOrGetDefault(optionKey, persisters);

            _currentValues = _currentValues.Add(optionKey, value);

            return value;
        }

        public void SetGlobalOption<T>(Option2<T> option, T value)
            => SetGlobalOption(new OptionKey2(option), value);

        public void SetGlobalOption<T>(PerLanguageOption2<T> option, string language, T value)
            => SetGlobalOption(new OptionKey2(option, language), value);

        public void SetGlobalOption(OptionKey2 optionKey, object? value)
            => SetGlobalOptions(OneOrMany.Create(KeyValuePairUtil.Create(optionKey, value)));

        public bool SetGlobalOptions(ImmutableArray<KeyValuePair<OptionKey2, object?>> options)
            => SetGlobalOptions(OneOrMany.Create(options));

        private bool SetGlobalOptions(OneOrMany<KeyValuePair<OptionKey2, object?>> options)
        {
            var changedOptions = new List<OptionChangedEventArgs>();
            var persisters = GetOptionPersisters();

            lock (_gate)
            {
                foreach (var (optionKey, value) in options)
                {
                    var existingValue = GetOption_NoLock(optionKey, persisters);
                    if (Equals(value, existingValue))
                    {
                        continue;
                    }

                    _currentValues = _currentValues.SetItem(optionKey, value);
                    changedOptions.Add(new OptionChangedEventArgs(optionKey, value));
                }
            }

            if (changedOptions.Count == 0)
            {
                return false;
            }

            foreach (var changedOption in changedOptions)
            {
                PersistOption(persisters, changedOption.OptionKey, changedOption.Value);
            }

            RaiseOptionChangedEvent(changedOptions);
            return true;
        }

        private static void PersistOption(ImmutableArray<IOptionPersister> persisters, OptionKey2 optionKey, object? value)
        {
            foreach (var persister in persisters)
            {
                if (persister.TryPersist(optionKey, value))
                {
                    break;
                }
            }
        }

        public bool RefreshOption(OptionKey2 optionKey, object? newValue)
        {
            lock (_gate)
            {
                if (_currentValues.TryGetValue(optionKey, out var oldValue))
                {
                    if (Equals(oldValue, newValue))
                    {
                        // Value is still the same, no reason to raise events
                        return false;
                    }
                }

                _currentValues = _currentValues.SetItem(optionKey, newValue);
            }

            var changedOptions = new List<OptionChangedEventArgs> { new OptionChangedEventArgs(optionKey, newValue) };
            RaiseOptionChangedEvent(changedOptions);
            return true;
        }

        private void RaiseOptionChangedEvent(List<OptionChangedEventArgs> changedOptions)
        {
            Debug.Assert(changedOptions.Count > 0);

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

        // for testing
        public void ClearCachedValues()
        {
            lock (_gate)
            {
                _currentValues = ImmutableDictionary.Create<OptionKey2, object?>();
            }
        }
    }
}
