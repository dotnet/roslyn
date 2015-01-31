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
    internal abstract class AbstractSettingStoreOptionSerializer : ForegroundThreadAffinitizedObject, IOptionSerializer
    {
        // The gate object guards the RegistryKey
        protected readonly object Gate = new object();
        protected readonly RegistryKey RegistryKey;

        protected abstract Tuple<string, string> GetCollectionPathAndPropertyNameForOption(IOption key, string languageName);

        public AbstractSettingStoreOptionSerializer(IServiceProvider serviceProvider)
            : base(assertIsForeground: true) // The VSRegistry.RegistryRoot call requires being on the UI thread or else it will marshal and risk deadlock
        {
            this.RegistryKey = VSRegistry.RegistryRoot(serviceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: true);
        }

        public virtual bool TryFetch(OptionKey optionKey, out object value)
        {
            if (this.RegistryKey == null)
            {
                throw new InvalidOperationException();
            }

            var collectionPathAndPropertyName = GetCollectionPathAndPropertyNameForOption(optionKey.Option, optionKey.Language);
            if (collectionPathAndPropertyName == null)
            {
                value = null;
                return false;
            }

            lock (Gate)
            {
                using (var openSubKey = this.RegistryKey.OpenSubKey(collectionPathAndPropertyName.Item1))
                {
                    if (openSubKey == null)
                    {
                        value = null;
                        return false;
                    }

                    value = openSubKey.GetValue(collectionPathAndPropertyName.Item2, defaultValue: (bool)optionKey.Option.DefaultValue ? 1 : 0).Equals(1);
                    return true;
                }
            }
        }

        public virtual bool TryPersist(OptionKey optionKey, object value)
        {
            // We ignore languageName, since the current use of this class is only for
            // language-specific options that apply to a single language. The underlying option
            // service has already ensured the languageName is right, so we'll drop it on the floor.
            if (this.RegistryKey == null)
            {
                throw new InvalidOperationException();
            }

            var collectionPathAndPropertyName = GetCollectionPathAndPropertyNameForOption(optionKey.Option, optionKey.Language);
            if (collectionPathAndPropertyName == null)
            {
                return false;
            }

            lock (Gate)
            {
                using (var subKey = this.RegistryKey.CreateSubKey(collectionPathAndPropertyName.Item1))
                {
                    subKey.SetValue(collectionPathAndPropertyName.Item2, (bool)value ? 1 : 0, RegistryValueKind.DWord);
                }

                return true;
            }
        }
    }
}
