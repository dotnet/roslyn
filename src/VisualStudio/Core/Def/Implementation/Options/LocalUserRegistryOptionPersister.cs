// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// Serializes options marked with <see cref="LocalUserProfileStorageLocation"/> to the local hive-specific registry.
    /// </summary>
    [Export(typeof(IOptionPersister))]
    internal sealed class LocalUserRegistryOptionPersister : IOptionPersister
    {
        /// <summary>
        /// An object to gate access to <see cref="_registryKey"/>.
        /// </summary>
        private readonly object _gate = new object();
        private readonly RegistryKey _registryKey;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LocalUserRegistryOptionPersister([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            // Starting in Dev16, the ILocalRegistry service behind this call is free-threaded, and since the service is offered by msenv.dll can be requested
            // without any marshalling (explicit or otherwise) to the UI thread.
            this._registryKey = VSRegistry.RegistryRoot(serviceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: true);
        }

        private static bool TryGetKeyPathAndName(IOption option, out string path, out string key)
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

        bool IOptionPersister.TryFetch(OptionKey optionKey, out object value)
        {
            if (!TryGetKeyPathAndName(optionKey.Option, out var path, out var key))
            {
                value = null;
                return false;
            }

            lock (_gate)
            {
                using var subKey = this._registryKey.OpenSubKey(path);
                if (subKey == null)
                {
                    value = null;
                    return false;
                }

                // Options that are of type bool have to be serialized as integers
                if (optionKey.Option.Type == typeof(bool))
                {
                    value = subKey.GetValue(key, defaultValue: (bool)optionKey.Option.DefaultValue ? 1 : 0).Equals(1);
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

        bool IOptionPersister.TryPersist(OptionKey optionKey, object value)
        {
            if (this._registryKey == null)
            {
                throw new InvalidOperationException();
            }

            if (!TryGetKeyPathAndName(optionKey.Option, out var path, out var key))
            {
                return false;
            }

            lock (_gate)
            {
                using var subKey = this._registryKey.CreateSubKey(path);
                // Options that are of type bool have to be serialized as integers
                if (optionKey.Option.Type == typeof(bool))
                {
                    subKey.SetValue(key, (bool)value ? 1 : 0, RegistryValueKind.DWord);
                    return true;
                }
                else if (optionKey.Option.Type == typeof(long))
                {
                    subKey.SetValue(key, value, RegistryValueKind.QWord);
                    return true;
                }
                else if (optionKey.Option.Type.IsEnum)
                {
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
                else
                {
                    subKey.SetValue(key, value);
                    return true;
                }
            }
        }
    }
}
