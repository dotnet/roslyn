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
            foreach (var storageLocation in optionKey.Option.StorageLocations)
            {
                if (storageLocation is IEditorConfigStorageLocation editorConfigStorageLocation &&
                    editorConfigStorageLocation.TryGetOption(analyzerConfigOptions, optionKey.Option.Type, out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
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
