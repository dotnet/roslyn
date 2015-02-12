// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    [ExportOptionSerializer(
        EditorComponentOnOffOptions.OptionName,
        InternalFeatureOnOffOptions.OptionName,
        PerformanceFunctionIdOptionsProvider.Name,
        LoggerOptions.FeatureName,
        CacheOptions.FeatureName,
        InternalDiagnosticsOptions.OptionName), Shared]
    internal class InternalOptionSerializer : AbstractSettingStoreOptionSerializer
    {
        private const string CachePath = @"Roslyn\Internal\Performance\Cache";

        [ImportingConstructor]
        public InternalOptionSerializer(SVsServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override Tuple<string, string> GetCollectionPathAndPropertyNameForOption(IOption key, string languageName)
        {
            if (key.Feature == EditorComponentOnOffOptions.OptionName)
            {
                return Tuple.Create(@"Roslyn\Internal\OnOff\Components", key.Name);
            }
            else if (key.Feature == InternalFeatureOnOffOptions.OptionName)
            {
                return Tuple.Create(@"Roslyn\Internal\OnOff\Features", key.Name);
            }
            else if (key.Feature == PerformanceFunctionIdOptionsProvider.Name)
            {
                return Tuple.Create(@"Roslyn\Internal\Performance\FunctionId", key.Name);
            }
            else if (key.Feature == LoggerOptions.FeatureName)
            {
                return Tuple.Create(@"Roslyn\Internal\Performance\Logger", key.Name);
            }
            else if (key.Feature == InternalDiagnosticsOptions.OptionName)
            {
                return Tuple.Create(@"Roslyn\Internal\Diagnostics", key.Name);
            }

            throw ExceptionUtilities.Unreachable;
        }

        public override bool TryFetch(OptionKey optionKey, out object value)
        {
            if (optionKey.Option.Feature == CacheOptions.FeatureName)
            {
                lock (Gate)
                {
                    using (var openSubKey = this.RegistryKey.OpenSubKey(CachePath))
                    {
                        if (openSubKey == null)
                        {
                            value = null;
                            return false;
                        }

                        value = openSubKey.GetValue(optionKey.Option.Name, defaultValue: optionKey.Option.DefaultValue);
                        return true;
                    }
                }
            }

            return base.TryFetch(optionKey, out value);
        }

        public override bool TryPersist(OptionKey optionKey, object value)
        {
            if (optionKey.Option.Feature == CacheOptions.FeatureName)
            {
                lock (Gate)
                {
                    using (var subKey = this.RegistryKey.CreateSubKey(CachePath))
                    {
                        subKey.SetValue(optionKey.Option.Name, value, optionKey.Option.Type == typeof(int) ? RegistryValueKind.DWord : RegistryValueKind.QWord);
                        return true;
                    }
                }
            }

            return base.TryPersist(optionKey, value);
        }
    }
}
