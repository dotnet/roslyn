// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal abstract class AbstractLocalUserRegistryOptionSerializer : ForegroundThreadAffinitizedObject, IOptionSerializer
    {
        /// <summary>
        /// An object to gate access to <see cref="_registryKey"/>.
        /// </summary>
        private readonly object _gate = new object();
        private readonly RegistryKey _registryKey;

        protected abstract string GetCollectionPathForOption(OptionKey key);

        public AbstractLocalUserRegistryOptionSerializer(IServiceProvider serviceProvider)
            : base(assertIsForeground: true) // The VSRegistry.RegistryRoot call requires being on the UI thread or else it will marshal and risk deadlock
        {
            this._registryKey = VSRegistry.RegistryRoot(serviceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: true);
        }

        bool IOptionSerializer.TryFetch(OptionKey optionKey, out object value)
        {
            var collectionPath = GetCollectionPathForOption(optionKey);
            if (collectionPath == null)
            {
                value = null;
                return false;
            }

            lock (_gate)
            {
                using (var subKey = this._registryKey.OpenSubKey(collectionPath))
                {
                    if (subKey == null)
                    {
                        value = null;
                        return false;
                    }

                    // Options that are of type bool have to be serialized as integers
                    if (optionKey.Option.Type == typeof(bool))
                    {
                        value = subKey.GetValue(optionKey.Option.Name, defaultValue: (bool)optionKey.Option.DefaultValue ? 1 : 0).Equals(1);
                        return true;
                    }
                    else
                    {
                        // Otherwise we can just store normally
                        value = subKey.GetValue(optionKey.Option.Name, defaultValue: optionKey.Option.DefaultValue);
                        return true;
                    }
                }
            }
        }

        bool IOptionSerializer.TryPersist(OptionKey optionKey, object value)
        {
            if (this._registryKey == null)
            {
                throw new InvalidOperationException();
            }

            var collectionPath = GetCollectionPathForOption(optionKey);
            if (collectionPath == null)
            {
                return false;
            }

            lock (_gate)
            {
                using (var subKey = this._registryKey.CreateSubKey(collectionPath))
                {
                    // Options that are of type bool have to be serialized as integers
                    if (optionKey.Option.Type == typeof(bool))
                    {
                        subKey.SetValue(optionKey.Option.Name, (bool)value ? 1 : 0, RegistryValueKind.DWord);
                        return true;
                    }
                    else
                    {
                        subKey.SetValue(optionKey.Option.Name, value);
                        return true;
                    }
                }
            }
        }
    }
}
