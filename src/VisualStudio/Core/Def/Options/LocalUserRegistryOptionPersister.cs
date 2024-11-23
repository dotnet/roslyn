// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class LocalUserRegistryOptionPersister
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

    public static LocalUserRegistryOptionPersister Create(ILocalRegistry4 localRegistry)
    {
        // SLocalRegistry service is free-threaded -- see https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1408594.
        Contract.ThrowIfFalse(ErrorHandler.Succeeded(localRegistry.GetLocalRegistryRootEx((uint)__VsLocalRegistryType.RegType_UserSettings, out var rootHandle, out var rootPath)));

        var handle = (__VsLocalRegistryRootHandle)rootHandle;
        Contract.ThrowIfTrue(string.IsNullOrEmpty(rootPath));
        Contract.ThrowIfTrue(handle == __VsLocalRegistryRootHandle.RegHandle_Invalid);

        var root = (__VsLocalRegistryRootHandle.RegHandle_LocalMachine == handle) ? Registry.LocalMachine : Registry.CurrentUser;
        return new LocalUserRegistryOptionPersister(root.CreateSubKey(rootPath, RegistryKeyPermissionCheck.ReadWriteSubTree));
    }

    public bool TryFetch(OptionKey2 optionKey, string path, string key, out object? value)
    {
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

                if (optionKey.Option.Type.IsEnum)
                {
                    try
                    {
                        value = Enum.ToObject(optionKey.Option.Type, value);
                    }
                    catch (ArgumentException)
                    {
                        // the value may be out of range for the base type of the enum
                        value = null;
                        return false;
                    }
                }

                return true;
            }
        }

        value = null;
        return false;
    }

    public void Persist(OptionKey2 optionKey, string path, string key, object? value)
    {
        Contract.ThrowIfNull(_registryKey);

        lock (_gate)
        {
            using var subKey = _registryKey.CreateSubKey(path);

            var optionType = optionKey.Option.Type;

            // Options that are of type bool have to be serialized as integers
            if (optionType == typeof(bool))
            {
                Contract.ThrowIfNull(value);
                subKey.SetValue(key, (bool)value ? 1 : 0, RegistryValueKind.DWord);
                return;
            }

            if (optionType == typeof(long))
            {
                Contract.ThrowIfNull(value);

                subKey.SetValue(key, value, RegistryValueKind.QWord);
                return;
            }

            if (optionType.IsEnum)
            {
                Contract.ThrowIfNull(value);

                // If the enum is larger than an int, store as a QWord
                if (Marshal.SizeOf(Enum.GetUnderlyingType(optionType)) > Marshal.SizeOf(typeof(int)))
                {
                    subKey.SetValue(key, (long)value, RegistryValueKind.QWord);
                }
                else
                {
                    subKey.SetValue(key, (int)value, RegistryValueKind.DWord);
                }

                return;
            }

            subKey.SetValue(key, value);
        }
    }
}
