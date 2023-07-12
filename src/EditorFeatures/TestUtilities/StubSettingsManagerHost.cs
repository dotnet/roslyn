// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Settings.Telemetry;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

internal sealed class StubSettingsManagerHost : ISettingsManagerHost5
{
    Task ISettingsManagerHost.AppInitCompletionTask => throw new NotImplementedException();

    ISettingNameTranslator? ISettingsManagerHost.NameTranslator => null;

    IStringStorage ISettingsManagerHost.PrivateStorage { get; } = new StringStorage();

    ISettingsLogger? ISettingsManagerHost.Logger => null;

    string ISettingsManagerHost.CollectionName => throw new NotImplementedException();

    string? ISettingsManagerHost.TelemetrySettings => null;

    string ISettingsManagerHost.AppDir { get; } = Path.GetRandomFileName();

    IRemoteDefaultsStore? ISettingsManagerHost3.RemoteDefaultsStore => null;

    string ISettingsManagerHost4.DurableHostIdentity => "roslyn-CI";

    IStoreUpdateLogger? ISettingsManagerHost4.StoreUpdateLogger => null;

    bool ISettingsManagerHost4.IsRoamingEnabledByDefault => false;

    bool ISettingsManagerHost5.IsRoamingAndSharingAllowed => false;

    event EventHandler<IdleStateChangedEventArgs> ISettingsManagerHost.IdleStateChanged
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    event AsyncEventHandler ISettingsManagerHost.HostShuttingDown
    {
        add
        {
            throw new NotImplementedException();
        }

        remove
        {
            throw new NotImplementedException();
        }
    }

    bool ISettingsManagerHost.IsSharedOrRoamedSetting(string settingName)
    {
        // Don't roam settings in tests
        return false;
    }

    Task<string> ISettingsManagerHost2.GetTelemetrySettingsAsync()
    {
        throw new NotImplementedException();
    }

    Task<Stream> ISettingsManagerHost5.GetServiceStreamAsync(string serviceMoniker, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private sealed class StringStorage : IStringStorage, IAsyncStringStorage
    {
        private ImmutableDictionary<string, VersionedString> _values = ImmutableDictionary<string, VersionedString>.Empty;
        private PropertyChangedEventHandler? _propertyChanged;
        private PropertyChangedAsyncEventHandler? _propertyChangedAsync;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => _propertyChanged += value;
            remove => _propertyChanged -= value;
        }

        event PropertyChangedAsyncEventHandler IStringStorage.PropertyChangedAsync
        {
            add => _propertyChangedAsync += value;
            remove => _propertyChangedAsync -= value;
        }

        event PropertyChangedAsyncEventHandler IAsyncStringStorage.PropertyChangedAsync
        {
            add => ((IStringStorage)this).PropertyChangedAsync += value;
            remove => ((IStringStorage)this).PropertyChangedAsync -= value;
        }

        event StoreUpdatedEventHandler IAsyncStringStorage.StoreUpdated
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        private async Task FireChangeEventAsync(string name)
        {
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            await (_propertyChangedAsync?.RaiseEventAsync(this, new PropertyChangedEventArgs(name)) ?? Task.CompletedTask);
        }

        Task IStringStorage.ClearAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task IAsyncStringStorage.ClearAsync(CancellationToken cancellationToken)
            => ((IStringStorage)this).ClearAsync(cancellationToken);

        async Task IStringStorage.DeleteIfExistsAsync(string name, CancellationToken cancellationToken)
        {
            if (ImmutableInterlocked.TryRemove(ref _values, name, out _))
            {
                await FireChangeEventAsync(name);
            }
        }

        Task IAsyncStringStorage.DeleteIfExistsAsync(string name, CancellationToken cancellationToken)
            => ((IStringStorage)this).DeleteIfExistsAsync(name, cancellationToken);

        StringWithMachineLocalFlag? IStringStorage.Get(string name)
        {
            return _values.GetValueOrDefault(name);
        }

        Task<IEnumerable<NamedVersionedString>> IAsyncStringStorage.GetAllSinceVersionAsync(int modifiedAfterRevision, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<VersionedString?> IAsyncStringStorage.GetAsync(string name, CancellationToken cancellationToken)
        {
            return Task.FromResult(_values.GetValueOrDefault(name));
        }

        Task<string> IAsyncStringStorage.GetStoreIdentityAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        string[] IStringStorage.NamesStartingWith(string prefix)
        {
            throw new NotImplementedException();
        }

        Task IStringStorage.SetAsync(string name, StringWithMachineLocalFlag value, Action onBeforePropertyChanged, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        Task<ServiceUploadResult> IAsyncStringStorage.SetAsync(NamedVersionedString value, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
