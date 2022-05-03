// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// Serializes options marked with <see cref="LocalUserProfileStorageLocation"/> to the local hive-specific registry.
    /// </summary>
    internal sealed class LocalUserRegistryOptionPersister : IOptionPersister
    {
        /// <summary>
        /// An object to gate access to <see cref="_registryKey"/>.
        /// </summary>
        private readonly object _gate = new();
        private readonly RegistryKey _registryKey;

        private LocalUserRegistryOptionPersister(RegistryKey registryKey)
        {
            _registryKey = registryKey;
        }

        public static async Task<LocalUserRegistryOptionPersister> CreateAsync(IAsyncServiceProvider provider)
        {
            // SLocalRegistry service is free-threaded -- see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1408594.
            // Note: not using IAsyncServiceProvider.GetServiceAsync<TService, TInterface> since the extension method might switch to UI thread.
            // See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1408619/.
            var localRegistry = (ILocalRegistry4?)await provider.GetServiceAsync(typeof(SLocalRegistry)).ConfigureAwait(false);
            Contract.ThrowIfNull(localRegistry);
            Contract.ThrowIfFalse(ErrorHandler.Succeeded(localRegistry.GetLocalRegistryRootEx((uint)__VsLocalRegistryType.RegType_UserSettings, out var rootHandle, out var rootPath)));

            var handle = (__VsLocalRegistryRootHandle)rootHandle;
            Contract.ThrowIfTrue(string.IsNullOrEmpty(rootPath));
            Contract.ThrowIfTrue(handle == __VsLocalRegistryRootHandle.RegHandle_Invalid);

            var root = (__VsLocalRegistryRootHandle.RegHandle_LocalMachine == handle) ? Registry.LocalMachine : Registry.CurrentUser;
            return new LocalUserRegistryOptionPersister(root.CreateSubKey(rootPath, RegistryKeyPermissionCheck.ReadWriteSubTree));
        }

        private static bool TryGetKeyPathAndName(IOption option, [NotNullWhen(true)] out string? path, [NotNullWhen(true)] out string? key)
        {
            var serialization = option.StorageLocations.OfType<LocalUserProfileStorageLocation>().SingleOrDefault();

            if (serialization == null)
            {
                path = null;
                key = null;
                return false;
            }
            else
            {
                // We'll just use the filesystem APIs to decompose this
                path = Path.GetDirectoryName(serialization.KeyName);
                key = Path.GetFileName(serialization.KeyName);
                return true;
            }
        }

        bool IOptionPersister.TryFetch(OptionKey optionKey, out object? value)
        {
            if (!TryGetKeyPathAndName(optionKey.Option, out var path, out var key))
            {
                value = null;
                return false;
            }

            lock (_gate)
            {
                using var subKey = _registryKey.OpenSubKey(path);
                if (subKey == null)
                {
                    value = null;
                    return false;
                }

                // Options that are of type bool have to be serialized as integers
                if (optionKey.Option.Type == typeof(bool))
                {
                    value = subKey.GetValue(key, defaultValue: (bool)optionKey.Option.DefaultValue! ? 1 : 0).Equals(1);
                    return true;
                }
                else if (optionKey.Option.Type == typeof(long))
                {
                    var untypedValue = subKey.GetValue(key, defaultValue: optionKey.Option.DefaultValue);
                    switch (untypedValue)
                    {
                        case string stringValue:
                            {
                                // Due to a previous bug we were accidentally serializing longs as strings.
                                // Gracefully convert those back.
                                var suceeded = long.TryParse(stringValue, out var longValue);
                                value = longValue;
                                return suceeded;
                            }

                        case long longValue:
                            value = longValue;
                            return true;
                    }
                }
                else if (optionKey.Option.Type == typeof(int))
                {
                    var untypedValue = subKey.GetValue(key, defaultValue: optionKey.Option.DefaultValue);
                    switch (untypedValue)
                    {
                        case string stringValue:
                            {
                                // Due to a previous bug we were accidentally serializing ints as strings. 
                                // Gracefully convert those back.
                                var suceeded = int.TryParse(stringValue, out var intValue);
                                value = intValue;
                                return suceeded;
                            }

                        case int intValue:
                            value = intValue;
                            return true;
                    }
                }
                else
                {
                    // Otherwise we can just store normally
                    value = subKey.GetValue(key, defaultValue: optionKey.Option.DefaultValue);
                    return true;
                }
            }

            value = null;
            return false;
        }

        bool IOptionPersister.TryPersist(OptionKey optionKey, object? value)
        {
            if (_registryKey == null)
            {
                throw new InvalidOperationException();
            }

            if (!TryGetKeyPathAndName(optionKey.Option, out var path, out var key))
            {
                return false;
            }

            lock (_gate)
            {
                using var subKey = _registryKey.CreateSubKey(path);

                // Options that are of type bool have to be serialized as integers
                if (optionKey.Option.Type == typeof(bool))
                {
                    Contract.ThrowIfNull(value);
                    subKey.SetValue(key, (bool)value ? 1 : 0, RegistryValueKind.DWord);
                    return true;
                }

                if (optionKey.Option.Type == typeof(long))
                {
                    Contract.ThrowIfNull(value);

                    subKey.SetValue(key, value, RegistryValueKind.QWord);
                    return true;
                }

                if (optionKey.Option.Type.IsEnum)
                {
                    Contract.ThrowIfNull(value);

                    // If the enum is larger than an int, store as a QWord
                    if (Marshal.SizeOf(Enum.GetUnderlyingType(optionKey.Option.Type)) > Marshal.SizeOf(typeof(int)))
                    {
                        subKey.SetValue(key, (long)value, RegistryValueKind.QWord);
                    }
                    else
                    {
                        subKey.SetValue(key, (int)value, RegistryValueKind.DWord);
                    }

                    return true;
                }

                subKey.SetValue(key, value);
                return true;
            }
        }
    }
}
