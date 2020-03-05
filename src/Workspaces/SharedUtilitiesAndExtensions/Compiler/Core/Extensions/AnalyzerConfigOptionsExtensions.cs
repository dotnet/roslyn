// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Options;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis
{
    internal static class AnalyzerConfigOptionsExtensions
    {
#if CODE_STYLE
        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption<T> option, string language)
        {
            // Language is not used for .editorconfig lookups
            _ = language;

            return GetOption(analyzerConfigOptions, option);
        }
#endif

        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Option<T> option)
        {
            if (!TryGetEditorConfigOptionOrDefault(analyzerConfigOptions, option, out T value))
            {
                Debug.Fail("Failed to find a .editorconfig key for the option.");
                value = option.DefaultValue;
            }

            return value;
        }

        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption<T> option)
        {
            if (!TryGetEditorConfigOptionOrDefault(analyzerConfigOptions, option, out T value))
            {
                Debug.Fail("Failed to find a .editorconfig key for the option.");
                value = option.DefaultValue;
            }

            return value;
        }

        public static bool TryGetEditorConfigOptionOrDefault<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption option, out T value)
            => TryGetEditorConfigOption(analyzerConfigOptions, option, useDefaultIfMissing: true, out value);

        public static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption option, out T value)
            => TryGetEditorConfigOption(analyzerConfigOptions, option, useDefaultIfMissing: false, out value);

        private static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption option, bool useDefaultIfMissing, out T value)
        {
            var hasEditorConfigStorage = false;
            foreach (var storageLocation in option.StorageLocations)
            {
                // This code path will avoid allocating a Dictionary wrapper since we can get direct access to the KeyName.
                if (storageLocation is EditorConfigStorageLocation<T> editorConfigStorageLocation &&
                    analyzerConfigOptions.TryGetValue(editorConfigStorageLocation.KeyName, out var stringValue) &&
                    editorConfigStorageLocation.TryGetOption(stringValue, typeof(T), out value))
                {
                    return true;
                }

                if (!(storageLocation is IEditorConfigStorageLocation configStorageLocation))
                {
                    continue;
                }

                // This option has .editorconfig storage defined, even if the current configuration does not provide a
                // value for it.
                hasEditorConfigStorage = true;
                if (configStorageLocation.TryGetOption(analyzerConfigOptions, option.Type, out var objectValue))
                {
                    value = (T)objectValue;
                    return true;
                }
            }

            if (useDefaultIfMissing)
            {
                value = (T)option.DefaultValue;
                return hasEditorConfigStorage;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
