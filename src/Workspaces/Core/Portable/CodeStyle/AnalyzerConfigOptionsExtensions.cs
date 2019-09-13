// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis
{
    internal static class AnalyzerConfigOptionsExtensions
    {
        private readonly static MethodInfo _tryGetEditorConfigOptionMethodInfo = typeof(AnalyzerConfigOptionsExtensions).GetMethod("TryGetEditorConfigOption", BindingFlags.NonPublic | BindingFlags.Static);

        public static bool TryGetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Option<T> option, out T value)
        {
            return TryGetEditorConfigOption(analyzerConfigOptions, option, out value);
        }

        public static bool TryGetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption<T> option, string language, out T value)
        {
            return TryGetEditorConfigOption(analyzerConfigOptions, option, out value);
        }

        internal static bool TryGetOption(this AnalyzerConfigOptions analyzerConfigOptions, OptionKey optionKey, out object value)
        {
            var valueType = optionKey.Option.GetType().GenericTypeArguments[0];
            var parameters = new object[]
            {
                analyzerConfigOptions,
                optionKey.Option,
                null
            };

            var optionAvailable = (bool)_tryGetEditorConfigOptionMethodInfo.MakeGenericMethod(valueType).Invoke(null, parameters);
            value = parameters[2];

            return optionAvailable;
        }

        private static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption option, out T value)
        {
            foreach (var storageLocation in option.StorageLocations)
            {
                if (storageLocation is EditorConfigStorageLocation<T> editorConfigStorageLocation &&
                    analyzerConfigOptions.TryGetValue(editorConfigStorageLocation.KeyName, out var stringValue) &&
                    editorConfigStorageLocation.TryGetOption(stringValue, typeof(T), out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
